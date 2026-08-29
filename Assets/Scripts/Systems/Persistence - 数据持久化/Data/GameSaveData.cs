using System;
using System.Collections.Generic;

namespace InnsmouthCafe.Persistence
{
    /// <summary>
    /// 单个长期经营存档的完整运行数据。
    /// 该数据只保存局内进度，不保存局外图鉴收藏。
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public const int CurrentVersion = 6;

        public int version = CurrentVersion;
        public int slotIndex;
        public string saveName;
        public string createdAtUtc;
        public string lastPlayedAtUtc;
        public int currentDay = 1;
        public DayPhase currentPhase = DayPhase.Exploration;
        public string currentMapId;

        public int dailyCash;
        public int totalAssets;
        public List<IngredientInventoryEntryData> ingredientInventory =
            new List<IngredientInventoryEntryData>();
        public List<string> unlockedLiquidIds = new List<string>();
        public List<string> unlockedToppingIds = new List<string>();

        public int shopLevel = 1;
        public int beautyScore;
        public List<string> installedDecorationIds = new List<string>();
        public int maxBusinessTimeSeconds;
        public float basePatienceBonus;

        public List<string> visitedAreaIds = new List<string>();
        public List<string> unlockedVehicleIds = new List<string>();
        public List<IdLevelData> equipmentLevels = new List<IdLevelData>();
        public int maxEnergy = 100;
        public List<IdLevelData> facilityLevels = new List<IdLevelData>();

        public List<string> completedStoryIds = new List<string>();
        public List<TaskProgressData> taskProgresses = new List<TaskProgressData>();
        public List<SpecialGuestProgressData> specialGuestProgresses =
            new List<SpecialGuestProgressData>();

        public int totalOrders;
        public int totalCustomersServed;
        public int fiveStarOrders;
        public int totalSatisfiedReviews;
        public int totalIncome;
        public int totalCollectedMaterials;
        public int totalExplorations;
        public List<IdCountData> servedCustomerCounts = new List<IdCountData>();
        public List<IdCountData> collectedMaterialCounts = new List<IdCountData>();

        /// <summary>
        /// 创建具有默认局内状态的新存档。
        /// </summary>
        public static GameSaveData CreateNew(int slotIndex, string saveName)
        {
            string now = DateTime.UtcNow.ToString("O");
            return new GameSaveData
            {
                slotIndex = slotIndex,
                saveName = saveName,
                currentDay = 1,
                currentPhase = DayPhase.Exploration,
                createdAtUtc = now,
                lastPlayedAtUtc = now,
                taskProgresses = new List<TaskProgressData>()
            };
        }
    }

    /// <summary>
    /// 单个任务在当前存档中的运行时进度。
    /// 任务配置来自 TaskDefinitionSO，本数据只保存玩家实际进度。
    /// </summary>
    [Serializable]
    public class TaskProgressData
    {
        public string mapId;
        public string chapterId;
        public string taskId;
        public int activationBaseline;
        public int currentValue;
        public bool isCompleted;
    }

    /// <summary>
    /// 按对象 ID 记录的累计数量，用于指定顾客或指定材料任务。
    /// </summary>
    [Serializable]
    public class IdCountData
    {
        public string id;
        public int count;
    }

    /// <summary>
    /// 材料库存条目，避免将不可控的字典结构直接写入 JsonUtility。
    /// </summary>
    [Serializable]
    public class IngredientInventoryEntryData
    {
        public IngredientKind ingredientKind;
        public string itemId;
        public int amount;
    }

    /// <summary>
    /// 可进入当前存档库存的材料类别。
    /// </summary>
    public enum IngredientKind
    {
        Liquid = 0,
        Topping = 1
    }

    /// <summary>
    /// 一天经营循环中的当前阶段，不代表章节或地图进度。
    /// </summary>
    public enum DayPhase
    {
        Exploration = 0,
        Preparation = 1,
        Business = 2,
        Settlement = 3
    }

    /// <summary>
    /// 通用等级条目，用于装备和设施等具有等级的局内进度。
    /// </summary>
    [Serializable]
    public class IdLevelData
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// 特殊来宾在单个存档内的接待与奖励进度。
    /// </summary>
    [Serializable]
    public class SpecialGuestProgressData
    {
        public string specialCustomerId;
        public bool hasMet;
        public int bestRatingHalfStars;
        public List<int> claimedRewardTiers = new List<int>();
        public int nextVisitDay;
        public SpecialGuestVisitState visitState;
    }

    /// <summary>
    /// 特殊来宾当前的局内来访状态。
    /// </summary>
    public enum SpecialGuestVisitState
    {
        Undiscovered = 0,
        Announced = 1,
        AwaitingVisit = 2,
        Completed = 3
    }

    /// <summary>
    /// 存档槽位的轻量预览数据，不携带完整库存与进度列表。
    /// </summary>
    [Serializable]
    public class SaveSlotPreviewData
    {
        public int slotIndex;
        public bool hasSave;
        public bool isCorrupted;
        public string saveName;
        public string lastPlayedAtUtc;
        public int currentDay;
        public string currentMapId;
        public int totalAssets;
    }
}
