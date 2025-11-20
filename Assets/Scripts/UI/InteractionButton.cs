using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Meow.UI
{
    /// <summary>
    /// 상호작용 버튼 (Tap + Hold)
    /// </summary>
    public class InteractionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField] private Button button;
        [SerializeField] private float holdThreshold = 0.5f;

        [Header("Visual Feedback (Optional)")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = Color.yellow;
        [SerializeField] private Color holdColor = Color.green;

        private bool _isPressed = false;
        private bool _isHolding = false;
        private float _pressStartTime = 0f;

        private bool _wasTappedThisFrame = false;
        private bool _wasHoldStartedThisFrame = false;

        private Coroutine _holdCheckCoroutine;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void LateUpdate()
        {
            // 한 프레임 후 초기화 (GetKeyDown 방식)
            _wasTappedThisFrame = false;
            _wasHoldStartedThisFrame = false;
        }

        // ========================================
        // 버튼 눌렀을 때
        // ========================================
        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _isHolding = false;
            _pressStartTime = Time.time;

            // 시각 피드백
            if (buttonImage != null)
            {
                buttonImage.color = pressedColor;
            }

            // 홀드 체크 시작
            if (_holdCheckCoroutine != null)
            {
                StopCoroutine(_holdCheckCoroutine);
            }
            _holdCheckCoroutine = StartCoroutine(CheckHoldRoutine());

            Debug.Log("[InteractionButton] Button pressed");
        }

        // ========================================
        // 버튼 뗐을 때
        // ========================================
        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            float pressDuration = Time.time - _pressStartTime;

            // 홀드 체크 중지
            if (_holdCheckCoroutine != null)
            {
                StopCoroutine(_holdCheckCoroutine);
                _holdCheckCoroutine = null;
            }

            // 시각 피드백
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }

            // ========================================
            // 홀드 중이 아니었으면 → Tap!
            // ========================================
            if (!_isHolding && pressDuration < holdThreshold)
            {
                _wasTappedThisFrame = true;
                Debug.Log($"[InteractionButton] TAP! (duration: {pressDuration:F2}s)");
            }
            else if (_isHolding)
            {
                Debug.Log($"[InteractionButton] HOLD RELEASED (duration: {pressDuration:F2}s)");
            }

            _isHolding = false;
        }

        // ========================================
        // 홀드 체크 코루틴
        // ========================================
        private IEnumerator CheckHoldRoutine()
        {
            yield return new WaitForSeconds(holdThreshold);

            // 아직 누르고 있으면 홀드 시작!
            if (_isPressed)
            {
                _isHolding = true;
                _wasHoldStartedThisFrame = true;

                // 시각 피드백
                if (buttonImage != null)
                {
                    buttonImage.color = holdColor;
                }

                Debug.Log("[InteractionButton] HOLD STARTED!");
            }
        }

        // ========================================
        // Public 프로퍼티
        // ========================================

        /// <summary>
        /// 이번 프레임에 탭했는가? (짧게 누르기)
        /// </summary>
        public bool WasTappedThisFrame => _wasTappedThisFrame;

        /// <summary>
        /// 이번 프레임에 홀드가 시작됐는가?
        /// </summary>
        public bool WasHoldStartedThisFrame => _wasHoldStartedThisFrame;

        /// <summary>
        /// 현재 홀드 중인가?
        /// </summary>
        public bool IsHolding => _isHolding;

        /// <summary>
        /// 현재 버튼을 누르고 있는가?
        /// </summary>
        public bool IsPressed => _isPressed;
    }
}