using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Meow.ECS.Components;
using Meow.Data;
using Meow.ECS.Authoring;

namespace Meow.ECS.View
{
    public class ServingCounterUI : MonoBehaviour
    {
        public GameObject uiPrefab;

        public Transform uiParent;

        public float heightOffset = 0.25f;

        public Vector2 screenOffset = new Vector2(0f, 50f);

        public Camera worldCamera;

        [Header("데이터 연결")]
        public string orderIconName = "OrderIcon";
        public string sliderName = "PatienceSlider";
        public ItemIconSO iconData;

        private ServingCounterAuthoring _authoring;
        private EntityManager _em;
        private EntityQuery _customerQuery;

        private GameObject _uiInstance;
        private RectTransform _uiRect;
        private Image _orderIconImage;
        private Slider _patienceSlider;
        private Image _sliderFillImage;
        private CanvasGroup _canvasGroup;

        private RectTransform _parentRect;

        private bool _isShowState = false;

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _customerQuery = _em.CreateEntityQuery(typeof(CustomerComponent), typeof(CustomerTag));
            _authoring = GetComponentInParent<ServingCounterAuthoring>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (uiPrefab == null || uiParent == null)
            {
                Debug.LogError($"[ServingCounterUI] {name}에 UI Prefab 또는 UI Parent가 연결안됨");
                return;
            }

            _parentRect = uiParent.GetComponent<RectTransform>();

            // UI 생성
            _uiInstance = Instantiate(uiPrefab, uiParent);
            _uiRect = _uiInstance.GetComponent<RectTransform>();

            // 말풍선 루트 RectTransform 기본값 정리
            if (_uiRect != null)
            {
                _uiRect.anchorMin = new Vector2(0.5f, 0.5f);
                _uiRect.anchorMax = new Vector2(0.5f, 0.5f);

                _uiRect.pivot = new Vector2(0.5f, 0f);

                _uiRect.anchoredPosition3D = Vector3.zero;
                _uiRect.localScale = Vector3.one;
            }

            FindUIComponents();

            _canvasGroup = _uiInstance.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = _uiInstance.AddComponent<CanvasGroup>();

            // 시작할 땐 숨김
            _uiInstance.SetActive(false);
            _isShowState = false;
        }

        private void FindUIComponents()
        {
            // 아이콘 찾기
            var images = _uiInstance.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name == orderIconName)
                {
                    _orderIconImage = img;
                    break;
                }
            }

            // 슬라이더 찾기
            var sliders = _uiInstance.GetComponentsInChildren<Slider>(true);
            foreach (var sld in sliders)
            {
                if (sld.name == sliderName)
                {
                    _patienceSlider = sld;
                    if (_patienceSlider.fillRect != null)
                        _sliderFillImage = _patienceSlider.fillRect.GetComponent<Image>();
                    break;
                }
            }

            if (_orderIconImage == null) Debug.LogError($"[ServingCounterUI] OrderIcon '{orderIconName}' 못찾음");
            if (_patienceSlider == null) Debug.LogError($"[ServingCounterUI] Slider '{sliderName}' 못찾음");
        }


        private void LateUpdate()
        {
            if (_uiInstance == null || _authoring == null || worldCamera == null)
                return;

            Entity myStationEntity = _authoring.GetEntity();
            if (myStationEntity == Entity.Null) return;

            // 해당 손님 찾기
            bool customerFound = false;
            CustomerComponent targetCustomer = default;

            var customers = _customerQuery.ToComponentDataArray<CustomerComponent>(Allocator.Temp);
            foreach (var customer in customers)
            {
                if (customer.TargetStation == myStationEntity &&
                    customer.QueueIndex == 0 &&
                    (customer.State == CustomerState.Ordering ||
                     customer.State == CustomerState.WaitingLate ||
                     customer.State == CustomerState.WaitingInQueue))
                {
                    targetCustomer = customer;
                    customerFound = true;
                    break;
                }
            }
            customers.Dispose();

            // UI 처리
            if (customerFound)
            {
                if (!_isShowState) ShowUI();

                UpdatePosition();           // 위치 동기화
                UpdateData(targetCustomer); // 데이터 갱신
            }
            else
            {
                if (_isShowState) HideUI();
            }
        }

        // 좌표 변환 로직
        private void UpdatePosition()
        {
            if (_uiRect == null || worldCamera == null || _parentRect == null) return;

            // 1) 월드 기준 위치 (카운터 중심 + 높이)
            Vector3 worldPos = transform.position + Vector3.up * heightOffset;

            // 2) 월드 좌표 -> 스크린 좌표 (픽셀 단위)
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

            // 카메라 뒤에 있으면 숨김
            if (screenPos.z < 0f)
            {
                if (_canvasGroup != null) _canvasGroup.alpha = 0f;
                return;
            }

            // 3) 스크린 좌표 -> UI 부모(Canvas) 기준 로컬 좌표로 변환
            Vector2 localPos;

            // Screen Space - Overlay 모드라면 3번째 인자에 null을 넣습니다
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentRect,
                screenPos,
                null,
                out localPos
            );

            // 4) 오프셋 적용
            _uiRect.anchoredPosition = localPos + screenOffset;

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        private void UpdateData(CustomerComponent customer)
        {
            // 아이콘 갱신
            if (_orderIconImage != null)
            {
                Sprite icon = (iconData != null) ? iconData.GetSprite(customer.OrderDish) : null;

                if (icon != null)
                {
                    _orderIconImage.sprite = icon;
                    _orderIconImage.enabled = true;
                }
                else
                {
                    _orderIconImage.enabled = false;
                }
            }

            // 게이지 갱신
            if (_patienceSlider != null && customer.MaxPatience > 0)
            {
                float ratio = Mathf.Clamp01(customer.Patience / customer.MaxPatience);
                _patienceSlider.value = ratio;

                // 색상 변경
                if (_sliderFillImage != null)
                {
                    if (ratio >= 0.7f) _sliderFillImage.color = Color.green;
                    else if (ratio >= 0.3f) _sliderFillImage.color = new Color(1f, 0.5f, 0f); // 주황
                    else _sliderFillImage.color = Color.red;
                }
            }
        }


        private void ShowUI()
        {
            _isShowState = true;
            _uiInstance.SetActive(true);
        }

        private void HideUI()
        {
            _isShowState = false;
            _uiInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_uiInstance != null) Destroy(_uiInstance);
        }
    }
}