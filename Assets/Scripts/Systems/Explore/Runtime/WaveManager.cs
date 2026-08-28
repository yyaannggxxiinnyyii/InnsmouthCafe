using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using InnsmouthCafe.Core;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 浪花管理器（饥荒风格）。
    /// 根据 AreaWaveConfig 在不同地区生成不同密度/种类的浪花。
    /// 支持按面积密度生成、对象池复用、位置漂移。
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("区域浪花配置列表（每个地区一个）")]
        public AreaWaveConfigSO[] areaConfigs = new AreaWaveConfigSO[0];

        [Header("引用")]
        [Tooltip("SanityManager 引用（用于查找 Tilemap）")]
        public ExploreSanityManager sanityManager;

        [Header("调试")]
        [Tooltip("是否在 Start 时自动生成")]
        public bool autoGenerateOnStart = true;

        private SimpleObjectPool _pool;
        private Dictionary<AreaConfigSO, List<GameObject>> _spawnedWaves = new Dictionary<AreaConfigSO, List<GameObject>>();
        private Transform _poolRoot;

        private void Awake()
        {
            // 创建对象池根节点
            _poolRoot = new GameObject("[WavePool]").transform;
            _poolRoot.SetParent(transform);
            _pool = new SimpleObjectPool(_poolRoot);

            if (sanityManager == null)
            {
                sanityManager = FindObjectOfType<ExploreSanityManager>();
            }
        }

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                Generate();
            }
        }

        /// <summary>
        /// 生成所有地区的浪花。
        /// </summary>
        public void Generate()
        {
            Clear();

            if (areaConfigs == null || areaConfigs.Length == 0)
            {
                Debug.LogWarning("[WaveManager] 没有配置 AreaWaveConfig");
                return;
            }

            foreach (var config in areaConfigs)
            {
                if (config == null || config.targetArea == null) continue;
                GenerateForArea(config);
            }

            Debug.Log($"[WaveManager] 浪花生成完成。池统计: {_pool.GetStats()}");
        }

        /// <summary>
        /// 清除所有浪花（回收到池）。
        /// </summary>
        public void Clear()
        {
            if (_pool != null)
            {
                _pool.ReturnAll();
            }
            _spawnedWaves.Clear();
        }

        /// <summary>
        /// 为指定地区生成浪花。
        /// </summary>
        private void GenerateForArea(AreaWaveConfigSO config)
        {
            // 找到目标地区对应的 Tilemap
            Tilemap tilemap = FindTilemapForArea(config.targetArea);
            if (tilemap == null)
            {
                Debug.LogWarning($"[WaveManager] 找不到 {config.targetArea.areaName} 对应的 Tilemap");
                return;
            }

            // 收集所有有效格子
            List<Vector3Int> validCells = GetValidCells(tilemap);
            if (validCells.Count == 0)
            {
                Debug.LogWarning($"[WaveManager] {config.targetArea.areaName} 没有有效格子");
                return;
            }

            // 计算地区面积
            float cellArea = tilemap.cellSize.x * tilemap.cellSize.y;
            float totalArea = validCells.Count * cellArea;

            // 初始化浪花列表
            if (!_spawnedWaves.ContainsKey(config.targetArea))
            {
                _spawnedWaves[config.targetArea] = new List<GameObject>();
            }

            // 遍历每种浪花配置
            foreach (var entry in config.waveEntries)
            {
                if (entry.wavePrefab == null || entry.density <= 0f) continue;

                // 根据密度计算生成数量
                int count = Mathf.RoundToInt(totalArea * entry.density);

                for (int i = 0; i < count; i++)
                {
                    // 随机选一个格子
                    Vector3Int randomCell = validCells[Random.Range(0, validCells.Count)];
                    Vector3 cellCenter = tilemap.GetCellCenterWorld(randomCell);

                    // 格子内随机偏移（只在 XZ 平面，Y 固定）
                    float offsetX = Random.Range(-0.4f, 0.4f);
                    float offsetZ = Random.Range(-0.4f, 0.4f);
                    Vector3 spawnPos = new Vector3(cellCenter.x + offsetX, 0.01f, cellCenter.z + offsetZ);

                    // 从对象池获取浪花（旋转 X=90 让 XY 平面的动画躺到 XZ 平面）
                    Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
                    GameObject waveObj = _pool.Get(entry.wavePrefab, spawnPos, rotation, transform);
                    if (waveObj == null)
                    {
                        continue;
                    }

                    // 强制锁定 Y 轴（确保不会因为对象池复用而带来旧的 Y 值）
                    waveObj.transform.position = new Vector3(
                        waveObj.transform.position.x,
                        0.01f,
                        waveObj.transform.position.z);

                    // 配置 SpriteRenderer
                    SpriteRenderer sr = waveObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = config.sortingOrder;
                        sr.color = entry.color;
                    }

                    // 漂移作用在集合根节点上，不改变各个海浪子物体之间的相对位置。
                    WaveAnimator waveAnimScript = waveObj.GetComponent<WaveAnimator>();
                    if (waveAnimScript == null)
                    {
                        waveAnimScript = waveObj.AddComponent<WaveAnimator>();
                    }

                    if (waveAnimScript != null)
                    {
                        waveAnimScript.enableDrift = entry.enableDrift;
                        waveAnimScript.driftSpeed = entry.driftSpeed;
                        waveAnimScript.driftRange = entry.driftRange;
                        waveAnimScript.ResetPosition(spawnPos); // 重置漂移原点
                    }

                    // 每个海浪子物体独立旋转，避免集合根节点旋转导致海浪位置绕中心移动。
                    AddBillboardsToWaveChildren(waveObj);

                    // 错开 Animator 播放时间（关键！）
                    Animator animator = waveObj.GetComponent<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        // 随机归一化时间（0-1），让每个浪花从动画的不同时间点开始播放
                        float randomTime = Random.Range(0f, 1f);
                        animator.Play(0, 0, randomTime); // layer 0, state 0, normalized time

                        // 随机播放速度（0.8-1.2 倍速）
                        animator.speed = Random.Range(0.8f, 1.2f);
                    }

                    _spawnedWaves[config.targetArea].Add(waveObj);
                }

                Debug.Log($"[WaveManager] {config.targetArea.areaName} 生成了 {count} 个 {entry.wavePrefab.name}（面积 {totalArea:F1}，密度 {entry.density}）");
            }
        }

        /// <summary>找到地区对应的 Tilemap</summary>
        private Tilemap FindTilemapForArea(AreaConfigSO area)
        {
            if (sanityManager == null || sanityManager.groundTilemaps == null) return null;

            foreach (var tilemap in sanityManager.groundTilemaps)
            {
                if (tilemap == null) continue;

                // 取这个 Tilemap 的任意一个 Tile，检查是否属于目标 Area
                var cellPos = GetRandomCellInTilemap(tilemap);
                if (cellPos == null) continue;

                var tile = tilemap.GetTile(cellPos.Value);
                if (tile != null && area.ContainsTile(tile))
                {
                    return tilemap;
                }
            }

            return null;
        }

        /// <summary>收集 Tilemap 所有有 Tile 的格子</summary>
        private List<Vector3Int> GetValidCells(Tilemap tilemap)
        {
            List<Vector3Int> cells = new List<Vector3Int>();
            BoundsInt bounds = tilemap.cellBounds;

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

            return cells;
        }

        /// <summary>从 Tilemap 随机取一个有 Tile 的格子</summary>
        private Vector3Int? GetRandomCellInTilemap(Tilemap tilemap)
        {
            var cells = GetValidCells(tilemap);
            if (cells.Count == 0) return null;
            return cells[Random.Range(0, cells.Count)];
        }

        /// <summary>
        /// 为浪花预制体的所有视觉子物体添加独立 Billboard。
        /// 只修改子物体的朝向，不修改其位置，保持浪花集合的布局不变。
        /// </summary>
        private void AddBillboardsToWaveChildren(GameObject waveObj)
        {
            SpriteRenderer[] renderers = waveObj.GetComponentsInChildren<SpriteRenderer>(true);

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.transform == waveObj.transform)
                {
                    continue;
                }

                if (renderer.GetComponent<SimpleBillboard>() == null)
                {
                    renderer.gameObject.AddComponent<SimpleBillboard>();
                }
            }
        }

        private void OnDestroy()
        {
            Clear();

            // 销毁对象池
            if (_pool != null)
            {
                _pool.Clear();
            }
        }
    }
}
