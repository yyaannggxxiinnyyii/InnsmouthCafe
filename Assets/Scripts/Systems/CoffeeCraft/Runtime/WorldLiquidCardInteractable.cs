using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 经营场景中的辅助液对象，点击后打开空间加液面板。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldLiquidCardInteractable : MonoBehaviour
    {
        [Header("辅助液配置")]
        [SerializeField] private LiquidSO _liquid;

        [SerializeField] private LiquidAddPanelController _panelController;

        /// <summary>
        /// 当前牌子绑定的辅助液配置。
        /// </summary>
        public LiquidSO Liquid => _liquid;

        /// <summary>
        /// 点击辅助液对象并打开加液面板。
        /// </summary>
        public bool TryOpenPanel()
        {
            if (_liquid == null || _panelController == null)
            {
                return false;
            }

            if (NewCoffeeCraftManager.Instance == null
                || !NewCoffeeCraftManager.Instance.CanAddLiquid()
                || _panelController.IsOpen)
            {
                return false;
            }

            WorldCoffeeWorkCup targetCup = FindObjectOfType<WorldCoffeeWorkCup>();
            if (targetCup != null && _panelController.Open(_liquid, targetCup, this))
            {
                gameObject.SetActive(false);
                return true;
            }
            return false;
        }

        /// <summary>侧剖面已打开时切换当前辅助液。</summary>
        public bool TrySelectLiquid()
        {
            return _panelController != null && _panelController.SwitchLiquid(_liquid, this);
        }
    }
}
