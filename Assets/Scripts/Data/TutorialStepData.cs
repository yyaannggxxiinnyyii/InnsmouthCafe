using System;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 教学引导步骤数据
    /// 描述单个引导步骤的内容和行为
    /// </summary>
    [Serializable]
    public class TutorialStepData
    {
        [Header("提示内容")]
        [Tooltip("提示文本")]
        public string tipText;

        [Tooltip("提示标题（可选）")]
        public string tipTitle;

        [Header("目标定位")]
        [Tooltip("目标UI的路径（用于高亮定位，留空则不高亮）")]
        public string targetPath;

        [Tooltip("目标UI的RectTransform引用（运行时赋值，优先于targetPath）")]
        [NonSerialized]
        public RectTransform targetRect;

        [Header("推进条件")]
        [Tooltip("是否需要点击目标才能继续（否则点击任意位置继续）")]
        public bool requireClickTarget;

        [Tooltip("自动推进延迟秒数（0=不自动推进，需玩家操作）")]
        public float autoAdvanceDelay;

        [Header("高亮设置")]
        [Tooltip("高亮区域额外边距")]
        public float highlightPadding = 20f;

        [Tooltip("是否允许点击高亮区域外的遮罩继续")]
        public bool allowMaskClick = true;
    }
}
