using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meow.Data;
using Meow.Run;
using System;

namespace Meow.UI
{
    public class SkillCardView : MonoBehaviour
    {
        [Header("UI Components")]
        public Image icon;
        public Image rankBackground;
        public TMP_Text title;
        public TMP_Text desc;
        public TMP_Text positive;
        public TMP_Text negative;
        public Button selectButton;

        public Color[] skillRankColor;

        private int _index;
        private Action<int> _onClickCallback;

        //보상 선택 시 호출
        public void Bind(RewardOption option, int index, Action<int> onClickCallback)
        {
            _index = index;
            _onClickCallback = onClickCallback;

            var skill = option.skill;
            if (skill != null)
            {
                if (icon) icon.sprite = skill.icon;
                if (title) title.text = skill.displayName;
                if (desc) desc.text = skill.description;

                SetTextOrHide(positive, GetPositive(skill));
                SetTextOrHide(negative, GetNegative(skill));

                if (rankBackground != null && skillRankColor != null && skillRankColor.Length > 0)
                {
                    int rankIndex = Mathf.Clamp((int)skill.skillRank, 0, skillRankColor.Length - 1);
                    rankBackground.color = skillRankColor[rankIndex];
                }
            }

            if (selectButton)
            {
                selectButton.interactable = true;
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnBtnClick);
            }
        }

        // 단순 정보 조회용(밑에 활성스킬확인용)
        public void Bind(SkillDefinitionSO skill)
        {
            if (skill == null) return;

            if (icon) icon.sprite = skill.icon;
            if (title) title.text = skill.displayName;
            if (desc) desc.text = skill.description;

            SetTextOrHide(positive, GetPositive(skill));
            SetTextOrHide(negative, GetNegative(skill));

            if (rankBackground != null && skillRankColor != null && skillRankColor.Length > 0)
            {
                int rankIndex = Mathf.Clamp((int)skill.skillRank, 0, skillRankColor.Length - 1);
                rankBackground.color = skillRankColor[rankIndex];
            }

            if (selectButton) selectButton.interactable = false;
        }


        private void SetTextOrHide(TMP_Text textComponent, string content)
        {
            if (textComponent == null) return;

            bool isEmpty = string.IsNullOrEmpty(content);

            textComponent.text = content;

            textComponent.gameObject.SetActive(!isEmpty);
        }

        private string GetPositive(SkillDefinitionSO skill)
        {
            if (!string.IsNullOrEmpty(skill.positiveText)) return skill.positiveText;
            return $"{skill.buffType} x{skill.multiplier:0.##}";
        }

        private string GetNegative(SkillDefinitionSO skill)
        {
            if (!string.IsNullOrEmpty(skill.negativeText) && skill.negativeText != "0")
                return skill.negativeText;

            if (skill.additionalCustomers > 0)
            {
                return $"Extra customers: +{skill.additionalCustomers}";
            }

            return string.Empty;
        }

        private void OnBtnClick()
        {
            _onClickCallback?.Invoke(_index);
        }
    }
}