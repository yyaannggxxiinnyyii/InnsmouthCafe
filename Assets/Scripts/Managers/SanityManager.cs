using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 理智值等级
    /// </summary>
    public enum SanityLevel
    {
        /// <summary>
        /// 高理智 (80-100)
        /// </summary>
        High,

        /// <summary>
        /// 中等理智 (50-79)
        /// </summary>
        Medium,

        /// <summary>
        /// 低理智 (20-49)
        /// </summary>
        Low,

        /// <summary>
        /// 危险理智 (0-19)
        /// </summary>
        Critical
    }

    /// <summary>
    /// 理智值变化记录
    /// 用于日结算和调试追踪
    /// </summary>
    [System.Serializable]
    public class SanityChangeRecord
    {
        public float oldValue;
        public float newValue;
        public float delta;
        public string reason;
        public float timestamp;

        public SanityChangeRecord(float oldValue, float newValue, string reason)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.delta = newValue - oldValue;
            this.reason = reason;
            this.timestamp = Time.time;
        }
    }

    /// <summary>
    /// 理智值管理器
    /// 负责管理玩家的理智值，包括增减、事件通知、等级判定
    /// 理智值贯穿整个7天游戏流程，不会每日重置
    /// </summary>
    public class SanityManager : Singleton<SanityManager>
    {
        [Header("理智值配置")]
        [SerializeField]
        [Tooltip("初始理智值")]
        [Range(0, 100)]
        private float _initialSanity = 100f;

        [SerializeField]
        [Tooltip("最小理智值")]
        [Range(0, 100)]
        private float _minSanity = 0f;

        [SerializeField]
        [Tooltip("最大理智值")]
        [Range(0, 100)]
        private float _maxSanity = 100f;

        [Header("理智值变化量配置")]
        [SerializeField]
        [Tooltip("满意评价理智值奖励")]
        private float _satisfiedReward = 5f;

        [SerializeField]
        [Tooltip("一般评价理智值变化")]
        private float _neutralChange = 0f;

        [SerializeField]
        [Tooltip("不满意评价理智值惩罚")]
        private float _dissatisfiedPenalty = -5f;

        [Header("理智值等级阈值")]
        [SerializeField]
        [Tooltip("高理智阈值 (>=此值为High)")]
        [Range(0, 100)]
        private float _highThreshold = 80f;

        [SerializeField]
        [Tooltip("中等理智阈值 (>=此值为Medium)")]
        [Range(0, 100)]
        private float _mediumThreshold = 50f;

        [SerializeField]
        [Tooltip("低理智阈值 (>=此值为Low)")]
        [Range(0, 100)]
        private float _lowThreshold = 20f;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        [SerializeField]
        [Tooltip("是否记录理智值变化历史")]
        private bool _recordHistory = true;

        [SerializeField]
        [Tooltip("最大历史记录数量")]
        private int _maxHistoryCount = 100;

        [Header("当前状态 - 只读")]
        [SerializeField]
        [Tooltip("当前理智值")]
        private float _currentSanity;

        [SerializeField]
        [Tooltip("当前理智值等级")]
        private SanityLevel _currentLevel;

        /// <summary>
        /// 理智值变化历史记录
        /// </summary>
        private List<SanityChangeRecord> _changeHistory = new List<SanityChangeRecord>();

        /// <summary>
        /// 当前理智值
        /// </summary>
        public float CurrentSanity => _currentSanity;

        /// <summary>
        /// 当前理智值百分比 (0-1)
        /// </summary>
        public float SanityRatio => Mathf.Clamp01(_currentSanity / _maxSanity);

        /// <summary>
        /// 当前理智值等级
        /// </summary>
        public SanityLevel CurrentLevel => _currentLevel;

        /// <summary>
        /// 理智值变化历史记录（只读）
        /// </summary>
        public IReadOnlyList<SanityChangeRecord> ChangeHistory => _changeHistory.AsReadOnly();

        /// <summary>
        /// 理智值变化事件
        /// 参数：旧值, 新值, 变化原因
        /// </summary>
        public event Action<float, float, string> OnSanityChanged;

        /// <summary>
        /// 理智值等级变化事件
        /// 参数：新等级
        /// </summary>
        public event Action<SanityLevel> OnSanityLevelChanged;

        /// <summary>
        /// 理智值归零事件
        /// </summary>
        public event Action OnSanityDepleted;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            // 初始化理智值
            _currentSanity = _initialSanity;
            _currentLevel = CalculateSanityLevel(_currentSanity);

            if (_showDebugLog)
            {
                Debug.Log($"[SanityManager] 初始化完成，初始理智值: {_currentSanity}, 等级: {_currentLevel}");
            }
        }

        /// <summary>
        /// 增加理智值
        /// </summary>
        /// <param name="amount">增加量（正数）</param>
        /// <param name="reason">变化原因</param>
        public void AddSanity(float amount, string reason = "未知原因")
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[SanityManager] AddSanity 参数应为正数，当前值: {amount}，请使用 ReduceSanity");
                amount = Mathf.Abs(amount);
            }

            ChangeSanity(amount, reason);
        }

        /// <summary>
        /// 减少理智值
        /// </summary>
        /// <param name="amount">减少量（正数）</param>
        /// <param name="reason">变化原因</param>
        public void ReduceSanity(float amount, string reason = "未知原因")
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[SanityManager] ReduceSanity 参数应为正数，当前值: {amount}，请使用 AddSanity");
                amount = Mathf.Abs(amount);
            }

            ChangeSanity(-amount, reason);
        }

        /// <summary>
        /// 根据订单评价档位调整理智值
        /// </summary>
        /// <param name="feedbackLevel">评价档位 (0=不满意, 1=一般, 2=满意)</param>
        public void ApplyFeedbackSanityChange(int feedbackLevel)
        {
            float change = feedbackLevel switch
            {
                0 => _dissatisfiedPenalty,
                1 => _neutralChange,
                2 => _satisfiedReward,
                _ => 0f
            };

            string feedbackText = feedbackLevel switch
            {
                0 => "不满意",
                1 => "一般",
                2 => "满意",
                _ => "未知"
            };

            if (change != 0)
            {
                ChangeSanity(change, $"顾客评价：{feedbackText}");
            }
        }

        /// <summary>
        /// 直接设置理智值
        /// </summary>
        /// <param name="value">目标理智值</param>
        public void SetSanity(float value)
        {
            float oldValue = _currentSanity;
            _currentSanity = Mathf.Clamp(value, _minSanity, _maxSanity);

            if (Mathf.Approximately(oldValue, _currentSanity))
            {
                return;
            }

            string reason = "直接设置理智值";
            RecordChange(oldValue, _currentSanity, reason);
            CheckLevelChange();
            OnSanityChanged?.Invoke(oldValue, _currentSanity, reason);

            if (_showDebugLog)
            {
                Debug.Log($"[SanityManager] {reason}: {oldValue:F1} → {_currentSanity:F1}");
            }
        }

        /// <summary>
        /// 重置理智值为初始值
        /// </summary>
        public void ResetSanity()
        {
            float oldValue = _currentSanity;
            _currentSanity = _initialSanity;
            _currentLevel = CalculateSanityLevel(_currentSanity);

            string reason = "重置理智值";
            RecordChange(oldValue, _currentSanity, reason);
            OnSanityChanged?.Invoke(oldValue, _currentSanity, reason);
            OnSanityLevelChanged?.Invoke(_currentLevel);

            if (_showDebugLog)
            {
                Debug.Log($"[SanityManager] {reason}: {oldValue:F1} → {_currentSanity:F1}, 等级: {_currentLevel}");
            }
        }

        /// <summary>
        /// 获取当前理智值等级
        /// </summary>
        public SanityLevel GetSanityLevel()
        {
            return _currentLevel;
        }

        /// <summary>
        /// 清空理智值变化历史记录
        /// </summary>
        public void ClearHistory()
        {
            _changeHistory.Clear();

            if (_showDebugLog)
            {
                Debug.Log("[SanityManager] 已清空理智值变化历史记录");
            }
        }

        /// <summary>
        /// 获取当天的理智值变化总量
        /// </summary>
        /// <param name="dayStartTime">当天开始时间戳</param>
        public float GetDailySanityChange(float dayStartTime)
        {
            float totalChange = 0f;

            foreach (var record in _changeHistory)
            {
                if (record.timestamp >= dayStartTime)
                {
                    totalChange += record.delta;
                }
            }

            return totalChange;
        }

        /// <summary>
        /// 核心方法：改变理智值
        /// </summary>
        private void ChangeSanity(float delta, string reason)
        {
            float oldValue = _currentSanity;
            _currentSanity = Mathf.Clamp(_currentSanity + delta, _minSanity, _maxSanity);

            // 如果值没有实际变化（已到达边界），仍然记录但不触发事件
            if (Mathf.Approximately(oldValue, _currentSanity))
            {
                if (_showDebugLog)
                {
                    Debug.Log($"[SanityManager] 理智值已达到边界，无法继续变化: {reason} ({delta:+0.0;-0.0})");
                }
                return;
            }

            // 记录变化
            RecordChange(oldValue, _currentSanity, reason);

            // 检查等级变化
            CheckLevelChange();

            // 触发事件
            OnSanityChanged?.Invoke(oldValue, _currentSanity, reason);

            // 发布操作日志（增加绿色，减少红色）
            //float actualDelta = _currentSanity - oldValue;
            //if (actualDelta > 0f)
            //    ActionLogBus.Log($"理智值：+{actualDelta:F1}", Color.green);
            //else
            //    ActionLogBus.Log($"理智值：{actualDelta:F1}", Color.red);

            // 检查是否归零
            if (_currentSanity <= _minSanity && oldValue > _minSanity)
            {
                OnSanityDepleted?.Invoke();

                if (_showDebugLog)
                {
                    Debug.LogWarning("[SanityManager] 理智值已归零！");
                }
            }

            if (_showDebugLog)
            {
                string changeText = delta > 0 ? $"+{delta:F1}" : $"{delta:F1}";
                Debug.Log($"[SanityManager] {reason}: {oldValue:F1} {changeText} → {_currentSanity:F1} (等级: {_currentLevel})");
            }
        }

        /// <summary>
        /// 记录理智值变化
        /// </summary>
        private void RecordChange(float oldValue, float newValue, string reason)
        {
            if (!_recordHistory)
            {
                return;
            }

            var record = new SanityChangeRecord(oldValue, newValue, reason);
            _changeHistory.Add(record);

            // 限制历史记录数量
            if (_changeHistory.Count > _maxHistoryCount)
            {
                _changeHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 检查理智值等级是否变化
        /// </summary>
        private void CheckLevelChange()
        {
            SanityLevel newLevel = CalculateSanityLevel(_currentSanity);

            if (newLevel != _currentLevel)
            {
                SanityLevel oldLevel = _currentLevel;
                _currentLevel = newLevel;

                OnSanityLevelChanged?.Invoke(_currentLevel);

                if (_showDebugLog)
                {
                    Debug.Log($"[SanityManager] 理智值等级变化: {oldLevel} → {_currentLevel}");
                }
            }
        }

        /// <summary>
        /// 计算理智值等级
        /// </summary>
        private SanityLevel CalculateSanityLevel(float sanity)
        {
            if (sanity >= _highThreshold)
            {
                return SanityLevel.High;
            }
            else if (sanity >= _mediumThreshold)
            {
                return SanityLevel.Medium;
            }
            else if (sanity >= _lowThreshold)
            {
                return SanityLevel.Low;
            }
            else
            {
                return SanityLevel.Critical;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器调试方法：增加理智值
        /// </summary>
        [ContextMenu("测试：增加理智值 +10")]
        private void DebugAddSanity()
        {
            AddSanity(10f, "编辑器测试");
        }

        /// <summary>
        /// 编辑器调试方法：减少理智值
        /// </summary>
        [ContextMenu("测试：减少理智值 -10")]
        private void DebugReduceSanity()
        {
            ReduceSanity(10f, "编辑器测试");
        }

        /// <summary>
        /// 编辑器调试方法：重置理智值
        /// </summary>
        [ContextMenu("测试：重置理智值")]
        private void DebugResetSanity()
        {
            ResetSanity();
        }

        /// <summary>
        /// 编辑器调试方法：设置为危险等级
        /// </summary>
        [ContextMenu("测试：设置为危险等级 (10)")]
        private void DebugSetCritical()
        {
            SetSanity(10f);
        }
#endif
    }
}
