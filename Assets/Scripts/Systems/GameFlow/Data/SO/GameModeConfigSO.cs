using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 游戏模式配置SO
    /// 每个游戏模式对应一个资源文件，包含流程、关卡和难度参数。
    /// </summary>
    [CreateAssetMenu(fileName = "GameModeConfig_", menuName = "InnsmouthCafe/Config/GameModeConfig", order = 10)]
    public class GameModeConfigSO : ScriptableObject
    {
        [Header("模式基础信息")]
        [Tooltip("游戏模式")]
        public GameMode gameMode = GameMode.Normal;

        [Tooltip("总天数")]
        public int totalDays = 7;

        [Header("顾客压力")]
        [SerializeField]
        [Tooltip("所有顾客基础耐心时间倍率，最终还会叠乘特殊顾客和无尽模式倍率")]
        [Min(0.01f)]
        private float _patienceMultiplier = 1f;

        [SerializeField]
        [Tooltip("顾客愤怒状态每秒理智损失倍率")]
        [Min(0f)]
        private float _angrySanityDrainMultiplier = 1f;

        [Header("评分规则")]
        [SerializeField]
        [Tooltip("辅助液与总量达到满分时允许的误差百分比")]
        [Range(0f, 50f)]
        private float _scoringErrorMargin = 10f;

        [SerializeField]
        [Tooltip("达到合格评价所需的最低分数")]
        [Range(0f, 100f)]
        private float _acceptableScoreThreshold = 50f;

        [SerializeField]
        [Tooltip("达到 Perfect 评价所需的最低分数")]
        [Range(0f, 100f)]
        private float _perfectScoreThreshold = 90f;

        [Header("理智变化")]
        [SerializeField]
        [Tooltip("所有正面理智变化倍率")]
        [Min(0f)]
        private float _positiveSanityMultiplier = 1f;

        [SerializeField]
        [Tooltip("所有负面理智变化倍率")]
        [Min(0f)]
        private float _negativeSanityMultiplier = 1f;

        [Header("每日顾客配置")]
        [Tooltip("每天的顾客配置列表（索引0=第1天）")]
        public List<DayCustomerConfigSO> dayConfigs = new List<DayCustomerConfigSO>();

        [Header("游戏开始延迟")]
        [Tooltip("场景加载后延迟多少秒自动开始（秒）")]
        [Range(0.5f, 5f)]
        public float startDelay = 2f;

        /// <summary>获取顾客耐心倍率，并叠乘无尽模式等运行时倍率。</summary>
        public float GetPatienceMultiplier(float runtimeMultiplier = 1f)
        {
            return _patienceMultiplier * Mathf.Max(0.01f, runtimeMultiplier);
        }

        /// <summary>获取顾客愤怒状态的理智损失倍率。</summary>
        public float GetAngrySanityDrainMultiplier(float runtimeMultiplier = 1f)
        {
            return _angrySanityDrainMultiplier * Mathf.Max(0f, runtimeMultiplier);
        }

        /// <summary>获取评分允许误差百分比。</summary>
        public float GetScoringErrorMargin(float runtimeMultiplier = 1f)
        {
            return Mathf.Clamp(_scoringErrorMargin * Mathf.Max(0f, runtimeMultiplier), 0f, 50f);
        }

        /// <summary>获取合格评价最低分数。</summary>
        public float GetAcceptableScoreThreshold(float runtimeOffset = 0f)
        {
            return Mathf.Clamp(_acceptableScoreThreshold + runtimeOffset, 0f, 100f);
        }

        /// <summary>获取 Perfect 评价最低分数。</summary>
        public float GetPerfectScoreThreshold(float runtimeOffset = 0f)
        {
            return Mathf.Clamp(_perfectScoreThreshold + runtimeOffset, 0f, 100f);
        }

        /// <summary>获取正面理智变化倍率。</summary>
        public float GetPositiveSanityMultiplier(float runtimeMultiplier = 1f)
        {
            return _positiveSanityMultiplier * Mathf.Max(0f, runtimeMultiplier);
        }

        /// <summary>获取负面理智变化倍率。</summary>
        public float GetNegativeSanityMultiplier(float runtimeMultiplier = 1f)
        {
            return _negativeSanityMultiplier * Mathf.Max(0f, runtimeMultiplier);
        }

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
            if (_perfectScoreThreshold < _acceptableScoreThreshold)
            {
                _perfectScoreThreshold = _acceptableScoreThreshold;
            }

            if (totalDays > 0 && dayConfigs.Count != totalDays)
            {
                Debug.LogWarning($"[GameModeConfig] {name}: dayConfigs数量({dayConfigs.Count})与totalDays({totalDays})不匹配", this);
            }
        }
#endif
    }
}
