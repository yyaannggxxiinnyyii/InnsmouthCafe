using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景杯子交互对象。
    /// 挂载在带有 3D Collider 的杯子物体上，负责提供杯子配置。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCupInteractable : MonoBehaviour
    {
        [Header("杯子配置")]
        [Tooltip("该场景杯子对应的配置 SO")]
        [SerializeField] private CupContainerSO _cup;

        /// <summary>
        /// 当前场景对象对应的杯子配置。
        /// </summary>
        public CupContainerSO Cup => _cup;

        /// <summary>
        /// 响应场景点击并尝试选择杯子。
        /// </summary>
        public void Select()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器");
                return;
            }

            if (!NewCoffeeCraftManager.Instance.TrySelectCup(_cup))
            {
                return;
            }

            CoffeeWorkCupController controller = FindObjectOfType<CoffeeWorkCupController>();
            if (controller == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中未配置 CoffeeWorkCupController", this);
                return;
            }

            controller.TakeCup(_cup, gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_cup == null)
            {
                Debug.LogWarning($"[NewCoffeeCraft] {name} 未配置杯子 SO");
            }
        }
#endif
    }
}
