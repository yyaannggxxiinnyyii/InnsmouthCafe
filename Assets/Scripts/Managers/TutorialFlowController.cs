using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 教学流程控制器
    /// 监听 TutorialEventBus，自动触发场景中匹配的 TutorialMark
    /// 支持队列：引导播放中收到新事件时先入队，播完后自动取下一批
    /// 仅在教学模式下激活（可在Inspector中关闭限制用于测试）
    /// </summary>
    public class TutorialFlowController : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] [Tooltip("是否仅在教学模式下工作（关闭则任何模式都触发引导）")]
        private bool _tutorialModeOnly = true;

        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        /// <summary>场景中所有TutorialMark缓存</summary>
        private List<TutorialMark> _allMarks = new List<TutorialMark>();

        /// <summary>待播放队列（每项是一批按序排好的Mark）</summary>
        private Queue<TutorialMark[]> _pendingQueue = new Queue<TutorialMark[]>();

        /// <summary>是否已初始化</summary>
        private bool _initialized;

        private void Awake()
        {
            // Awake中只做Mark收集，不做模式判断（此时跨场景Manager可能还没就绪）
            _allMarks.Clear();
            _allMarks.AddRange(FindObjectsOfType<TutorialMark>(true));
        }

        private void Start()
        {
            if (_tutorialModeOnly && !IsTutorialMode())
            {
                if (_showDebugLog)
                    Debug.Log("[TutorialFlow] 非教学模式，控制器已禁用");
                enabled = false;
                return;
            }

            Initialize();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            TutorialEventBus.Clear();
        }

        /// <summary>初始化：订阅事件总线</summary>
        private void Initialize()
        {
            if (_initialized) return;

            SubscribeEvents();
            _initialized = true;

            if (_showDebugLog)
                Debug.Log($"[TutorialFlow] 初始化完成，收集到 {_allMarks.Count} 个引导标记");
        }

        /// <summary>重新收集场景中所有TutorialMark</summary>
        public void RefreshMarks()
        {
            _allMarks.Clear();
            _allMarks.AddRange(FindObjectsOfType<TutorialMark>(true));
        }

        // ── 事件订阅 ─────────────────────────────────────────

        private void SubscribeEvents()
        {
            TutorialEventBus.Subscribe(TutorialEvents.DayStart,            OnEventDayStart);
            TutorialEventBus.Subscribe(TutorialEvents.DayEnd,              OnEventDayEnd);
            TutorialEventBus.Subscribe(TutorialEvents.CustomerEnter,       OnEventCustomerEnter);
            TutorialEventBus.Subscribe(TutorialEvents.CustomerReadyToTalk, OnEventCustomerReadyToTalk);
            TutorialEventBus.Subscribe(TutorialEvents.CustomerTicketShown, OnEventCustomerTicketShown);
            TutorialEventBus.Subscribe(TutorialEvents.FirstBarToCraftBaseSwitchComplete, OnEventFirstBarToCraftBaseSwitchComplete);
            TutorialEventBus.Subscribe(TutorialEvents.FirstCraftMixSwitchComplete, OnEventFirstCraftMixSwitchComplete);
            TutorialEventBus.Subscribe(TutorialEvents.CustomerLeave,       OnEventCustomerLeave);
            TutorialEventBus.Subscribe(TutorialEvents.OrderGenerated,      OnEventOrderGenerated);
            TutorialEventBus.Subscribe(TutorialEvents.CoffeeSubmit,        OnEventCoffeeSubmit);
            TutorialEventBus.Subscribe(TutorialEvents.ViewSwitchBar,       OnEventViewSwitchBar);
            TutorialEventBus.Subscribe(TutorialEvents.ViewSwitchCraftBase, OnEventViewSwitchCraftBase);
            TutorialEventBus.Subscribe(TutorialEvents.ViewSwitchCraftMix,  OnEventViewSwitchCraftMix);
            TutorialEventBus.Subscribe(TutorialEvents.OctopusOpeningComplete, OnEventOctopusOpeningComplete);
            TutorialEventBus.Subscribe(TutorialEvents.FirstCupSelected,       OnEventFirstCupSelected);
        }

        private void UnsubscribeEvents()
        {
            TutorialEventBus.Unsubscribe(TutorialEvents.DayStart,            OnEventDayStart);
            TutorialEventBus.Unsubscribe(TutorialEvents.DayEnd,              OnEventDayEnd);
            TutorialEventBus.Unsubscribe(TutorialEvents.CustomerEnter,       OnEventCustomerEnter);
            TutorialEventBus.Unsubscribe(TutorialEvents.CustomerReadyToTalk, OnEventCustomerReadyToTalk);
            TutorialEventBus.Unsubscribe(TutorialEvents.CustomerTicketShown, OnEventCustomerTicketShown);
            TutorialEventBus.Unsubscribe(TutorialEvents.FirstBarToCraftBaseSwitchComplete, OnEventFirstBarToCraftBaseSwitchComplete);
            TutorialEventBus.Unsubscribe(TutorialEvents.FirstCraftMixSwitchComplete, OnEventFirstCraftMixSwitchComplete);
            TutorialEventBus.Unsubscribe(TutorialEvents.CustomerLeave,       OnEventCustomerLeave);
            TutorialEventBus.Unsubscribe(TutorialEvents.OrderGenerated,      OnEventOrderGenerated);
            TutorialEventBus.Unsubscribe(TutorialEvents.CoffeeSubmit,        OnEventCoffeeSubmit);
            TutorialEventBus.Unsubscribe(TutorialEvents.ViewSwitchBar,       OnEventViewSwitchBar);
            TutorialEventBus.Unsubscribe(TutorialEvents.ViewSwitchCraftBase, OnEventViewSwitchCraftBase);
            TutorialEventBus.Unsubscribe(TutorialEvents.ViewSwitchCraftMix,  OnEventViewSwitchCraftMix);
            TutorialEventBus.Unsubscribe(TutorialEvents.OctopusOpeningComplete, OnEventOctopusOpeningComplete);
            TutorialEventBus.Unsubscribe(TutorialEvents.FirstCupSelected,       OnEventFirstCupSelected);
        }

        // 各事件对应的独立方法，方法引用稳定，可被 List.Remove 正确匹配
        private void OnEventDayStart()            => OnEvent(TutorialEvents.DayStart);
        private void OnEventDayEnd()              => OnEvent(TutorialEvents.DayEnd);
        private void OnEventCustomerEnter()       => OnEvent(TutorialEvents.CustomerEnter);
        private void OnEventCustomerReadyToTalk()  => OnEvent(TutorialEvents.CustomerReadyToTalk);
        private void OnEventCustomerTicketShown()  => OnEvent(TutorialEvents.CustomerTicketShown);
        private void OnEventFirstBarToCraftBaseSwitchComplete() => OnEvent(TutorialEvents.FirstBarToCraftBaseSwitchComplete);
        private void OnEventFirstCraftMixSwitchComplete() => OnEvent(TutorialEvents.FirstCraftMixSwitchComplete);
        private void OnEventCustomerLeave()       => OnEvent(TutorialEvents.CustomerLeave);
        private void OnEventOrderGenerated()      => OnEvent(TutorialEvents.OrderGenerated);
        private void OnEventCoffeeSubmit()        => OnEvent(TutorialEvents.CoffeeSubmit);
        private void OnEventViewSwitchBar()       => OnEvent(TutorialEvents.ViewSwitchBar);
        private void OnEventViewSwitchCraftBase() => OnEvent(TutorialEvents.ViewSwitchCraftBase);
        private void OnEventViewSwitchCraftMix()  => OnEvent(TutorialEvents.ViewSwitchCraftMix);
        private void OnEventOctopusOpeningComplete() => OnEvent(TutorialEvents.OctopusOpeningComplete);
        private void OnEventFirstCupSelected()        => OnEvent(TutorialEvents.FirstCupSelected);

        // ── 核心触发逻辑 ────────────────────────────────────

        private void OnEvent(string eventKey)
        {
            int currentDay = GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentDay
                : 0;

            var matchingMarks = _allMarks
                .Where(m => m != null
                    && m.TriggerEventKey == eventKey
                    && m.CanTrigger()
                    && (m.TriggerDay == 0 || m.TriggerDay == currentDay))
                .OrderBy(m => m.Order)
                .ToArray();

            if (matchingMarks.Length == 0) return;

            if (_showDebugLog)
                Debug.Log($"[TutorialFlow] 事件 '{eventKey}' 触发，匹配 {matchingMarks.Length} 个Mark");

            // 标记为已触发（提前标记，防止队列中重复入队）
            foreach (var mark in matchingMarks)
            {
                if (mark.OnlyOnce)
                    mark.MarkAsTriggered();
            }

            EnqueueOrPlay(matchingMarks);
        }

        /// <summary>
        /// 如果当前没有引导在播放则直接播放，否则入队
        /// </summary>
        private void EnqueueOrPlay(TutorialMark[] marks)
        {
            if (TutorialGuideManager.Instance == null) return;

            if (TutorialGuideManager.Instance.IsGuideActive)
            {
                _pendingQueue.Enqueue(marks);

                if (_showDebugLog)
                    Debug.Log($"[TutorialFlow] 引导进行中，{marks.Length} 个Mark已入队，队列长度: {_pendingQueue.Count}");
            }
            else
            {
                PlayMarks(marks);
            }
        }

        /// <summary>
        /// 播放一批Mark，播完后检查队列
        /// </summary>
        private void PlayMarks(TutorialMark[] marks)
        {
            TutorialGuideManager.Instance.TriggerMarkSequence(marks, OnSequenceComplete);
        }

        /// <summary>
        /// 当前序列播完，取队列中下一批
        /// </summary>
        private void OnSequenceComplete()
        {
            if (_pendingQueue.Count > 0)
            {
                var next = _pendingQueue.Dequeue();

                if (_showDebugLog)
                    Debug.Log($"[TutorialFlow] 序列完成，取出队列中下一批（{next.Length} 个Mark），剩余队列: {_pendingQueue.Count}");

                PlayMarks(next);
            }
        }

        // ── 工具方法 ─────────────────────────────────────────

        /// <summary>判断当前是否为教学模式</summary>
        private bool IsTutorialMode()
        {
            if (GameManager.Instance != null)
            {
                var config = GameManager.Instance.SelectedModeConfig;
                if (config != null)
                    return config.gameMode == GameMode.Tutorial;
            }

            return false;
        }
    }
}
