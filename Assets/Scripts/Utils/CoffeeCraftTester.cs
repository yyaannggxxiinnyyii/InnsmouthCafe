using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.Utils
{
    /// <summary>
    /// 咖啡制作系统测试工具（V0.2）
    /// 用于快速测试 CoffeeCraftManager 的各项功能
    /// </summary>
    public class CoffeeCraftTester : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _enableDebugUI = true;
        [SerializeField] private bool _enableKeyboardShortcuts = true;

        [Header("测试用杯子数据")]
        [SerializeField] private CupContainerData _testSmallCup;
        [SerializeField] private CupContainerData _testMediumCup;

        private CoffeeCraftManager _manager;
        private Vector2 _scrollPosition;
        private bool _showDebugWindow = true;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;
        }

        private void Start()
        {
            if (_manager == null)
            {
                _manager = CoffeeCraftManager.Instance;
            }

            if (_manager == null)
            {
                Debug.LogError("[CoffeeCraftTester] CoffeeCraftManager 未找到！");
                return;
            }

            _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            _manager.OnBatchDataChanged += OnBatchDataChanged;
            _manager.OnMainStateChanged += OnMainStateChanged;
            _manager.OnModuleStateChanged += OnModuleStateChanged;
            _manager.OnOverflowed += OnOverflowed;

            Debug.Log("[CoffeeCraftTester] 测试工具已启动");
        }

        private void Update()
        {
            if (!_enableKeyboardShortcuts) return;
            if (_manager == null) return;

            // 快捷键测试
            if (Input.GetKeyDown(KeyCode.Alpha1)) TestStartNewCraft();
            if (Input.GetKeyDown(KeyCode.Alpha2)) TestSelectSmallCup();
            if (Input.GetKeyDown(KeyCode.Alpha3)) TestSelectMediumCup();
            if (Input.GetKeyDown(KeyCode.Alpha4)) TestAddBean(BeanType.Normal);
            if (Input.GetKeyDown(KeyCode.Alpha5)) TestAddBean(BeanType.Arabica);
            if (Input.GetKeyDown(KeyCode.Alpha6)) TestSelectGrind(GrindType.Coarse);
            if (Input.GetKeyDown(KeyCode.Alpha7)) TestSelectGrind(GrindType.Fine);
            if (Input.GetKeyDown(KeyCode.Alpha8)) TestSelectGrind(GrindType.ExtraFine);
            if (Input.GetKeyDown(KeyCode.Alpha9)) TestFinishExtraction();
            if (Input.GetKeyDown(KeyCode.Alpha0)) TestSubmit();

            // 辅助液测试（按住）
            if (Input.GetKeyDown(KeyCode.Q)) _manager.StartPourLiquid(LiquidType.HotWater);
            if (Input.GetKeyUp(KeyCode.Q)) _manager.StopPourLiquid();

            if (Input.GetKeyDown(KeyCode.W)) _manager.StartPourLiquid(LiquidType.Milk);
            if (Input.GetKeyUp(KeyCode.W)) _manager.StopPourLiquid();

            if (Input.GetKeyDown(KeyCode.E)) _manager.StartPourLiquid(LiquidType.Foam);
            if (Input.GetKeyUp(KeyCode.E)) _manager.StopPourLiquid();

            // 小料测试
            if (Input.GetKeyDown(KeyCode.R)) TestAddTopping(ToppingType.CaramelCrisp);
            if (Input.GetKeyDown(KeyCode.T)) TestAddTopping(ToppingType.ChocolatePowder);
            if (Input.GetKeyDown(KeyCode.Y)) TestAddTopping(ToppingType.StarfishCandy);

            // 其他操作
            if (Input.GetKeyDown(KeyCode.C)) TestClearWholeCoffee();
            if (Input.GetKeyDown(KeyCode.F1)) _showDebugWindow = !_showDebugWindow;
        }

        private void TestStartNewCraft()
        {
            Debug.Log("[Test] 开始新的咖啡制作");
            var testOrder = new OrderRequirementData
            {
                targetTotalVolume = 400f,
                recommendedCupCapacity = 500f
            };
            _manager.StartNewCraft(testOrder);
        }

        private void TestSelectSmallCup()
        {
            if (_testSmallCup == null)
            {
                _testSmallCup = new CupContainerData
                {
                    cupId = "small",
                    cupName = "小杯",
                    capacity = 250f
                };
            }
            Debug.Log("[Test] 选择小杯");
            _manager.SelectCup(_testSmallCup);
        }

        private void TestSelectMediumCup()
        {
            if (_testMediumCup == null)
            {
                _testMediumCup = new CupContainerData
                {
                    cupId = "medium",
                    cupName = "中杯",
                    capacity = 400f
                };
            }
            Debug.Log("[Test] 选择中杯");
            _manager.SelectCup(_testMediumCup);
        }

        private void TestAddBean(BeanType beanType)
        {
            Debug.Log($"[Test] 添加咖啡豆: {beanType}");
            _manager.AddBean(beanType);
        }

        private void TestSelectGrind(GrindType grindType)
        {
            Debug.Log($"[Test] 选择研磨: {grindType}");
            _manager.SelectGrind(grindType);
        }

        private void TestFinishExtraction()
        {
            Debug.Log("[Test] 完成萃取");
            _manager.StartExtraction();
            _manager.FinishExtraction();
        }

        private void TestAddTopping(ToppingType toppingType)
        {
            Debug.Log($"[Test] 添加小料: {toppingType}");
            _manager.AddTopping(toppingType);
        }

        private void TestSubmit()
        {
            Debug.Log("[Test] 提交咖啡");
            var result = _manager.SubmitCoffee();
            if (result != null)
            {
                Debug.Log($"[Test] 提交成功！总容量: {result.currentTotalVolume}ml");
            }
        }

        private void TestClearWholeCoffee()
        {
            Debug.Log("[Test] 倒掉整杯咖啡");
            _manager.ClearWholeCoffee();
        }

        private void OnCoffeeDataChanged(CoffeeData data)
        {
            Debug.Log($"<color=yellow>[Event] 数据更新 - 总容量: {data.currentTotalVolume}ml</color>");
        }

        private void OnBatchDataChanged(CurrentBeanBatchData batch)
        {
            Debug.Log($"<color=cyan>[Event] 批次更新 - 豆量: {batch.beanGram}g</color>");
        }

        private void OnMainStateChanged(CraftMainState state)
        {
            Debug.Log($"<color=green>[Event] 主状态改变: {state}</color>");
        }

        private void OnModuleStateChanged(CraftModuleState state)
        {
            Debug.Log($"<color=blue>[Event] 模块状态改变: {state}</color>");
        }

        private void OnOverflowed()
        {
            Debug.LogWarning("<color=red>[Event] 咖啡溢出！</color>");
        }

        private void OnGUI()
        {
            if (!_enableDebugUI || !_showDebugWindow) return;
            if (_manager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 咖啡制作测试工具 V0.2 ===", GUI.skin.box);
            GUILayout.Label("按 F1 切换显示/隐藏");
            GUILayout.Space(10);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label($"<b>主状态:</b> {_manager.MainState}", GUI.skin.box);
            GUILayout.Label($"<b>模块状态:</b> {_manager.ModuleState}", GUI.skin.box);
            GUILayout.Space(5);

            if (_manager.CurrentCoffeeData != null)
            {
                var data = _manager.CurrentCoffeeData;
                GUILayout.Label("<b>咖啡数据:</b>", GUI.skin.box);
                GUILayout.Label($"  杯子: {(data.selectedCup != null ? data.selectedCup.cupName : "未选择")}");
                GUILayout.Label($"  咖啡液段数: {data.coffeeSegments.Count}");
                GUILayout.Label($"  辅助液段数: {data.liquidSegments.Count}");
                GUILayout.Label($"  总容量: {data.currentTotalVolume:F1}ml");
                GUILayout.Label($"  小料数量: {data.toppings.Count}");
                GUILayout.Label($"  溢出: {data.isOverflowed}");
            }

            if (_manager.CurrentBatch != null)
            {
                var batch = _manager.CurrentBatch;
                GUILayout.Space(5);
                GUILayout.Label("<b>当前批次:</b>", GUI.skin.box);
                GUILayout.Label($"  豆种: {(batch.beanType.HasValue ? batch.beanType.Value.ToString() : "未选择")}");
                GUILayout.Label($"  豆量: {batch.beanGram:F1}g");
                GUILayout.Label($"  研磨: {(batch.grindType.HasValue ? batch.grindType.Value.ToString() : "未选择")}");
            }

            GUILayout.Space(10);

            GUILayout.Label("<b>快捷键:</b>", GUI.skin.box);
            GUILayout.Label("1 - 开始新制作");
            GUILayout.Label("2 - 选择小杯");
            GUILayout.Label("3 - 选择中杯");
            GUILayout.Label("4 - 添加普通豆");
            GUILayout.Label("5 - 添加阿拉比卡豆");
            GUILayout.Label("6 - 选择粗磨");
            GUILayout.Label("7 - 选择细磨");
            GUILayout.Label("8 - 选择精磨");
            GUILayout.Label("9 - 完成萃取");
            GUILayout.Label("0 - 提交咖啡");
            GUILayout.Label("Q - 按住倒热水");
            GUILayout.Label("W - 按住倒牛奶");
            GUILayout.Label("E - 按住倒奶泡");
            GUILayout.Label("R - 添加焦糖碎");
            GUILayout.Label("T - 添加巧克力粉");
            GUILayout.Label("Y - 添加海星糖");
            GUILayout.Label("C - 倒掉整杯");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
