using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 存档数据（SaveData）。
    /// 包含玩家的完整游戏进度：金币、材料、工具、地图进度、章节完成状态等。
    /// 可序列化为 JSON 保存到文件。
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        [Header("存档元信息")]
        public string saveId;                    // 存档唯一 ID
        public string saveName;                  // 存档名称（玩家可命名）
        public long createTimestamp;             // 创建时间戳（Unix 时间）
        public long lastSaveTimestamp;           // 最后保存时间戳
        public float totalPlayTimeSeconds;       // 总游戏时长（秒）

        [Header("玩家基础状态")]
        public int money;                        // 当前金币
        public float maxSanity;                  // 理智值上限
        public float currentSanity;              // 当前理智值

        [Header("材料库存")]
        // key: materialId, value: count
        public SerializableDictionary<string, int> materials = new SerializableDictionary<string, int>();

        [Header("工具等级")]
        // key: ToolType, value: level (0-5)
        public SerializableDictionary<ToolType, int> toolLevels = new SerializableDictionary<ToolType, int>();

        [Header("载具")]
        // 已拥有的载具 ID 列表
        public List<string> ownedVehicles = new List<string>();

        [Header("地图进度")]
        public string currentMapId;              // 当前所在地图
        public List<string> unlockedMaps = new List<string>();  // 已解锁的地图

        [Header("章节进度")]
        // key: mapId, value: 该地图已完成的章节 ID 列表
        public SerializableDictionary<string, List<string>> completedChapters = new SerializableDictionary<string, List<string>>();

        [Header("探索进度")]
        // 已解锁的探索地区 ID 列表
        public List<string> unlockedAreas = new List<string>();

        [Header("统计数据")]
        public int totalRevenue;                 // 累计营业额
        public int totalServedCustomers;         // 累计服务顾客数
        public int totalPerfectOrders;           // 累计完美订单数
        public int totalCollectedMaterials;      // 累计采集材料数

        /// <summary>创建新存档</summary>
        public static SaveData CreateNew(string saveName)
        {
            return new SaveData
            {
                saveId = Guid.NewGuid().ToString(),
                saveName = saveName,
                createTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                totalPlayTimeSeconds = 0f,
                money = 0,
                maxSanity = 100f,
                currentSanity = 100f,
                currentMapId = "",
                materials = new SerializableDictionary<string, int>(),
                toolLevels = new SerializableDictionary<ToolType, int>(),
                ownedVehicles = new List<string>(),
                unlockedMaps = new List<string>(),
                completedChapters = new SerializableDictionary<string, List<string>>(),
                unlockedAreas = new List<string>(),
                totalRevenue = 0,
                totalServedCustomers = 0,
                totalPerfectOrders = 0,
                totalCollectedMaterials = 0
            };
        }

        /// <summary>更新最后保存时间</summary>
        public void UpdateSaveTimestamp()
        {
            lastSaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>增加游戏时长</summary>
        public void AddPlayTime(float deltaSeconds)
        {
            totalPlayTimeSeconds += deltaSeconds;
        }
    }

    /// <summary>可序列化的 Dictionary（Unity 原生 Dictionary 不支持序列化）</summary>
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();

        private Dictionary<TKey, TValue> _dict;

        public Dictionary<TKey, TValue> ToDictionary()
        {
            if (_dict == null)
            {
                _dict = new Dictionary<TKey, TValue>();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i < values.Count)
                    {
                        _dict[keys[i]] = values[i];
                    }
                }
            }
            return _dict;
        }

        public void FromDictionary(Dictionary<TKey, TValue> dict)
        {
            keys.Clear();
            values.Clear();
            _dict = dict;

            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public TValue this[TKey key]
        {
            get => ToDictionary()[key];
            set
            {
                var dict = ToDictionary();
                dict[key] = value;
                FromDictionary(dict);
            }
        }

        public bool ContainsKey(TKey key) => ToDictionary().ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => ToDictionary().TryGetValue(key, out value);
        public void Add(TKey key, TValue value)
        {
            var dict = ToDictionary();
            dict.Add(key, value);
            FromDictionary(dict);
        }
        public void Remove(TKey key)
        {
            var dict = ToDictionary();
            dict.Remove(key);
            FromDictionary(dict);
        }
    }
}
