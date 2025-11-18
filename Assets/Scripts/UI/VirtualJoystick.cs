using UnityEngine;
using UnityEngine.EventSystems;

namespace Meow.UI
{
    /// <summary>
    /// 모바일용 가상 조이스틱
    /// 터치 입력을 -1~1 범위의 Vector2로 변환
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [Header("References")]
        [Tooltip("조이스틱 배경 이미지")]
        public RectTransform background;

        [Tooltip("조이스틱 핸들(손잡이) 이미지")]
        public RectTransform handle;

        [Header("Settings")]
        [Tooltip("핸들이 움직일 수 있는 최대 거리")]
        [Range(0.5f, 2.0f)]
        public float handleRange = 1f;

        [Tooltip("입력 민감도")]
        [Range(0.5f, 2.0f)]
        public float sensitivity = 1.0f;

        [Tooltip("데드존 (이 값보다 작은 입력은 무시)")]
        [Range(0f, 0.3f)]
        public float deadZone = 0.1f;

        /// <summary>현재 조이스틱 입력값 (-1~1)</summary>
        private Vector2 _inputVector;

        /// <summary>외부에서 읽을 입력값</summary>
        public Vector2 InputVector => _inputVector;

        /// <summary>입력이 있는지 확인</summary>
        public bool HasInput => _inputVector.magnitude > 0.01f;

        private void Start()
        {
            // 자동 참조 설정
            if (background == null)
                background = GetComponent<RectTransform>();

            if (handle == null && transform.childCount > 0)
                handle = transform.GetChild(0).GetComponent<RectTransform>();
        }

        /// <summary>
        /// 터치/마우스 드래그 중
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;

            // 스크린 좌표 → 로컬 좌표 변환
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position))
            {
                // 배경 크기 기준 정규화 (-1 ~ 1)
                position = position / (background.sizeDelta / 2);

                // 범위 제한
                Vector2 rawInput = Vector2.ClampMagnitude(position, 1f);

                // 민감도 적용
                _inputVector = rawInput * sensitivity;
                _inputVector = Vector2.ClampMagnitude(_inputVector, 1f);

                // 데드존 적용
                if (_inputVector.magnitude < deadZone)
                {
                    _inputVector = Vector2.zero;
                }

                // 핸들 위치 업데이트
                handle.anchoredPosition = rawInput * handleRange * (background.sizeDelta / 2);
            }
        }

        /// <summary>
        /// 터치/마우스 누름
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        /// <summary>
        /// 터치/마우스 뗌
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            // 입력 초기화
            _inputVector = Vector2.zero;

            // 핸들 원위치
            handle.anchoredPosition = Vector2.zero;
        }
    }
}