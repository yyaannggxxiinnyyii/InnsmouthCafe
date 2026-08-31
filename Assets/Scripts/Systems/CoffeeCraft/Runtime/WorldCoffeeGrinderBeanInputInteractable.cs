using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景研磨机入豆口交互对象，负责接收手持豆勺中的咖啡豆。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCoffeeGrinderBeanInputInteractable : MonoBehaviour
    {
        /// <summary>
        /// 尝试将手持豆勺中的全部咖啡豆倒入研磨机。
        /// </summary>
        public bool LoadBeans()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return false;
            }

            return NewCoffeeCraftManager.Instance.TryLoadHeldBeansIntoGrinder();
        }
    }
}
