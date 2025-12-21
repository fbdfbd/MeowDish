using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Meow.UI
{
    public class UI_TopDynamic : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Slider progressSlider;
        [SerializeField] TMP_Text customerCountText;
        [SerializeField] TMP_Text todayCustomerText;

        [Header("Animation Settings")]
        [SerializeField] float sliderDuration = 0.5f;
        [SerializeField] Ease sliderEase = Ease.OutQuad;

        [Header("Text Formats")]
        [TextArea] public string countFormat = "<b><size=130%><#491300>{0}</color></size></b>명 남음";
        [TextArea] public string todayFormat = "오늘의 손님: <b><size=130%><#491300>{0}</color></size></b>명";

        private void Awake()
        {
            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }
        }

        public void UpdateCustomerInfo(int current, int max, int todayTotal)
        {
            float targetValue = 0f;
            if (max > 0)
                targetValue = (float)current / max;

            // 슬라이더
            progressSlider.DOValue(targetValue, sliderDuration).SetEase(sliderEase);

            // 텍스트
            int remaining = max - current;
            if (remaining < 0) remaining = 0;

            customerCountText.text = string.Format(countFormat, remaining);
            todayCustomerText.text = string.Format(todayFormat, todayTotal);
        }
    }
}