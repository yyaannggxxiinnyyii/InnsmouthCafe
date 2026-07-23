using UnityEngine;
using UnityEngine.EventSystems;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 图鉴卡牌悬停倾斜组件，根据鼠标在卡牌内的位置平滑旋转卡牌。
    /// </summary>
    public class CardTiltUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [Header("倾斜效果")]
        [SerializeField]
        [Tooltip("鼠标位于卡牌边缘时的最大倾斜角度")]
        private float _maxTiltAngle = 6f;

        [SerializeField]
        [Tooltip("卡牌跟随鼠标和回正时的平滑速度")]
        private float _smoothSpeed = 12f;

        [SerializeField]
        [Tooltip("鼠标位于卡牌边缘时，卡牌向对应方向平移的最大像素距离")]
        private float _maxPositionOffset = 12f;

        private RectTransform _rectTransform;
        private Quaternion _initialRotation;
        private Vector2 _initialAnchoredPosition;
        private Vector2 _targetAnchoredPosition;
        private bool _hasInitialAnchoredPosition;
        private bool _isPointerInside;
        private Vector2 _targetNormalizedPointerPosition;
        private Vector2 _normalizedPointerPosition;

        /// <summary>
        /// 获取鼠标相对卡牌中心的归一化位置，范围为 -1 到 1。
        /// </summary>
        public Vector2 NormalizedPointerPosition => _normalizedPointerPosition;

        /// <summary>
        /// 获取鼠标当前是否位于卡牌范围内。
        /// </summary>
        public bool IsPointerInside => _isPointerInside;

        /// <summary>
        /// 获取卡牌跟随与回正使用的平滑速度。
        /// </summary>
        public float SmoothSpeed => _smoothSpeed;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _initialRotation = _rectTransform != null ? _rectTransform.localRotation : Quaternion.identity;
        }

        private void OnDisable()
        {
            if (_rectTransform == null)
            {
                return;
            }

            _rectTransform.localRotation = _initialRotation;
            if (_hasInitialAnchoredPosition)
            {
                _targetAnchoredPosition = _initialAnchoredPosition;
                _rectTransform.anchoredPosition = _initialAnchoredPosition;
            }

            _hasInitialAnchoredPosition = false;
            _isPointerInside = false;
            _targetNormalizedPointerPosition = Vector2.zero;
            _normalizedPointerPosition = Vector2.zero;
        }

        private void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            float interpolation = 1f - Mathf.Exp(-_smoothSpeed * Time.unscaledDeltaTime);
            if (_hasInitialAnchoredPosition)
            {
                _rectTransform.anchoredPosition = Vector2.Lerp(
                    _rectTransform.anchoredPosition,
                    _targetAnchoredPosition,
                    interpolation);
            }

            _normalizedPointerPosition = Vector2.Lerp(
                _normalizedPointerPosition,
                _targetNormalizedPointerPosition,
                interpolation);
            float rotationX = _normalizedPointerPosition.y * _maxTiltAngle;
            float rotationY = -_normalizedPointerPosition.x * _maxTiltAngle;
            _rectTransform.localRotation = _initialRotation
                * Quaternion.Euler(rotationX, rotationY, 0f);
        }

        /// <summary>
        /// 鼠标进入卡牌时，根据当前位置设置倾斜目标。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;
            UpdateTiltTarget(eventData);
        }

        /// <summary>
        /// 鼠标在卡牌内移动时，更新对应方向的倾斜目标。
        /// </summary>
        public void OnPointerMove(PointerEventData eventData)
        {
            _isPointerInside = true;
            UpdateTiltTarget(eventData);
        }

        /// <summary>
        /// 鼠标离开卡牌时，将卡牌平滑恢复至初始角度。
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            if (_hasInitialAnchoredPosition)
            {
                _targetAnchoredPosition = _initialAnchoredPosition;
            }

            _targetNormalizedPointerPosition = Vector2.zero;
        }

        /// <summary>
        /// 将鼠标屏幕坐标转换为卡牌局部偏移，并更新旋转目标。
        /// </summary>
        private void UpdateTiltTarget(PointerEventData eventData)
        {
            if (_rectTransform == null || eventData == null)
            {
                return;
            }

            if (!_hasInitialAnchoredPosition)
            {
                _initialAnchoredPosition = _rectTransform.anchoredPosition;
                _targetAnchoredPosition = _initialAnchoredPosition;
                _hasInitialAnchoredPosition = true;
            }

            RectTransform parentRectTransform = _rectTransform.parent as RectTransform;
            if (parentRectTransform == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRectTransform,
                    eventData.position,
                    eventData.enterEventCamera,
                    out Vector2 pointerPosition))
            {
                return;
            }

            Rect rect = _rectTransform.rect;
            Vector2 cardCenter = parentRectTransform.InverseTransformPoint(_rectTransform.position);
            Vector2 localPosition = pointerPosition - cardCenter;
            float normalizedX = rect.width > 0f
                ? Mathf.Clamp(localPosition.x / (rect.width * 0.5f), -1f, 1f)
                : 0f;
            float normalizedY = rect.height > 0f
                ? Mathf.Clamp(localPosition.y / (rect.height * 0.5f), -1f, 1f)
                : 0f;
            _targetNormalizedPointerPosition = new Vector2(normalizedX, normalizedY);
            _targetAnchoredPosition = _initialAnchoredPosition
                + _targetNormalizedPointerPosition * _maxPositionOffset;
        }

    }
}
