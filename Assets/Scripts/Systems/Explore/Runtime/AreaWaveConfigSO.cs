using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 区域浪花配置（ScriptableObject）。
    /// 定义某个地区内生成哪些浪花、密度、颜色等参数。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Explore/Area Wave Config", fileName = "AreaWaveConfig")]
    public class AreaWaveConfigSO : ScriptableObject
    {
        [System.Serializable]
        public class WaveEntry
        {
            [Tooltip("浪花预制体（必须包含 WaveAnimator 组件）")]
            public GameObject wavePrefab;

            [Tooltip("密度（每平方单位生成几个）")]
            [Min(0f)]
            public float density = 0.5f;

            [Tooltip("浪花颜色（支持透明度）")]
            public Color color = new Color(1f, 1f, 1f, 0.6f);

            [Tooltip("帧率范围（随机）")]
            public Vector2 fpsRange = new Vector2(4f, 8f);

            [Header("位置漂移")]
            [Tooltip("是否启用位置漂移")]
            public bool enableDrift = true;

            [Tooltip("漂移速度")]
            public float driftSpeed = 0.2f;

            [Tooltip("漂移范围（单位）")]
            public float driftRange = 0.3f;
        }

        [Header("目标地区")]
        [Tooltip("在哪个地区生成浪花")]
        public AreaConfigSO targetArea;

        [Header("浪花配置")]
        [Tooltip("浪花种类列表（可配置多种）")]
        public WaveEntry[] waveEntries = new WaveEntry[0];

        [Header("渲染")]
        [Tooltip("浪花渲染层级（应该在海面之上）")]
        public int sortingOrder = 10;
    }
}
