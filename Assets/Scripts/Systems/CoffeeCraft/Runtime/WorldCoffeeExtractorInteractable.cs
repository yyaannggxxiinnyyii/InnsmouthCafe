using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景萃取机交互对象，负责启动萃取并判断工作杯是否承接咖啡液。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCoffeeExtractorInteractable : MonoBehaviour
    {
        [Header("工作杯引用")]
        [Tooltip("用于判断工作杯是否位于萃取承接位的场景控制器")]
        [SerializeField] private CoffeeWorkCupController _workCupController;

        /// <summary>
        /// 尝试启动一次萃取；工作杯未就位时会作废当前研磨机批次。
        /// </summary>
        public void StartExtraction()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return;
            }

            if (_workCupController == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 萃取机未配置 CoffeeWorkCupController", this);
                return;
            }

            NewCoffeeCraftManager.Instance.TryStartExtraction(_workCupController.IsWorkCupAtExtractor());
        }
    }
}
