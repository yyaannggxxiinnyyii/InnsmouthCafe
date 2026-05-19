using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 杯子锚点配置
    /// 定义杯子在不同场景和状态下的位置
    /// </summary>
    public class CupAnchorPoints : MonoBehaviour
    {
        [Header("杯子选择区锚点")]
        [SerializeField] [Tooltip("小杯锚点")]
        private RectTransform _smallCupAnchor;

        [SerializeField] [Tooltip("中杯锚点")]
        private RectTransform _mediumCupAnchor;

        [SerializeField] [Tooltip("大杯锚点")]
        private RectTransform _largeCupAnchor;

        [SerializeField] [Tooltip("超大杯锚点")]
        private RectTransform _xlargeCupAnchor;

        [Header("萃取机锚点")]
        [SerializeField] [Tooltip("萃取机内杯子位置（制作场景1）")]
        private RectTransform _extractionMachineAnchor;

        [Header("场景锚点")]
        [SerializeField] [Tooltip("吧台场景杯子位置")]
        private RectTransform _barSceneAnchor;

        [SerializeField] [Tooltip("制作场景2杯子位置")]
        private RectTransform _craftMixSceneAnchor;

        [Header("场景切换过渡锚点")]
        [SerializeField] [Tooltip("左侧过渡锚点（屏幕左边外，用于向左切换时的退出和向右切换时的进入）")]
        private RectTransform _leftTransitionAnchor;

        [SerializeField] [Tooltip("右侧过渡锚点（屏幕右边外，用于向右切换时的退出和向左切换时的进入）")]
        private RectTransform _rightTransitionAnchor;

        /// <summary>
        /// 根据杯子容量获取选择区锚点
        /// </summary>
        public RectTransform GetSelectionAnchor(float capacity)
        {
            if (capacity <= 250f) return _smallCupAnchor;
            if (capacity <= 400f) return _mediumCupAnchor;
            if (capacity <= 500f) return _largeCupAnchor;
            return _xlargeCupAnchor;
        }

        /// <summary>
        /// 根据杯子ID获取选择区锚点
        /// </summary>
        public RectTransform GetSelectionAnchor(string cupId)
        {
            switch (cupId)
            {
                case "cup_small": return _smallCupAnchor;
                case "cup_medium": return _mediumCupAnchor;
                case "cup_large": return _largeCupAnchor;
                case "cup_xlarge": return _xlargeCupAnchor;
                default: return _mediumCupAnchor;
            }
        }

        /// <summary>
        /// 获取萃取机锚点
        /// </summary>
        public RectTransform GetExtractionAnchor()
        {
            return _extractionMachineAnchor;
        }

        /// <summary>
        /// 根据场景类型获取锚点
        /// </summary>
        public RectTransform GetSceneAnchor(GameViewType viewType)
        {
            switch (viewType)
            {
                case GameViewType.Bar:
                    return _barSceneAnchor;
                case GameViewType.CraftBase:
                    return _extractionMachineAnchor;
                case GameViewType.CraftMix:
                    return _craftMixSceneAnchor;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取左侧过渡锚点（屏幕左边外）
        /// </summary>
        public RectTransform GetLeftTransitionAnchor()
        {
            return _leftTransitionAnchor;
        }

        /// <summary>
        /// 获取右侧过渡锚点（屏幕右边外）
        /// </summary>
        public RectTransform GetRightTransitionAnchor()
        {
            return _rightTransitionAnchor;
        }
    }
}
