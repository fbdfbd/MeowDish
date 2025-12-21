using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Meow.Data;
using Meow.Bridge;
using DG.Tweening;
using Meow.Run;

namespace Meow.UI
{
    public class UI_SkillSelectPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject panelContent;
        [SerializeField] Transform cardContainer;
        [SerializeField] GameObject cardPrefab;
        [SerializeField] LayoutGroup layoutGroup;

        [Header("Animation Settings")]
        [SerializeField] float dropHeight = 500f;
        [SerializeField] float dropDuration = 0.5f;
        [SerializeField] float interval = 0.5f;
        [SerializeField] Ease dropEase = Ease.OutBack;

        private List<GameObject> spawnedCards = new List<GameObject>();

        private CanvasGroup _panelCanvasGroup;

        private void Awake()
        {
            if (panelContent != null)
            {
                panelContent.SetActive(false);

                _panelCanvasGroup = panelContent.GetComponent<CanvasGroup>();
                if (_panelCanvasGroup == null)
                    _panelCanvasGroup = panelContent.AddComponent<CanvasGroup>();
            }

            if (layoutGroup == null && cardContainer != null)
                layoutGroup = cardContainer.GetComponent<LayoutGroup>();
        }

        private void Start()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnShowRewardUI += Show;
                GameBridge.Instance.OnHideRewardUI += Hide;
            }
        }

        private void OnDestroy()
        {
            if (GameBridge.Instance != null)
            {
                GameBridge.Instance.OnShowRewardUI -= Show;
                GameBridge.Instance.OnHideRewardUI -= Hide;
            }
        }

        public void Show(List<RewardOption> options)
        {
            ClearOldCards();
            if (panelContent != null) panelContent.SetActive(true);


            if (_panelCanvasGroup != null) _panelCanvasGroup.blocksRaycasts = false;

            // 카드 생성
            for (int i = 0; i < options.Count; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                spawnedCards.Add(cardObj);

                var cardView = cardObj.GetComponent<SkillCardView>();
                if (cardView != null)
                {
                    cardView.Bind(options[i], i, (idx) =>
                    {
                        if (GameBridge.Instance != null)
                        {
                            Managers.AudioManager.Instance?.PlaySfx2D(Audio.SfxId.SkillSelect);
                            GameBridge.Instance.SelectReward(idx);
                        }
                    });
                }

                CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }

            StartCoroutine(CoPlayAnimation());
        }

        private IEnumerator CoPlayAnimation()
        {
            if (layoutGroup != null) layoutGroup.enabled = true;

            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();
            if (layoutGroup != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer as RectTransform);

            if (layoutGroup != null) layoutGroup.enabled = false;

            PlayDropAnimation();
        }

        private void PlayDropAnimation()
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.PrependInterval(0.2f);

            foreach (var card in spawnedCards)
            {
                RectTransform rt = card.GetComponent<RectTransform>();
                CanvasGroup cg = card.GetComponent<CanvasGroup>();

                Vector2 finalPos = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(finalPos.x, finalPos.y + dropHeight);

                // 애니메이션 추가
                seq.Append(
                    DOTween.Sequence()
                        .Join(rt.DOAnchorPosY(finalPos.y, dropDuration).SetEase(dropEase))
                        .Join(cg.DOFade(1f, dropDuration * 0.5f))
                );

                // 효과음
                seq.AppendCallback(() =>
                {
                    if (Managers.AudioManager.Instance != null)
                        Managers.AudioManager.Instance.PlaySfx2D(Audio.SfxId.Click, 3);
                });

                // 다음 카드 대기
                seq.AppendInterval(interval - (dropDuration * 0.5f));
            }

            seq.OnComplete(() =>
            {
                if (layoutGroup != null) layoutGroup.enabled = true;

                // 버튼터치가능
                if (_panelCanvasGroup != null) _panelCanvasGroup.blocksRaycasts = true;
            });
        }

        public void Hide()
        {
            if (panelContent != null && panelContent.activeSelf)
            {
                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
                    {
                        panelContent.SetActive(false);
                        _panelCanvasGroup.alpha = 1f; 
                        ClearOldCards();
                    });
                }
                else
                {
                    panelContent.SetActive(false);
                    ClearOldCards();
                }
            }
        }

        private void ClearOldCards()
        {
            foreach (var card in spawnedCards) if (card != null) Destroy(card);
            spawnedCards.Clear();
        }
    }
}