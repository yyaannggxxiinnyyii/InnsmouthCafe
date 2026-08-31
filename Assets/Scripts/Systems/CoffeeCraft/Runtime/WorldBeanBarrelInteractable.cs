using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景咖啡豆桶交互对象，负责向手持豆勺取用指定豆种。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldBeanBarrelInteractable : MonoBehaviour
    {
        [Header("咖啡豆配置")]
        [Tooltip("该豆桶提供的咖啡豆 SO")]
        [SerializeField] private BeanSO _bean;

        /// <summary>
        /// 该场景豆桶对应的咖啡豆配置。
        /// </summary>
        public BeanSO Bean => _bean;

        /// <summary>
        /// 尝试从该豆桶向手持豆勺取豆。
        /// </summary>
        public bool TakeBeans()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return false;
            }

            return NewCoffeeCraftManager.Instance.TryTakeBeans(_bean);
        }
    }
}
