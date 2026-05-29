using System.Collections.Generic;
using UnityEngine;

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
        [System.Serializable]
        public class DialogueEntry
        {
            [TextArea(2, 5)]
            [SerializeField] private string _text;

            [SerializeField] private Sprite _portrait;

            public string Text => _text;
            public Sprite Portrait => _portrait;
        }

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

        [SerializeField] [Tooltip("当前引导播放完后，到下一条引导开始前的延迟（秒），0=使用统一延迟")]
        private float _nextDelay;

        [Header("提示内容")]
        [SerializeField] [Tooltip("兼容单句配置；多句时请使用下方对话列表")]
        [TextArea(2, 5)]
        private string _tipText;

        [SerializeField] [Tooltip("默认老板对话立绘（条目未配置时继承此立绘）")]
        private Sprite _bossPortrait;

        [SerializeField] [Tooltip("多句对话列表；为空时使用旧版提示正文")]
        private List<DialogueEntry> _dialogues = new List<DialogueEntry>();

        [Header("高亮设置")]
        [SerializeField] [Tooltip("高亮框额外边距")]
        private float _highlightPadding = 20f;

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

        /// <summary>下一条引导开始前的延迟（0=使用统一延迟）</summary>
        public float NextDelay => _nextDelay;

        /// <summary>旧版提示正文</summary>
        public string TipText => _tipText;

        /// <summary>默认老板对话立绘</summary>
        public Sprite BossPortrait => _bossPortrait;

        /// <summary>多句对话列表</summary>
        public List<DialogueEntry> Dialogues => _dialogues;

        /// <summary>是否包含多句对话</summary>
        public bool HasDialogues => _dialogues != null && _dialogues.Count > 0;

        /// <summary>高亮边距</summary>
        public float HighlightPadding => _highlightPadding;

        /// <summary>是否已触发过</summary>
        public bool HasTriggered => _hasTriggered;

        /// <summary>获取自身RectTransform作为高亮区域</summary>
        public RectTransform HighlightRect => GetComponent<RectTransform>();

        /// <summary>获取第 N 句的文本</summary>
        public string GetDialogueText(int index)
        {
            if (_dialogues != null && index >= 0 && index < _dialogues.Count)
            {
                return _dialogues[index] != null ? _dialogues[index].Text : string.Empty;
            }

            return index == 0 ? _tipText : string.Empty;
        }

        /// <summary>获取第 N 句的立绘，未配置则返回默认立绘</summary>
        public Sprite GetDialoguePortrait(int index)
        {
            if (_dialogues != null && index >= 0 && index < _dialogues.Count)
            {
                var entry = _dialogues[index];
                if (entry != null && entry.Portrait != null)
                {
                    return entry.Portrait;
                }
            }

            return _bossPortrait;
        }

        /// <summary>获取对话句数，兼容旧版单句配置</summary>
        public int GetDialogueCount()
        {
            return HasDialogues ? _dialogues.Count : (!string.IsNullOrEmpty(_tipText) ? 1 : 0);
        }

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
}
