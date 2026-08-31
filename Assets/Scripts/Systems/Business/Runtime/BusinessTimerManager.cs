using System;
using UnityEngine;
using InnsmouthCafe.Shop;

namespace InnsmouthCafe.Business
{
    /// <summary>
    /// 经营时间管理器（现实时间制）。
    /// 负责管理营业时间倒计时、停止接受新顾客、提前闭门谢客。
    /// 使用 Singleton 自动创建实例，无需手动添加到场景。
    /// </summary>
    public class BusinessTimerManager : Singleton<BusinessTimerManager>
    {
        [Header("调试信息 - 只读")]
        [Tooltip("最大营业时间（秒）")]
        [SerializeField] private float _maxBusinessTime;

        [Tooltip("当前已营业时间（秒）")]
        [SerializeField] private float _currentBusinessTime;

        [Tooltip("计时器是否正在运行")]
        [SerializeField] private bool _isTimerRunning;

        [Tooltip("是否接受新顾客")]
        [SerializeField] private bool _isAcceptingNewCustomers;

        /// <summary>
        /// 最大营业时间（秒）。
        /// </summary>
        public float MaxBusinessTime => _maxBusinessTime;

        /// <summary>
        /// 当前已营业时间（秒）。
        /// </summary>
        public float CurrentBusinessTime => _currentBusinessTime;

        /// <summary>
        /// 剩余营业时间（秒）。
        /// </summary>
        public float RemainingBusinessTime => Mathf.Max(0, _maxBusinessTime - _currentBusinessTime);

        /// <summary>
        /// 剩余营业时间比例（0-1）。
        /// </summary>
        public float RemainingTimeRatio => _maxBusinessTime > 0 ? RemainingBusinessTime / _maxBusinessTime : 0;

        /// <summary>
        /// 计时器是否正在运行。
        /// </summary>
        public bool IsTimerRunning => _isTimerRunning;

        /// <summary>
        /// 是否接受新顾客。
        /// </summary>
        public bool IsAcceptingNewCustomers => _isAcceptingNewCustomers;

        /// <summary>
        /// 时间警告事件（剩余时间低于指定秒数时触发）。
        /// 参数：剩余秒数。
        /// </summary>
        public event Action<float> OnTimeWarning;

        /// <summary>
        /// 时间耗尽事件。
        /// </summary>
        public event Action OnTimeUp;

        /// <summary>
        /// 营业时间变化事件（每秒触发一次，用于UI更新）。
        /// 参数：(当前时间, 最大时间)。
        /// </summary>
        public event Action<float, float> OnTimeChanged;

        /// <summary>
        /// 是否已触发时间警告（防止重复触发）。
        /// </summary>
        private bool _hasTriggeredWarning;

        private void Update()
        {
            if (!_isTimerRunning)
            {
                return;
            }

            _currentBusinessTime += Time.deltaTime;

            // 每秒触发时间变化事件
            OnTimeChanged?.Invoke(_currentBusinessTime, _maxBusinessTime);

            // 检查时间警告（剩余2分钟）
            if (!_hasTriggeredWarning && RemainingBusinessTime <= 120f)
            {
                _hasTriggeredWarning = true;
                OnTimeWarning?.Invoke(RemainingBusinessTime);
                Debug.Log($"[BusinessTimer] 时间警告：剩余 {RemainingBusinessTime:F0} 秒");
            }

            // 检查时间耗尽
            if (_currentBusinessTime >= _maxBusinessTime)
            {
                TimeUp();
            }
        }

        /// <summary>
        /// 开始营业计时。
        /// </summary>
        /// <param name="maxTime">最大营业时间（秒）。如果为0，则从 ShopUpgradeManager 读取。</param>
        public void StartBusinessTimer(float maxTime = 0f)
        {
            // 如果没有指定时间，从店铺等级读取
            if (maxTime <= 0f && ShopUpgradeManager.Instance != null)
            {
                maxTime = ShopUpgradeManager.Instance.GetBusinessTimeSeconds();
                Debug.Log($"[BusinessTimer] 从店铺等级读取营业时间：{maxTime}秒");
            }

            // 如果仍然没有时间，使用默认值
            if (maxTime <= 0f)
            {
                maxTime = 480f; // 默认8分钟
                Debug.LogWarning("[BusinessTimer] 未找到营业时间配置，使用默认值 8 分钟");
            }

            _maxBusinessTime = maxTime;
            _currentBusinessTime = 0f;
            _isTimerRunning = true;
            _isAcceptingNewCustomers = true;
            _hasTriggeredWarning = false;

            Debug.Log($"[BusinessTimer] 开始营业，营业时间：{maxTime / 60f:F1} 分钟");
        }

        /// <summary>
        /// 停止营业计时。
        /// </summary>
        public void StopBusinessTimer()
        {
            _isTimerRunning = false;
            _isAcceptingNewCustomers = false;

            Debug.Log($"[BusinessTimer] 停止营业，实际营业时间：{_currentBusinessTime / 60f:F1} 分钟");
        }

        /// <summary>
        /// 停止接受新顾客（但不停止计时）。
        /// </summary>
        public void StopAcceptingCustomers()
        {
            _isAcceptingNewCustomers = false;

            Debug.Log("[BusinessTimer] 停止接受新顾客");
        }

        /// <summary>
        /// 提前闭门谢客（玩家主动结束营业）。
        /// </summary>
        public void EndBusinessEarly()
        {
            _isAcceptingNewCustomers = false;

            Debug.Log("[BusinessTimer] 提前闭门谢客");
        }

        /// <summary>
        /// 时间耗尽处理。
        /// </summary>
        private void TimeUp()
        {
            _isTimerRunning = false;
            _isAcceptingNewCustomers = false;

            OnTimeUp?.Invoke();

            Debug.Log("[BusinessTimer] 营业时间结束");
        }

        /// <summary>
        /// 重置计时器状态（用于测试）。
        /// </summary>
        public void ResetTimer()
        {
            _currentBusinessTime = 0f;
            _isTimerRunning = false;
            _isAcceptingNewCustomers = false;
            _hasTriggeredWarning = false;
        }
    }
}
