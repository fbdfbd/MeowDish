using UnityEngine;
using UnityEngine.UI;
using Meow.ECS.Components;
using Meow.Data;

namespace Meow.ECS.View
{
    public class CustomerVisualHelper : MonoBehaviour
    {
        public SkinnedMeshRenderer meshRenderer;
        public int faceMaterialIndex = 0;

        [Header("표정 머터리얼")]
        public Material normalFace;
        public Material happyFace;
        public Material angryFace;
        public Material cryFace;

        private Animator _animator;

        [Header("UI 설정")]
        public Canvas worldCanvas;
        public Image orderBubbleImage;
        public Image patienceFill;
        public ItemIconSO iconData;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int HappyTriggerHash = Animator.StringToHash("Happy");
        private static readonly int CryTriggerHash = Animator.StringToHash("Cry");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (meshRenderer == null) meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        /// <summary>
        /// 스폰 시 주문 아이콘 설정
        /// </summary>
        public void SetupOrderUI(IngredientType orderDish)
        {
            if (orderBubbleImage != null && iconData != null)
            {
                Sprite icon = iconData.GetSprite(orderDish);
                if (icon != null)
                {
                    orderBubbleImage.sprite = icon;
                    orderBubbleImage.gameObject.SetActive(true);
                }
                else
                {
                    orderBubbleImage.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 매 프레임 UI 갱신
        /// </summary>
        public void UpdateUI(float currentPatience, float maxPatience, CustomerState state)
        {
            if (worldCanvas == null) return;

            bool showUI = (state == CustomerState.WaitingInQueue ||
                           state == CustomerState.Ordering ||
                           state == CustomerState.WaitingLate);

            worldCanvas.gameObject.SetActive(showUI);

            if (!showUI) return;

            if (Camera.main != null)
            {
                worldCanvas.transform.forward = Camera.main.transform.forward;
            }

            // 게이지 갱신
            if (patienceFill != null && maxPatience > 0)
            {
                float ratio = Mathf.Clamp01(currentPatience / maxPatience);
                patienceFill.fillAmount = ratio;
                patienceFill.color = Color.Lerp(Color.red, Color.green, ratio);
            }
        }

        /// <summary>
        /// 애니메이션 및 표정 업데이트
        /// </summary>
        public void UpdateVisuals(CustomerState state, bool isStateChanged)
        {
            // 줄 서러 갈 때만 걷기
            bool isMoving = (state == CustomerState.MovingToLine);

            if (_animator != null)
            {
                _animator.SetBool(IsMovingHash, isMoving);
            }


            if (isStateChanged)
            {
                switch (state)
                {
                    // 표정만 화남 아직 안 나감
                    case CustomerState.WaitingLate:
                        SetFaceMaterial(angryFace);
                        break;

                    // 실패 퇴장
                    case CustomerState.Leaving_Angry:
                        SetFaceMaterial(cryFace);
                        if (_animator != null) _animator.SetTrigger(CryTriggerHash);
                        break;

                    // 성공 퇴장
                    case CustomerState.Leaving_Happy:
                        SetFaceMaterial(happyFace);
                        if (_animator != null) _animator.SetTrigger(HappyTriggerHash);
                        break;

                    // 그 외(Spawned, FindingLine, InQueue, Ordering)
                    default:
                        SetFaceMaterial(normalFace);
                        break;
                }
            }
        }

        private void SetFaceMaterial(Material mat)
        {
            if (meshRenderer == null || mat == null) return;

            Material[] mats = meshRenderer.sharedMaterials;
            if (faceMaterialIndex < mats.Length && faceMaterialIndex >= 0)
            {
                if (mats[faceMaterialIndex] == mat) return;

                mats[faceMaterialIndex] = mat;
                meshRenderer.sharedMaterials = mats;
            }
        }
    }
}