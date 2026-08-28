using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 存档管理器（SaveManager）。
    /// 负责存档的保存、加载、创建、删除。
    /// 单例模式，全局访问。
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("设置")]
        [Tooltip("是否启用自动保存")]
        [SerializeField] private bool autoSaveEnabled = true;

        [Tooltip("自动保存间隔（秒）")]
        [SerializeField] private float autoSaveInterval = 300f; // 5分钟

        private SaveData _currentSave;
        private float _autoSaveTimer = 0f;
        private float _sessionStartTime;

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // 确保存档目录存在
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            _sessionStartTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            // 自动保存
            if (autoSaveEnabled && _currentSave != null)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= autoSaveInterval)
                {
                    _autoSaveTimer = 0f;
                    SaveCurrent();
                    Debug.Log("[SaveManager] 自动保存完成");
                }
            }

            // 更新游戏时长
            if (_currentSave != null)
            {
                _currentSave.AddPlayTime(Time.deltaTime);
            }
        }

        /// <summary>获取当前存档</summary>
        public SaveData CurrentSave => _currentSave;

        /// <summary>创建新存档</summary>
        public SaveData CreateNewSave(string saveName)
        {
            _currentSave = SaveData.CreateNew(saveName);
            SaveCurrent();
            Debug.Log($"[SaveManager] 创建新存档：{saveName} (ID: {_currentSave.saveId})");
            return _currentSave;
        }

        /// <summary>保存当前存档</summary>
        public bool SaveCurrent()
        {
            if (_currentSave == null)
            {
                Debug.LogWarning("[SaveManager] 没有当前存档，无法保存");
                return false;
            }

            return SaveToFile(_currentSave);
        }

        /// <summary>保存指定存档到文件</summary>
        public bool SaveToFile(SaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("[SaveManager] 存档数据为空");
                return false;
            }

            try
            {
                saveData.UpdateSaveTimestamp();

                string json = JsonUtility.ToJson(saveData, true);
                string filePath = GetSaveFilePath(saveData.saveId);

                File.WriteAllText(filePath, json);

                Debug.Log($"[SaveManager] 存档已保存：{filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 保存失败：{e.Message}");
                return false;
            }
        }

        /// <summary>从文件加载存档</summary>
        public SaveData LoadFromFile(string saveId)
        {
            string filePath = GetSaveFilePath(saveId);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] 存档文件不存在：{filePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                Debug.Log($"[SaveManager] 存档已加载：{saveData.saveName} (ID: {saveId})");
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 加载失败：{e.Message}");
                return null;
            }
        }

        /// <summary>加载存档并设为当前存档</summary>
        public bool LoadAndSetCurrent(string saveId)
        {
            SaveData saveData = LoadFromFile(saveId);
            if (saveData != null)
            {
                _currentSave = saveData;
                _sessionStartTime = Time.realtimeSinceStartup;
                return true;
            }
            return false;
        }

        /// <summary>删除存档</summary>
        public bool DeleteSave(string saveId)
        {
            string filePath = GetSaveFilePath(saveId);

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveManager] 存档文件不存在，无法删除：{filePath}");
                return false;
            }

            try
            {
                File.Delete(filePath);
                Debug.Log($"[SaveManager] 存档已删除：{saveId}");

                if (_currentSave != null && _currentSave.saveId == saveId)
                {
                    _currentSave = null;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 删除失败：{e.Message}");
                return false;
            }
        }

        /// <summary>获取所有存档列表</summary>
        public List<SaveData> GetAllSaves()
        {
            List<SaveData> saves = new List<SaveData>();

            if (!Directory.Exists(SaveDirectory))
            {
                return saves;
            }

            string[] files = Directory.GetFiles(SaveDirectory, "*.json");

            foreach (string filePath in files)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                    saves.Add(saveData);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] 无法读取存档文件 {filePath}：{e.Message}");
                }
            }

            // 按最后保存时间排序（最新的在前）
            saves = saves.OrderByDescending(s => s.lastSaveTimestamp).ToList();

            return saves;
        }

        /// <summary>获取存档文件路径</summary>
        private string GetSaveFilePath(string saveId)
        {
            return Path.Combine(SaveDirectory, $"{saveId}.json");
        }

        /// <summary>存档是否存在</summary>
        public bool SaveExists(string saveId)
        {
            return File.Exists(GetSaveFilePath(saveId));
        }

        private void OnApplicationQuit()
        {
            // 退出时自动保存
            if (_currentSave != null)
            {
                SaveCurrent();
                Debug.Log("[SaveManager] 退出时自动保存完成");
            }
        }
    }
}
