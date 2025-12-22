using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Meow.UI
{
    public class UI_TopDynamic : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressSlider;

        // 1) 남은 손님: [숫자] + "명 남음"
        [SerializeField] private TMP_Text remainingNumberText;
        [SerializeField] private TMP_Text remainingLabelText;

        // 2) 오늘의 손님: "오늘의 손님: " + [숫자] + "명"
        [SerializeField] private TMP_Text todayLabelText;
        [SerializeField] private TMP_Text todayNumberText;
        [SerializeField] private TMP_Text todaySuffixText;

        [Header("Animation Settings")]
        [SerializeField] private float sliderDuration = 0.5f;
        [SerializeField] private Ease sliderEase = Ease.OutQuad;

        [Header("Text (keep same content)")]
        [SerializeField] private string remainingLabel = "명 남음";
        [SerializeField] private string todayLabel = "오늘의 손님: ";
        [SerializeField] private string todaySuffix = "명";

        private void Awake()
        {
            if (progressSlider != null)
                progressSlider.value = 0f;

            // 고정 문구는 한 번만 세팅
            if (remainingLabelText != null)
                remainingLabelText.SetText(remainingLabel);

            if (todayLabelText != null)
                todayLabelText.SetText(todayLabel);

            if (todaySuffixText != null)
                todaySuffixText.SetText(todaySuffix);

            // 초기 숫자도 세팅(원하면)
            if (remainingNumberText != null)
                remainingNumberText.SetText("0");

            if (todayNumberText != null)
                todayNumberText.SetText("0");
        }

        public void UpdateCustomerInfo(int current, int max, int todayTotal)
        {
            // 슬라이더 목표값
            float targetValue = 0f;
            if (max > 0)
                targetValue = (float)current / max;

            // 슬라이더 트윈 중복 방지 + 트윈
            if (progressSlider != null)
            {
                progressSlider.DOKill();
                progressSlider.DOValue(targetValue, sliderDuration).SetEase(sliderEase);
            }

            // 남은 손님 계산
            int remaining = max - current;
            if (remaining < 0) remaining = 0;

            // 숫자만 갱신 (2번: SetText로 GC 줄이기)
            if (remainingNumberText != null)
                remainingNumberText.SetText("{0}", remaining);

            if (todayNumberText != null)
                todayNumberText.SetText("{0}", todayTotal);
        }
    }
}
