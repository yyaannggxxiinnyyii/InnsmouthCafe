using System;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 咖啡制作管理器（V0.2）
    /// 负责管理咖啡制作的完整流程和状态
    /// </summary>
    public class CoffeeCraftManager : Singleton<CoffeeCraftManager>
    {
        [Header("当前制作数据")]
        [SerializeField] private CoffeeData _currentCoffeeData;
        [SerializeField] private CurrentBeanBatchData _currentBatch;
        private OrderRequirementData _currentOrder;  // 移除 SerializeField，只能通过代码设置

        [Header("制作状态")]
        [SerializeField] private CraftMainState _mainState = CraftMainState.None;
        [SerializeField] private CraftModuleState _moduleState = CraftModuleState.CupSelect;

        [Header("配置参数")]
        [SerializeField] [Tooltip("每次取豆克数")]
        private float _beanPerClick = 5f;

        [SerializeField] [Tooltip("单批次最大豆量")]
        private float _maxBeanPerBatch = 20f;

        [SerializeField] [Tooltip("豆液转换比例（1g豆=Xml液）")]
        private float _beanToLiquidRatio = 5f;

        [Header("倒液速度")]
        [SerializeField] [Tooltip("慢倒速度（ml/s）")]
        private float _slowPourSpeed = 35f;

        [SerializeField] [Tooltip("快倒速度（ml/s）")]
        private float _fastPourSpeed = 90f;

        [Header("溢出惩罚")]
        [SerializeField] [Tooltip("每溢出多少ml扣理智")]
        private float _overflowPenaltyInterval = 25f;

        [SerializeField] [Tooltip("每次溢出扣除的理智值")]
        private float _overflowSanityPenalty = 0.2f;

        [Header("萃取配置")]
        [SerializeField] [Tooltip("萃取速度（ml/s）")]
        private float _extractionSpeed = 20f;

        [Header("理智值惩罚")]
        [SerializeField] [Tooltip("倒掉豆子理智惩罚")]
        private float _clearBeansSanityPenalty = 0.2f;

        [SerializeField] [Tooltip("倒掉咖啡粉理智惩罚")]
        private float _clearPowderSanityPenalty = 0.3f;

        [SerializeField] [Tooltip("倒掉整杯咖啡理智惩罚")]
        private float _clearWholeCoffeeSanityPenalty = 0.4f;

        /// 倒液状态
        private bool _isPouring = false;
        private LiquidSO _currentPouringLiquid;
        private float _accumulatedOverflow = 0f;

        /// 萃取状态
        private bool _isExtracting = false;
        private float _currentExtractionVolume = 0f;
        private float _targetExtractionVolume = 0f;
        private int _currentExtractingSegmentIndex = -1;

        /// 属性访问器
        public CoffeeData CurrentCoffeeData => _currentCoffeeData;
        public CurrentBeanBatchData CurrentBatch => _currentBatch;
        public OrderRequirementData CurrentOrder => _currentOrder;
        public CraftMainState MainState => _mainState;
        public CraftModuleState ModuleState => _moduleState;
        public bool IsExtracting => _isExtracting;
        public float ExtractionProgress => _targetExtractionVolume > 0 ? _currentExtractionVolume / _targetExtractionVolume : 0f;
        public LiquidSO CurrentPouringLiquid => _isPouring ? _currentPouringLiquid : null;

        /// 事件
        public event Action<CoffeeData> OnCoffeeDataChanged;
        public event Action<CurrentBeanBatchData> OnBatchDataChanged;
        public event Action<CraftMainState> OnMainStateChanged;
        public event Action<CraftModuleState> OnModuleStateChanged;
        public event Action OnOverflowed;
        public event Action<float, float> OnExtractionProgressChanged; // 参数：当前萃取量, 目标萃取量
        public event Action OnExtractionCompleted; // 萃取完成事件（用于播放音效等）
        public event Action OnCraftReset; // 提交后重置，UI组件可监听此事件清理自身状态

        protected override void Awake()
        {
            base.Awake();
            _currentCoffeeData = new CoffeeData();
            _currentBatch = new CurrentBeanBatchData();
        }

        /// <summary>
        /// 开始新的咖啡制作
        /// </summary>
        /// <param name="orderRequirement">订单需求数据</param>
        public void StartNewCraft(OrderRequirementData orderRequirement)
        {
            if (orderRequirement == null)
            {
                Debug.LogError("[CoffeeCraft] 订单需求数据为空，无法开始制作");
                return;
            }

            _currentCoffeeData.Clear();
            _currentBatch.Clear();
            _currentOrder = orderRequirement;
            _isPouring = false;
            _accumulatedOverflow = 0f;

            _mainState = CraftMainState.Crafting;
            _moduleState = CraftModuleState.CupSelect;

            OnMainStateChanged?.Invoke(_mainState);
            OnModuleStateChanged?.Invoke(_moduleState);

            Debug.Log($"[CoffeeCraft] 开始新制作，目标总量：{orderRequirement.targetTotalVolume}ml");
        }

        /// <summary>
        /// 选择杯子
        /// 只能在萃取前选择或更换杯子
        /// </summary>
        /// <param name="cup">杯子数据</param>
        public void SelectCup(CupContainerData cup)
        {
            if (cup == null)
            {
                Debug.LogWarning("[CoffeeCraft] 杯子数据为空");
                return;
            }

            // 检查是否有订单
            if (_currentOrder == null)
            {
                Debug.LogWarning("[CoffeeCraft] 没有订单，无法选择杯子");
                return;
            }

            if (_currentCoffeeData.coffeeSegments.Count > 0)
            {
                Debug.LogWarning("[CoffeeCraft] 已经萃取咖啡液，无法更换杯子");
                return;
            }

            _currentCoffeeData.selectedCup = cup;
            _moduleState = CraftModuleState.BeanSelect;

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放选择杯子音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeCupSelect);

            Debug.Log($"[CoffeeCraft] 选择杯子：{cup.cupName}，容量：{cup.capacity}ml");
        }

        /// <summary>
        /// 检查是否可以选择杯子
        /// </summary>
        public bool CanSelectCup()
        {
            return _currentCoffeeData.coffeeSegments.Count == 0;
        }

        /// <summary>
        /// 添加咖啡豆到当前批次
        /// 每次添加固定克数
        /// </summary>
        /// <param name="bean">豆种配置</param>
        public void AddBean(BeanSO bean)
        {
            if (bean == null)
            {
                Debug.LogWarning("[CoffeeCraft] 豆配置为空");
                return;
            }

            // 检查是否有订单
            if (_currentOrder == null)
            {
                Debug.LogWarning("[CoffeeCraft] 没有订单，无法取豆");
                return;
            }

            // 检查是否已经研磨过（研磨后不能再取豆）
            if (_currentBatch.grindType.HasValue)
            {
                Debug.LogWarning("[CoffeeCraft] 咖啡粉已研磨，不能再添加豆子。请先清空咖啡粉或萃取完成后再取豆");
                return;
            }

            if (_currentBatch.beanGram + _beanPerClick > _maxBeanPerBatch)
            {
                Debug.LogWarning($"[CoffeeCraft] 单批次豆量已达上限：{_maxBeanPerBatch}g");
                return;
            }

            // 检查是否混合不同豆种
            if (_currentBatch.bean != null && _currentBatch.bean != bean)
            {
                Debug.LogWarning($"[CoffeeCraft] 当前批次已有{_currentBatch.bean.beanName}豆，不能混合不同豆种");
                return;
            }

            _currentBatch.bean = bean;
            _currentBatch.beanGram += _beanPerClick;
            _moduleState = CraftModuleState.GrindSelect;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放取豆音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeBeanAdd);

            Debug.Log($"[CoffeeCraft] 添加{bean.beanName}豆{_beanPerClick}g，当前批次：{_currentBatch.beanGram}g");
        }

        /// <summary>
        /// 清空当前批次豆子（倒掉豆子）
        /// </summary>
        public void ClearCurrentBeans()
        {
            if (_currentBatch.beanGram == 0f)
            {
                Debug.LogWarning("[CoffeeCraft] 当前批次没有豆子");
                return;
            }

            float oldGram = _currentBatch.beanGram;
            _currentBatch.Clear();
            _moduleState = CraftModuleState.BeanSelect;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放倒掉豆子音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeBeanClear);

            // 扣除理智值
            SanityManager.Instance?.ReduceSanity(_clearBeansSanityPenalty, "浪费咖啡豆");
            Debug.Log($"[CoffeeCraft] 倒掉豆子：{oldGram}g，理智值-{_clearBeansSanityPenalty}");
        }

        /// <summary>
        /// 选择研磨程度
        /// </summary>
        /// <param name="grindType">研磨程度</param>
        public void SelectGrind(GrindType grindType)
        {
            if (_currentBatch.bean == null || _currentBatch.beanGram <= 0f)
            {
                Debug.LogWarning("[CoffeeCraft] 当前批次没有豆子，无法研磨");
                return;
            }

            _currentBatch.grindType = grindType;
            _moduleState = CraftModuleState.Extract;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放研磨音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeGrind);

            Debug.Log($"[CoffeeCraft] 选择研磨程度：{grindType}");
        }

        /// <summary>
        /// 清空当前批次研磨（倒掉咖啡粉）
        /// </summary>
        public void ClearCurrentPowder()
        {
            if (!_currentBatch.grindType.HasValue)
            {
                Debug.LogWarning("[CoffeeCraft] 当前批次未研磨");
                return;
            }

            _currentBatch.Clear();
            _moduleState = CraftModuleState.BeanSelect;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放倒掉咖啡粉音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeePowderClear);

            // 扣除理智值
            SanityManager.Instance?.ReduceSanity(_clearPowderSanityPenalty, "倒掉咖啡粉");
            Debug.Log($"[CoffeeCraft] 倒掉咖啡粉，理智值-{_clearPowderSanityPenalty}");
        }

        /// <summary>
        /// 开始萃取
        /// </summary>
        public void StartExtraction()
        {
            if (_mainState != CraftMainState.Crafting)
            {
                Debug.LogWarning("[CoffeeCraft] 当前不在制作状态，无法萃取");
                return;
            }

            if (_currentCoffeeData.selectedCup == null)
            {
                Debug.LogWarning("[CoffeeCraft] 请先选择杯子");
                return;
            }

            if (!_currentBatch.CanExtract())
            {
                Debug.LogWarning("[CoffeeCraft] 当前批次不满足萃取条件");
                return;
            }

            if (_isExtracting)
            {
                Debug.LogWarning("[CoffeeCraft] 正在萃取中");
                return;
            }

            // 计算目标萃取量
            _targetExtractionVolume = _currentBatch.beanGram * _beanToLiquidRatio;
            _currentExtractionVolume = 0f;
            _isExtracting = true;

            // 创建咖啡液段（初始volume为0）
            var segment = new CoffeeExtractSegmentData
            {
                bean = _currentBatch.bean,
                grindType = _currentBatch.grindType.Value,
                beanGram = _currentBatch.beanGram,
                extractedVolume = 0f
            };

            _currentCoffeeData.coffeeSegments.Add(segment);
            _currentExtractingSegmentIndex = _currentCoffeeData.coffeeSegments.Count - 1;

            OnExtractionProgressChanged?.Invoke(_currentExtractionVolume, _targetExtractionVolume);

            // 播放开始萃取音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeExtraction);

            Debug.Log($"[CoffeeCraft] 开始萃取，目标萃取量：{_targetExtractionVolume}ml");
        }

        /// <summary>
        /// 完成萃取（内部调用，由Update自动触发）
        /// </summary>
        private void FinishExtraction()
        {
            if (!_isExtracting)
            {
                return;
            }

            _isExtracting = false;

            // 确保最终volume精确等于目标值
            if (_currentExtractingSegmentIndex >= 0 && _currentExtractingSegmentIndex < _currentCoffeeData.coffeeSegments.Count)
            {
                var segment = _currentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex];
                segment.extractedVolume = _targetExtractionVolume;
                _currentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex] = segment;
            }

            _currentBatch.Clear();
            RefreshTotalVolume();

            _moduleState = CraftModuleState.LiquidAdd;

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);
            OnExtractionCompleted?.Invoke(); // 触发完成事件（用于播放音效）

            Debug.Log($"[CoffeeCraft] 萃取完成：{_targetExtractionVolume}ml咖啡液");

            // 重置萃取状态
            _currentExtractionVolume = 0f;
            _targetExtractionVolume = 0f;
            _currentExtractingSegmentIndex = -1;
        }

        /// <summary>
        /// 开始倒入辅助液
        /// </summary>
        /// <param name="liquid">辅助液配置</param>
        public void StartPourLiquid(LiquidSO liquid)
        {
            if (liquid == null)
            {
                Debug.LogWarning("[CoffeeCraft] 辅助液配置为空");
                return;
            }

            if (_currentCoffeeData.selectedCup == null)
            {
                Debug.LogWarning("[CoffeeCraft] 请先选择杯子");
                return;
            }

            if (_currentCoffeeData.coffeeSegments.Count == 0)
            {
                Debug.LogWarning("[CoffeeCraft] 尚未萃取，不能倒入辅助液");
                return;
            }

            _currentPouringLiquid = liquid;
            _isPouring = true;

            // 播放开始倒液音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeLiquidPourStart);

            Debug.Log($"[CoffeeCraft] 开始倒入{liquid.liquidName}");
        }

        /// <summary>
        /// 停止倒入辅助液
        /// </summary>
        public void StopPourLiquid()
        {
            if (!_isPouring)
            {
                return;
            }

            _isPouring = false;

            Debug.Log($"[CoffeeCraft] 停止倒入{_currentPouringLiquid}");
        }

        private void Update()
        {
            // 处理萃取进度
            if (_isExtracting)
            {
                float extractAmount = _extractionSpeed * Time.deltaTime;
                _currentExtractionVolume += extractAmount;

                // 更新当前萃取液段的volume
                if (_currentExtractingSegmentIndex >= 0 && _currentExtractingSegmentIndex < _currentCoffeeData.coffeeSegments.Count)
                {
                    var segment = _currentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex];
                    segment.extractedVolume = Mathf.Min(_currentExtractionVolume, _targetExtractionVolume);
                    _currentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex] = segment;

                    // 更新总容量
                    RefreshTotalVolume();

                    // 触发事件
                    OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
                    OnExtractionProgressChanged?.Invoke(_currentExtractionVolume, _targetExtractionVolume);
                }

                // 检查是否完成
                if (_currentExtractionVolume >= _targetExtractionVolume)
                {
                    FinishExtraction();
                }
            }

            // 处理倒液
            if (_isPouring)
            {
                // 检查是否有杯子
                if (_currentCoffeeData.selectedCup == null)
                {
                    Debug.LogWarning("[CoffeeCraft] 没有杯子，停止倒液");
                    StopPourLiquid();
                    return;
                }

                bool isFastPour = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float speed = isFastPour ? _fastPourSpeed : _slowPourSpeed;
                float amount = speed * Time.deltaTime;

                AddLiquidAmount(_currentPouringLiquid, amount);
            }
        }

        /// <summary>
        /// 添加辅助液量（支持合并记录）
        /// </summary>
        private void AddLiquidAmount(LiquidSO liquid, float amount)
        {
            if (liquid == null)
            {
                Debug.LogWarning("[CoffeeCraft] 辅助液配置为空");
                return;
            }

            if (_currentCoffeeData.isOverflowed)
            {
                _accumulatedOverflow += amount;

                if (_accumulatedOverflow >= _overflowPenaltyInterval)
                {
                    int penaltyCount = Mathf.FloorToInt(_accumulatedOverflow / _overflowPenaltyInterval);
                    _accumulatedOverflow -= penaltyCount * _overflowPenaltyInterval;

                    // 扣除理智值
                    SanityManager.Instance?.ReduceSanity(penaltyCount * _overflowSanityPenalty, "咖啡溢出");
                    Debug.Log($"[CoffeeCraft] 溢出惩罚：理智值-{penaltyCount * _overflowSanityPenalty}");
                }
                return;
            }

            // 同类型液段全局合并，不论添加顺序
            int existingIndex = _currentCoffeeData.liquidSegments.FindIndex(s => s.liquid == liquid);
            if (existingIndex >= 0)
            {
                var seg = _currentCoffeeData.liquidSegments[existingIndex];
                seg.amountMl += amount;
                _currentCoffeeData.liquidSegments[existingIndex] = seg;
            }
            else
            {
                _currentCoffeeData.liquidSegments.Add(new LiquidSegmentData
                {
                    liquid = liquid,
                    amountMl = amount
                });
            }

            RefreshTotalVolume();
            CheckOverflow();
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
        }

        /// <summary>
        /// 刷新总容量
        /// </summary>
        private void RefreshTotalVolume()
        {
            _currentCoffeeData.currentTotalVolume =
                _currentCoffeeData.GetTotalCoffeeVolume() +
                _currentCoffeeData.GetTotalLiquidVolume();
        }

        /// <summary>
        /// 检查溢出
        /// </summary>
        private void CheckOverflow()
        {
            if (_currentCoffeeData.selectedCup == null) return;

            if (_currentCoffeeData.currentTotalVolume > _currentCoffeeData.selectedCup.capacity)
            {
                if (!_currentCoffeeData.isOverflowed)
                {
                    _currentCoffeeData.isOverflowed = true;
                    _currentCoffeeData.currentTotalVolume = _currentCoffeeData.selectedCup.capacity;
                    OnOverflowed?.Invoke();

                    // 播放溢出音效
                    AudioManager.Instance?.PlaySfx(SoundId.CoffeeOverflow);

                    Debug.LogWarning("[CoffeeCraft] 咖啡溢出！");
                }
            }
        }

        /// <summary>
        /// 添加小料（拖拽放置，localPosition 为相对 workCupRect 的本地坐标）
        /// </summary>
        public void AddTopping(ToppingSO topping, Vector2 localPosition)
        {
            if (topping == null)
            {
                Debug.LogWarning("[CoffeeCraft] 小料配置为空");
                return;
            }

            if (_currentCoffeeData.coffeeSegments.Count == 0)
            {
                Debug.LogWarning("[CoffeeCraft] 尚未萃取，不能添加小料");
                return;
            }

            if (_currentCoffeeData.toppings.Count >= 6)
            {
                Debug.LogWarning("[CoffeeCraft] 小料已满（6个）");
                return;
            }

            _currentCoffeeData.toppings.Add(new ToppingInstanceData
            {
                topping = topping,
                localPosition = localPosition
            });

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);

            // 播放添加小料音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeToppingAdd);

            Debug.Log($"[CoffeeCraft] 添加小料：{topping.toppingName}");
        }

        /// <summary>
        /// 按索引移除指定小料（右键点击已放置图标时调用）
        /// </summary>
        public void RemoveToppingAt(int index)
        {
            if (index < 0 || index >= _currentCoffeeData.toppings.Count)
            {
                Debug.LogWarning($"[CoffeeCraft] 小料索引越界：{index}");
                return;
            }

            string name = _currentCoffeeData.toppings[index].topping?.toppingName ?? "unknown";
            _currentCoffeeData.toppings.RemoveAt(index);
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);

            AudioManager.Instance?.PlaySfx(SoundId.CoffeeToppingRemove);
            Debug.Log($"[CoffeeCraft] 移除小料：{name}");
        }

        /// <summary>
        /// 移除指定类型的最后一个小料
        /// </summary>
        public void RemoveLastToppingOfType(ToppingSO topping)
        {
            if (topping == null)
            {
                Debug.LogWarning("[CoffeeCraft] 小料配置为空");
                return;
            }

            for (int i = _currentCoffeeData.toppings.Count - 1; i >= 0; i--)
            {
                if (_currentCoffeeData.toppings[i].topping == topping)
                {
                    _currentCoffeeData.toppings.RemoveAt(i);
                    OnCoffeeDataChanged?.Invoke(_currentCoffeeData);

                    // 播放移除小料音效
                    AudioManager.Instance?.PlaySfx(SoundId.CoffeeToppingRemove);

                    Debug.Log($"[CoffeeCraft] 移除小料：{topping.toppingName}");
                    return;
                }
            }

            Debug.LogWarning($"[CoffeeCraft] 未找到小料：{topping.toppingName}");
        }

        /// <summary>
        /// 刷新小料索引（已废弃，保留空实现兼容旧调用）
        /// </summary>
        private void RefreshToppingIndices() { }

        /// <summary>
        /// 清空整杯咖啡（倒掉重做）
        /// </summary>
        public void ClearWholeCoffee()
        {
            _currentCoffeeData.Clear();
            _currentBatch.Clear();
            _isPouring = false;
            _accumulatedOverflow = 0f;

            _moduleState = CraftModuleState.CupSelect;

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // 播放倒掉整杯音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeClearWhole);

            // 扣除理智值
            SanityManager.Instance?.ReduceSanity(_clearWholeCoffeeSanityPenalty, "倒掉整杯咖啡");
            Debug.Log($"[CoffeeCraft] 倒掉整杯，理智值-{_clearWholeCoffeeSanityPenalty}");
        }

        /// <summary>
        /// 检查是否可以提交
        /// </summary>
        public bool CanSubmit()
        {
            if (_currentCoffeeData.selectedCup == null)
                return false;

            if (_currentCoffeeData.coffeeSegments.Count == 0)
                return false;

            if (_currentCoffeeData.currentTotalVolume > _currentCoffeeData.selectedCup.capacity)
                return false;

            if (_mainState != CraftMainState.Crafting)
                return false;

            return true;
        }

        /// <summary>
        /// 提交咖啡
        /// </summary>
        public CoffeeData SubmitCoffee()
        {
            if (!CanSubmit())
            {
                Debug.LogWarning("[CoffeeCraft] 当前不满足提交条件");
                return null;
            }

            // 保存提交的咖啡数据
            CoffeeData submittedCoffee = _currentCoffeeData.Clone();

            _mainState = CraftMainState.Submitted;
            OnMainStateChanged?.Invoke(_mainState);

            // 播放提交咖啡音效
            AudioManager.Instance?.PlaySfx(SoundId.CoffeeSubmit);

            Debug.Log("[CoffeeCraft] 咖啡已提交");

            // 清理所有数据，准备下一单
            ResetAfterSubmit();

            return submittedCoffee;
        }

        /// <summary>
        /// 提交后重置所有数据
        /// </summary>
        private void ResetAfterSubmit()
        {
            // 清空当前订单
            _currentOrder = null;

            // 重置咖啡数据
            _currentCoffeeData = new CoffeeData();

            // 重置批次数据
            _currentBatch = new CurrentBeanBatchData();

            // 重置状态
            _mainState = CraftMainState.None;
            _moduleState = CraftModuleState.CupSelect;

            // 通知所有监听者
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnBatchDataChanged?.Invoke(_currentBatch);
            OnMainStateChanged?.Invoke(_mainState);
            OnModuleStateChanged?.Invoke(_moduleState);
            OnCraftReset?.Invoke();

            Debug.Log("[CoffeeCraft] 已重置所有数据，等待新订单");
        }
    }
}
