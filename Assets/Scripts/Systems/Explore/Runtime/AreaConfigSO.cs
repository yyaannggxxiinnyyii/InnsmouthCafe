using UnityEngine;
using UnityEngine.Tilemaps;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 地区配置（设计文档 §4）。地区只管理智消耗速率 + 视觉氛围。
    /// 支持两种判定方式：
    ///   1. 矩形范围判定（旧方案，适合快速验证）
    ///   2. Tilemap Tile 类型判定（推荐，适合不规则地形）
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Explore/Area Config", fileName = "AreaConfig")]
    public class AreaConfigSO : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("地区唯一 ID")]
        public string areaId = "area_1";

        [Tooltip("地区显示名")]
        public string areaName = "未命名地区";

        [Header("理智消耗")]
        [Tooltip("该地区理智消耗速率（理智/秒）。数值越小越安全。")]
        public float energyDrainPerSecond = 0.18f;

        [Header("区域判定方式")]
        [Tooltip("判定方式：矩形范围 或 Tile 类型")]
        public AreaDetectionMode detectionMode = AreaDetectionMode.Rectangle;

        [Header("方式 1：矩形范围（世界坐标）")]
        [Tooltip("地区矩形中心点世界坐标")]
        public Vector2 center = Vector2.zero;

        [Tooltip("地区矩形半径（半宽/半高）")]
        public Vector2 size = new Vector2(10f, 10f);

        [Header("方式 2：Tile 类型绑定（推荐）")]
        [Tooltip("属于本地区的 Tile 列表。玩家脚下是这些 Tile 之一时，判定为在本地区。")]
        public TileBase[] areaTiles = new TileBase[0];

        /// <summary>判断世界坐标是否落在本地区范围内（矩形模式）。</summary>
        public bool Contains(Vector3 worldPos)
        {
            Vector2 local = (Vector2)worldPos - center;
            return Mathf.Abs(local.x) <= size.x && Mathf.Abs(local.y) <= size.y;
        }

        /// <summary>判断指定 Tile 是否属于本地区（Tile 模式）。</summary>
        public bool ContainsTile(TileBase tile)
        {
            if (areaTiles == null || areaTiles.Length == 0) return false;
            foreach (var t in areaTiles)
            {
                if (t == tile) return true;
            }
            return false;
        }

#if UNITY_EDITOR
        /// <summary>编辑器下可视化地区范围（仅矩形模式）。</summary>
        private void OnDrawGizmosSelected()
        {
            if (detectionMode == AreaDetectionMode.Rectangle)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
                Gizmos.DrawWireCube(center, size * 2f);
            }
        }
#endif
    }

    /// <summary>区域判定模式。</summary>
    public enum AreaDetectionMode
    {
        Rectangle = 0,  // 矩形范围判定（简单，适合验证）
        Tilemap = 1     // Tile 类型判定（灵活，适合不规则地形）
    }
}
