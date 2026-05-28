using System;
using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;
/// <summary>
/// 顾客系统管理器
/// 负责顾客队列生成、排队管理和单例
/// 遵循单例模式，确保场景内只有一个实例
/// </summary>
public class CustomerManager : Singleton<CustomerManager>
{
        [Header("调试信息 - 只读")]
        [SerializeField] private CustomerSO _currentCustomer;
        [SerializeField] private CustomerState _currentState;
        [SerializeField] private List<CustomerSO> _todayQueue = new List<CustomerSO>();

        public CustomerSO CurrentCustomer => _currentCustomer;
        public CustomerState CurrentState => _currentState;
        public int QueueCount => _todayQueue.Count;

        // 全局时间阶段比例 V0.1
        private const float STAGE_ONE_RATIO = 0.5f;
        private const float STAGE_TWO_RATIO = 0.8f;
        private const float STAGE_THREE_RATIO = 1.0f;

        // 当前耐心的计时
        private float _currentWaitTime = 0f;
        private bool _isTimerRunning = false;
        private bool _hasTriggeredAngry = false;

        // 当日索引管理
        private int _currentCustomerIndex = 0;

        // 委托与事件
        public event Action<CustomerSO> OnCustomerSpawned;
        public event Action<CustomerState> OnCustomerStateChanged;
        public event Action<float> OnSanityDrop; // 每帧掉San的回调
        public event Action<CustomerSO> OnCustomerLeft;

        private void Update()
        {
            if (_isTimerRunning && _currentCustomer != null && _currentState == CustomerState.Waiting)
            {
                _currentWaitTime += Time.deltaTime;
                float timeRatio = _currentWaitTime / _currentCustomer.basePatienceTime;

                // 检查是否进入不同阶段
                if (timeRatio > STAGE_THREE_RATIO && !_hasTriggeredAngry)
                {
                    ChangeState(CustomerState.Angry);
                }
                else if (timeRatio > STAGE_TWO_RATIO && timeRatio <= STAGE_THREE_RATIO)
                {
                    // 第三阶段：不耐烦，可触发 UI 警告
                    // 这里可以额外发送事件通知 UI 进行视觉警告
                }
            }

            // 愤怒状态持续掉 San
            if (_currentState == CustomerState.Angry)
            {
                OnSanityDrop?.Invoke(0.1f * Time.deltaTime);
            }
        }

        /// <summary>
        /// 生成当天的顾客队列
        /// </summary>
        public void GenerateTodayQueue(DayCustomerConfigSO config, GameMode mode)
        {
            _todayQueue.Clear();
            _currentCustomerIndex = 0;

            if (mode == GameMode.Tutorial || (config.fixedCustomers != null && config.fixedCustomers.Count > 0))
            {
                // 固定模式
                _todayQueue.AddRange(config.fixedCustomers);
            }
            else
            {
                // 随机模式生成
                if (config.normalCustomerPool != null && config.normalCustomerPool.Count > 0)
                {
                    for (int i = 0; i < config.baseCustomerCount; i++)
                    {
                        int randIdx = UnityEngine.Random.Range(0, config.normalCustomerPool.Count);
                        _todayQueue.Add(config.normalCustomerPool[randIdx]);
                    }
                }

                // 统一追加特殊顾客
                if (config.extraCustomers != null && config.extraCustomers.Count > 0)
                {
                    _todayQueue.AddRange(config.extraCustomers);
                }
            }

            Debug.Log($"[Customer] 当天包含 {_todayQueue.Count} 名顾客.");
        }

        /// <summary>
        /// 生成（召唤）下一位顾客
        /// </summary>
        public bool SpawnNextCustomer()
        {
            if (_currentCustomerIndex >= _todayQueue.Count)
            {
                Debug.Log("[Customer] 队列已空，没有后续顾客");
                return false; // 队列处理完成
            }

            _currentCustomer = _todayQueue[_currentCustomerIndex];
            _currentCustomerIndex++;

            _currentWaitTime = 0f;
            _isTimerRunning = false;
            _hasTriggeredAngry = false;

            ChangeState(CustomerState.Entering);
            OnCustomerSpawned?.Invoke(_currentCustomer);
            TutorialEventBus.Publish("CustomerEnter");
            return true;
        }

        private void ChangeState(CustomerState newState)
        {
            _currentState = newState;
            OnCustomerStateChanged?.Invoke(_currentState);

            if (newState == CustomerState.Angry)
            {
                _hasTriggeredAngry = true;
                Debug.Log($"[Customer] {_currentCustomer.customerName} 已超时，进入愤怒状态并开始掉San！");
            }
        }

        #region 生命周期流程
        /// <summary>
        /// 开始对话阶段（播放开场白）
        /// 对话期间不消耗耐心时间
        /// </summary>
        public void StartTalking()
        {
            if (_currentState == CustomerState.Entering)
            {
                ChangeState(CustomerState.Talking);
                Debug.Log($"[Customer] {_currentCustomer.customerName} 开始对话");
            }
        }

        /// <summary>
        /// 开始等待阶段（订单小票显示后）
        /// 从此刻开始消耗耐心时间
        /// </summary>
        public void StartWaiting()
        {
            if (_currentState == CustomerState.Talking)
            {
                ChangeState(CustomerState.Waiting);
                _isTimerRunning = true;
                Debug.Log($"[Customer] 顾客 {_currentCustomer.customerName} 开始耐心计时，总耐心: {_currentCustomer.basePatienceTime}s");
            }
        }

        /// <summary>
        /// 完成订单并准备显示反馈
        /// </summary>
        /// <param name="scoreLevel">咖啡评分档位 (0=不满意, 1=一般, 2=满意)</param>
        public int FinishOrderAndShowFeedback(int scoreLevel)
        {
            // 提交了咖啡，停止耐心
            _isTimerRunning = false;

            // Angry限制：如果曾经陷入过Angry，评价最高只能为一般(评分档位映射暂由其他服务解决，此方法假设为0-2)
            // 分别为 0=不满意(怒), 1=一般(正常), 2=满意(好)
            int finalFeedbackLevel = scoreLevel;
            if (_hasTriggeredAngry)
            {
                finalFeedbackLevel = Mathf.Min(scoreLevel, 1); // 限制最高由于愤怒降到1（一般）
            }

            ChangeState(CustomerState.Feedback);

            // 下方可以通过事件发送结果给 UI...或者由 GameManager 调用获取文本
            Debug.Log($"[Customer] 顾客评价档位结算完成，曾发怒状态: {_hasTriggeredAngry}，最终档位: {finalFeedbackLevel}");
            return finalFeedbackLevel;
        }

        /// <summary>
        /// 切换到开心状态
        /// </summary>
        public void SetHappy()
        {
            if (_currentState == CustomerState.Feedback)
            {
                ChangeState(CustomerState.Happy);
            }
        }

        /// <summary>
        /// 顾客离开
        /// </summary>
        public void Leave()
        {
            ChangeState(CustomerState.Leaving);
            OnCustomerLeft?.Invoke(_currentCustomer);
            TutorialEventBus.Publish("CustomerLeave");
            _currentCustomer = null;
            Debug.Log("[Customer] 顾客已离开");
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 获取当前耐心百分比 (0-1)
        /// </summary>
        public float GetCurrentPatienceRatio()
        {
            if (_currentCustomer == null || _currentCustomer.basePatienceTime <= 0)
                return 0f;

            return Mathf.Clamp01(_currentWaitTime / _currentCustomer.basePatienceTime);
        }

        /// <summary>
        /// 获取当前耐心阶段
        /// </summary>
        public int GetCurrentPatienceStage()
        {
            float ratio = GetCurrentPatienceRatio();

            if (ratio <= STAGE_ONE_RATIO)
                return 1;
            else if (ratio <= STAGE_TWO_RATIO)
                return 2;
            else if (ratio <= STAGE_THREE_RATIO)
                return 3;
            else
                return 4; // Angry
        }

        /// <summary>
        /// 获取当前剩余耐心时间
        /// </summary>
        public float GetRemainingPatienceTime()
        {
            if (_currentCustomer == null)
                return 0f;

            return Mathf.Max(0, _currentCustomer.basePatienceTime - _currentWaitTime);
        }

        /// <summary>
        /// 是否已经进入愤怒状态
        /// </summary>
        public bool HasTriggeredAngry => _hasTriggeredAngry;

        /// <summary>
        /// 获取随机进店对话
        /// </summary>
        public string GetRandomEnterDialogue()
        {
            if (_currentCustomer == null || _currentCustomer.enterDialogueTexts.Count == 0)
                return "欢迎光临！";

            int randomIndex = UnityEngine.Random.Range(0, _currentCustomer.enterDialogueTexts.Count);
            return _currentCustomer.enterDialogueTexts[randomIndex];
        }

        /// <summary>
        /// 获取随机评价文本
        /// </summary>
        public string GetRandomFeedbackText(int feedbackLevel)
        {
            if (_currentCustomer == null)
                return "谢谢！";

            List<string> feedbackList = feedbackLevel switch
            {
                0 => _currentCustomer.dissatisfiedFeedbackTexts,
                1 => _currentCustomer.neutralFeedbackTexts,
                2 => _currentCustomer.satisfiedFeedbackTexts,
                _ => _currentCustomer.neutralFeedbackTexts
            };

            if (feedbackList == null || feedbackList.Count == 0)
                return "谢谢！";

            int randomIndex = UnityEngine.Random.Range(0, feedbackList.Count);
            return feedbackList[randomIndex];
        }

        /// <summary>
        /// 获取随机特殊评价文本
        /// </summary>
        public string GetRandomSpecialFeedbackText()
        {
            if (_currentCustomer == null || _currentCustomer.SpecialFeedbackTexts == null
                || _currentCustomer.SpecialFeedbackTexts.Count == 0)
                return null;

            int randomIndex = UnityEngine.Random.Range(0, _currentCustomer.SpecialFeedbackTexts.Count);
            return _currentCustomer.SpecialFeedbackTexts[randomIndex];
        }
        #endregion
    }
