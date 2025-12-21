using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Meow.Bridge;
using TMPro;
using DG.Tweening;

namespace Meow.UI
{
    public class UI_TopPanel : MonoBehaviour
    {
        [SerializeField] UI_TopDynamic topDynamic;

        [Header("생명")]
        [SerializeField] Transform heartContainer;
        [SerializeField] GameObject heartPrefab;
        [SerializeField] float emptyHeartAlpha = 0.3f;
        private readonly List<Image> heartImages = new List<Image>();

        [Header("골드 및 날짜")]
        [SerializeField] TMP_Text goldText;
        [SerializeField] TMP_Text dayText;
        [SerializeField] string goldFormat = "{0}";
        [SerializeField] string dayFormat = "Day {0}";

        [Header("버튼")]
        [SerializeField] Button pauseButton;

        [SerializeField] bool playIntroAfterInitialSkillSelect = true;

        [Header("시작 시 내려올 탑 패널")]
        [SerializeField] RectTransform introTarget;
        [SerializeField] CanvasGroup introTargetCanvasGroup;

        [Header("시작 애니메이션 설정")]
        [SerializeField] float introOffsetY = 200f;
        [SerializeField] float introDuration = 0.5f;
        [SerializeField] Ease introEase = Ease.OutBack;

        private GameBridge _boundBridge;
        private Vector2 _shownAnchoredPos;
        private bool _introPlayed;

        private void Awake()
        {
            ResolveIntroTarget();

            if (introTarget != null)
                _shownAnchoredPos = introTarget.anchoredPosition;

            if (playIntroAfterInitialSkillSelect)
                SetHiddenForIntro();
        }

        private void Start()
        {
            if (GameBridge.Instance != null)
                Bind(GameBridge.Instance);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void ResolveIntroTarget()
        {
            if (introTarget == null)
                introTarget = GetComponent<RectTransform>();

            if (introTarget == null)
            {
                Debug.LogWarning("[UI_TopPanel] introTarget이 null입니다.");
                return;
            }

            if (introTargetCanvasGroup == null)
                introTargetCanvasGroup = introTarget.GetComponent<CanvasGroup>();

            if (introTargetCanvasGroup == null)
                introTargetCanvasGroup = introTarget.gameObject.AddComponent<CanvasGroup>();
        }

        public void Bind(GameBridge bridge)
        {
            if (_boundBridge == bridge) return;
            Unbind();

            _boundBridge = bridge;
            if (_boundBridge == null) return;

            _boundBridge.OnGoldChanged += UpdateGold;
            _boundBridge.OnDayChanged += UpdateDay;
            _boundBridge.OnLifeChanged += UpdateLife;

            _boundBridge.OnCustomerDataChanged += HandleCustomerDataChanged;

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() =>
                {
                    _boundBridge.TogglePause();
                });
            }

            _boundBridge.OnHideRewardUI += HandleRewardUiHidden;
        }

        private void Unbind()
        {
            if (_boundBridge == null) return;

            _boundBridge.OnGoldChanged -= UpdateGold;
            _boundBridge.OnDayChanged -= UpdateDay;
            _boundBridge.OnLifeChanged -= UpdateLife;

            _boundBridge.OnCustomerDataChanged -= HandleCustomerDataChanged;
            _boundBridge.OnHideRewardUI -= HandleRewardUiHidden;

            _boundBridge = null;
        }

        private void HandleCustomerDataChanged(int curr, int max, int total)
        {
            if (topDynamic != null)
                topDynamic.UpdateCustomerInfo(curr, max, total);
        }

        private void HandleRewardUiHidden()
        {
            if (!playIntroAfterInitialSkillSelect) return;
            if (_introPlayed) return;

            _introPlayed = true;
            PlayIntroDrop();
        }

        private void SetHiddenForIntro()
        {
            ResolveIntroTarget();
            if (introTarget == null || introTargetCanvasGroup == null) return;

            // 오브젝트는 Active 상태 유지
            if (!introTarget.gameObject.activeSelf)
                introTarget.gameObject.SetActive(true);

            introTargetCanvasGroup.alpha = 0f;
            introTargetCanvasGroup.interactable = false;
            introTargetCanvasGroup.blocksRaycasts = false;

            _shownAnchoredPos = introTarget.anchoredPosition;
            introTarget.anchoredPosition = _shownAnchoredPos + Vector2.up * introOffsetY;
        }

        private void PlayIntroDrop()
        {
            ResolveIntroTarget();
            if (introTarget == null || introTargetCanvasGroup == null) return;

            if (!introTarget.gameObject.activeSelf)
                introTarget.gameObject.SetActive(true);

            DOTween.Kill(introTarget);
            DOTween.Kill(introTargetCanvasGroup);

            var seq = DOTween.Sequence().SetUpdate(true);

            seq.Join(introTarget.DOAnchorPos(_shownAnchoredPos, introDuration).SetEase(introEase));
            seq.Join(introTargetCanvasGroup.DOFade(1f, introDuration * 0.6f));

            seq.OnComplete(() =>
            {
                introTargetCanvasGroup.alpha = 1f;
                introTargetCanvasGroup.interactable = true;
                introTargetCanvasGroup.blocksRaycasts = true;
                introTarget.anchoredPosition = _shownAnchoredPos;
            });
        }


        private void UpdateGold(int gold)
        {
            if (goldText) goldText.text = string.Format(goldFormat, gold);
        }

        private void UpdateDay(int day)
        {
            if (dayText) dayText.text = string.Format(dayFormat, day);
        }

        private void UpdateLife(int currentHp, int maxHp)
        {
            while (heartImages.Count < maxHp)
                CreateHeart();

            for (int i = 0; i < heartImages.Count; i++)
            {
                bool isFull = i < currentHp;
                SetHeartVisual(heartImages[i], isFull);
                heartImages[i].gameObject.SetActive(i < maxHp);
            }
        }

        private void CreateHeart()
        {
            if (heartPrefab == null || heartContainer == null) return;

            GameObject newHeart = Instantiate(heartPrefab, heartContainer);
            Image img = newHeart.GetComponent<Image>();
            if (img != null) heartImages.Add(img);
        }

        private void SetHeartVisual(Image heart, bool isFull)
        {
            if (heart == null) return;
            Color color = heart.color;
            color.a = isFull ? 1.0f : emptyHeartAlpha;
            heart.color = color;
        }
    }
}
