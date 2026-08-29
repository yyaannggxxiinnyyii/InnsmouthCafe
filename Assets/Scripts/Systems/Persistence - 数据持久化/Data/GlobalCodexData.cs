using System;
using System.Collections.Generic;

namespace InnsmouthCafe.Persistence
{
    /// <summary>
    /// 跨存档永久保留的局外图鉴数据。
    /// 只记录收藏状态，不提供任何局内玩法奖励。
    /// </summary>
    [Serializable]
    public class GlobalCodexData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<string> discoveredCharacterIds = new List<string>();
        public List<string> discoveredToppingIds = new List<string>();
        public List<string> discoveredLiquidIds = new List<string>();
        public List<string> discoveredDecorationIds = new List<string>();
        public List<string> discoveredBossDecorationIds = new List<string>();
        public List<string> legacyCollectibleIds = new List<string>();
        public List<string> legacyEndingIds = new List<string>();
    }
}
