using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 结局配置SO
    /// 包含三种结局（迷失/回归/好结局）的过场序列和结局面板内容
    /// </summary>
    [CreateAssetMenu(fileName = "EndingConfig_", menuName = "InnsmouthCafe/Config/EndingConfig", order = 11)]
    public class EndingConfigSO : ScriptableObject
    {
        [Header("迷失结局")]
        [Tooltip("迷失结局配置")]
        public EndingConfig endingLost;

        [Header("回归结局")]
        [Tooltip("回归结局配置")]
        public EndingConfig endingReturn;

        [Header("好结局")]
        [Tooltip("好结局配置")]
        public EndingConfig endingGood;

        /// <summary>
        /// 根据结局类型获取对应的结局配置
        /// </summary>
        public EndingConfig GetEndingConfig(GameEnding ending)
        {
            switch (ending)
            {
                case GameEnding.Good:   return endingGood;
                case GameEnding.Return: return endingReturn;
                default:                return endingLost;
            }
        }
    }
}
