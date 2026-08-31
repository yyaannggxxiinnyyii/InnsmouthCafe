using System.Collections.Generic;
using UnityEngine;
using InnsmouthCafe.Data;
using InnsmouthCafe.Persistence;
using InnsmouthCafe.Shop;

namespace InnsmouthCafe.Decoration
{
    /// <summary>
    /// 装饰管理器。
    /// 负责管理装饰物的安装/卸载、计算美观度、管理装饰槽位。
    /// </summary>
    public class DecorationManager : Singleton<DecorationManager>
    {
        [Header("装饰物配置")]
        [Tooltip("所有可用的装饰物配置")]
        [SerializeField] private List<DecorationSO> _allDecorations = new List<DecorationSO>();

        [Header("场景槽位")]
        [Tooltip("场景中的普通装饰槽位（提前摆好的Transform）")]
        [SerializeField] private List<Transform> _normalSlots = new List<Transform>();

        [Tooltip("场景中的Boss装饰槽位（提前摆好的Transform）")]
        [SerializeField] private List<Transform> _bossSlots = new List<Transform>();

        [Header("调试信息 - 只读")]
        [Tooltip("当前总美观度")]
        [SerializeField] private int _currentBeautyScore = 0;

        [Tooltip("已安装的装饰物ID列表")]
        [SerializeField] private List<string> _installedDecorationIds = new List<string>();

        [Tooltip("已安装装饰物提供的加成汇总")]
        [SerializeField] private List<DecorationBonusEntry> _activeBonuses = new List<DecorationBonusEntry>();

        /// <summary>
        /// 装饰物安装/卸载事件。
        /// </summary>
        public event System.Action OnDecorationsChanged;

        /// <summary>
        /// 当前总美观度。
        /// </summary>
        public int CurrentBeautyScore => _currentBeautyScore;

        /// <summary>
        /// 当前已安装的装饰物ID列表（只读）。
        /// </summary>
        public IReadOnlyList<string> InstalledDecorationIds => _installedDecorationIds;

        /// <summary>
        /// 当前生效的加成汇总（只读）。
        /// </summary>
        public IReadOnlyList<DecorationBonusEntry> ActiveBonuses => _activeBonuses;

        /// <summary>
        /// 已安装的装饰物实例（运行时生成的GameObject）。
        /// </summary>
        private Dictionary<string, GameObject> _installedObjects = new Dictionary<string, GameObject>();

        private void Start()
        {
            // 从存档加载已安装的装饰物
            LoadInstalledDecorations();
        }

        /// <summary>
        /// 从存档加载已安装的装饰物。
        /// </summary>
        private void LoadInstalledDecorations()
        {
            if (SaveSlotService.Instance?.CurrentSave == null)
            {
                Debug.LogWarning("[Decoration] 存档不存在，无法加载装饰物");
                return;
            }

            var saveData = SaveSlotService.Instance.CurrentSave;
            _installedDecorationIds = new List<string>(saveData.installedDecorationIds ?? new List<string>());
            _currentBeautyScore = saveData.beautyScore;

            Debug.Log($"[Decoration] 加载装饰物：{_installedDecorationIds.Count} 个，美观度：{_currentBeautyScore}");

            // 重新汇总加成
            RecalculateBonuses();

            // 在场景中实例化已安装的装饰物
            InstantiateInstalledDecorations();
        }

        /// <summary>
        /// 重新汇总所有已安装装饰物提供的加成。
        /// 同类型加成会合并累加，结果写入 _activeBonuses 供其他系统读取。
        /// </summary>
        private void RecalculateBonuses()
        {
            _activeBonuses.Clear();

            foreach (string decorationId in _installedDecorationIds)
            {
                DecorationSO decoration = GetDecorationById(decorationId);
                if (decoration == null || decoration.bonuses == null)
                {
                    continue;
                }

                foreach (DecorationBonusEntry entry in decoration.bonuses)
                {
                    if (entry == null || Mathf.Approximately(entry.bonusValue, 0f))
                    {
                        continue;
                    }

                    AccumulateBonus(entry.bonusType, entry.bonusValue);
                }
            }

            if (_activeBonuses.Count > 0)
            {
                Debug.Log($"[Decoration] 加成汇总完成：{FormatBonusSummary()}");
            }
        }

        /// <summary>
        /// 将单条加成累加进汇总列表。
        /// </summary>
        /// <param name="type">加成类型。</param>
        /// <param name="value">加成数值。</param>
        private void AccumulateBonus(DecorationBonusType type, float value)
        {
            foreach (DecorationBonusEntry existing in _activeBonuses)
            {
                if (existing.bonusType == type)
                {
                    existing.bonusValue += value;
                    return;
                }
            }

            _activeBonuses.Add(new DecorationBonusEntry
            {
                bonusType = type,
                bonusValue = value
            });
        }

        /// <summary>
        /// 生成加成汇总的日志文本。
        /// </summary>
        /// <returns>可读的加成描述。</returns>
        private string FormatBonusSummary()
        {
            var parts = new List<string>();
            foreach (DecorationBonusEntry entry in _activeBonuses)
            {
                parts.Add($"{entry.bonusType}={entry.bonusValue}");
            }

            return string.Join("，", parts);
        }

        /// <summary>
        /// 获取指定类型的加成总值，供其他系统应用装饰加成。
        /// </summary>
        /// <param name="type">加成类型。</param>
        /// <returns>加成总值；没有该类型加成时返回 0。</returns>
        public float GetTotalBonus(DecorationBonusType type)
        {
            foreach (DecorationBonusEntry entry in _activeBonuses)
            {
                if (entry.bonusType == type)
                {
                    return entry.bonusValue;
                }
            }

            return 0f;
        }

        /// <summary>
        /// 获取当前已安装的装饰物配置列表。
        /// </summary>
        /// <returns>已安装的装饰物配置；无效ID会被跳过。</returns>
        public List<DecorationSO> GetInstalledDecorations()
        {
            var result = new List<DecorationSO>();
            foreach (string decorationId in _installedDecorationIds)
            {
                DecorationSO decoration = GetDecorationById(decorationId);
                if (decoration != null)
                {
                    result.Add(decoration);
                }
            }

            return result;
        }

        /// <summary>
        /// 在场景中实例化已安装的装饰物。
        /// </summary>
        private void InstantiateInstalledDecorations()
        {
            // 清空旧的实例
            foreach (var obj in _installedObjects.Values)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _installedObjects.Clear();

            int normalSlotIndex = 0;
            int bossSlotIndex = 0;

            // 遍历已安装的装饰物
            foreach (var decorationId in _installedDecorationIds)
            {
                var decoration = GetDecorationById(decorationId);
                if (decoration == null || decoration.prefab == null)
                {
                    continue;
                }

                Transform slot = null;

                // 根据类型选择槽位
                if (decoration.decorationType == DecorationType.Normal)
                {
                    if (normalSlotIndex < _normalSlots.Count)
                    {
                        slot = _normalSlots[normalSlotIndex];
                        normalSlotIndex++;
                    }
                }
                else if (decoration.decorationType == DecorationType.Boss)
                {
                    if (bossSlotIndex < _bossSlots.Count)
                    {
                        slot = _bossSlots[bossSlotIndex];
                        bossSlotIndex++;
                    }
                }

                if (slot != null)
                {
                    // 实例化装饰物
                    GameObject obj = Instantiate(decoration.prefab, slot.position, slot.rotation, slot);
                    _installedObjects[decorationId] = obj;
                }
            }

            Debug.Log($"[Decoration] 实例化装饰物完成：{_installedObjects.Count} 个");
        }

        /// <summary>
        /// 根据ID获取装饰物配置。
        /// </summary>
        /// <param name="decorationId">装饰物ID。</param>
        /// <returns>装饰物配置，找不到返回null。</returns>
        public DecorationSO GetDecorationById(string decorationId)
        {
            if (_allDecorations == null)
            {
                return null;
            }

            foreach (var decoration in _allDecorations)
            {
                if (decoration != null && decoration.decorationId == decorationId)
                {
                    return decoration;
                }
            }

            return null;
        }

        /// <summary>
        /// 安装装饰物。
        /// </summary>
        /// <param name="decorationId">装饰物ID。</param>
        /// <returns>true表示安装成功，false表示安装失败。</returns>
        public bool InstallDecoration(string decorationId)
        {
            var decoration = GetDecorationById(decorationId);
            if (decoration == null)
            {
                Debug.LogError($"[Decoration] 找不到装饰物：{decorationId}");
                return false;
            }

            // 检查是否已安装
            if (_installedDecorationIds.Contains(decorationId))
            {
                Debug.LogWarning($"[Decoration] 装饰物已安装：{decoration.decorationName}");
                return false;
            }

            // 检查槽位是否足够
            if (!HasAvailableSlot(decoration.decorationType))
            {
                Debug.LogWarning($"[Decoration] 没有可用的槽位：{decoration.decorationType}");
                return false;
            }

            // 安装装饰物
            _installedDecorationIds.Add(decorationId);
            _currentBeautyScore += decoration.beautyValue;

            // 重新汇总加成
            RecalculateBonuses();

            // 保存到存档
            SaveToSaveData();

            // 在场景中实例化
            InstantiateInstalledDecorations();

            Debug.Log($"[Decoration] 安装装饰物：{decoration.decorationName}，美观度 +{decoration.beautyValue}，总美观度：{_currentBeautyScore}");

            // 触发事件
            OnDecorationsChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// 卸载装饰物。
        /// </summary>
        /// <param name="decorationId">装饰物ID。</param>
        /// <returns>true表示卸载成功，false表示卸载失败。</returns>
        public bool UninstallDecoration(string decorationId)
        {
            var decoration = GetDecorationById(decorationId);
            if (decoration == null)
            {
                Debug.LogError($"[Decoration] 找不到装饰物：{decorationId}");
                return false;
            }

            // 检查是否已安装
            if (!_installedDecorationIds.Contains(decorationId))
            {
                Debug.LogWarning($"[Decoration] 装饰物未安装：{decoration.decorationName}");
                return false;
            }

            // 卸载装饰物
            _installedDecorationIds.Remove(decorationId);
            _currentBeautyScore -= decoration.beautyValue;
            _currentBeautyScore = Mathf.Max(0, _currentBeautyScore);

            // 重新汇总加成
            RecalculateBonuses();

            // 保存到存档
            SaveToSaveData();

            // 重新实例化
            InstantiateInstalledDecorations();

            Debug.Log($"[Decoration] 卸载装饰物：{decoration.decorationName}，美观度 -{decoration.beautyValue}，总美观度：{_currentBeautyScore}");

            // 触发事件
            OnDecorationsChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// 检查是否有可用的槽位。
        /// </summary>
        /// <param name="type">装饰物类型。</param>
        /// <returns>true表示有可用槽位。</returns>
        private bool HasAvailableSlot(DecorationType type)
        {
            int usedSlots = 0;
            int maxSlots = 0;

            if (type == DecorationType.Normal)
            {
                // 统计已安装的普通装饰物数量
                foreach (var id in _installedDecorationIds)
                {
                    var deco = GetDecorationById(id);
                    if (deco != null && deco.decorationType == DecorationType.Normal)
                    {
                        usedSlots++;
                    }
                }

                // 从店铺等级获取最大槽位数
                maxSlots = ShopUpgradeManager.Instance != null
                    ? ShopUpgradeManager.Instance.GetNormalDecorationSlots()
                    : 3;
            }
            else if (type == DecorationType.Boss)
            {
                // 统计已安装的Boss装饰物数量
                foreach (var id in _installedDecorationIds)
                {
                    var deco = GetDecorationById(id);
                    if (deco != null && deco.decorationType == DecorationType.Boss)
                    {
                        usedSlots++;
                    }
                }

                // 从店铺等级获取最大槽位数
                maxSlots = ShopUpgradeManager.Instance != null
                    ? ShopUpgradeManager.Instance.GetBossDecorationSlots()
                    : 0;
            }

            return usedSlots < maxSlots;
        }

        /// <summary>
        /// 保存到存档。
        /// </summary>
        private void SaveToSaveData()
        {
            if (SaveSlotService.Instance?.CurrentSave == null)
            {
                return;
            }

            var saveData = SaveSlotService.Instance.CurrentSave;
            saveData.installedDecorationIds = new List<string>(_installedDecorationIds);
            saveData.beautyScore = _currentBeautyScore;

            SaveSlotService.Instance.SaveCurrent();
        }

        /// <summary>
        /// 获取美观度对应的评分上限。
        /// </summary>
        /// <returns>评分上限（百分制）。</returns>
        public float GetBeautyScoreCap()
        {
            if (_currentBeautyScore >= 500)
            {
                return 100f; // 5.0星上限
            }
            else if (_currentBeautyScore >= 300)
            {
                return 90f; // 4.5星上限
            }
            else
            {
                return 80f; // 4.0星上限
            }
        }
    }
}
