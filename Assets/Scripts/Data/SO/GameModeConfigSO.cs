using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 游戏模式配置SO
    /// 每个游戏模式（教程/新手/普通/无尽）对应一个资源文件
    /// </summary>
    [CreateAssetMenu(fileName = "GameModeConfig_", menuName = "InnsmouthCafe/Config/GameModeConfig", order = 10)]
    public class GameModeConfigSO : ScriptableObject
    {
        [Header("模式基础信息")]
        [Tooltip("游戏模式")]
        public GameMode gameMode = GameMode.Normal;

        [Tooltip("总天数（无尽模式设为-1）")]
        public int totalDays = 7;

        [Header("每日顾客配置")]
        [Tooltip("每天的顾客配置列表（索引0=第1天）")]
        public List<DayCustomerConfigSO> dayConfigs = new List<DayCustomerConfigSO>();

        [Header("游戏开始延迟")]
        [Tooltip("场景加载后延迟多少秒自动开始（秒）")]
        [Range(0.5f, 5f)]
        public float startDelay = 2f;

        /// <summary>
        /// 获取指定天数的顾客配置
        /// </summary>
        /// <param name="dayIndex">天数索引（从0开始）</param>
        public DayCustomerConfigSO GetDayConfig(int dayIndex)
        {
            if (dayConfigs == null || dayIndex < 0 || dayIndex >= dayConfigs.Count)
            {
                Debug.LogError($"[GameModeConfig] 无法获取第{dayIndex + 1}天的顾客配置");
                return null;
            }
            return dayConfigs[dayIndex];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (gameMode != GameMode.Endless && totalDays > 0)
            {
                if (dayConfigs.Count != totalDays)
                {
                    Debug.LogWarning($"[GameModeConfig] {name}: dayConfigs数量({dayConfigs.Count})与totalDays({totalDays})不匹配", this);
                }
            }
        }
#endif
    }
}
