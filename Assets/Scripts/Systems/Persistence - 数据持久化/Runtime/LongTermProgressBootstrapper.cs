using InnsmouthCafe.Data;
using InnsmouthCafe.GameFlow;
using UnityEngine;

namespace InnsmouthCafe.Progression
{
    /// <summary>
    /// 长期经营流程的最小启动入口，负责进入游戏场景后初始化当前地图进度。
    /// </summary>
    public class LongTermProgressBootstrapper : MonoBehaviour
    {
        [Header("地图配置")]
        [Tooltip("当前长期经营流程使用的地图配置")]
        [SerializeField] private MapConfigSO mapConfig;

        private void Start()
        {
            InitializeProgress();
        }

        /// <summary>
        /// 使用 Inspector 配置的地图初始化章节与任务进度。
        /// </summary>
        private void InitializeProgress()
        {
            if (mapConfig == null)
            {
                Debug.LogWarning("[LongTermProgress] 未配置地图，无法初始化章节进度。");
                return;
            }

            if (!ChapterProgressService.Instance.InitializeMap(mapConfig))
            {
                return;
            }

            NewGameFlowManager gameFlow = NewGameFlowManager.Instance;
            if (gameFlow != null
                && gameFlow.CurrentState == InnsmouthCafe.GameFlow.GameFlowState.None)
            {
                gameFlow.StartNewDay();
            }
        }
    }
}
