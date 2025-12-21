using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using Meow.Bridge;

namespace Meow.UI
{
    public class UI_StageEffectPanel : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] GameObject panelContent;
        [SerializeField] TMP_Text clearText;
        [SerializeField] CanvasGroup canvasGroup;

        private void Awake()
        {
            if (panelContent) panelContent.SetActive(false);
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnPlayClearEffect += PlayClearEffect;
            }
        }

        private void OnDestroy()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnPlayClearEffect -= PlayClearEffect;
            }
        }

        private void PlayClearEffect(Action onComplete)
        {
            if (panelContent == null)
            {
                onComplete?.Invoke();
                return;
            }

            panelContent.SetActive(true);
            clearText.transform.localScale = Vector3.one * 5f;
            clearText.alpha = 0f;
            canvasGroup.alpha = 1f;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            seq.Append(clearText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce));
            seq.Join(clearText.DOFade(1f, 0.5f));

            seq.AppendInterval(2.0f);

            seq.Append(canvasGroup.DOFade(0f, 0.5f));

            seq.OnComplete(() =>
            {
                panelContent.SetActive(false);
                onComplete?.Invoke();
            });
        }
    }
}