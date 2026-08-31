using System;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 新咖啡制作管理器。
    /// 负责维护场景制作流程中的当前杯子和制作阶段。
    /// </summary>
    public class NewCoffeeCraftManager : Singleton<NewCoffeeCraftManager>
    {
        [Header("当前制作数据")]
        [Tooltip("当前选择的杯子")]
        [SerializeField] private CupContainerData _selectedCup;

        [Tooltip("当前正在制作的咖啡成品数据")]
        [SerializeField] private CoffeeData _currentCoffeeData = new CoffeeData();

        [Tooltip("当前制作阶段")]
        [SerializeField] private NewCoffeeCraftStage _currentStage = NewCoffeeCraftStage.SelectCup;

        [Tooltip("当前正在制作的顾客订单")]
        [SerializeField] private OrderSO _currentOrder;

        [Header("咖啡豆批次")]
        [Tooltip("当前手持豆勺中的咖啡豆")]
        [SerializeField] private BeanSO _heldBean;

        [Tooltip("当前手持豆勺中的咖啡豆重量（克）")]
        [SerializeField] private float _heldBeanGrams;

        [Tooltip("已倒入研磨机、等待研磨或萃取的咖啡豆批次")]
        [SerializeField] private CurrentBeanBatchData _grinderBeanBatch = new CurrentBeanBatchData();

        [Header("取豆规则")]
        [Tooltip("每次从豆桶取出的咖啡豆重量（克）")]
        [SerializeField] private float _beanGramsPerScoop = 5f;

        [Tooltip("单次可手持的最大咖啡豆重量（克）")]
        [SerializeField] private float _maxHeldBeanGrams = 20f;

        [Header("萃取配置")]
        [Tooltip("每克咖啡粉可萃取的咖啡液体积（毫升）")]
        [SerializeField] private float _extractionMlPerBeanGram = 5f;

        [Tooltip("咖啡液萃取速度（毫升/秒）")]
        [SerializeField] private float _extractionSpeed = 20f;

        [Header("萃取运行时")]
        [Tooltip("当前是否正在萃取")]
        [SerializeField] private bool _isExtracting;

        [Tooltip("本次萃取当前已产出的咖啡液体积（毫升）")]
        [SerializeField] private float _currentExtractionVolume;

        [Tooltip("本次萃取目标咖啡液体积（毫升）")]
        [SerializeField] private float _targetExtractionVolume;

        [Tooltip("当前正在更新的咖啡液段索引")]
        [SerializeField] private int _currentExtractingSegmentIndex = -1;

        /// <summary>
        /// 当前选择的杯子。
        /// </summary>
        public CupContainerData SelectedCup => _selectedCup;

        /// <summary>
        /// 当前正在制作的咖啡成品数据。
        /// </summary>
        public CoffeeData CurrentCoffeeData
        {
            get
            {
                EnsureCoffeeData();
                return _currentCoffeeData;
            }
        }

        /// <summary>
        /// 当前制作阶段。
        /// </summary>
        public NewCoffeeCraftStage CurrentStage => _currentStage;

        /// <summary>
        /// 当前正在制作的订单。
        /// </summary>
        public OrderSO CurrentOrder => _currentOrder;

        /// <summary>
        /// 当前是否存在已接受订单对应的有效制作会话。
        /// </summary>
        public bool IsCraftSessionActive => _currentOrder != null;

        /// <summary>
        /// 当前是否正在萃取咖啡液。
        /// </summary>
        public bool IsExtracting => _isExtracting;

        /// <summary>
        /// 当前萃取的完成进度，范围为 0 到 1。
        /// </summary>
        public float ExtractionProgress => _targetExtractionVolume > 0f
            ? _currentExtractionVolume / _targetExtractionVolume
            : 0f;

        /// <summary>
        /// 当前手持豆勺中的咖啡豆配置。
        /// </summary>
        public BeanSO HeldBean => _heldBean;

        /// <summary>
        /// 当前手持豆勺中的咖啡豆重量。
        /// </summary>
        public float HeldBeanGrams => _heldBeanGrams;

        /// <summary>
        /// 当前手持豆勺允许容纳的最大豆量。
        /// </summary>
        public float MaxHeldBeanGrams => _maxHeldBeanGrams;

        /// <summary>
        /// 当前研磨机内的豆子批次。
        /// </summary>
        public CurrentBeanBatchData GrinderBeanBatch
        {
            get
            {
                EnsureGrinderBeanBatch();
                return _grinderBeanBatch;
            }
        }

        /// <summary>
        /// 当前是否有正在手持、尚未倒入研磨机的咖啡豆。
        /// </summary>
        public bool HasHeldBeans => _heldBean != null && _heldBeanGrams > 0f;

        /// <summary>
        /// 当前杯子中是否已实际添加任何咖啡液或辅助液。
        /// </summary>
        public bool HasCupContents => CurrentCoffeeData.currentTotalVolume > 0f;

        /// <summary>
        /// 当前研磨机内是否已有一批咖啡豆。
        /// </summary>
        public bool HasGrinderBeans => GrinderBeanBatch.bean != null && GrinderBeanBatch.beanGram > 0f;

        /// <summary>
        /// 杯子选择完成事件。
        /// </summary>
        public event Action<CupContainerData> OnCupSelected;

        /// <summary>
        /// 当前订单开始制作事件。
        /// </summary>
        public event Action<OrderSO> OnOrderStarted;

        /// <summary>
        /// 制作流程重置事件。
        /// </summary>
        public event Action OnCraftReset;

        /// <summary>
        /// 手持豆勺内容变化事件，参数为豆种和当前重量。
        /// </summary>
        public event Action<BeanSO, float> OnHeldBeansChanged;

        /// <summary>
        /// 研磨机内豆子批次变化事件。
        /// </summary>
        public event Action<CurrentBeanBatchData> OnGrinderBeanBatchChanged;

        /// <summary>
        /// 当前咖啡成品数据变化事件。
        /// </summary>
        public event Action<CoffeeData> OnCoffeeDataChanged;

        /// <summary>
        /// 萃取状态变化事件，参数为是否正在萃取。
        /// </summary>
        public event Action<bool> OnExtractionStateChanged;

        /// <summary>
        /// 萃取进度变化事件，参数为当前体积和目标体积。
        /// </summary>
        public event Action<float, float> OnExtractionProgressChanged;

        /// <summary>
        /// 萃取完成事件。
        /// </summary>
        public event Action OnExtractionCompleted;

        /// <summary>
        /// 萃取启动时没有杯子承接而导致咖啡粉作废的事件。
        /// </summary>
        public event Action OnExtractionWasted;

        private void Update()
        {
            if (!_isExtracting)
            {
                return;
            }

            UpdateExtraction();
        }

        /// <summary>
        /// 开始制作指定订单并重置制作阶段。
        /// </summary>
        public void BeginOrder(OrderSO order)
        {
            if (order == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 订单为空，无法开始制作");
                return;
            }

            ResetCraft();
            _currentOrder = order;
            OnOrderStarted?.Invoke(order);
            Debug.Log($"[NewCoffeeCraft] 开始制作订单：{order.orderName}");
        }

        /// <summary>
        /// 判断当前是否允许选择杯子。
        /// </summary>
        /// <returns>处于选杯阶段且尚未选择杯子时返回 true。</returns>
        public bool CanSelectCup()
        {
            return IsCraftSessionActive
                && !_isExtracting
                && !HasCupContents;
        }

        /// <summary>
        /// 判断当前制作流程是否允许打开辅助液添加面板。
        /// </summary>
        public bool CanAddLiquid()
        {
            return IsCraftSessionActive
                && _currentStage == NewCoffeeCraftStage.AddLiquid
                && HasSelectedCup()
                && !_isExtracting;
        }

        /// <summary>
        /// 将一次真实倒入的辅助液容量写入当前咖啡数据。
        /// </summary>
        /// <param name="liquid">本次倒入的辅助液配置。</param>
        /// <param name="amountMl">本次实际倒入的毫升数。</param>
        /// <returns>写入成功返回 true。</returns>
        public bool TryCommitLiquid(LiquidSO liquid, float amountMl)
        {
            if (!CanAddLiquid() || liquid == null || amountMl <= 0f)
            {
                return false;
            }

            EnsureCoffeeData();
            int existingIndex = _currentCoffeeData.liquidSegments.FindIndex(segment => segment.liquid == liquid);
            if (existingIndex >= 0)
            {
                LiquidSegmentData segment = _currentCoffeeData.liquidSegments[existingIndex];
                segment.amountMl += amountMl;
                _currentCoffeeData.liquidSegments[existingIndex] = segment;
            }
            else
            {
                _currentCoffeeData.liquidSegments.Add(new LiquidSegmentData
                {
                    liquid = liquid,
                    amountMl = amountMl
                });
            }

            RefreshCoffeeVolume();
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            Debug.Log($"[NewCoffeeCraft] 添加辅助液：{liquid.liquidName} {amountMl:F1}ml");
            return true;
        }

        /// <summary>
        /// 判断当前是否存在有效的杯子数据。
        /// </summary>
        private bool HasSelectedCup()
        {
            return _selectedCup != null
                && (!string.IsNullOrWhiteSpace(_selectedCup.cupId) || _selectedCup.capacity > 0f);
        }

        /// <summary>
        /// 选择场景中的杯子。
        /// </summary>
        /// <param name="cup">杯子配置。</param>
        /// <returns>选择成功返回 true。</returns>
        public bool TrySelectCup(CupContainerSO cup)
        {
            if (cup == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 杯子配置为空，无法选择");
                return false;
            }

            if (!CanSelectCup())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前没有已接受的订单或制作阶段不允许选择杯子");
                return false;
            }

            bool isFirstCupSelection = !HasSelectedCup();
            _selectedCup = cup.ToData();
            EnsureCoffeeData();
            _currentCoffeeData.selectedCup = _selectedCup;
            if (isFirstCupSelection)
            {
                _currentStage = NewCoffeeCraftStage.GrindBeans;
            }

            OnCupSelected?.Invoke(_selectedCup);
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);

            Debug.Log($"[NewCoffeeCraft] 选择杯子：{_selectedCup.cupName}（{_selectedCup.capacity}ml）");
            return true;
        }

        /// <summary>
        /// 判断当前是否允许从场景豆桶取豆。
        /// </summary>
        public bool CanTakeBeans()
        {
            return IsCraftSessionActive
                && IsBeanSelectionStage()
                && !HasGrinderBeans;
        }

        /// <summary>
        /// 从指定豆桶向手持豆勺加入一次固定重量的咖啡豆。
        /// </summary>
        /// <param name="bean">要取用的咖啡豆配置。</param>
        /// <returns>取豆成功返回 true。</returns>
        public bool TryTakeBeans(BeanSO bean)
        {
            if (bean == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 豆子配置为空，无法取豆");
                return false;
            }

            if (!CanTakeBeans())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前阶段或研磨机状态不允许取豆");
                return false;
            }

            if (HasHeldBeans && _heldBean != bean)
            {
                Debug.LogWarning("[NewCoffeeCraft] 手持豆勺中已有其他豆种，不能混合取豆");
                return false;
            }

            float scoopAmount = Mathf.Max(0.01f, _beanGramsPerScoop);
            float maxAmount = Mathf.Max(scoopAmount, _maxHeldBeanGrams);
            if (_heldBeanGrams + scoopAmount > maxAmount + Mathf.Epsilon)
            {
                Debug.LogWarning("[NewCoffeeCraft] 手持豆勺已达到豆量上限");
                return false;
            }

            _heldBean = bean;
            _heldBeanGrams += scoopAmount;
            OnHeldBeansChanged?.Invoke(_heldBean, _heldBeanGrams);

            Debug.Log($"[NewCoffeeCraft] 取豆：{bean.beanName} {scoopAmount:F0}g，手持 {_heldBeanGrams:F0}g");
            return true;
        }

        /// <summary>
        /// 判断当前是否允许将手持豆勺中的豆子倒入研磨机。
        /// </summary>
        public bool CanLoadHeldBeansIntoGrinder()
        {
            return IsCraftSessionActive
                && IsBeanSelectionStage()
                && HasHeldBeans
                && !HasGrinderBeans;
        }

        /// <summary>
        /// 将当前手持豆勺中的全部豆子倒入研磨机。
        /// </summary>
        /// <returns>倒入成功返回 true。</returns>
        public bool TryLoadHeldBeansIntoGrinder()
        {
            if (!CanLoadHeldBeansIntoGrinder())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前没有可倒入研磨机的手持豆子");
                return false;
            }

            EnsureGrinderBeanBatch();
            _grinderBeanBatch.bean = _heldBean;
            _grinderBeanBatch.beanGram = _heldBeanGrams;
            _grinderBeanBatch.grindType = null;

            string beanName = _heldBean.beanName;
            float beanGrams = _heldBeanGrams;
            ClearHeldBeans();
            _currentStage = NewCoffeeCraftStage.GrindBeans;
            OnGrinderBeanBatchChanged?.Invoke(_grinderBeanBatch);

            Debug.Log($"[NewCoffeeCraft] 豆子已倒入研磨机：{beanName} {beanGrams:F0}g");
            return true;
        }

        /// <summary>
        /// 判断当前是否允许继续操作研磨机手柄。
        /// </summary>
        public bool CanGrindBeans()
        {
            if (_currentStage != NewCoffeeCraftStage.GrindBeans
                && _currentStage != NewCoffeeCraftStage.ExtractCoffee)
            {
                return false;
            }

            return IsCraftSessionActive
                && HasGrinderBeans
                && !HasHeldBeans
                && GrinderBeanBatch.grindType != GrindType.ExtraFine;
        }

        /// <summary>
        /// 操作一次研磨机手柄，并将研磨度提升一级。
        /// </summary>
        /// <returns>研磨成功返回 true。</returns>
        public bool TryGrindBeans()
        {
            if (!CanGrindBeans())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前没有可继续研磨的豆子");
                return false;
            }

            EnsureGrinderBeanBatch();
            _grinderBeanBatch.grindType = GetNextGrindType(_grinderBeanBatch.grindType);
            _currentStage = NewCoffeeCraftStage.ExtractCoffee;
            OnGrinderBeanBatchChanged?.Invoke(_grinderBeanBatch);

            Debug.Log($"[NewCoffeeCraft] 研磨完成：{_grinderBeanBatch.bean.beanName}，{GetGrindTypeName(_grinderBeanBatch.grindType.Value)}");
            return true;
        }

        /// <summary>
        /// 尝试启动萃取。未将工作杯放入萃取位时，本批咖啡粉会直接作废。
        /// </summary>
        /// <param name="hasCupAtExtractor">启动时工作杯是否已放入萃取机承接位。</param>
        /// <returns>成功开始萃取或成功结算作废时返回 true。</returns>
        public bool TryStartExtraction(bool hasCupAtExtractor)
        {
            if (!CanStartExtraction())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前没有可萃取的咖啡粉");
                return false;
            }

            if (!hasCupAtExtractor)
            {
                WasteCurrentGrinderBatch();
                return true;
            }

            EnsureCoffeeData();
            _targetExtractionVolume = _grinderBeanBatch.beanGram
                * Mathf.Max(0f, _extractionMlPerBeanGram);
            _currentExtractionVolume = 0f;
            _isExtracting = true;

            CoffeeExtractSegmentData segment = new CoffeeExtractSegmentData
            {
                bean = _grinderBeanBatch.bean,
                grindType = _grinderBeanBatch.grindType.Value,
                beanGram = _grinderBeanBatch.beanGram,
                extractedVolume = 0f
            };
            _currentCoffeeData.coffeeSegments.Add(segment);
            _currentExtractingSegmentIndex = _currentCoffeeData.coffeeSegments.Count - 1;

            OnExtractionStateChanged?.Invoke(true);
            OnExtractionProgressChanged?.Invoke(_currentExtractionVolume, _targetExtractionVolume);
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);

            Debug.Log($"[NewCoffeeCraft] 开始萃取：{segment.bean.beanName}，目标 {_targetExtractionVolume:F0}ml");
            return true;
        }

        /// <summary>
        /// 判断当前是否满足启动萃取机的制作数据条件。
        /// </summary>
        public bool CanStartExtraction()
        {
            return IsCraftSessionActive
                && _currentStage == NewCoffeeCraftStage.ExtractCoffee
                && HasGrinderBeans
                && GrinderBeanBatch.CanExtract()
                && !_isExtracting;
        }

        /// <summary>
        /// 倒掉当前手持豆勺中的豆子，已倒入研磨机的豆子不会受到影响。
        /// </summary>
        /// <returns>存在并成功倒掉手持豆子时返回 true。</returns>
        public bool TryDiscardHeldBeans()
        {
            if (!IsCraftSessionActive || _isExtracting || !HasHeldBeans)
            {
                return false;
            }

            string beanName = _heldBean.beanName;
            float beanGrams = _heldBeanGrams;
            ClearHeldBeans();
            Debug.Log($"[NewCoffeeCraft] 倒掉手持豆子：{beanName} {beanGrams:F0}g");
            return true;
        }

        /// <summary>
        /// 倒掉当前整杯咖啡，清空杯子、豆子和咖啡数据后重新进入选杯阶段。
        /// </summary>
        /// <returns>存在可倒掉的当前杯子时返回 true。</returns>
        public bool TryDiscardCurrentCup()
        {
            if (!IsCraftSessionActive || !HasSelectedCup() || _isExtracting)
            {
                return false;
            }

            Debug.Log($"[NewCoffeeCraft] 倒掉当前咖啡：{_selectedCup.cupName}");
            ResetCraft();
            return true;
        }

        /// <summary>
        /// 重置当前制作流程，回到选杯阶段。
        /// </summary>
        public void ResetCraft()
        {
            _selectedCup = null;
            _currentStage = NewCoffeeCraftStage.SelectCup;
            EnsureCoffeeData();
            _currentCoffeeData.Clear();
            EnsureGrinderBeanBatch();
            ClearHeldBeans();
            _grinderBeanBatch.Clear();
            ResetExtractionRuntime();
            OnCoffeeDataChanged?.Invoke(_currentCoffeeData);
            OnGrinderBeanBatchChanged?.Invoke(_grinderBeanBatch);
            OnCraftReset?.Invoke();
        }

        /// <summary>
        /// 清空制作流程和当前订单，锁定设备并等待顾客接受下一张订单。
        /// </summary>
        public void ResetForBusiness()
        {
            _currentOrder = null;
            ResetCraft();
            Debug.Log("[NewCoffeeCraft] 制作流程已重置，等待接受订单");
        }

        /// <summary>
        /// 确保研磨机批次数据始终可用。
        /// </summary>
        private void EnsureGrinderBeanBatch()
        {
            if (_grinderBeanBatch == null)
            {
                _grinderBeanBatch = new CurrentBeanBatchData();
            }
        }

        /// <summary>
        /// 判断当前是否处于允许再次取豆的制作阶段。
        /// </summary>
        private bool IsBeanSelectionStage()
        {
            return _currentStage == NewCoffeeCraftStage.GrindBeans
                || _currentStage == NewCoffeeCraftStage.AddLiquid;
        }

        /// <summary>
        /// 确保咖啡成品数据始终可用。
        /// </summary>
        private void EnsureCoffeeData()
        {
            if (_currentCoffeeData == null)
            {
                _currentCoffeeData = new CoffeeData();
            }
        }

        /// <summary>
        /// 清空当前手持豆勺数据并通知显示层。
        /// </summary>
        private void ClearHeldBeans()
        {
            _heldBean = null;
            _heldBeanGrams = 0f;
            OnHeldBeansChanged?.Invoke(null, 0f);
        }

        /// <summary>
        /// 根据当前研磨度计算下一次手柄操作对应的研磨度。
        /// </summary>
        private GrindType GetNextGrindType(GrindType? currentGrindType)
        {
            if (!currentGrindType.HasValue)
            {
                return GrindType.Coarse;
            }

            return currentGrindType.Value switch
            {
                GrindType.Coarse => GrindType.Fine,
                GrindType.Fine => GrindType.ExtraFine,
                _ => GrindType.ExtraFine
            };
        }

        /// <summary>
        /// 获取研磨度的中文显示名称。
        /// </summary>
        private string GetGrindTypeName(GrindType grindType)
        {
            return grindType switch
            {
                GrindType.Coarse => "粗磨",
                GrindType.Fine => "细磨",
                GrindType.ExtraFine => "精磨",
                _ => grindType.ToString()
            };
        }

        /// <summary>
        /// 推进当前萃取的咖啡液体积，并同步成品数据和场景工作杯显示。
        /// </summary>
        private void UpdateExtraction()
        {
            float extractionSpeed = Mathf.Max(0f, _extractionSpeed);
            _currentExtractionVolume = Mathf.Min(
                _currentExtractionVolume + extractionSpeed * Time.deltaTime,
                _targetExtractionVolume);

            if (_currentExtractingSegmentIndex >= 0
                && _currentExtractingSegmentIndex < CurrentCoffeeData.coffeeSegments.Count)
            {
                CoffeeExtractSegmentData segment = CurrentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex];
                segment.extractedVolume = _currentExtractionVolume;
                CurrentCoffeeData.coffeeSegments[_currentExtractingSegmentIndex] = segment;
                RefreshCoffeeVolume();

                OnCoffeeDataChanged?.Invoke(CurrentCoffeeData);
                OnExtractionProgressChanged?.Invoke(_currentExtractionVolume, _targetExtractionVolume);
            }

            if (_currentExtractionVolume >= _targetExtractionVolume)
            {
                CompleteExtraction();
            }
        }

        /// <summary>
        /// 完成当前萃取，清空研磨机批次并进入加辅助液阶段。
        /// </summary>
        private void CompleteExtraction()
        {
            _isExtracting = false;
            EnsureGrinderBeanBatch();
            _grinderBeanBatch.Clear();
            _currentStage = NewCoffeeCraftStage.AddLiquid;

            OnGrinderBeanBatchChanged?.Invoke(_grinderBeanBatch);
            OnExtractionStateChanged?.Invoke(false);
            OnExtractionCompleted?.Invoke();
            Debug.Log($"[NewCoffeeCraft] 萃取完成：{CurrentCoffeeData.currentTotalVolume:F0}ml 咖啡液");

            ResetExtractionRuntime();
        }

        /// <summary>
        /// 处理未承接萃取：清空本批咖啡粉并回到取豆阶段。
        /// </summary>
        private void WasteCurrentGrinderBatch()
        {
            string beanName = _grinderBeanBatch.bean != null ? _grinderBeanBatch.bean.beanName : "未知豆种";
            float beanGrams = _grinderBeanBatch.beanGram;
            _grinderBeanBatch.Clear();
            _currentStage = NewCoffeeCraftStage.GrindBeans;

            OnGrinderBeanBatchChanged?.Invoke(_grinderBeanBatch);
            OnExtractionWasted?.Invoke();
            Debug.LogWarning($"[NewCoffeeCraft] 萃取未承接，咖啡粉已作废：{beanName} {beanGrams:F0}g");
        }

        /// <summary>
        /// 刷新当前咖啡总容量。
        /// </summary>
        private void RefreshCoffeeVolume()
        {
            CurrentCoffeeData.currentTotalVolume = CurrentCoffeeData.GetTotalCoffeeVolume()
                + CurrentCoffeeData.GetTotalLiquidVolume();
        }

        /// <summary>
        /// 清空当前萃取过程的运行时数据并解除工作杯锁定。
        /// </summary>
        private void ResetExtractionRuntime()
        {
            bool wasExtracting = _isExtracting;
            _isExtracting = false;
            _currentExtractionVolume = 0f;
            _targetExtractionVolume = 0f;
            _currentExtractingSegmentIndex = -1;

            if (wasExtracting)
            {
                OnExtractionStateChanged?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// 新咖啡制作阶段。
    /// </summary>
    public enum NewCoffeeCraftStage
    {
        SelectCup = 0,
        GrindBeans = 1,
        ExtractCoffee = 2,
        AddLiquid = 3,
        AddTopping = 4,
        Submit = 5
    }
}
