using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InnsmouthCafe.Explore;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 地图管理器（MapManager）。
    /// 管理当前激活的地图、章节进度、地区解锁。
    /// 提供数据访问接口给经营系统和探索系统。
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        private static MapManager _instance;
        public static MapManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("MapManager");
                    _instance = go.AddComponent<MapManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("当前地图")]
        [SerializeField] private MapConfigSO _currentMap;

        private SaveData CurrentSave => SaveManager.Instance.CurrentSave;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region 地图管理

        /// <summary>获取当前地图</summary>
        public MapConfigSO GetCurrentMap()
        {
            return _currentMap;
        }

        /// <summary>加载地图（从 MapConfigSO）</summary>
        public bool LoadMap(MapConfigSO mapConfig)
        {
            if (mapConfig == null)
            {
                Debug.LogError("[MapManager] 地图配置为空");
                return false;
            }

            if (CurrentSave == null)
            {
                Debug.LogError("[MapManager] 没有当前存档");
                return false;
            }

            _currentMap = mapConfig;
            CurrentSave.currentMapId = mapConfig.mapId;

            // 如果是首次进入该地图，应用初始状态
            if (!CurrentSave.unlockedMaps.Contains(mapConfig.mapId))
            {
                ApplyMapInitialState(mapConfig);
                CurrentSave.unlockedMaps.Add(mapConfig.mapId);
            }

            Debug.Log($"[MapManager] 已加载地图：{mapConfig.mapName} (ID: {mapConfig.mapId})");
            return true;
        }

        /// <summary>应用地图初始状态（首次进入时）</summary>
        private void ApplyMapInitialState(MapConfigSO mapConfig)
        {
            // 应用初始金币（如果当前金币为0，说明是新存档）
            if (CurrentSave.money == 0)
            {
                CurrentSave.money = mapConfig.initialMoney;
            }

            // 应用初始理智值
            if (CurrentSave.maxSanity == 0)
            {
                CurrentSave.maxSanity = mapConfig.initialMaxSanity;
                CurrentSave.currentSanity = mapConfig.initialMaxSanity;
            }

            // 应用初始材料
            if (mapConfig.initialMaterials != null)
            {
                foreach (var entry in mapConfig.initialMaterials)
                {
                    PlayerInventory.Instance.AddMaterial(entry.materialId, entry.count);
                }
            }

            // 应用初始工具
            if (mapConfig.initialTools != null)
            {
                foreach (var entry in mapConfig.initialTools)
                {
                    PlayerInventory.Instance.SetToolLevel(entry.toolType, entry.level);
                }
            }

            // 应用初始载具
            if (mapConfig.initialVehicles != null)
            {
                foreach (var vehicle in mapConfig.initialVehicles)
                {
                    if (vehicle != null)
                    {
                        PlayerInventory.Instance.AddVehicle(vehicle.vehicleId);
                    }
                }
            }

            // 解锁该地图的初始地区（areas 列表的第一个）
            if (mapConfig.areas != null && mapConfig.areas.Count > 0)
            {
                var firstArea = mapConfig.areas[0];
                if (firstArea != null && !CurrentSave.unlockedAreas.Contains(firstArea.areaId))
                {
                    CurrentSave.unlockedAreas.Add(firstArea.areaId);
                }
            }

            Debug.Log($"[MapManager] 已应用地图 {mapConfig.mapName} 的初始状态");
        }

        /// <summary>地图是否已解锁</summary>
        public bool IsMapUnlocked(string mapId)
        {
            if (CurrentSave == null) return false;
            return CurrentSave.unlockedMaps.Contains(mapId);
        }

        /// <summary>解锁地图</summary>
        public bool UnlockMap(string mapId)
        {
            if (CurrentSave == null) return false;

            if (CurrentSave.unlockedMaps.Contains(mapId))
            {
                Debug.LogWarning($"[MapManager] 地图 {mapId} 已解锁");
                return false;
            }

            CurrentSave.unlockedMaps.Add(mapId);
            Debug.Log($"[MapManager] 解锁地图：{mapId}");
            return true;
        }

        #endregion

        #region 章节管理

        /// <summary>获取当前章节</summary>
        public ChapterConfigSO GetCurrentChapter()
        {
            if (_currentMap == null || _currentMap.chapters == null || _currentMap.chapters.Count == 0)
            {
                return null;
            }

            // 返回第一个未完成的章节
            foreach (var chapter in _currentMap.chapters.OrderBy(c => c.sortOrder))
            {
                if (!IsChapterCompleted(_currentMap.mapId, chapter.chapterId))
                {
                    return chapter;
                }
            }

            // 所有章节都完成了
            return null;
        }

        /// <summary>章节是否已完成</summary>
        public bool IsChapterCompleted(string mapId, string chapterId)
        {
            if (CurrentSave == null) return false;

            var dict = CurrentSave.completedChapters.ToDictionary();
            if (!dict.ContainsKey(mapId)) return false;

            return dict[mapId].Contains(chapterId);
        }

        /// <summary>完成章节</summary>
        public bool CompleteChapter(string mapId, string chapterId)
        {
            if (CurrentSave == null) return false;

            var dict = CurrentSave.completedChapters.ToDictionary();

            if (!dict.ContainsKey(mapId))
            {
                dict[mapId] = new List<string>();
            }

            if (dict[mapId].Contains(chapterId))
            {
                Debug.LogWarning($"[MapManager] 章节 {chapterId} 已完成");
                return false;
            }

            dict[mapId].Add(chapterId);
            CurrentSave.completedChapters.FromDictionary(dict);

            Debug.Log($"[MapManager] 完成章节：{chapterId}");

            // 应用章节奖励
            ApplyChapterReward(mapId, chapterId);

            return true;
        }

        /// <summary>应用章节奖励</summary>
        private void ApplyChapterReward(string mapId, string chapterId)
        {
            if (_currentMap == null || _currentMap.mapId != mapId) return;

            var chapter = _currentMap.chapters.FirstOrDefault(c => c.chapterId == chapterId);
            if (chapter == null) return;

            // 奖励金币
            if (chapter.rewardMoney > 0)
            {
                PlayerInventory.Instance.AddMoney(chapter.rewardMoney);
            }

            // 奖励材料
            if (chapter.rewardMaterials != null)
            {
                foreach (var entry in chapter.rewardMaterials)
                {
                    PlayerInventory.Instance.AddMaterial(entry.materialId, entry.count);
                }
            }

            // 解锁探索地区
            if (chapter.unlockAreas != null)
            {
                foreach (var area in chapter.unlockAreas)
                {
                    if (area != null)
                    {
                        UnlockArea(area.areaId);
                    }
                }
            }

            Debug.Log($"[MapManager] 已发放章节 {chapterId} 的奖励");
        }

        /// <summary>检查章节目标是否达成</summary>
        public bool CheckChapterGoals(ChapterConfigSO chapter)
        {
            if (chapter == null || chapter.goals == null) return false;
            if (CurrentSave == null) return false;

            foreach (var goal in chapter.goals)
            {
                bool goalMet = false;

                switch (goal.type)
                {
                    case ChapterConfigSO.GoalType.TotalRevenue:
                        goalMet = CurrentSave.totalRevenue >= goal.targetValue;
                        break;

                    case ChapterConfigSO.GoalType.ServedCustomers:
                        goalMet = CurrentSave.totalServedCustomers >= goal.targetValue;
                        break;

                    case ChapterConfigSO.GoalType.PerfectOrders:
                        goalMet = CurrentSave.totalPerfectOrders >= goal.targetValue;
                        break;

                    case ChapterConfigSO.GoalType.CollectedMaterials:
                        goalMet = CurrentSave.totalCollectedMaterials >= goal.targetValue;
                        break;
                }

                if (!goalMet)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 地区管理

        /// <summary>获取已解锁的探索地区</summary>
        public List<AreaConfigSO> GetUnlockedAreas()
        {
            if (_currentMap == null || _currentMap.areas == null) return new List<AreaConfigSO>();
            if (CurrentSave == null) return new List<AreaConfigSO>();

            return _currentMap.areas.Where(area => area != null && CurrentSave.unlockedAreas.Contains(area.areaId)).ToList();
        }

        /// <summary>地区是否已解锁</summary>
        public bool IsAreaUnlocked(string areaId)
        {
            if (CurrentSave == null) return false;
            return CurrentSave.unlockedAreas.Contains(areaId);
        }

        /// <summary>解锁探索地区</summary>
        public bool UnlockArea(string areaId)
        {
            if (CurrentSave == null) return false;

            if (CurrentSave.unlockedAreas.Contains(areaId))
            {
                Debug.LogWarning($"[MapManager] 地区 {areaId} 已解锁");
                return false;
            }

            CurrentSave.unlockedAreas.Add(areaId);
            Debug.Log($"[MapManager] 解锁探索地区：{areaId}");
            return true;
        }

        #endregion

        #region 数据访问接口

        /// <summary>获取顾客池</summary>
        public CustomerPoolSO GetCustomerPool()
        {
            return _currentMap?.customerPool;
        }

        /// <summary>获取特殊顾客池</summary>
        public CustomerPoolSO GetSpecialCustomerPool()
        {
            return _currentMap?.specialCustomerPool;
        }

        /// <summary>获取订单池</summary>
        public OrderPoolSO GetOrderPool()
        {
            return _currentMap?.orderPool;
        }

        #endregion
    }
}
