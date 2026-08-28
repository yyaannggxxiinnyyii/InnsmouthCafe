using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Test
{
    /// <summary>
    /// 存档系统测试脚本。
    /// 用于验证 SaveManager、PlayerInventory、MapManager 的功能。
    /// 挂载到场景中任意 GameObject 上，按键盘触发测试。
    /// </summary>
    public class SaveSystemTest : MonoBehaviour
    {
        [Header("测试用地图配置")]
        [Tooltip("拖入一个 MapConfigSO 用于测试")]
        public MapConfigSO testMap;

        private void Update()
        {
            // 按键测试
            if (Input.GetKeyDown(KeyCode.Alpha1)) Test_CreateNewSave();
            if (Input.GetKeyDown(KeyCode.Alpha2)) Test_SaveAndLoad();
            if (Input.GetKeyDown(KeyCode.Alpha3)) Test_PlayerInventory();
            if (Input.GetKeyDown(KeyCode.Alpha4)) Test_MapManager();
            if (Input.GetKeyDown(KeyCode.Alpha5)) Test_ListAllSaves();
            if (Input.GetKeyDown(KeyCode.Alpha6)) Test_DeleteCurrentSave();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 500));
            GUILayout.Label("=== 存档系统测试 ===", GUI.skin.box);
            GUILayout.Label("按键触发测试：");
            GUILayout.Label("1 - 创建新存档");
            GUILayout.Label("2 - 保存并重新加载");
            GUILayout.Label("3 - 测试玩家库存");
            GUILayout.Label("4 - 测试地图管理器");
            GUILayout.Label("5 - 列出所有存档");
            GUILayout.Label("6 - 删除当前存档");
            GUILayout.Space(10);

            var currentSave = SaveManager.Instance.CurrentSave;
            if (currentSave != null)
            {
                GUILayout.Label($"当前存档：{currentSave.saveName}", GUI.skin.box);
                GUILayout.Label($"金币：{currentSave.money}");
                GUILayout.Label($"理智值：{currentSave.currentSanity:F1} / {currentSave.maxSanity:F1}");
                GUILayout.Label($"游戏时长：{currentSave.totalPlayTimeSeconds:F1} 秒");
                GUILayout.Label($"已解锁地图：{currentSave.unlockedMaps.Count} 个");
                GUILayout.Label($"已解锁地区：{currentSave.unlockedAreas.Count} 个");
            }
            else
            {
                GUILayout.Label("当前无存档", GUI.skin.box);
            }

            GUILayout.EndArea();
        }

        /// <summary>测试1：创建新存档</summary>
        private void Test_CreateNewSave()
        {
            Debug.Log("=== 测试1：创建新存档 ===");

            string saveName = $"测试存档_{System.DateTime.Now:HHmmss}";
            SaveData newSave = SaveManager.Instance.CreateNewSave(saveName);

            if (newSave != null)
            {
                Debug.Log($"✅ 创建成功：{newSave.saveName}");
                Debug.Log($"   存档ID：{newSave.saveId}");
                Debug.Log($"   创建时间：{System.DateTimeOffset.FromUnixTimeSeconds(newSave.createTimestamp).LocalDateTime}");
            }
            else
            {
                Debug.LogError("❌ 创建失败");
            }
        }

        /// <summary>测试2：保存并重新加载</summary>
        private void Test_SaveAndLoad()
        {
            Debug.Log("=== 测试2：保存并重新加载 ===");

            var currentSave = SaveManager.Instance.CurrentSave;
            if (currentSave == null)
            {
                Debug.LogWarning("请先创建存档（按1）");
                return;
            }

            string saveId = currentSave.saveId;
            int oldMoney = currentSave.money;

            // 修改一些数据
            currentSave.money += 100;
            Debug.Log($"修改金币：{oldMoney} → {currentSave.money}");

            // 保存
            bool saved = SaveManager.Instance.SaveCurrent();
            Debug.Log(saved ? "✅ 保存成功" : "❌ 保存失败");

            // 重新加载
            bool loaded = SaveManager.Instance.LoadAndSetCurrent(saveId);
            if (loaded)
            {
                Debug.Log($"✅ 加载成功，金币：{SaveManager.Instance.CurrentSave.money}");
            }
            else
            {
                Debug.LogError("❌ 加载失败");
            }
        }

        /// <summary>测试3：玩家库存</summary>
        private void Test_PlayerInventory()
        {
            Debug.Log("=== 测试3：玩家库存 ===");

            if (SaveManager.Instance.CurrentSave == null)
            {
                Debug.LogWarning("请先创建存档（按1）");
                return;
            }

            var inv = PlayerInventory.Instance;

            // 测试金币
            Debug.Log($"当前金币：{inv.GetMoney()}");
            inv.AddMoney(500);
            Debug.Log($"增加金币 500 → {inv.GetMoney()}");
            inv.RemoveMoney(200);
            Debug.Log($"减少金币 200 → {inv.GetMoney()}");

            // 测试材料
            inv.AddMaterial("mat_starfish", 10);
            inv.AddMaterial("mat_coral", 5);
            Debug.Log($"海星数量：{inv.GetMaterialCount("mat_starfish")}");
            Debug.Log($"珊瑚数量：{inv.GetMaterialCount("mat_coral")}");
            inv.RemoveMaterial("mat_starfish", 3);
            Debug.Log($"减少海星 3 → {inv.GetMaterialCount("mat_starfish")}");

            // 测试工具
            inv.SetToolLevel(ToolType.Hook, 1);
            inv.UpgradeTool(ToolType.Hook);
            Debug.Log($"稿子等级：{inv.GetToolLevel(ToolType.Hook)}");

            // 测试理智值
            inv.SetCurrentSanity(80f);
            Debug.Log($"理智值：{inv.GetCurrentSanity()} / {inv.GetMaxSanity()}");
            inv.RemoveSanity(20f);
            Debug.Log($"消耗理智 20 → {inv.GetCurrentSanity()}");

            Debug.Log("✅ 库存测试完成");
        }

        /// <summary>测试4：地图管理器</summary>
        private void Test_MapManager()
        {
            Debug.Log("=== 测试4：地图管理器 ===");

            if (SaveManager.Instance.CurrentSave == null)
            {
                Debug.LogWarning("请先创建存档（按1）");
                return;
            }

            if (testMap == null)
            {
                Debug.LogWarning("请在 Inspector 中拖入 testMap");
                return;
            }

            var mapMgr = MapManager.Instance;

            // 加载地图
            bool loaded = mapMgr.LoadMap(testMap);
            Debug.Log(loaded ? $"✅ 加载地图：{testMap.mapName}" : "❌ 加载失败");

            if (!loaded) return;

            // 检查初始状态
            Debug.Log($"初始金币：{PlayerInventory.Instance.GetMoney()}");
            Debug.Log($"初始理智上限：{PlayerInventory.Instance.GetMaxSanity()}");

            // 检查章节
            var currentChapter = mapMgr.GetCurrentChapter();
            if (currentChapter != null)
            {
                Debug.Log($"当前章节：{currentChapter.chapterName}");

                // 模拟完成章节目标
                SaveManager.Instance.CurrentSave.totalRevenue = 1000;
                SaveManager.Instance.CurrentSave.totalServedCustomers = 20;

                bool goalsMet = mapMgr.CheckChapterGoals(currentChapter);
                Debug.Log($"章节目标达成：{goalsMet}");

                if (goalsMet)
                {
                    mapMgr.CompleteChapter(testMap.mapId, currentChapter.chapterId);
                    Debug.Log($"✅ 完成章节：{currentChapter.chapterName}");
                }
            }
            else
            {
                Debug.Log("当前无章节或所有章节已完成");
            }

            // 检查地区
            var unlockedAreas = mapMgr.GetUnlockedAreas();
            Debug.Log($"已解锁地区：{unlockedAreas.Count} 个");
            foreach (var area in unlockedAreas)
            {
                Debug.Log($"  - {area.areaName}");
            }

            Debug.Log("✅ 地图管理器测试完成");
        }

        /// <summary>测试5：列出所有存档</summary>
        private void Test_ListAllSaves()
        {
            Debug.Log("=== 测试5：列出所有存档 ===");

            var saves = SaveManager.Instance.GetAllSaves();
            Debug.Log($"共找到 {saves.Count} 个存档：");

            foreach (var save in saves)
            {
                var lastSaveTime = System.DateTimeOffset.FromUnixTimeSeconds(save.lastSaveTimestamp).LocalDateTime;
                Debug.Log($"  - {save.saveName} (ID: {save.saveId})");
                Debug.Log($"    金币：{save.money}, 理智：{save.currentSanity:F1}/{save.maxSanity:F1}");
                Debug.Log($"    最后保存：{lastSaveTime}");
            }
        }

        /// <summary>测试6：删除当前存档</summary>
        private void Test_DeleteCurrentSave()
        {
            Debug.Log("=== 测试6：删除当前存档 ===");

            var currentSave = SaveManager.Instance.CurrentSave;
            if (currentSave == null)
            {
                Debug.LogWarning("当前无存档");
                return;
            }

            string saveId = currentSave.saveId;
            string saveName = currentSave.saveName;

            bool deleted = SaveManager.Instance.DeleteSave(saveId);
            Debug.Log(deleted ? $"✅ 已删除存档：{saveName}" : "❌ 删除失败");
        }
    }
}
