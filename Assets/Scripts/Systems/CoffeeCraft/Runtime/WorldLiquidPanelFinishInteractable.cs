using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 空间加液面板中的完成交互对象，提交杯内液体容量并关闭面板。
    /// </summary>
    public class WorldLiquidPanelFinishInteractable : MonoBehaviour
    {
        [SerializeField] private LiquidAddPanelController _panelController;

        private void Awake()
        {
            if (GetComponent<Collider>() == null && GetComponent<Collider2D>() == null)
            {
                Debug.LogWarning("[LiquidAddPanel] 完成对象缺少 Collider 或 Collider2D，无法接收点击", this);
            }
        }

        /// <summary>
        /// 尝试提交当前面板中的辅助液并关闭面板。
        /// </summary>
        public void Finish()
        {
            if (_panelController == null || !_panelController.IsOpen)
            {
                return;
            }

            if (_panelController.CommitAndClose())
            {
                return;
            }

            if (!_panelController.HasLiquidVolume)
            {
                Debug.LogWarning("[LiquidAddPanel] 未绑定 CoffeeCupLiquidVolume，无法读取液体容量，面板保持打开", this);
                return;
            }

            if (_panelController.CurrentVolumeMilliliters <= 0.01f)
            {
                _panelController.Close();
                Debug.Log("[LiquidAddPanel] 当前没有液体，已关闭加液面板", this);
                return;
            }

            Debug.LogWarning("[LiquidAddPanel] 当前液体容量未能提交，面板保持打开", this);
        }

        /// <summary>
        /// 支持不经过总射线管理器的直接空间点击。
        /// </summary>
        private void OnMouseDown()
        {
            Finish();
        }
    }
}
