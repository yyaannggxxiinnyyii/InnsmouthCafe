using UnityEngine;

namespace InnsmouthCafe.Diorama
{
    /// <summary>
    /// 控制箱庭经营场景的分层视差，以及非常轻微的相机跟随效果。
    /// </summary>
    public class DioramaParallaxController : MonoBehaviour
    {
        [Header("目标")]
        [SerializeField]
        [Tooltip("用于显示箱庭场景的相机；留空时自动查找 Main Camera")]
        private Camera _targetCamera;

        [Header("场景层")]
        [SerializeField]
        [Tooltip("场景后部物件层，通常包含墙面之间的固定摆件")]
        private Transform _backLayer;

        [SerializeField]
        [Tooltip("顾客层")]
        private Transform _customerLayer;

        [SerializeField]
        [Tooltip("前景摆件层")]
        private Transform _frontObjectLayer;

        [SerializeField]
        [Tooltip("最前方遮挡层")]
        private Transform _foregroundLayer;

        [Header("层级视差强度")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("后部物件层的最大位移，单位为世界单位")]
        private float _backStrength = 0.03f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("顾客层的最大位移，单位为世界单位")]
        private float _customerStrength = 0.06f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("前景摆件层的最大位移，单位为世界单位")]
        private float _frontObjectStrength = 0.1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("最前方遮挡层的最大位移，单位为世界单位")]
        private float _foregroundStrength = 0.14f;

        [Header("相机反馈")]
        [SerializeField]
        [Tooltip("是否启用相机的轻微位置和角度反馈")]
        private bool _enableCameraFeedback = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("相机跟随鼠标的最大位置偏移，单位为世界单位")]
        private float _cameraPositionStrength = 0.02f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("相机跟随鼠标的最大俯仰和偏航角度")]
        private float _cameraTiltStrength = 0.5f;

        [Header("平滑")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("视差和相机反馈的平滑时间，单位为秒")]
        private float _smoothTime = 0.18f;

        private Vector3 _backInitialPosition;
        private Vector3 _customerInitialPosition;
        private Vector3 _frontObjectInitialPosition;
        private Vector3 _foregroundInitialPosition;
        private Vector3 _cameraInitialPosition;
        private Quaternion _cameraInitialRotation;
        private Vector2 _normalizedMousePosition;
        private Vector2 _targetMousePosition;
        private bool _isParallaxEnabled = true;
        private bool _hasBackLayer;
        private bool _hasCustomerLayer;
        private bool _hasFrontObjectLayer;
        private bool _hasForegroundLayer;
        private bool _hasCamera;

        private void Awake()
        {
            CacheInitialTransforms();
        }

        private void OnEnable()
        {
            _normalizedMousePosition = Vector2.zero;
            _targetMousePosition = Vector2.zero;
        }

        private void OnDisable()
        {
            ApplySceneTransforms(Vector2.zero, true);
        }

        private void LateUpdate()
        {
            if (!_isParallaxEnabled)
            {
                return;
            }

            UpdateMouseTarget();

            float interpolation = CalculateInterpolation();
            _normalizedMousePosition = Vector2.Lerp(
                _normalizedMousePosition,
                _targetMousePosition,
                interpolation);

            ApplySceneTransforms(_normalizedMousePosition, false);
        }

        /// <summary>
        /// 设置视差是否参与更新；关闭时将场景层和相机反馈立即恢复到中心状态。
        /// </summary>
        public void SetParallaxEnabled(bool enabled)
        {
            _isParallaxEnabled = enabled;
            if (enabled)
            {
                if (_targetCamera != null)
                {
                    _cameraInitialPosition = _targetCamera.transform.position;
                    _cameraInitialRotation = _targetCamera.transform.rotation;
                    _hasCamera = true;
                }

                return;
            }

            _normalizedMousePosition = Vector2.zero;
            _targetMousePosition = Vector2.zero;
            ApplyLayerTransforms(Vector2.zero);
        }

        /// <summary>
        /// 缓存各场景层和相机的初始变换，后续所有偏移都基于这些值计算。
        /// </summary>
        private void CacheInitialTransforms()
        {
            _hasBackLayer = CacheLayerPosition(_backLayer, out _backInitialPosition);
            _hasCustomerLayer = CacheLayerPosition(_customerLayer, out _customerInitialPosition);
            _hasFrontObjectLayer = CacheLayerPosition(
                _frontObjectLayer,
                out _frontObjectInitialPosition);
            _hasForegroundLayer = CacheLayerPosition(
                _foregroundLayer,
                out _foregroundInitialPosition);

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_targetCamera != null)
            {
                _cameraInitialPosition = _targetCamera.transform.position;
                _cameraInitialRotation = _targetCamera.transform.rotation;
                _hasCamera = true;
            }
        }

        /// <summary>
        /// 记录指定层的初始本地位置，并返回该层是否有效。
        /// </summary>
        private static bool CacheLayerPosition(Transform layer, out Vector3 initialPosition)
        {
            initialPosition = layer != null ? layer.localPosition : Vector3.zero;
            return layer != null;
        }

        /// <summary>
        /// 根据窗口焦点和游戏暂停状态更新鼠标目标位置。
        /// </summary>
        private void UpdateMouseTarget()
        {
            if (Time.timeScale == 0f || !Application.isFocused)
            {
                _targetMousePosition = Vector2.zero;
                return;
            }

            if (_hasCamera && !IsCameraReferenceValid())
            {
                _hasCamera = false;
                return;
            }

            Rect cameraRect = _hasCamera
                ? _targetCamera.pixelRect
                : new Rect(0f, 0f, Screen.width, Screen.height);
            if (cameraRect.width <= 0f || cameraRect.height <= 0f)
            {
                _targetMousePosition = Vector2.zero;
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            float normalizedX = Mathf.Clamp01(
                (mousePosition.x - cameraRect.x) / cameraRect.width) * 2f - 1f;
            float normalizedY = Mathf.Clamp01(
                (mousePosition.y - cameraRect.y) / cameraRect.height) * 2f - 1f;
            _targetMousePosition = new Vector2(normalizedX, normalizedY);
        }

        /// <summary>
        /// 计算不受暂停影响的平滑插值系数。
        /// </summary>
        private float CalculateInterpolation()
        {
            if (_smoothTime <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-Time.unscaledDeltaTime / _smoothTime);
        }

        /// <summary>
        /// 应用场景层位移和相机反馈，并支持禁用时立即恢复初始状态。
        /// </summary>
        private void ApplySceneTransforms(Vector2 normalizedPosition, bool snap)
        {
            ApplyLayerTransforms(normalizedPosition);

            if (!_hasCamera || !IsCameraReferenceValid())
            {
                _hasCamera = false;
                return;
            }

            Vector2 cameraPosition = _enableCameraFeedback && !snap
                ? normalizedPosition * _cameraPositionStrength
                : Vector2.zero;
            Vector3 cameraRight = _cameraInitialRotation * Vector3.right;
            Vector3 cameraUp = _cameraInitialRotation * Vector3.up;
            _targetCamera.transform.position = _cameraInitialPosition
                + cameraRight * cameraPosition.x
                + cameraUp * cameraPosition.y;

            Vector3 tilt = _enableCameraFeedback && !snap
                ? new Vector3(-normalizedPosition.y, normalizedPosition.x, 0f)
                    * _cameraTiltStrength
                : Vector3.zero;
            _targetCamera.transform.rotation = _cameraInitialRotation * Quaternion.Euler(tilt);
        }

        /// <summary>
        /// 检查相机引用是否仍然有效，避免场景切换后访问已销毁对象。
        /// </summary>
        private bool IsCameraReferenceValid()
        {
            return _targetCamera != null && _targetCamera.gameObject != null;
        }

        /// <summary>
        /// 应用所有场景层的视差位移，不改写相机当前的世界变换。
        /// </summary>
        private void ApplyLayerTransforms(Vector2 normalizedPosition)
        {
            ApplyLayerPosition(
                _backLayer,
                _backInitialPosition,
                normalizedPosition,
                _backStrength,
                _hasBackLayer);
            ApplyLayerPosition(
                _customerLayer,
                _customerInitialPosition,
                normalizedPosition,
                _customerStrength,
                _hasCustomerLayer);
            ApplyLayerPosition(
                _frontObjectLayer,
                _frontObjectInitialPosition,
                normalizedPosition,
                _frontObjectStrength,
                _hasFrontObjectLayer);
            ApplyLayerPosition(
                _foregroundLayer,
                _foregroundInitialPosition,
                normalizedPosition,
                _foregroundStrength,
                _hasForegroundLayer);
        }

        /// <summary>
        /// 将指定层移动到初始位置附近，鼠标越偏离中心该层位移越明显。
        /// </summary>
        private static void ApplyLayerPosition(
            Transform layer,
            Vector3 initialPosition,
            Vector2 normalizedPosition,
            float strength,
            bool isValid)
        {
            if (!isValid || layer == null)
            {
                return;
            }

            Vector3 offset = new Vector3(
                -normalizedPosition.x * strength,
                -normalizedPosition.y * strength,
                0f);
            layer.localPosition = initialPosition + offset;
        }
    }
}
