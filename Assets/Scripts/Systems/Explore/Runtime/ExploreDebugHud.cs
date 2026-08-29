using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索调试 HUD（极简 OnGUI）。仅用于早期验证骨架手感，验证通过后可删除。
    /// 展示：理智值、当前地区、采集交互提示、采集进度条。
    /// </summary>
    public class ExploreDebugHud : MonoBehaviour
    {
        [Header("引用")]
        [Tooltip("理智管理器；为空时自动查找")]
        public ExploreSanityManager sanity;

        [Tooltip("采集控制器；为空时自动查找")]
        public HarvestController harvest;

        [Tooltip("是否显示调试 HUD")]
        public bool show = true;

        private void Awake()
        {
            if (sanity == null) sanity = FindObjectOfType<ExploreSanityManager>();
            if (harvest == null) harvest = FindObjectOfType<HarvestController>();
        }

        private void OnGUI()
        {
            if (!show) return;

            float x = 12f, y = 12f;
            GUILayout.BeginArea(new Rect(x, y, 320f, 200f)); // 右上角

            // 理智条
            string sanityText = sanity != null
                ? $"理智: {sanity.CurrentSanity:F1} / {sanity.maxSanity:F0}"
                : "理智管理器未挂载";
            GUILayout.Label(sanityText);

            // 当前地区
            if (sanity != null)
            {
                GUI.color = sanity.CurrentArea != null
                    ? Color.green : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.Label($"地区: {sanity.CurrentArea?.AreaName ?? "未配置区域"}");
                GUI.color = Color.white;
            }

            // 采集交互提示
            if (harvest != null)
            {
                var node = harvest.FocusedNode;
                if (harvest.IsHarvesting)
                {
                    // 进度条
                    string progress = $"采集进度: {harvest.HarvestProgress * 100f:F0}%";
                    GUI.color = Color.yellow;
                    GUILayout.Label(progress);
                    GUI.color = Color.white;
                }
                else if (node != null)
                {
                    GUILayout.Label($"按 F 采集「{node.name}」");
                    if (!harvest.CheckTool(node))
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("工具不足");
                        GUI.color = Color.white;
                    }
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("靠近采集点");
                    GUI.color = Color.white;
                }
            }

            GUILayout.EndArea();
        }
    }
}
