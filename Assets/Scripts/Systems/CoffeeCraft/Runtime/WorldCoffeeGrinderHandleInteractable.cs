using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景研磨机手柄交互对象，负责逐次提升咖啡豆研磨度。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCoffeeGrinderHandleInteractable : MonoBehaviour
    {
        /// <summary>
        /// 尝试操作一次研磨机手柄。
        /// </summary>
        public void GrindBeans()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return;
            }

            NewCoffeeCraftManager.Instance.TryGrindBeans();
        }
    }
}
