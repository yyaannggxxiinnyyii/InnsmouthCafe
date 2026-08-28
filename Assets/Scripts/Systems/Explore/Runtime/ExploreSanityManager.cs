using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索理智管理器（设计文档 §9.4 / §4.3）。
    /// 理智值作为探索阶段的时间/体力限制资源（0-100，可成长）。
    /// 支持两种地区判定模式：
    ///   1. 矩形范围判定（简单，适合验证）
    ///   2. Tilemap Tile 类型判定（灵活，适合不规则地形，类似饥荒）
    /// </summary>
    public class ExploreSanityManager : MonoBehaviour
    {
        [Header("理智上限")]
        [Tooltip("探索理智上限（可成长，MVP 固定 100）")]
        public float maxSanity = 100f;

        [Header("地区引用")]
        [Tooltip("本场景所有地区配置。根据地区的判定模式自动选择判定方式。")]
        public AreaConfigSO[] areas = new AreaConfigSO[0];

        [Header("Tilemap 引用（Tile 模式）")]
        [Tooltip("用于 Tile 类型判定的 Tilemap 数组。支持多层 Tilemap（不同海洋类型各占一层）。")]
        public Tilemap[] groundTilemaps = new Tilemap[0];

        [Header("默认理智消耗 [只读]")]
        [Tooltip("未命中任何地区时的回退消耗速率")]
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

            float drain = CurrentArea != null ? CurrentArea.energyDrainPerSecond : defaultDrainPerSecond;
            CurrentSanity = Mathf.Max(0f, CurrentSanity - drain * deltaTime);
        }

        /// <summary>
        /// 判定玩家所属地区。支持矩形模式和 Tilemap 模式。
        /// 期望每帧调用，也可手动调用。
        /// </summary>
        public void UpdateArea(Vector3 worldPos)
        {
            if (areas == null || areas.Length == 0)
            {
                CurrentArea = null;
                return;
            }

            // 优先检查 Tilemap 模式的地区（遍历所有 Tilemap 层）
            if (groundTilemaps != null && groundTilemaps.Length > 0)
            {
                foreach (var tilemap in groundTilemaps)
                {
                    if (tilemap == null) continue;
                    AreaConfigSO tilemapArea = GetAreaByTile(worldPos, tilemap);
                    if (tilemapArea != null)
                    {
                        CurrentArea = tilemapArea;
                        return;
                    }
                }
            }

            // 回退到矩形模式
            foreach (var area in areas)
            {
                if (area != null && area.detectionMode == AreaDetectionMode.Rectangle && area.Contains(worldPos))
                {
                    CurrentArea = area;
                    return;
                }
            }

            CurrentArea = null;
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
                if (area != null && area.detectionMode == AreaDetectionMode.Tilemap && area.ContainsTile(tile))
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
