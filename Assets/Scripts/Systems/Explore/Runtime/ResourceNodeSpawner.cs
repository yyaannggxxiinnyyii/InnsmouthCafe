using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 程序化资源生成器（设计文档 §5.5，两级数量结构）。
    /// 每个 SpawnEntry = 一个资源类型、生成处数和每处资源点数量。
    /// 所有资源点都从所属区域的 Tilemap 有效格子中生成。
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

            [Tooltip("可生成区域（资源会从其中一个有效区域的 Tilemap 格子内生成）")]
            public AreaConfigSO[] belongAreas = new AreaConfigSO[0];
        }

        /// <summary>区域 Tilemap 中的有效格子。</summary>
        private struct AreaCell
        {
            public Tilemap tilemap;
            public Vector3Int position;
        }

        [Header("生成配置")]
        [Tooltip("资源生成表（每个条目一种资源类型，两级数量）")]
        public SpawnEntry[] spawnEntries = new SpawnEntry[0];

        [Header("生成参数")]
        [Tooltip("子节点在单处生成点附近的散布半径")]
        public float clusterRadius = 0.8f;

        [Tooltip("生成节点时是否重置剔除的旧节点")]
        public bool clearOnGenerate = true;

        [Header("引用")]
        [Tooltip("生成的节点挂到该父节点下；为空则挂到本对象")]
        public Transform nodeParent;

        [Tooltip("SanityManager 引用（用于获取区域 Tilemap）")]
        public ExploreSanityManager sanityManager;

        private readonly List<ResourceNode> _nodes = new List<ResourceNode>();
        private readonly Dictionary<AreaConfigSO, List<Tilemap>> _areaTilemapCache =
            new Dictionary<AreaConfigSO, List<Tilemap>>();

        /// <summary>当前场景中已生成的采集点。</summary>
        public IReadOnlyList<ResourceNode> Nodes => _nodes;

        private void Start()
        {
            if (sanityManager == null)
            {
                sanityManager = FindObjectOfType<ExploreSanityManager>();
            }

            Generate();
        }

        /// <summary>
        /// 按生成表在区域 Tilemap 内随机撒点。
        /// 区域未配置 Tilemap 时跳过该条目，不生成到区域外。
        /// </summary>
        public void Generate()
        {
            if (clearOnGenerate)
            {
                Clear();
            }

            if (spawnEntries == null)
            {
                return;
            }

            Transform parent = nodeParent != null ? nodeParent : transform;
            BuildAreaTilemapCache();

            foreach (SpawnEntry entry in spawnEntries)
            {
                if (entry == null || entry.config == null || entry.nodeCount <= 0)
                {
                    continue;
                }

                if (entry.belongAreas == null || entry.belongAreas.Length == 0)
                {
                    Debug.LogWarning($"[ResourceNodeSpawner] 资源 {entry.config.resourceName} 未配置所属区域，跳过生成。");
                    continue;
                }

                for (int i = 0; i < entry.nodeCount; i++)
                {
                    if (!TryGetRandomPositionInAreas(
                        entry.belongAreas,
                        out AreaConfigSO belongArea,
                        out Vector2 center))
                    {
                        Debug.LogWarning($"[ResourceNodeSpawner] 找不到资源 {entry.config.resourceName} 的有效区域 Tile，跳过资源生成。");
                        break;
                    }

                    int subs = Mathf.Max(1, entry.perNodeCount);
                    for (int j = 0; j < subs; j++)
                    {
                        Vector2 offset = Random.insideUnitCircle * clusterRadius;
                        SpawnOne(entry.config, belongArea, center + offset, parent);
                    }
                }
            }
        }

        /// <summary>清理全部已生成的采集点。</summary>
        public void Clear()
        {
            foreach (ResourceNode node in _nodes)
            {
                if (node != null)
                {
                    Destroy(node.gameObject);
                }
            }

            _nodes.Clear();
        }

        /// <summary>在指定世界坐标生成单个采集点实例。</summary>
        public ResourceNode SpawnOne(ResourceNodeConfig config, AreaConfigSO belongArea, Vector2 position, Transform parent)
        {
            if (config == null)
            {
                Debug.LogWarning("[ResourceNodeSpawner] 生成采集点失败：资源配置为空。");
                return null;
            }

            GameObject go = new GameObject($"[{config.resourceId}] {config.resourceName}");
            go.transform.SetParent(parent, true);
            go.transform.position = new Vector3(position.x, 0f, position.y);

            ResourceNode node = go.AddComponent<ResourceNode>();
            node.Initialize(config, belongArea);
            _nodes.Add(node);
            return node;
        }

        /// <summary>构建区域到 Tilemap 的映射缓存。</summary>
        private void BuildAreaTilemapCache()
        {
            _areaTilemapCache.Clear();

            if (sanityManager == null || sanityManager.groundTilemaps == null
                || sanityManager.areas == null)
            {
                return;
            }

            foreach (Tilemap tilemap in sanityManager.groundTilemaps)
            {
                if (tilemap == null)
                {
                    continue;
                }

                foreach (AreaConfigSO area in sanityManager.areas)
                {
                    if (area == null || !ContainsAreaTile(tilemap, area))
                    {
                        continue;
                    }

                    if (!_areaTilemapCache.TryGetValue(area, out List<Tilemap> tilemaps))
                    {
                        tilemaps = new List<Tilemap>();
                        _areaTilemapCache.Add(area, tilemaps);
                    }

                    if (!tilemaps.Contains(tilemap))
                    {
                        tilemaps.Add(tilemap);
                    }
                }
            }
        }

        /// <summary>在指定区域的有效 Tile 内随机选一个位置。</summary>
        private bool TryGetRandomPositionInArea(AreaConfigSO area, out Vector2 position)
        {
            position = Vector2.zero;

            if (area == null || !_areaTilemapCache.TryGetValue(area, out List<Tilemap> tilemaps)
                || tilemaps == null || tilemaps.Count == 0)
            {
                return false;
            }

            List<AreaCell> validCells = new List<AreaCell>();
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap == null)
                {
                    continue;
                }

                BoundsInt bounds = tilemap.cellBounds;
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        Vector3Int cellPos = new Vector3Int(x, y, 0);
                        if (tilemap.HasTile(cellPos) && area.ContainsTile(tilemap.GetTile(cellPos)))
                        {
                            validCells.Add(new AreaCell
                            {
                                tilemap = tilemap,
                                position = cellPos
                            });
                        }
                    }
                }
            }

            if (validCells.Count == 0)
            {
                return false;
            }

            AreaCell randomCell = validCells[Random.Range(0, validCells.Count)];
            Vector3 cellCenter = randomCell.tilemap.GetCellCenterWorld(randomCell.position);
            Vector2 randomOffset = Random.insideUnitCircle * 0.4f;
            position = new Vector2(cellCenter.x + randomOffset.x, cellCenter.z + randomOffset.y);
            return true;
        }

        /// <summary>从候选区域中选择一个有效区域并随机返回位置。</summary>
        private bool TryGetRandomPositionInAreas(
            AreaConfigSO[] areas,
            out AreaConfigSO selectedArea,
            out Vector2 position)
        {
            selectedArea = null;
            position = Vector2.zero;

            if (areas == null || areas.Length == 0)
            {
                return false;
            }

            List<AreaConfigSO> candidates = new List<AreaConfigSO>();
            foreach (AreaConfigSO area in areas)
            {
                if (area != null && !candidates.Contains(area))
                {
                    candidates.Add(area);
                }
            }

            while (candidates.Count > 0)
            {
                int index = Random.Range(0, candidates.Count);
                AreaConfigSO area = candidates[index];
                candidates.RemoveAt(index);

                if (TryGetRandomPositionInArea(area, out position))
                {
                    selectedArea = area;
                    return true;
                }
            }

            return false;
        }

        /// <summary>判断 Tilemap 是否包含指定区域的 Tile。</summary>
        private bool ContainsAreaTile(Tilemap tilemap, AreaConfigSO area)
        {
            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cellPos) && area.ContainsTile(tilemap.GetTile(cellPos)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
