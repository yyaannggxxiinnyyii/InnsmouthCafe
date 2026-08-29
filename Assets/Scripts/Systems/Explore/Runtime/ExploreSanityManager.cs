using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using InnsmouthCafe.Progression;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索理智管理器（设计文档 §9.4 / §4.3）。
    /// 理智值作为探索阶段的时间/体力限制资源（0-100，可成长）。
    /// 地区范围统一由场景 Tilemap 的 Tile 类型判定。
    /// </summary>
    public class ExploreSanityManager : MonoBehaviour
    {
        [Header("理智上限")]
        [Tooltip("探索理智上限（可成长，MVP 固定 100）")]
        public float maxSanity = 100f;

        [Header("地区引用")]
        [Tooltip("本场景所有地区配置。地区范围统一由 Tilemap 判定。")]
        public AreaConfigSO[] areas = new AreaConfigSO[0];

        [Header("区域 Tilemap 引用")]
        [Tooltip("用于区域判定的 Tilemap 数组。支持多个 Tilemap，每个区域可由一个或多个 Tilemap 标记。")]
        public Tilemap[] groundTilemaps = new Tilemap[0];

        [Header("默认理智消耗 [只读]")]
        [Tooltip("未命中任何区域时的消耗速率")]
        public float defaultDrainPerSecond = 0.3f;

        [Header("玩家引用")]
        [Tooltip("玩家 Transform，用于地区判定；为空时自动查找 PlayerMotor")]
        public Transform player;

        /// <summary>当前理智值。</summary>
        public float CurrentSanity { get; private set; }

        /// <summary>当前所在地区（可能为 null，表示在未配置区）。</summary>
        public AreaConfigSO CurrentArea { get; private set; }

        /// <summary>理智是否已耗尽。</summary>
        public bool IsDepleted => CurrentSanity <= 0f;

        private void Awake()
        {
            CurrentSanity = maxSanity;
        }

        private void Update()
        {
            // 每帧按玩家位置持续消耗理智（地区判定 + 消耗速率）
            if (player == null)
            {
                var motor = FindObjectOfType<PlayerMotor>();
                if (motor != null) player = motor.transform;
            }

            if (player != null)
            {
                Tick(player.position, Time.deltaTime);
            }
        }

        /// <summary>重置理智到上限。探索开始时调用。</summary>
        public void ResetSanity()
        {
            CurrentSanity = maxSanity;
        }

        /// <summary>
        /// 每帧更新：判定当前地区并按其消耗速率扣除理智。
        /// 需在 Update/LateUpdate 或由外部驱动。返回本帧剩余理智。
        /// </summary>
        public void Tick(Vector3 playerWorldPos, float deltaTime)
        {
            UpdateArea(playerWorldPos);

            float drain = CurrentArea != null
                ? CurrentArea.EnergyDrainPerSecond
                : defaultDrainPerSecond;
            CurrentSanity = Mathf.Max(0f, CurrentSanity - drain * deltaTime);
        }

        /// <summary>
        /// 判定玩家所属地区。区域范围只由 Tilemap 中的 Tile 决定。
        /// 期望每帧调用，也可手动调用。
        /// </summary>
        public void UpdateArea(Vector3 worldPos)
        {
            AreaConfigSO previousArea = CurrentArea;
            if (areas == null || areas.Length == 0)
            {
                CurrentArea = null;
                return;
            }

            if (groundTilemaps != null && groundTilemaps.Length > 0)
            {
                foreach (var tilemap in groundTilemaps)
                {
                    if (tilemap == null) continue;
                    AreaConfigSO tilemapArea = GetAreaByTile(worldPos, tilemap);
                    if (tilemapArea != null)
                    {
                        CurrentArea = tilemapArea;
                        RecordAreaVisit(previousArea);
                        return;
                    }
                }
            }

            CurrentArea = null;
        }

        /// <summary>
        /// 在玩家首次进入一个区域时记录区域访问任务事件。
        /// </summary>
        private void RecordAreaVisit(AreaConfigSO previousArea)
        {
            if (CurrentArea == null || CurrentArea == previousArea)
            {
                return;
            }

            if (!string.IsNullOrEmpty(CurrentArea.AreaId))
            {
                TaskProgressService.Instance.RecordAreaVisited(CurrentArea.AreaId);
            }
        }

        /// <summary>根据玩家位置查询指定 Tilemap 的 Tile，返回对应的地区。</summary>
        private AreaConfigSO GetAreaByTile(Vector3 worldPos, Tilemap tilemap)
        {
            if (tilemap == null) return null;

            // 世界坐标 → Tilemap Cell 坐标
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            TileBase tile = tilemap.GetTile(cellPos);

            if (tile == null) return null;

            // 查找哪个地区包含此 Tile
            foreach (var area in areas)
            {
                if (area != null && area.ContainsTile(tile))
                {
                    return area;
                }
            }

            return null;
        }

        /// <summary>扣除一笔采集消耗理智。返回是否成功（理智不足则不外扣）。</summary>
        public bool TrySpend(float amount)
        {
            if (CurrentSanity <= 0f) return false;
            CurrentSanity = Mathf.Max(0f, CurrentSanity - amount);
            return true;
        }

        /// <summary>按比例补回理智（预留：装备加成/道具等）。</summary>
        public void Recover(float amount)
        {
            CurrentSanity = Mathf.Min(maxSanity, CurrentSanity + amount);
        }
    }
}
