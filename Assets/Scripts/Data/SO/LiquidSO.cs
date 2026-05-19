using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 辅助液配置数据
    /// 定义辅助液的基础属性和显示信息
    /// </summary>
    [CreateAssetMenu(fileName = "Liquid_", menuName = "InnsmouthCafe/Config/Liquid Config", order = 3)]
    public class LiquidSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("辅助液唯一ID")]
        public string liquidId;

        [Tooltip("辅助液显示名称")]
        public string liquidName;

        [Header("视觉资源")]
        [Tooltip("辅助液图标")]
        public Sprite icon;

        [Tooltip("辅助液容器图标（制作界面2使用）")]
        public Sprite containerIcon;

        [Tooltip("辅助液显示颜色（用于容量条）")]
        public Color displayColor = Color.white;

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("辅助液描述文本")]
        public string description;

        [Header("倒入效果")]
        [Tooltip("倒入时的粒子效果颜色")]
        public Color particleColor = Color.white;

        [Tooltip("倒入音效")]
        public AudioClip pourSound;

        [Tooltip("停止倒入音效")]
        public AudioClip stopSound;

        /// <summary>
        /// 获取带透明度的颜色（用于UI叠加显示）
        /// </summary>
        public Color GetColorWithAlpha(float alpha)
        {
            Color color = displayColor;
            color.a = alpha;
            return color;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // Error: liquidId为空
            if (string.IsNullOrEmpty(liquidId))
            {
                Debug.LogError($"[LiquidSO] 辅助液ID不能为空", this);
            }

            // Error: liquidName为空
            if (string.IsNullOrEmpty(liquidName))
            {
                Debug.LogError($"[LiquidSO] 辅助液名称不能为空", this);
            }

            // Error: icon为空
            if (icon == null)
            {
                Debug.LogError($"[{liquidName}] 辅助液图标不能为空", this);
            }

            // Warning: description为空
            if (string.IsNullOrEmpty(description))
            {
                Debug.LogWarning($"[{liquidName}] 辅助液描述为空，建议填写描述文本", this);
            }

            // 检查颜色是否设置
            if (displayColor == Color.clear)
            {
                Debug.LogWarning($"[{liquidName}] 辅助液颜色未设置，建议设置颜色用于进度条显示", this);
            }
        }
#endif
    }
}
