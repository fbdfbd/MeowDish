using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Meow.ECS.Components;
using Meow.ECS.Authoring;

namespace Meow.ECS.View
{
    public class StoveUI : MonoBehaviour
    {
        public GameObject uiPrefab;
        public Transform uiParent;

        public float heightOffset = 1.5f;

        public Vector2 screenOffset = new Vector2(0f, 50f);

        [Header("데이터 연결")]
        public string sliderName = "name";

        private StoveAuthoring _authoring;
        private EntityManager _em;

        private GameObject _uiInstance;
        private RectTransform _uiRect;
        private RectTransform _parentRect;

        private Slider _cookSlider;
        private Image _sliderFillImage;
        private CanvasGroup _canvasGroup;

        private Camera _mainCamera;

        private bool _isShowState = false;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _authoring = GetComponentInParent<StoveAuthoring>();
            _mainCamera = Camera.main;

            if (uiPrefab != null && uiParent != null)
            {
                _parentRect = uiParent.GetComponent<RectTransform>();

                _uiInstance = Instantiate(uiPrefab, uiParent);
                _uiRect = _uiInstance.GetComponent<RectTransform>();

                if (_uiRect != null)
                {
                    _uiRect.anchorMin = new Vector2(0.5f, 0.5f);
                    _uiRect.anchorMax = new Vector2(0.5f, 0.5f);
                    _uiRect.pivot = new Vector2(0.5f, 0f);

                    _uiRect.anchoredPosition3D = Vector3.zero;
                    _uiRect.localScale = Vector3.one;
                }

                var sliders = _uiInstance.GetComponentsInChildren<Slider>(true);
                foreach (var sld in sliders)
                {
                    if (sld.name == sliderName)
                    {
                        _cookSlider = sld;
                        if (_cookSlider.fillRect != null)
                            _sliderFillImage = _cookSlider.fillRect.GetComponent<Image>();
                        break;
                    }
                }

                _canvasGroup = _uiInstance.GetComponent<CanvasGroup>();
                if (_canvasGroup == null) _canvasGroup = _uiInstance.AddComponent<CanvasGroup>();

                SetUIVisible(false);
            }
        }

        private void LateUpdate()
        {
            if (_uiInstance == null || _authoring == null) return;

            Entity myEntity = _authoring.GetEntity();
            if (myEntity == Entity.Null) return;

            if (_em.HasComponent<StoveCookingState>(myEntity) && _em.HasComponent<StoveComponent>(myEntity))
            {
                var state = _em.GetComponentData<StoveCookingState>(myEntity);

                if (state.IsCooking && state.ItemEntity != Entity.Null && _em.HasComponent<CookableComponent>(state.ItemEntity))
                {
                    var cookable = _em.GetComponentData<CookableComponent>(state.ItemEntity);
                    float progress = state.CurrentCookProgress;
                    if (_em.HasComponent<CookingState>(state.ItemEntity))
                    {
                        progress = _em.GetComponentData<CookingState>(state.ItemEntity).Elapsed;
                    }

                    if (!_isShowState) SetUIVisible(true);

                    UpdatePosition();
                    UpdateSlider(progress, cookable.CookTime, cookable.BurnTime);
                }
                else
                {
                    if (_isShowState) SetUIVisible(false);
                }
            }
        }

        private void UpdatePosition()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null || _uiRect == null || _parentRect == null) return;

            Vector3 worldPos = transform.position + new Vector3(0, heightOffset, 0);
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                if (_canvasGroup != null) _canvasGroup.alpha = 0;
                return;
            }

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect,
                screenPos,
                null,
                out localPos
            );

            if (_canvasGroup != null) _canvasGroup.alpha = 1;

            _uiRect.anchoredPosition = localPos + screenOffset;
        }

        private void UpdateSlider(float current, float cookTime, float burnTime)
        {
            if (_cookSlider == null) return;

            float totalTime = cookTime + burnTime;
            float ratio = Mathf.Clamp01(current / totalTime);

            _cookSlider.value = ratio;

            if (_sliderFillImage != null)
            {
                if (current < cookTime)
                {
                    _sliderFillImage.color = Color.yellow;
                }
                else if (current < totalTime)
                {
                    _sliderFillImage.color = Color.green;
                }
                else
                {
                    _sliderFillImage.color = Color.black;
                }
            }
        }

        private void SetUIVisible(bool visible)
        {
            _isShowState = visible;
            _uiInstance.SetActive(visible);
        }

        private void OnDestroy()
        {
            if (_uiInstance != null) Destroy(_uiInstance);
        }
    }
}
