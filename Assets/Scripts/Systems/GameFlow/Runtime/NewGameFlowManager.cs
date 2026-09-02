using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using InnsmouthCafe.Business;
using InnsmouthCafe.Customer;
using InnsmouthCafe.Scoring;
using InnsmouthCafe.Patience;
using InnsmouthCafe.Shop;
using InnsmouthCafe.Decoration;
using InnsmouthCafe.Data;
using InnsmouthCafe.Persistence;
using InnsmouthCafe.CoffeeCraft;

namespace InnsmouthCafe.GameFlow
{
    /// <summary>
    /// 新游戏流程管理器（长期经营版）。
    /// 负责串联：探索 → 备料 → 营业 → 结算 的一天循环。
    /// 不再有7天限制，天数无限累加。
    /// </summary>
    public class NewGameFlowManager : Singleton<NewGameFlowManager>
    {
        [Header("场景配置")]
        [Tooltip("店铺场景名称；留空时不执行场景加载")]
        [SerializeField] private string _shopSceneName = "GameScene_DioramaPrototype";

        [Tooltip("探索场景名称；留空时不执行场景加载")]
        [SerializeField] private string _explorationSceneName = "";

        [Header("调试信息 - 只读")]
        [Tooltip("当前游戏流程状态")]
        [SerializeField] private GameFlowState _currentState = GameFlowState.None;

        [Tooltip("当前天数（从1开始）")]
        [SerializeField] private int _currentDay = 1;

        /// <summary>
        /// 当前游戏流程状态。
        /// </summary>
        public GameFlowState CurrentState => _currentState;

        /// <summary>
        /// 当前天数（从1开始）。
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// 游戏流程状态变化事件。
        /// 参数：新状态。
        /// </summary>
        public event Action<GameFlowState> OnStateChanged;

        /// <summary>
        /// 新一天开始事件。
        /// 参数：天数。
        /// </summary>
        public event Action<int> OnNewDayStarted;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            Debug.Log("[NewGameFlow] 流程管理器已就绪，等待开始新的一天");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        /// <summary>
        /// 确保所有依赖的Manager实例已创建。
        /// 如果场景中不存在，则自动创建GameObject并添加组件。
        /// </summary>
        private void EnsureManagers()
        {
            // 检查并创建 BusinessTimerManager
            if (BusinessTimerManager.Instance == null)
            {
                GameObject timerObj = new GameObject("BusinessTimerManager");
                timerObj.AddComponent<BusinessTimerManager>();
                DontDestroyOnLoad(timerObj);
            }

            // 检查并创建 NewCustomerManager
            if (NewCustomerManager.Instance == null)
            {
                GameObject customerObj = new GameObject("NewCustomerManager");
                customerObj.AddComponent<NewCustomerManager>();
                DontDestroyOnLoad(customerObj);
            }

            // 检查并创建 NewScoringManager
            if (NewScoringManager.Instance == null)
            {
                GameObject scoringObj = new GameObject("NewScoringManager");
                scoringObj.AddComponent<NewScoringManager>();
                DontDestroyOnLoad(scoringObj);
            }

            // 检查并创建 NewCoffeeCraftManager
            if (NewCoffeeCraftManager.Instance == null)
            {
                GameObject coffeeCraftObj = new GameObject("NewCoffeeCraftManager");
                coffeeCraftObj.AddComponent<NewCoffeeCraftManager>();
                DontDestroyOnLoad(coffeeCraftObj);
            }

            // 检查并创建 CustomerPatienceManager
            if (CustomerPatienceManager.Instance == null)
            {
                GameObject patienceObj = new GameObject("CustomerPatienceManager");
                patienceObj.AddComponent<CustomerPatienceManager>();
                DontDestroyOnLoad(patienceObj);
            }

            // 检查并创建 ShopUpgradeManager
            if (ShopUpgradeManager.Instance == null)
            {
                GameObject shopObj = new GameObject("ShopUpgradeManager");
                shopObj.AddComponent<ShopUpgradeManager>();
                DontDestroyOnLoad(shopObj);
            }

            // 检查并创建 DecorationManager
            if (DecorationManager.Instance == null)
            {
                GameObject decorationObj = new GameObject("DecorationManager");
                decorationObj.AddComponent<DecorationManager>();
                DontDestroyOnLoad(decorationObj);
            }

            Debug.Log($"[NewGameFlow] 管理器初始化完成");
        }

        /// <summary>
        /// 开始新的一天。
        /// </summary>
        public void StartNewDay()
        {
            // 确保所有Manager已初始化
            EnsureManagers();
            NewCoffeeCraftManager.Instance?.ResetForBusiness();

            _currentDay = SaveSlotService.Instance.CurrentSave?.currentDay ?? 1;

            OnNewDayStarted?.Invoke(_currentDay);

            Debug.Log($"[NewGameFlow] ========== 第 {_currentDay} 天 ==========");

            ChangeState(GameFlowState.DayStarting);
            ChangeState(GameFlowState.ShopPreparation);
        }

        /// <summary>
        /// 请求从店铺进入探索场景。
        /// </summary>
        public void RequestEnterExploration()
        {
            if (_currentState != GameFlowState.ShopPreparation)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不允许进入探索");
                return;
            }

            if (string.IsNullOrWhiteSpace(_explorationSceneName))
            {
                Debug.LogError("[NewGameFlow] 未配置探索场景名称");
                return;
            }

            ChangeState(GameFlowState.LoadingExploration);
            SceneManager.LoadSceneAsync(_explorationSceneName);
        }

        /// <summary>
        /// 探索场景中的撤离点调用此方法完成探索并返回店铺。
        /// </summary>
        public void CompleteExploration()
        {
            if (_currentState != GameFlowState.Exploring)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不能完成探索");
                return;
            }

            if (string.IsNullOrWhiteSpace(_shopSceneName))
            {
                Debug.LogError("[NewGameFlow] 未配置店铺场景名称");
                return;
            }

            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                SaveSlotService.Instance.CurrentSave.totalExplorations++;
                SaveSlotService.Instance.SaveCurrent();
            }

            ChangeState(GameFlowState.ReturningFromExploration);
            SceneManager.LoadSceneAsync(_shopSceneName);
        }

        /// <summary>
        /// 请求开始营业；由店铺场景的开始营业入口调用。
        /// </summary>
        public void RequestStartBusiness()
        {
            if (_currentState != GameFlowState.BusinessPreparation)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不允许开始营业");
                return;
            }

            ChangeState(GameFlowState.Business);
        }

        /// <summary>
        /// 初始化营业阶段的运行系统。
        /// </summary>
        private void EnterBusinessPhase()
        {
            ChangeState(GameFlowState.Business);

            Debug.Log("[NewGameFlow] 进入营业阶段");


            // 2. 启动营业计时器（从店铺等级读取时间）

            // 3. 启动顾客生成

            // 4. 监听营业时间结束事件

        }

        /// <summary>
        /// 营业时间结束处理。
        /// </summary>
        private void HandleBusinessTimeUp()
        {
            BusinessTimerManager.Instance.OnTimeUp -= HandleBusinessTimeUp;

            Debug.Log("[NewGameFlow] 营业时间结束，等待所有顾客服务完成...");

            // 停止生成新顾客
            NewCustomerManager.Instance.EndBusinessPhase();

            Debug.Log("[NewGameFlow] 等待营业场景报告所有顾客处理完成");
        }

        /// <summary>
        /// 场景加载完成后推进对应的跨场景流程状态。
        /// </summary>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_currentState == GameFlowState.LoadingExploration
                && scene.name == _explorationSceneName)
            {
                ChangeState(GameFlowState.Exploring);
                return;
            }

            if (_currentState == GameFlowState.ReturningFromExploration
                && scene.name == _shopSceneName)
            {
                ChangeState(GameFlowState.BusinessPreparation);
            }
        }

        /// <summary>
        /// 改变游戏流程状态。
        /// </summary>
        /// <param name="newState">新的流程状态。</param>
        private void ChangeState(GameFlowState newState)
        {
            _currentState = newState;

            OnStateChanged?.Invoke(newState);

            Debug.Log($"[NewGameFlow] 流程状态：{newState}");
        }

        /// <summary>
        /// 手动结束营业（提前闭门谢客）。
        /// </summary>
        public void EndBusinessEarly()
        {
            if (_currentState != GameFlowState.Business)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不在营业中");
                return;
            }

            BusinessTimerManager.Instance.EndBusinessEarly();

            Debug.Log("[NewGameFlow] 玩家提前结束营业");
        }

        /// <summary>
        /// 在营业场景确认所有顾客已处理完成后进入日结算。
        /// </summary>
        public void CompleteBusiness()
        {
            if (_currentState != GameFlowState.Business)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不能完成营业");
                return;
            }

            BusinessTimerManager.Instance?.StopBusinessTimer();
            NewCustomerManager.Instance?.EndBusinessPhase();
            ChangeState(GameFlowState.Settlement);
        }

        /// <summary>
        /// 玩家确认日结算后开始下一天。
        /// </summary>
        public void CompleteSettlement()
        {
            if (_currentState != GameFlowState.Settlement)
            {
                Debug.LogWarning($"[NewGameFlow] 当前状态 {_currentState} 不能完成结算");
                return;
            }

            if (SaveSlotService.Instance?.CurrentSave != null)
            {
                SaveSlotService.Instance.CurrentSave.currentDay++;
                SaveSlotService.Instance.SaveCurrent();
            }

            StartNewDay();
        }
    }

    /// <summary>
    /// 游戏流程状态枚举。
    /// </summary>
    public enum GameFlowState
    {
        None = 0,
        DayStarting = 1,
        ShopPreparation = 2,
        LoadingExploration = 3,
        Exploring = 4,
        ReturningFromExploration = 5,
        BusinessPreparation = 6,
        Business = 7,
        Settlement = 8
    }
}
