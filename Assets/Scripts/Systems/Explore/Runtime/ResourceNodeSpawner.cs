using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 程序化资源生成器（设计文档 §5.5，两级数量结构）。
    /// 每个 SpawnEntry = 一个资源类型（ResourceNodeConfig 模板）
    ///   ├─ 资源点数量（nodeCount）：生成几处
    ///   └─ 每资源点开采点数量（perNodeCount）：每处几个
    /// 生成点总数 = nodeCount × perNodeCount。每次调用 Generate() 重新随机撒点。
    /// 支持两种生成模式：
    ///   1. 矩形范围随机（旧方案，快速验证）
    ///   2. 从 Tilemap 区域内生成（推荐，符合饥荒风格）
    /// </summary>
    public class ResourceNodeSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("资源类型模板（自含采集条件/产出）")]
            public ResourceNodeConfig config;

            [Tooltip("资源点数量（生成几处）")]
            [Min(1)]
            public int nodeCount = 1;

            [Tooltip("每资源点开采点数量（每处几个）")]
            [Min(1)]
            public int perNodeCount = 1;

            [Tooltip("所属地区（用于查找对应的 Tilemap，在该 Tilemap 画的区域内生成）")]
            public AreaConfigSO belongArea;
        }

        [Header("生成配置")]
        [Tooltip("资源生成表（每个条目一种资源类型，两级数量）")]
        public SpawnEntry[] spawnEntries = new SpawnEntry[0];

        [Header("生成模式")]
        [Tooltip("从 Tilemap 区域内生成（推荐）还是矩形范围随机")]
        public SpawnMode spawnMode = SpawnMode.FromTilemap;

        [Header("模式 1：矩形范围（旧方案）")]
        [Tooltip("生成点散落的矩形半宽/半高")]
        public Vector2 spawnSize = new Vector2(14f, 10f);

        [Tooltip("生成点矩形中心")]
        public Vector2 spawnCenter = Vector2.zero;

        [Header("共用参数")]
        [Tooltip("子节点在单处生成点附近的散布半径")]
        public float clusterRadius = 0.8f;

        [Tooltip("生成节点时是否重置剔除的旧节点")]
        public bool clearOnGenerate = true;

        [Header("引用")]
        [Tooltip("生成的节点挂到该父节点下；为空则挂到本对象")]
        public Transform nodeParent;

        [Tooltip("SanityManager 引用（用于获取 Tilemap 数组，从中查找对应区域）")]
        public ExploreSanityManager sanityManager;

        private readonly List<ResourceNode> _nodes = new List<ResourceNode>();

        /// <summary>当前场景中已生成的采集点。</summary>
        public IReadOnlyList<ResourceNode> Nodes => _nodes;

        /// <summary>每个 Area 对应的 Tilemap（缓存，避免重复查找）</summary>
        private Dictionary<AreaConfigSO, Tilemap> _areaTilemapCache = new Dictionary<AreaConfigSO, Tilemap>();

        private void Start()
        {
            // 自动查找 SanityManager
            if (sanityManager == null)
            {
                sanityManager = FindObjectOfType<ExploreSanityManager>();
            }

            Generate();
        }

        /// <summary>
        /// 按生成表随机撒点。清理旧节点后重新生成。
        /// 每次探索开始时调用，即可得到与本轮不同的资源分布。
        /// </summary>
        public void Generate()
        {
            if (clearOnGenerate)
            {
                Clear();
            }

            if (spawnEntries == null) return;

            Transform parent = nodeParent != null ? nodeParent : transform;

            // 构建 Area → Tilemap 缓存
            BuildAreaTilemapCache();

            foreach (var entry in spawnEntries)
            {
                if (entry == null || entry.config == null || entry.nodeCount <= 0) continue;

                for (int i = 0; i < entry.nodeCount; i++)
                {
                    // 根据模式选择生成位置
                    Vector2 center = Vector2.zero;
                    bool validPos = false;

                    if (spawnMode == SpawnMode.FromTilemap && entry.belongArea != null)
                    {
                        // 从 Tilemap 区域内随机选位置
                        center = GetRandomPositionInArea(entry.belongArea);
                        validPos = center != Vector2.zero; // Vector2.zero 表示失败
                    }

                    if (!validPos)
                    {
                        // 回退到矩形随机
                        center = randInRect();
                    }

                    // 每处散落 perNodeCount 个子开采点
                    int subs = Mathf.Max(1, entry.perNodeCount);
                    for (int j = 0; j < subs; j++)
                    {
                        Vector2 offset = Random.insideUnitCircle * clusterRadius;
                        SpawnOne(entry.config, entry.belongArea, center + offset, parent);
                    }
                }
            }
        }

        /// <summary>清理全部已生成的采集点。</summary>
        public void Clear()
        {
            foreach (var node in _nodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            _nodes.Clear();
        }

        /// <summary>在指定世界坐标生成单个采集点实例。</summary>
        public ResourceNode SpawnOne(ResourceNodeConfig config, AreaConfigSO belongArea, Vector2 position, Transform parent)
        {
            var go = new GameObject($"[{config.resourceId}] {config.resourceName}");
            go.transform.SetParent(parent, true);
            // 修正：XZ 平面，position.x → world.x, position.y → world.z
            go.transform.position = new Vector3(position.x, 0f, position.y);

            var node = go.AddComponent<ResourceNode>();
            node.Initialize(config, belongArea);
            _nodes.Add(node);
            return node;
        }

        /// <summary>在矩形范围内取一个随机点。</summary>
        private Vector2 randInRect()
        {
            float x = spawnCenter.x + Random.Range(-spawnSize.x, spawnSize.x);
            float y = spawnCenter.y + Random.Range(-spawnSize.y, spawnSize.y);
            return new Vector2(x, y);
        }

        /// <summary>构建 Area → Tilemap 的映射缓存</summary>
        private void BuildAreaTilemapCache()
        {
            _areaTilemapCache.Clear();

            if (sanityManager == null || sanityManager.groundTilemaps == null) return;

            // 遍历所有 Tilemap，找出每个 Area 对应的 Tilemap
            foreach (var tilemap in sanityManager.groundTilemaps)
            {
                if (tilemap == null) continue;

                // 从这个 Tilemap 随机取一个有 Tile 的格子，查它属于哪个 Area
                var cellPos = GetRandomCellInTilemap(tilemap);
                if (cellPos == null) continue;

                var tile = tilemap.GetTile(cellPos.Value);
                if (tile == null) continue;

                // 查找包含此 Tile 的 Area
                foreach (var area in sanityManager.areas)
                {
                    if (area != null && area.detectionMode == AreaDetectionMode.Tilemap && area.ContainsTile(tile))
                    {
                        _areaTilemapCache[area] = tilemap;
                        break;
                    }
                }
            }
        }

        /// <summary>在指定区域（Tilemap）内随机选一个位置</summary>
        private Vector2 GetRandomPositionInArea(AreaConfigSO area)
        {
            if (!_areaTilemapCache.TryGetValue(area, out Tilemap tilemap) || tilemap == null)
            {
                return Vector2.zero; // 失败
            }

            // 收集所有有 Tile 的格子
            List<Vector3Int> validCells = new List<Vector3Int>();
            BoundsInt bounds = tilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cellPos))
                    {
                        validCells.Add(cellPos);
                    }
                }
            }

            if (validCells.Count == 0)
            {
                return Vector2.zero; // 没有有效格子
            }

            // 随机选一个格子
            Vector3Int randomCell = validCells[Random.Range(0, validCells.Count)];

            // 转换为世界坐标，并在格子内随机偏移
            Vector3 cellCenter = tilemap.GetCellCenterWorld(randomCell);
            Vector2 randomOffset = Random.insideUnitCircle * 0.4f; // 格子内随机偏移

            return new Vector2(cellCenter.x + randomOffset.x, cellCenter.z + randomOffset.y);
        }

        /// <summary>从 Tilemap 中随机取一个有 Tile 的格子（用于缓存构建）</summary>
        private Vector3Int? GetRandomCellInTilemap(Tilemap tilemap)
        {
            BoundsInt bounds = tilemap.cellBounds;
            List<Vector3Int> cells = new List<Vector3Int>();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cellPos))
                    {
                        cells.Add(cellPos);
                    }
                }
            }

            if (cells.Count == 0) return null;
            return cells[Random.Range(0, cells.Count)];
        }
    }

    /// <summary>资源生成模式</summary>
    public enum SpawnMode
    {
        Rectangle = 0,      // 矩形范围随机（旧方案）
        FromTilemap = 1     // 从 Tilemap 区域内生成（推荐）
    }
}
