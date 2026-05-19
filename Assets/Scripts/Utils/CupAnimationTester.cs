using UnityEngine;
using InnsmouthCafe.Managers;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Utils
{
    /// <summary>
    /// 杯子动画测试工具
    /// 用于测试杯子选择和场景切换动画
    /// </summary>
    public class CupAnimationTester : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] [Tooltip("测试用杯子配置")]
        private CupContainerSO[] _testCups;

        [Header("测试用豆子")]
        [SerializeField] [Tooltip("测试豆子配置")]
        private BeanSO _testBeanConfig;

        [Header("快捷键说明")]
        [SerializeField] [Tooltip("显示快捷键提示")]
        private bool _showHints = true;

        private CoffeeCraftManager _craftManager;
        private ViewSwitchManager _viewManager;
        private int _currentCupIndex = 0;

        private void Start()
        {
            _craftManager = CoffeeCraftManager.Instance;
            _viewManager = ViewSwitchManager.Instance;

            if (_showHints)
            {
                PrintHints();
            }
        }

        private void Update()
        {
            // F1: 开始制作
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TestStartCrafting();
            }

            // F2: 选择第一个杯子
            if (Input.GetKeyDown(KeyCode.F2))
            {
                TestSelectCup(0);
            }

            // F3: 选择第二个杯子
            if (Input.GetKeyDown(KeyCode.F3))
            {
                TestSelectCup(1);
            }

            // F4: 选择第三个杯子
            if (Input.GetKeyDown(KeyCode.F4))
            {
                TestSelectCup(2);
            }

            // F5: 切换到下一个场景
            if (Input.GetKeyDown(KeyCode.F5))
            {
                TestSwitchNextView();
            }

            // F6: 切换到上一个场景
            if (Input.GetKeyDown(KeyCode.F6))
            {
                TestSwitchPreviousView();
            }

            // F7: 模拟萃取（让杯子显示）
            if (Input.GetKeyDown(KeyCode.F7))
            {
                TestExtraction();
            }

            // F8: 循环切换杯子
            if (Input.GetKeyDown(KeyCode.F8))
            {
                TestCycleCup();
            }
        }

        private void TestStartCrafting()
        {
            if (_craftManager == null)
            {
                Debug.LogError("[CupAnimationTester] CoffeeCraftManager未找到");
                return;
            }

            // TestStartCrafting 需要真实需求数据，这里只给个空的默认值测试
            _craftManager.StartNewCraft(new OrderRequirementData());
            Debug.Log("[CupAnimationTester] 开始制作");
        }

        private void TestSelectCup(int index)
        {
            if (_craftManager == null)
            {
                Debug.LogError("[CupAnimationTester] CoffeeCraftManager未找到");
                return;
            }

            if (_testCups == null || index >= _testCups.Length)
            {
                Debug.LogError($"[CupAnimationTester] 杯子索引越界: {index}");
                return;
            }

            CupContainerSO cupSO = _testCups[index];
            if (cupSO == null)
            {
                Debug.LogError($"[CupAnimationTester] 杯子配置为空: {index}");
                return;
            }

            _craftManager.SelectCup(cupSO.ToData());
            _currentCupIndex = index;
            Debug.Log($"[CupAnimationTester] 选择杯子: {cupSO.cupName}");
        }

        private void TestCycleCup()
        {
            if (_testCups == null || _testCups.Length == 0)
            {
                Debug.LogError("[CupAnimationTester] 没有配置测试杯子");
                return;
            }

            _currentCupIndex = (_currentCupIndex + 1) % _testCups.Length;
            TestSelectCup(_currentCupIndex);
        }

        private void TestSwitchNextView()
        {
            if (_viewManager == null)
            {
                Debug.LogError("[CupAnimationTester] ViewSwitchManager未找到");
                return;
            }

            _viewManager.SwitchNextView();
            Debug.Log("[CupAnimationTester] 切换到下一个场景");
        }

        private void TestSwitchPreviousView()
        {
            if (_viewManager == null)
            {
                Debug.LogError("[CupAnimationTester] ViewSwitchManager未找到");
                return;
            }

            _viewManager.SwitchPreviousView();
            Debug.Log("[CupAnimationTester] 切换到上一个场景");
        }

        private void TestExtraction()
        {
            if (_craftManager == null)
            {
                Debug.LogError("[CupAnimationTester] CoffeeCraftManager未找到");
                return;
            }

            // 模拟取豆和研磨 (按照最新 Manager 接口)
            _craftManager.AddBean(_testBeanConfig);
            _craftManager.AddBean(_testBeanConfig);
            _craftManager.AddBean(_testBeanConfig);
            _craftManager.AddBean(_testBeanConfig);
            _craftManager.SelectGrind(GrindType.Fine);

            // 开始萃取
            _craftManager.StartExtraction();
            Debug.Log("[CupAnimationTester] 开始萃取（模拟）");

            // 2秒后由于自动执行，不一定需要手动StopExtraction（如果没有该接口），不过你可以根据当前API测试。
            // 查看到CoffeeCraftManager中有一个私有FinishExtraction并且会在Update自动触发，所以我们可以注释手动Stop
            // Invoke(nameof(CompleteExtraction), 2f);
        }

        // private void CompleteExtraction()
        // {
        //     if (_craftManager == null) return;
        //     // _craftManager.StopExtraction();
        //     Debug.Log("[CupAnimationTester] 萃取完成");
        // }

        private void PrintHints()
        {
            Debug.Log("=== 杯子动画测试快捷键 ===");
            Debug.Log("F1: 开始制作");
            Debug.Log("F2: 选择杯子1");
            Debug.Log("F3: 选择杯子2");
            Debug.Log("F4: 选择杯子3");
            Debug.Log("F5: 切换到下一个场景");
            Debug.Log("F6: 切换到上一个场景");
            Debug.Log("F7: 模拟萃取（让杯子显示）");
            Debug.Log("F8: 循环切换杯子");
            Debug.Log("========================");
        }

        private void OnGUI()
        {
            if (!_showHints) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== 杯子动画测试 ===");
            GUILayout.Label("F1: 开始制作");
            GUILayout.Label("F2/F3/F4: 选择杯子1/2/3");
            GUILayout.Label("F5: 下一个场景");
            GUILayout.Label("F6: 上一个场景");
            GUILayout.Label("F7: 模拟萃取");
            GUILayout.Label("F8: 循环切换杯子");
            GUILayout.Space(10);

            if (_craftManager != null)
            {
                GUILayout.Label($"当前阶段: {_craftManager.ModuleState}");
                GUILayout.Label($"当前杯子: {(_craftManager.CurrentCoffeeData?.selectedCup?.cupName ?? "未选择")}");
            }

            if (_viewManager != null)
            {
                GUILayout.Label($"当前场景: {_viewManager.CurrentViewType}");
            }

            GUILayout.EndArea();
        }
    }
}
