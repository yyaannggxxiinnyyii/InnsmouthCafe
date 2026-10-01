using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

namespace InnsmouthCafe.Persistence
{
    /// <summary>
    /// 管理三槽局内存档与全局图鉴文件。
    /// 此服务独立于旧存档原型，后续由主菜单和游戏流程显式接入。
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveSlotService : MonoBehaviour
    {
        public const int MaxSaveSlots = 3;

        private const string SaveDirectoryName = "Saves";
        private const string SaveFileFormat = "save_slot_{0}.json";

        private static SaveSlotService _instance;

        private GameSaveData _currentSave;
        private int _currentSlotIndex = -1;

        /// <summary>
        /// 获取全局唯一的存档服务实例；首次访问时自动创建运行时对象。
        /// </summary>
        public static SaveSlotService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(SaveSlotService));
                    _instance = serviceObject.AddComponent<SaveSlotService>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 获取当前已加载的局内存档；未加载时返回空。
        /// </summary>
        public GameSaveData CurrentSave => _currentSave;

        /// <summary>
        /// 获取当前已加载的存档槽位；未加载时为 -1。
        /// </summary>
        public int CurrentSlotIndex => _currentSlotIndex;

        private string SaveDirectoryPath =>
            Path.Combine(Application.persistentDataPath, SaveDirectoryName);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSaveDirectory();
        }

        /// <summary>
        /// 创建指定槽位的新存档并设为当前存档。
        /// </summary>
        public bool TryCreateNewSave(int slotIndex, string saveName, out GameSaveData saveData)
        {
            saveData = null;
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning($"[SaveSlot] 无效的存档槽位：{slotIndex}");
                return false;
            }

            if (HasSaveInSlot(slotIndex))
            {
                Debug.LogWarning($"[SaveSlot] 槽位 {slotIndex} 已存在存档，拒绝覆盖创建。");
                return false;
            }

            string normalizedSaveName = NormalizeSaveName(saveName, slotIndex);
            saveData = GameSaveData.CreateNew(slotIndex, normalizedSaveName);
            _currentSave = saveData;
            _currentSlotIndex = slotIndex;

            if (SaveCurrent())
            {
                return true;
            }

            _currentSave = null;
            _currentSlotIndex = -1;
            saveData = null;
            return false;
        }

        /// <summary>
        /// 从指定槽位加载存档并设为当前存档。
        /// </summary>
        public bool TryLoadSave(int slotIndex, out GameSaveData saveData)
        {
            saveData = null;
            if (!TryReadSave(slotIndex, out GameSaveData loadedSave))
            {
                return false;
            }

            loadedSave.slotIndex = slotIndex;
            loadedSave.lastPlayedAtUtc = DateTime.UtcNow.ToString("O");
            _currentSave = loadedSave;
            _currentSlotIndex = slotIndex;
            saveData = loadedSave;
            return true;
        }

        /// <summary>
        /// 加载最近一次游玩的存档，用于主菜单的快速继续入口。
        /// </summary>
        public bool TryLoadMostRecentSave(out GameSaveData saveData)
        {
            saveData = null;
            int mostRecentSlotIndex = -1;
            DateTime mostRecentTime = DateTime.MinValue;

            for (int slotIndex = 0; slotIndex < MaxSaveSlots; slotIndex++)
            {
                if (!TryReadSave(slotIndex, out GameSaveData candidateSave))
                {
                    continue;
                }

                DateTime candidateTime = DateTime.MinValue;
                DateTime.TryParse(
                    candidateSave.lastPlayedAtUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out candidateTime);

                if (mostRecentSlotIndex < 0 || candidateTime > mostRecentTime)
                {
                    mostRecentSlotIndex = slotIndex;
                    mostRecentTime = candidateTime;
                }
            }

            return mostRecentSlotIndex >= 0
                && TryLoadSave(mostRecentSlotIndex, out saveData);
        }

        /// <summary>
        /// 保存当前已加载的局内存档。
        /// </summary>
        public bool SaveCurrent()
        {
            if (_currentSave == null || !IsValidSlotIndex(_currentSlotIndex))
            {
                Debug.LogWarning("[SaveSlot] 当前没有可保存的存档。");
                return false;
            }

            _currentSave.slotIndex = _currentSlotIndex;
            _currentSave.version = GameSaveData.CurrentVersion;
            _currentSave.lastPlayedAtUtc = DateTime.UtcNow.ToString("O");
            return TryWriteJson(GetSaveFilePath(_currentSlotIndex), _currentSave);
        }

        /// <summary>
        /// 删除指定槽位的局内存档。
        /// </summary>
        public bool DeleteSave(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning($"[SaveSlot] 无效的存档槽位：{slotIndex}");
                return false;
            }

            string filePath = GetSaveFilePath(slotIndex);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath);
                if (_currentSlotIndex == slotIndex)
                {
                    _currentSave = null;
                    _currentSlotIndex = -1;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSlot] 删除槽位 {slotIndex} 失败：{exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取固定三槽的预览数据，损坏文件会标记为不可用而不会阻断其他槽位。
        /// </summary>
        public List<SaveSlotPreviewData> GetAllSlotPreviews()
        {
            var previews = new List<SaveSlotPreviewData>(MaxSaveSlots);
            for (int slotIndex = 0; slotIndex < MaxSaveSlots; slotIndex++)
            {
                previews.Add(GetSlotPreview(slotIndex));
            }

            return previews;
        }

        /// <summary>
        /// 获取指定槽位的预览数据。
        /// </summary>
        public SaveSlotPreviewData GetSlotPreview(int slotIndex)
        {
            var preview = new SaveSlotPreviewData
            {
                slotIndex = slotIndex
            };

            if (!IsValidSlotIndex(slotIndex) || !HasSaveInSlot(slotIndex))
            {
                return preview;
            }

            if (!TryReadSave(slotIndex, out GameSaveData saveData))
            {
                preview.isCorrupted = true;
                return preview;
            }

            preview.hasSave = true;
            preview.saveName = saveData.saveName;
            preview.lastPlayedAtUtc = saveData.lastPlayedAtUtc;
            preview.currentDay = saveData.currentDay;
            preview.currentMapId = saveData.currentMapId;
            preview.totalAssets = saveData.totalAssets;
            return preview;
        }

        /// <summary>
        /// 判断指定槽位是否存在局内存档文件。
        /// </summary>
        public bool HasSaveInSlot(int slotIndex)
        {
            return IsValidSlotIndex(slotIndex) && File.Exists(GetSaveFilePath(slotIndex));
        }

        /// <summary>
        /// 判断任意槽位中是否存在局内存档。
        /// </summary>
        public bool HasAnySave()
        {
            for (int slotIndex = 0; slotIndex < MaxSaveSlots; slotIndex++)
            {
                if (TryReadSave(slotIndex, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnApplicationQuit()
        {
            SaveCurrent();
        }

        /// <summary>
        /// 从指定槽位文件读取完整局内存档。
        /// </summary>
        private bool TryReadSave(int slotIndex, out GameSaveData saveData)
        {
            saveData = null;
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            string filePath = GetSaveFilePath(slotIndex);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
                if (saveData == null)
                {
                    Debug.LogWarning($"[SaveSlot] 槽位 {slotIndex} 的存档内容为空。");
                    return false;
                }

                NormalizeSaveData(saveData);

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSlot] 读取槽位 {slotIndex} 失败：{exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 原子替换目标 JSON 文件，避免异常中断时直接破坏旧存档。
        /// </summary>
        private bool TryWriteJson<T>(string filePath, T data)
        {
            try
            {
                EnsureSaveDirectory();

                string temporaryFilePath = filePath + ".tmp";
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(temporaryFilePath, json);
                File.Copy(temporaryFilePath, filePath, true);
                File.Delete(temporaryFilePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSlot] 保存文件失败：{exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取指定槽位对应的固定文件路径。
        /// </summary>
        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(SaveDirectoryPath, string.Format(SaveFileFormat, slotIndex));
        }

        /// <summary>
        /// 确保持久化目录在首次读写前存在。
        /// </summary>
        private void EnsureSaveDirectory()
        {
            if (!Directory.Exists(SaveDirectoryPath))
            {
                Directory.CreateDirectory(SaveDirectoryPath);
            }
        }

        /// <summary>
        /// 判断槽位索引是否位于固定三槽范围内。
        /// </summary>
        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxSaveSlots;
        }

        /// <summary>
        /// 补齐存档版本新增字段的默认值，确保旧版新存档可以继续使用。
        /// </summary>
        private void NormalizeSaveData(GameSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            if (saveData.currentDay < 1)
            {
                saveData.currentDay = 1;
            }

            if (!Enum.IsDefined(typeof(DayPhase), saveData.currentPhase))
            {
                saveData.currentPhase = DayPhase.Exploration;
            }

            if (saveData.ingredientInventory == null)
            {
                saveData.ingredientInventory = new List<IngredientInventoryEntryData>();
            }

            if (saveData.unlockedLiquidIds == null)
            {
                saveData.unlockedLiquidIds = new List<string>();
            }

            if (saveData.unlockedToppingIds == null)
            {
                saveData.unlockedToppingIds = new List<string>();
            }

            if (saveData.visitedAreaIds == null)
            {
                saveData.visitedAreaIds = new List<string>();
            }

            if (saveData.completedStoryIds == null)
            {
                saveData.completedStoryIds = new List<string>();
            }

            if (saveData.taskProgresses == null)
            {
                saveData.taskProgresses = new List<TaskProgressData>();
            }

            if (saveData.specialGuestProgresses == null)
            {
                saveData.specialGuestProgresses = new List<SpecialGuestProgressData>();
            }

            if (saveData.servedCustomerCounts == null)
            {
                saveData.servedCustomerCounts = new List<IdCountData>();
            }

            if (saveData.collectedMaterialCounts == null)
            {
                saveData.collectedMaterialCounts = new List<IdCountData>();
            }

        }

        /// <summary>
        /// 规范化存档名称，保证空名称时仍有可展示的默认名称。
        /// </summary>
        private string NormalizeSaveName(string saveName, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(saveName))
            {
                return $"我的咖啡馆 {slotIndex + 1}";
            }

            return saveName.Trim();
        }
    }
}
