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
        [SerializeField] private OrderRequirementData _currentOrder;

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

        [Header("理智值惩罚")]
        [SerializeField] [Tooltip("倒掉豆子理智惩罚")]
        private float _clearBeansSanityPenalty = 0.2f;

        [SerializeField] [Tooltip("倒掉咖啡粉理智惩罚")]
        private float _clearPowderSanityPenalty = 0.3f;

        [SerializeField] [Tooltip("倒掉整杯咖啡理智惩罚")]
        private float _clearWholeCoffeeSanityPenalty = 0.4f;

        /// 倒液状态
        private bool _isPouring = false;
        private LiquidType _currentPouringLiquid;
        private float _accumulatedOverflow = 0f;

        /// 属性访问器
        public CoffeeData CurrentCoffeeData => _currentCoffeeData;
        public CurrentBeanBatchData CurrentBatch => _currentBatch;
        public OrderRequirementData CurrentOrder => _currentOrder;
        public CraftMainState MainState => _mainState;
        public CraftModuleState ModuleState => _moduleState;

        /// 事件
        public event Action<CoffeeData> OnCoffeeDataChanged;
        public event Action<CurrentBeanBatchData> OnBatchDataChanged;
        public event Action<CraftMainState> OnMainStateChanged;
        public event Action<CraftModuleState> OnModuleStateChanged;
        public event Action OnOverflowed;

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

            if (_currentCoffeeData.coffeeSegments.Count > 0)
            {
                Debug.LogWarning("[CoffeeCraft] 已经萃取咖啡液，无法更换杯子");
                return;
            }

            _currentCoffeeData.selectedCup = cup;
            _moduleState = CraftModuleState.BeanSelect;

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnModuleStateChanged?.Invoke(_moduleState);

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
        /// <param name="beanType">豆种类型</param>
        public void AddBean(BeanType beanType)
        {
            if (_currentBatch.beanGram + _beanPerClick > _maxBeanPerBatch)
            {
                Debug.LogWarning($"[CoffeeCraft] 单批次豆量已达上限：{_maxBeanPerBatch}g");
                return;
            }

            // 检查是否混合不同豆种
            if (_currentBatch.beanType.HasValue && _currentBatch.beanType.Value != beanType)
            {
                Debug.LogWarning($"[CoffeeCraft] 当前批次已有{_currentBatch.beanType.Value}豆，不能混合不同豆种");
                return;
            }

            _currentBatch.beanType = beanType;
            _currentBatch.beanGram += _beanPerClick;
            _moduleState = CraftModuleState.GrindSelect;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            Debug.Log($"[CoffeeCraft] 添加{beanType}豆{_beanPerClick}g，当前批次：{_currentBatch.beanGram}g");
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

            // TODO: 扣除理智值 -0.2
            Debug.Log($"[CoffeeCraft] 倒掉豆子：{oldGram}g，理智值-{_clearBeansSanityPenalty}");
        }

        /// <summary>
        /// 选择研磨程度
        /// </summary>
        /// <param name="grindType">研磨程度</param>
        public void SelectGrind(GrindType grindType)
        {
            if (!_currentBatch.beanType.HasValue || _currentBatch.beanGram <= 0f)
            {
                Debug.LogWarning("[CoffeeCraft] 当前批次没有豆子，无法研磨");
                return;
            }

            _currentBatch.grindType = grindType;
            _moduleState = CraftModuleState.Extract;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

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

            _currentBatch.grindType = null;
            _moduleState = CraftModuleState.GrindSelect;

            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            // TODO: 扣除理智值 -0.3
            Debug.Log($"[CoffeeCraft] 倒掉咖啡粉，理智值-{_clearPowderSanityPenalty}");
        }

        /// <summary>
        /// 开始萃取
        /// </summary>
        public void StartExtraction()
        {
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

            // TODO: 播放萃取动画，动画结束后调用 FinishExtraction()
            Debug.Log("[CoffeeCraft] 开始萃取...");
        }

        /// <summary>
        /// 完成萃取
        /// </summary>
        public void FinishExtraction()
        {
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

            float extractedVolume = _currentBatch.beanGram * _beanToLiquidRatio;

            var segment = new CoffeeExtractSegmentData
            {
                beanType = _currentBatch.beanType.Value,
                grindType = _currentBatch.grindType.Value,
                beanGram = _currentBatch.beanGram,
                extractedVolume = extractedVolume
            };

            _currentCoffeeData.coffeeSegments.Add(segment);
            _currentBatch.Clear();
            RefreshTotalVolume();

            _moduleState = CraftModuleState.LiquidAdd;

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnBatchDataChanged?.Invoke(_currentBatch);
            OnModuleStateChanged?.Invoke(_moduleState);

            Debug.Log($"[CoffeeCraft] 萃取完成：{extractedVolume}ml咖啡液");
        }

        /// <summary>
        /// 开始倒入辅助液
        /// </summary>
        /// <param name="liquidType">辅助液类型</param>
        public void StartPourLiquid(LiquidType liquidType)
        {
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

            _currentPouringLiquid = liquidType;
            _isPouring = true;

            Debug.Log($"[CoffeeCraft] 开始倒入{liquidType}");
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
        private void AddLiquidAmount(LiquidType liquidType, float amount)
        {
            if (_currentCoffeeData.isOverflowed)
            {
                _accumulatedOverflow += amount;

                if (_accumulatedOverflow >= _overflowPenaltyInterval)
                {
                    int penaltyCount = Mathf.FloorToInt(_accumulatedOverflow / _overflowPenaltyInterval);
                    _accumulatedOverflow -= penaltyCount * _overflowPenaltyInterval;

                    // TODO: 扣除理智值
                    Debug.Log($"[CoffeeCraft] 溢出惩罚：理智值-{penaltyCount * _overflowSanityPenalty}");
                }
                return;
            }

            var lastSegment = _currentCoffeeData.liquidSegments.Count > 0
                ? _currentCoffeeData.liquidSegments[_currentCoffeeData.liquidSegments.Count - 1]
                : null;

            if (lastSegment != null && lastSegment.liquidType == liquidType)
            {
                lastSegment.amountMl += amount;
            }
            else
            {
                _currentCoffeeData.liquidSegments.Add(new LiquidSegmentData
                {
                    liquidType = liquidType,
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
                    Debug.LogWarning("[CoffeeCraft] 咖啡溢出！");
                }
            }
        }

        /// <summary>
        /// 添加小料
        /// </summary>
        public void AddTopping(ToppingType toppingType)
        {
            if (_currentCoffeeData.coffeeSegments.Count == 0)
            {
                Debug.LogWarning("[CoffeeCraft] 尚未萃取，不能添加小料");
                return;
            }

            if (_currentCoffeeData.toppings.Count >= 20)
            {
                Debug.LogWarning("[CoffeeCraft] 小料锚点已满（20个）");
                return;
            }

            _currentCoffeeData.toppings.Add(new ToppingInstanceData
            {
                toppingType = toppingType,
                orderIndex = _currentCoffeeData.toppings.Count
            });

            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            Debug.Log($"[CoffeeCraft] 添加小料：{toppingType}");
        }

        /// <summary>
        /// 移除指定类型的最后一个小料
        /// </summary>
        public void RemoveLastToppingOfType(ToppingType toppingType)
        {
            for (int i = _currentCoffeeData.toppings.Count - 1; i >= 0; i--)
            {
                if (_currentCoffeeData.toppings[i].toppingType == toppingType)
                {
                    _currentCoffeeData.toppings.RemoveAt(i);
                    RefreshToppingIndices();
                    OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
                    Debug.Log($"[CoffeeCraft] 移除小料：{toppingType}");
                    return;
                }
            }

            Debug.LogWarning($"[CoffeeCraft] 未找到小料：{toppingType}");
        }

        /// <summary>
        /// 刷新小料索引（移除后自动前移补位）
        /// </summary>
        private void RefreshToppingIndices()
        {
            for (int i = 0; i < _currentCoffeeData.toppings.Count; i++)
            {
                _currentCoffeeData.toppings[i].orderIndex = i;
            }
        }

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

            // TODO: 扣除理智值 -0.4
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

            _mainState = CraftMainState.Submitted;
            OnMainStateChanged?.Invoke(_mainState);

            Debug.Log("[CoffeeCraft] 咖啡已提交");
            return _currentCoffeeData;
        }
    }
}
