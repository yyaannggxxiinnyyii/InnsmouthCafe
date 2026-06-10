using UnityEngine;
using InnsmouthCafe.UI;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 游戏场景快捷键管理器
    /// A/D：左右切换场景视图
    /// Q：展开/收起订单小票详情
    /// Esc：暂停（由 PausePanelUI 自行处理）
    /// </summary>
    public class GameInputManager : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] [Tooltip("订单小票UI（为空时自动查找）")]
        private OrderTicketUI _orderTicketUI;

        private void Start()
        {
            if (_orderTicketUI == null)
                _orderTicketUI = FindObjectOfType<OrderTicketUI>();
        }

        private void Update()
        {
            // 暂停时不处理快捷键（Esc 由 PausePanelUI 处理，不受此限制）
            if (Time.timeScale == 0f) return;

            // A / 左方向键 → 切换到上一个视图
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                ViewSwitchManager.Instance?.SwitchPreviousView();

            // D / 右方向键 → 切换到下一个视图
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                ViewSwitchManager.Instance?.SwitchNextView();

            // Q 按住展开小票，松开收起
            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (_orderTicketUI != null && _orderTicketUI.gameObject.activeInHierarchy)
                    _orderTicketUI.ExpandToDetail();
            }
            if (Input.GetKeyUp(KeyCode.Q))
            {
                if (_orderTicketUI != null && _orderTicketUI.gameObject.activeInHierarchy)
                    _orderTicketUI.CollapseToPreview();
            }
        }
    }
}
