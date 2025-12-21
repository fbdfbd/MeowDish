using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Meow.Bridge;
using Meow.Data;

namespace Meow.UI
{
    public class UI_BottomPanel : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField] Toggle buffPanelToggle;

        [Header("Buff List Panel")]
        [SerializeField] RectTransform panelContainer;
        [SerializeField] Transform listContent;
        [SerializeField] GameObject skillCardPrefab;

        [Header("Animation Settings")]
        [SerializeField] Vector2 closedPosition = new Vector2(0, -500);
        [SerializeField] Vector2 openPosition = new Vector2(0, 0);

        private void Start()
        {
            if (buffPanelToggle != null)
            {
                buffPanelToggle.onValueChanged.RemoveAllListeners();
                buffPanelToggle.onValueChanged.AddListener(OnTogglePanel);
                buffPanelToggle.isOn = false;
                if (panelContainer) panelContainer.anchoredPosition = closedPosition;
            }
        }

        private void OnDestroy()
        {
            if (buffPanelToggle != null) buffPanelToggle.onValueChanged.RemoveListener(OnTogglePanel);
        }

        private void OnTogglePanel(bool isOpen)
        {
            if (panelContainer != null)
                panelContainer.anchoredPosition = isOpen ? openPosition : closedPosition;

            if (isOpen) RefreshSkillList();
        }

        private void RefreshSkillList()
        {
            if (listContent == null || skillCardPrefab == null) return;

            foreach (Transform child in listContent) Destroy(child.gameObject);

            if (GameBridge.Instance != null)
            {
                var skills = GameBridge.Instance.GetOwnedSkills();
                if (skills != null)
                {
                    foreach (var skill in skills)
                    {
                        GameObject go = Instantiate(skillCardPrefab, listContent);
                        SkillCardView view = go.GetComponent<SkillCardView>();
                        if (view != null) view.Bind(skill);
                    }
                }
            }
        }

        private void Update()
        {
            if (panelContainer == null || buffPanelToggle == null) return;
            Vector2 targetPos = buffPanelToggle.isOn ? openPosition : closedPosition;
            panelContainer.anchoredPosition = Vector2.Lerp(panelContainer.anchoredPosition, targetPos, Time.deltaTime * 10f);
        }
    }
}