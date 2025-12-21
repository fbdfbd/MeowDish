using System.Collections.Generic;
using UnityEngine;
using Meow.Audio;

namespace Meow.Managers
{
    /// <summary>
    /// 전역 오디오 매니저
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM Sources")]
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;

        [Header("SFX 2D Pool")]
        [SerializeField, Tooltip("2D SFX 풀 사이즈")] private int sfxPoolSize = 8;
        [SerializeField] private AudioSource sfx2DPrefab;

        [Header("SFX 3D (옵션)")]
        [SerializeField, Tooltip("3D 재생이 필요 없으면 비워두기")] private AudioSource sfx3DPrefab;

        [Header("Libraries")]
        [SerializeField] private AudioCueLibrarySfxSO sfxLibrary;
        [SerializeField] private AudioCueLibraryBgmSO bgmLibrary;

        private bool _sfxEnabled = true;
        private bool _bgmEnabled = true;
        private AudioSource _currentBgm;
        private AudioSource _nextBgm;
        private readonly List<AudioSource> _sfxPool = new();
        private readonly Dictionary<int, LoopInstance> _loopingSfx = new();
        private int _nextLoopHandle = 1;

        private class LoopInstance
        {
            public AudioSource Source;
            public Transform Follow;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // BGM 소스 바인딩
            _currentBgm = bgmSourceA;
            _nextBgm = bgmSourceB;

            // 2D SFX 풀 생성
            if (sfx2DPrefab != null && sfxPoolSize > 0)
            {
                for (int i = 0; i < sfxPoolSize; i++)
                {
                    var src = Instantiate(sfx2DPrefab, transform);
                    src.playOnAwake = false;
                    src.spatialBlend = 0f;
                    _sfxPool.Add(src);
                }
            }
        }

        public void SetSfxEnabled(bool enabled)
        {
            _sfxEnabled = enabled;
            foreach (var src in _sfxPool) src.mute = !enabled;
            foreach (var kv in _loopingSfx) kv.Value.Source.mute = !enabled;
        }

        public void SetBgmEnabled(bool enabled)
        {
            _bgmEnabled = enabled;
            if (_currentBgm != null) _currentBgm.mute = !enabled;
            if (_nextBgm != null) _nextBgm.mute = !enabled;
        }

        public void PlayBgm(BgmId id, bool loop = true, float fadeTime = 0.5f)
        {
            if (bgmLibrary == null || _currentBgm == null || _nextBgm == null) return;
            var clip = bgmLibrary.GetClip(id);
            if (clip == null) return;
            if (_currentBgm.clip == clip) return; // 이미 재생 중

            _nextBgm.clip = clip;
            _nextBgm.loop = loop;
            _nextBgm.volume = _bgmEnabled ? 1f : 0f;
            _nextBgm.Play();

            if (fadeTime > 0f && _currentBgm.isPlaying)
            {
                StartCoroutine(CoCrossFade(_currentBgm, _nextBgm, fadeTime));
            }
            else
            {
                _currentBgm.Stop();
                SwapBgmSources();
            }
        }

        public void PlaySfx2D(SfxId id, int variantIndex = -1, float volume = 1f, float pitch = 1f)
        {
            if (!_sfxEnabled || sfxLibrary == null || _sfxPool.Count == 0) return;
            var clip = variantIndex < 0
                ? sfxLibrary.GetRandomClip(id)
                : sfxLibrary.GetClipByIndex(id, variantIndex);
            if (clip == null) return;

            var src = GetFreeSfxSource();
            if (src == null) return;

            src.pitch = pitch;
            src.volume = volume;
            src.mute = !_sfxEnabled;
            src.loop = false;
            src.PlayOneShot(clip, volume);
        }


        public void PlaySfx3D(SfxId id, Vector3 position, int variantIndex = -1, float volume = 1f, float pitch = 1f)
        {
            if (!_sfxEnabled || sfxLibrary == null || sfx3DPrefab == null) return;
            var clip = variantIndex < 0
                ? sfxLibrary.GetRandomClip(id)
                : sfxLibrary.GetClipByIndex(id, variantIndex);
            if (clip == null) return;

            var src = Instantiate(sfx3DPrefab, position, Quaternion.identity);
            src.pitch = pitch;
            src.clip = clip;
            src.volume = volume;
            src.loop = false;
            src.mute = !_sfxEnabled;
            src.Play();
            Destroy(src.gameObject, clip.length + 0.2f);
        }


        public int PlayLoop(SfxId id, Transform follow = null, int variantIndex = -1, float volume = 1f, float pitch = 1f)
        {
            if (sfxLibrary == null) return 0;
            var clip = variantIndex < 0
                ? sfxLibrary.GetRandomClip(id)
                : sfxLibrary.GetClipByIndex(id, variantIndex);
            if (clip == null) return 0;

            AudioSource src = null;
            if (sfx3DPrefab != null)
            {
                src = Instantiate(sfx3DPrefab, follow != null ? follow.position : Vector3.zero, Quaternion.identity);
                src.spatialBlend = sfx3DPrefab.spatialBlend;
            }
            else if (sfx2DPrefab != null)
            {
                src = Instantiate(sfx2DPrefab, transform);
                src.spatialBlend = 0f;
            }
            if (src == null) return 0;

            src.loop = true;
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.mute = !_sfxEnabled;
            src.Play();

            int handle = _nextLoopHandle++;
            _loopingSfx[handle] = new LoopInstance { Source = src, Follow = follow };
            return handle;
        }

        public void StopLoop(int handle)
        {
            if (handle == 0) return;
            if (_loopingSfx.TryGetValue(handle, out var inst))
            {
                if (inst.Source != null)
                {
                    inst.Source.Stop();
                    Destroy(inst.Source.gameObject);
                }
                _loopingSfx.Remove(handle);
            }
        }

        private void Update()
        {
            if (_loopingSfx.Count == 0) return;
            foreach (var kv in _loopingSfx.Values)
            {
                if (kv.Follow != null && kv.Source != null)
                {
                    kv.Source.transform.position = kv.Follow.position;
                }
            }
        }

        private AudioSource GetFreeSfxSource()
        {
            // 사용 중이 아닌 소스 찾기
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                var src = _sfxPool[i];
                if (!src.isPlaying) return src;
            }
            // 모두 재생 중이면 첫 번째를 재사용
            return _sfxPool.Count > 0 ? _sfxPool[0] : null;
        }

        private void SwapBgmSources()
        {
            var temp = _currentBgm;
            _currentBgm = _nextBgm;
            _nextBgm = temp;
        }

        private System.Collections.IEnumerator CoCrossFade(AudioSource from, AudioSource to, float time)
        {
            float t = 0f;
            SwapBgmSources(); // 현재/다음 교체

            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(t / time);
                from.volume = _bgmEnabled ? (1f - a) : 0f;
                to.volume = _bgmEnabled ? a : 0f;
                yield return null;
            }

            from.Stop();
        }
    }
}
