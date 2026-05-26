using UnityEngine;
using TMPro;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 教学引导标记组件
    /// 挂在需要高亮引导的UI节点上，自身RectTransform定义高亮区域
    /// 通过 TriggerEventKey 配置触发时机，运行时由 TutorialFlowController 自动驱动
    /// 常用 key 参见 TutorialEvents 常量类
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TutorialMark : MonoBehaviour
    {
        [Header("触发时机")]
        [SerializeField] [Tooltip("触发此引导的事件 key（参照 TutorialEvents 常量类，如 DayStart / CustomerEnter）")]
        private string _triggerEventKey = string.Empty;

        [SerializeField] [Tooltip("限定在第几天触发（0=不限天数）")]
        private int _triggerDay;

        [SerializeField] [Tooltip("是否只触发一次（触发后自动标记完成）")]
        private bool _onlyOnce = true;

        [Header("序列排序")]
        [SerializeField] [Tooltip("同一触发时机下的执行顺序（小的先执行）")]
        private int _order;

        [Header("提示内容")]
        [SerializeField] [Tooltip("提示标题（可选，留空则不显示标题）")]
        private string _tipTitle;

        [SerializeField] [Tooltip("提示正文")]
        [TextArea(2, 5)]
        private string _tipText;

        [Header("行为设置")]
        [SerializeField] [Tooltip("是否必须点击高亮目标才能继续")]
        private bool _requireClickTarget;

        [SerializeField] [Tooltip("自动推进延迟（秒）。0=需要手动点击继续")]
        private float _autoAdvanceDelay;

        [Header("显示设置")]
        [SerializeField] [Tooltip("高亮框额外边距")]
        private float _highlightPadding = 20f;

        [SerializeField] [Tooltip("TipPanel显示位置（相对于高亮区域）")]
        private TipPosition _tipPosition = TipPosition.Auto;

        [SerializeField] [Tooltip("TipPanel与高亮区域的间距")]
        private float _tipOffset = 20f;

        /// <summary>是否已触发过</summary>
        private bool _hasTriggered;

        // ── 公共属性 ─────────────────────────────────────────

        /// <summary>触发事件 key</summary>
        public string TriggerEventKey => _triggerEventKey;

        /// <summary>限定天数（0=不限）</summary>
        public int TriggerDay => _triggerDay;

        /// <summary>是否只触发一次</summary>
        public bool OnlyOnce => _onlyOnce;

        /// <summary>排序权重</summary>
        public int Order => _order;

        /// <summary>提示标题</summary>
        public string TipTitle => _tipTitle;

        /// <summary>提示正文</summary>
        public string TipText => _tipText;

        /// <summary>是否必须点击目标</summary>
        public bool RequireClickTarget => _requireClickTarget;

        /// <summary>自动推进延迟</summary>
        public float AutoAdvanceDelay => _autoAdvanceDelay;

        /// <summary>高亮边距</summary>
        public float HighlightPadding => _highlightPadding;

        /// <summary>TipPanel位置</summary>
        public TipPosition TipPositionMode => _tipPosition;

        /// <summary>TipPanel偏移距离</summary>
        public float TipOffset => _tipOffset;

        /// <summary>是否已触发过</summary>
        public bool HasTriggered => _hasTriggered;

        /// <summary>获取自身RectTransform作为高亮区域</summary>
        public RectTransform HighlightRect => GetComponent<RectTransform>();

        /// <summary>标记为已触发</summary>
        public void MarkAsTriggered()
        {
            _hasTriggered = true;
        }

        /// <summary>重置触发状态</summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        /// <summary>是否可以触发（未触发过，或允许重复触发）</summary>
        public bool CanTrigger()
        {
            return !_onlyOnce || !_hasTriggered;
        }

#if UNITY_EDITOR
        /// <summary>Scene视图中绘制高亮区域预览</summary>
        private void OnDrawGizmosSelected()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) return;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }
#endif
    }

    /// <summary>
    /// TipPanel相对于高亮区域的显示位置
    /// </summary>
    public enum TipPosition
    {
        /// <summary>自动判断（优先下方，空间不够则上方）</summary>
        Auto,
        /// <summary>高亮区域上方</summary>
        Above,
        /// <summary>高亮区域下方</summary>
        Below,
        /// <summary>高亮区域左侧</summary>
        Left,
        /// <summary>高亮区域右侧</summary>
        Right
    }
}
