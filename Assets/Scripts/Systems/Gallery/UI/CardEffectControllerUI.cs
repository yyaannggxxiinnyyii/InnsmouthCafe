using UnityEngine;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 统一计算卡牌倾斜产生的光照状态，并向所有特效层提供一致的 Shader 参数。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class CardEffectControllerUI : MonoBehaviour
    {
        private static readonly int CardPointerPositionPropertyId = Shader.PropertyToID("_CardPointerPosition");
        private static readonly int CardTiltOffsetPropertyId = Shader.PropertyToID("_CardTiltOffset");
        private static readonly int CardLightDirectionPropertyId = Shader.PropertyToID("_CardLightDirection");
        private static readonly int CardHighlightAnglePropertyId = Shader.PropertyToID("_CardHighlightAngle");
        private static readonly int CardHighlightPositionPropertyId = Shader.PropertyToID("_CardHighlightPosition");
        private static readonly int CardReflectionStrengthPropertyId = Shader.PropertyToID("_CardReflectionStrength");
        private static readonly int LegacyTiltOffsetPropertyId = Shader.PropertyToID("_TiltOffset");
        private static readonly int LegacyHighlightAnglePropertyId = Shader.PropertyToID("_HighlightAngle");

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("所属卡牌的倾斜组件；为空时自动获取当前节点的 CardTiltUI")]
        private CardTiltUI _cardTilt;

        [Header("统一光照响应")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("所有卡牌特效跟随倾斜的整体强度")]
        private float _tiltResponse = 1f;

        [SerializeField]
        [Range(0f, 2f)]
        [Tooltip("传递给各特效 Shader 的倾斜位移上限")]
        private float _tiltOffsetRange = 0.45f;

        [SerializeField]
        [Range(0f, 360f)]
        [Tooltip("统一光照方向跟随鼠标旋转的范围；360 为完整方向跟随")]
        private float _highlightRotationRange = 15f;

        [SerializeField]
        [Range(-180f, 180f)]
        [Tooltip("统一光照基础方向的角度偏移")]
        private float _highlightRotationOffset;

        private float _currentHighlightAngle;
        private bool _hasHighlightAngle;

        public Vector2 PointerPosition { get; private set; }
        public Vector2 TiltOffset { get; private set; }
        public Vector2 LightDirection { get; private set; } = Vector2.one.normalized;
        public float HighlightAngle { get; private set; } = 45f;
        public Vector2 HighlightPosition { get; private set; }
        public float ReflectionStrength { get; private set; }

        private void Awake()
        {
            _cardTilt ??= GetComponent<CardTiltUI>();
        }

        private void OnDisable()
        {
            _hasHighlightAngle = false;
        }

        private void Update()
        {
            if (_cardTilt == null)
            {
                return;
            }

            PointerPosition = _cardTilt.NormalizedPointerPosition;
            float tiltResponse = Mathf.Clamp01(_tiltResponse);
            TiltOffset = PointerPosition
                * (tiltResponse * Mathf.Max(0f, _tiltOffsetRange));
            ReflectionStrength = Mathf.Clamp01(PointerPosition.magnitude * tiltResponse);
            HighlightAngle = CalculateHighlightAngle(PointerPosition, tiltResponse);
            float highlightAngleRadians = HighlightAngle * Mathf.Deg2Rad;
            LightDirection = new Vector2(
                Mathf.Cos(highlightAngleRadians),
                Mathf.Sin(highlightAngleRadians));
            HighlightPosition = TiltOffset;
        }

        /// <summary>
        /// 将统一卡牌光照状态写入目标材质，并兼容当前镭射 Shader 的旧属性名称。
        /// </summary>
        public void ApplyToMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            SetVectorIfPresent(material, CardPointerPositionPropertyId, PointerPosition);
            SetVectorIfPresent(material, CardTiltOffsetPropertyId, TiltOffset);
            SetVectorIfPresent(material, CardLightDirectionPropertyId, LightDirection);
            SetFloatIfPresent(material, CardHighlightAnglePropertyId, HighlightAngle);
            SetVectorIfPresent(material, CardHighlightPositionPropertyId, HighlightPosition);
            SetFloatIfPresent(material, CardReflectionStrengthPropertyId, ReflectionStrength);
            SetVectorIfPresent(material, LegacyTiltOffsetPropertyId, TiltOffset);
            SetFloatIfPresent(material, LegacyHighlightAnglePropertyId, HighlightAngle);
        }

        /// <summary>
        /// 根据鼠标方向计算一圈内的目标角度，并沿最短路径平滑更新统一光照方向。
        /// </summary>
        private float CalculateHighlightAngle(Vector2 pointerPosition, float tiltResponse)
        {
            float rotationOffset = Mathf.Clamp(_highlightRotationOffset, -180f, 180f);
            float baseHighlightAngle = 45f + rotationOffset;
            if (!_hasHighlightAngle)
            {
                _currentHighlightAngle = baseHighlightAngle;
                _hasHighlightAngle = true;
            }

            float targetHighlightAngle = baseHighlightAngle;
            if (_cardTilt.IsPointerInside && pointerPosition.sqrMagnitude > 0.000001f)
            {
                float pointerAngle = Mathf.Atan2(pointerPosition.y, pointerPosition.x)
                    * Mathf.Rad2Deg + rotationOffset;
                float rotationScale = Mathf.Clamp(_highlightRotationRange, 0f, 360f)
                    / 360f * tiltResponse;
                targetHighlightAngle = baseHighlightAngle
                    + Mathf.DeltaAngle(baseHighlightAngle, pointerAngle) * rotationScale;
            }

            float interpolation = 1f - Mathf.Exp(
                -Mathf.Max(0f, _cardTilt.SmoothSpeed) * Time.unscaledDeltaTime);
            _currentHighlightAngle = Mathf.LerpAngle(
                _currentHighlightAngle,
                targetHighlightAngle,
                interpolation);
            _currentHighlightAngle = baseHighlightAngle
                + Mathf.DeltaAngle(baseHighlightAngle, _currentHighlightAngle);
            return _currentHighlightAngle;
        }

        /// <summary>
        /// 仅在目标 Shader 声明对应属性时写入二维向量。
        /// </summary>
        private static void SetVectorIfPresent(Material material, int propertyId, Vector2 value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetVector(propertyId, value);
            }
        }

        /// <summary>
        /// 仅在目标 Shader 声明对应属性时写入浮点值。
        /// </summary>
        private static void SetFloatIfPresent(Material material, int propertyId, float value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetFloat(propertyId, value);
            }
        }
    }
}
