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

        /// <summary>
        /// 当天所有顾客的耐心倍率。
        /// </summary>
        public float DayPatienceMultiplier => _dayPatienceMultiplier;

        /// <summary>
        /// 当天所有顾客的耐心倍率，由特殊顾客每日效果叠乘得到。
        /// </summary>
        private float _dayPatienceMultiplier = 1f;

        /// <summary>
        /// 查询当天队列中是否包含特殊顾客。
        /// </summary>
        public bool HasSpecialCustomerInTodayQueue()
        {
            foreach (CustomerSO customer in _todayQueue)
            {
                if (customer != null && customer.specialProfile != null)
                {
                    return true;
                }
            }

            return false;
        }

        [Header("耐心阶段阈值（剩余耐心比例）")]
        [SerializeField] [Tooltip("阶段1→2的剩余耐心比例（默认0.6，即剩余60%时进入阶段2）")]
        [Range(0f, 1f)] private float _stageOneThreshold = 0.6f;

        [SerializeField] [Tooltip("阶段2→3的剩余耐心比例（默认0.3，即剩余30%时进入阶段3）")]
        [Range(0f, 1f)] private float _stageTwoThreshold = 0.3f;

        // 当前耐心的计时
        private float _currentWaitTime = 0f;
        private bool _isTimerRunning = false;
        private bool _hasTriggeredAngry = false;

        // 当日索引管理
        private int _currentCustomerIndex = 0;

        /// <summary>整局游戏内已出现过的特殊客人（不会再次进入随机池）</summary>
        private readonly HashSet<CustomerSO> _seenSpecialCustomers = new HashSet<CustomerSO>();

        // 委托与事件
        public event Action<CustomerSO> OnCustomerSpawned;
        public event Action<CustomerState> OnCustomerStateChanged;
        public event Action<float> OnSanityDrop; // 每帧掉San的回调
        public event Action<CustomerSO> OnCustomerLeft;
        public event Action<float> OnPatienceChanged; // 耐心值变化（0-1，剩余比例）

        private void Update()
        {
            if (_isTimerRunning && _currentCustomer != null && _currentState == CustomerState.Waiting)
            {
                _currentWaitTime += Time.deltaTime;
                float remaining = GetRemainingPatienceRatio();

                // 广播耐心变化
                OnPatienceChanged?.Invoke(remaining);

                // 耐心耗尽进入愤怒
                if (remaining <= 0f && !_hasTriggeredAngry)
                {
                    ChangeState(CustomerState.Angry);
                }
            }

            // 愤怒状态持续掉 San
            if (_currentState == CustomerState.Angry)
            {
                OnSanityDrop?.Invoke(0.1f * Time.deltaTime);
            }
        }

        /// <summary>
        /// 生成当天的顾客队列。
        /// 规则：固定队列按顺序优先（可强制出现已见过的特殊客人），
        /// 随机普通池 + 随机特殊池各自防重复抽取后混合 Shuffle 追加。
        /// 固定队列中的客人会计入"已出现"，后续随机特殊池不会再抽到。
        /// </summary>
        public void GenerateTodayQueue(DayCustomerConfigSO config, GameMode mode)
        {
            _todayQueue.Clear();
            _currentCustomerIndex = 0;
            _dayPatienceMultiplier = 1f;

            // 1. 固定队列（保持顺序，可强制出现已见过的特殊客人）
            if (config.fixedQueue != null && config.fixedQueue.Count > 0)
            {
                _todayQueue.AddRange(config.fixedQueue);
                // 固定队列中的客人也计入"已出现"，后续随机特殊池不会再抽到
                foreach (var c in config.fixedQueue)
                {
                    if (c != null)
                        _seenSpecialCustomers.Add(c);
                }
            }

            // 2. 两个随机池各自抽取后混合 Shuffle
            var randomBatch = new List<CustomerSO>();

            if (config.randomNormalCount > 0)
                randomBatch.AddRange(DrawFromPool(config.randomNormalPool, config.randomNormalCount, "普通"));

            if (config.randomSpecialCount > 0)
                randomBatch.AddRange(DrawFromSpecialPool(config.randomSpecialPool, config.randomSpecialCount));

            ShuffleList(randomBatch);
            _todayQueue.AddRange(randomBatch);

            ApplyDayStartSpecialEffects();

            Debug.Log($"[Customer] 今日顾客共 {_todayQueue.Count} 名" +
                      $"（固定 {config.fixedQueue?.Count ?? 0}" +
                      $" + 随机普通 {config.randomNormalCount}" +
                      $" + 随机特殊 {config.randomSpecialCount}）");
        }

        /// <summary>
        /// 从特殊顾客池中抽取，自动过滤已出现过的特殊客人。
        /// 抽出后立即记录到 _seenSpecialCustomers，本局不再重复出现。
        /// 若过滤后池子不足，只取剩余可用数量并给出警告。
        /// </summary>
        private List<CustomerSO> DrawFromSpecialPool(List<CustomerSO> pool, int count)
        {
            var result = new List<CustomerSO>();
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("[Customer] 特殊顾客池为空，无法抽取。");
                return result;
            }

            // 过滤掉已出现过的特殊客人
            var available = new List<CustomerSO>();
            foreach (var c in pool)
            {
                if (c != null && !_seenSpecialCustomers.Contains(c))
                    available.Add(c);
            }

            if (available.Count == 0)
            {
                Debug.Log("[Customer] 特殊顾客池中所有客人均已出现过，本次不再抽取特殊客人。");
                return result;
            }

            int actualCount = Mathf.Min(count, available.Count);
            if (actualCount < count)
                Debug.LogWarning($"[Customer] 特殊顾客池可用数量不足（需要 {count}，剩余可用 {available.Count}），本次只抽取 {actualCount} 名。");

            for (int i = 0; i < actualCount; i++)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                var chosen = available[idx];
                result.Add(chosen);
                _seenSpecialCustomers.Add(chosen); // 立即标记，防止同一天内重复
                available.RemoveAt(idx);
            }

            return result;
        }

        /// <summary>
        /// 应用当天队列中特殊顾客配置的每日效果。
        /// </summary>
        private void ApplyDayStartSpecialEffects()
        {
            List<SpecialCustomerProfileSO> profiles = CollectTodaySpecialProfiles();
            if (profiles.Count == 0)
            {
                return;
            }

            int queueCountBeforeEffects = _todayQueue.Count;
            SpecialCustomerEffectContext context = new SpecialCustomerEffectContext(_todayQueue);

            foreach (SpecialCustomerProfileSO profile in profiles)
            {
                if (profile.dayStartEffects == null)
                {
                    continue;
                }

                foreach (SpecialCustomerEffectSO effect in profile.dayStartEffects)
                {
                    effect?.Apply(context);
                }
            }

            _dayPatienceMultiplier = context.PatienceMultiplier;

            Debug.Log($"[Customer] 特殊顾客每日效果已应用：顾客数 {queueCountBeforeEffects}->{_todayQueue.Count}，耐心倍率 {_dayPatienceMultiplier:F2}");
        }

        /// <summary>
        /// 收集当天队列中的特殊顾客配置，重复配置只应用一次。
        /// </summary>
        private List<SpecialCustomerProfileSO> CollectTodaySpecialProfiles()
        {
            List<SpecialCustomerProfileSO> profiles = new List<SpecialCustomerProfileSO>();

            foreach (CustomerSO customer in _todayQueue)
            {
                SpecialCustomerProfileSO profile = customer?.specialProfile;
                if (profile != null && !profiles.Contains(profile))
                {
                    profiles.Add(profile);
                }
            }

            return profiles;
        }

        /// <summary>
        /// 从顾客池中不重复随机抽取指定数量。
        /// 若池的数量不足，耗尽后重新放回整个池继续抽（允许重复并警告）。
        /// </summary>
        private List<CustomerSO> DrawFromPool(List<CustomerSO> pool, int count, string poolName)
        {
            var result = new List<CustomerSO>();
            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning($"[Customer] {poolName}顾客池为空，无法抽取 {count} 个顾客。");
                return result;
            }

            var available = new List<CustomerSO>(pool);
            for (int i = 0; i < count; i++)
            {
                if (available.Count == 0)
                {
                    Debug.LogWarning($"[Customer] {poolName}顾客池数量不足（需要 {count} 个，池中只有 {pool.Count} 个），已允许重复抽取。");
                    available.AddRange(pool);
                }
                int idx = UnityEngine.Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return result;
        }

        /// <summary>Fisher-Yates 洗牌</summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 重置特殊客人出现记录，新局开始时调用。
        /// </summary>
        public void ResetSeenSpecials()
        {
            _seenSpecialCustomers.Clear();
            Debug.Log("[Customer] 特殊客人出现记录已重置。");
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
                Debug.Log($"[Customer] 顾客 {_currentCustomer.customerName} 开始耐心计时，总耐心: {GetCurrentEffectivePatienceTime():F1}s");
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
        /// 根据评价档位切换对应反馈状态（0=差评/Angry，1=一般/Neutral，2=好评/Happy）
        /// </summary>
        public void SetFeedbackState(int feedbackLevel)
        {
            if (_currentState != CustomerState.Feedback) return;

            CustomerState target = feedbackLevel switch
            {
                >= 2 => CustomerState.Happy,
                1    => CustomerState.Neutral,
                _    => CustomerState.Angry
            };
            ChangeState(target);
        }

        /// <summary>
        /// 切换到开心状态（保留兼容）
        /// </summary>
        public void SetHappy()
        {
            if (_currentState == CustomerState.Feedback)
                ChangeState(CustomerState.Happy);
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
        /// 获取当前耐心百分比 (0-1)，已用时间比例
        /// </summary>
        public float GetCurrentPatienceRatio()
        {
            float effectivePatienceTime = GetCurrentEffectivePatienceTime();
            if (_currentCustomer == null || effectivePatienceTime <= 0)
                return 0f;
            return Mathf.Clamp01(_currentWaitTime / effectivePatienceTime);
        }

        /// <summary>
        /// 获取当前顾客受当天特殊效果影响后的有效耐心时间。
        /// </summary>
        public float GetCurrentEffectivePatienceTime()
        {
            if (_currentCustomer == null)
            {
                return 0f;
            }

            return _currentCustomer.basePatienceTime * _dayPatienceMultiplier;
        }

        /// <summary>
        /// 获取当前剩余耐心比例 (0-1)，1=满，0=耗尽
        /// </summary>
        public float GetRemainingPatienceRatio()
        {
            return 1f - GetCurrentPatienceRatio();
        }

        /// <summary>
        /// 获取当前耐心阶段（基于剩余耐心比例）
        /// 阶段1：剩余 > stageOneThreshold（有耐心）
        /// 阶段2：剩余 > stageTwoThreshold（有点等不及）
        /// 阶段3：剩余 > 0（不耐烦）
        /// 阶段4：耗尽（愤怒）
        /// </summary>
        public int GetCurrentPatienceStage()
        {
            float remaining = GetRemainingPatienceRatio();

            if (remaining > _stageOneThreshold)
                return 1;
            else if (remaining > _stageTwoThreshold)
                return 2;
            else if (remaining > 0f)
                return 3;
            else
                return 4;
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
