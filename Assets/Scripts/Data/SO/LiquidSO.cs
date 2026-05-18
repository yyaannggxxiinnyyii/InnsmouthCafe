using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 辅助液配置数据
    /// 定义每种辅助液的属性和显示效果
    /// </summary>
    [CreateAssetMenu(fileName = "Liquid_", menuName = "InnsmouthCafe/Config/Liquid Config", order = 3)]
    public class LiquidSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("辅助液类型")]
        public LiquidType liquidType;

        [Tooltip("辅助液名称")]
        public string liquidName;

        [Tooltip("辅助液描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("UI显示")]
        [Tooltip("辅助液图标")]
        public Sprite liquidIcon;

        [Tooltip("辅助液容器图标（制作界面2使用）")]
        public Sprite containerIcon;

        [Tooltip("辅助液颜色（用于容量进度条显示）")]
        public Color liquidColor = Color.white;

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
            Color color = liquidColor;
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 检查颜色是否设置
            if (liquidColor == Color.clear)
            {
                Debug.LogWarning($"[{liquidName}] 辅助液颜色未设置，建议设置颜色用于进度条显示");
            }

            // 根据液体类型建议颜色
            switch (liquidType)
            {
                case LiquidType.HotWater:
                    if (liquidColor != new Color(0.7f, 0.8f, 0.9f, 0.5f))
                    {
                        Debug.Log($"[{liquidName}] 建议颜色：浅灰蓝/半透明");
                    }
                    break;

                case LiquidType.Milk:
                    if (liquidColor != Color.white)
                    {
                        Debug.Log($"[{liquidName}] 建议颜色：白色");
                    }
                    break;

                case LiquidType.Foam:
                    if (liquidColor != new Color(1f, 0.95f, 0.8f))
                    {
                        Debug.Log($"[{liquidName}] 建议颜色：淡黄色/乳白色");
                    }
                    break;

                case LiquidType.IceWater:
                    if (liquidColor != new Color(0.6f, 0.8f, 1f))
                    {
                        Debug.Log($"[{liquidName}] 建议颜色：浅蓝色");
                    }
                    break;
            }
        }
    }
}
