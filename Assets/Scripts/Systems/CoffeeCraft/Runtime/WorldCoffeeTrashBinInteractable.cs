using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 场景咖啡制作垃圾桶，支持点击倒掉手持豆子和拖入倒掉整杯咖啡。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldCoffeeTrashBinInteractable : MonoBehaviour
    {
        [Header("倒杯范围")]
        [Tooltip("工作杯拖拽到垃圾桶附近时可被倒掉的距离")]
        [SerializeField] private float _cupDiscardRadius = 0.8f;

        /// <summary>
        /// 判断指定工作杯坐标是否位于垃圾桶的倒杯范围内。
        /// </summary>
        public bool IsCupWithinDiscardRange(Vector3 cupPosition)
        {
            return Vector3.Distance(cupPosition, transform.position)
                <= Mathf.Max(0.01f, _cupDiscardRadius);
        }

        /// <summary>
        /// 点击垃圾桶时尝试倒掉手持豆勺中的豆子。
        /// </summary>
        public void DiscardHeldBeans()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return;
            }

            if (!NewCoffeeCraftManager.Instance.TryDiscardHeldBeans())
            {
                Debug.LogWarning("[NewCoffeeCraft] 当前没有可倒掉的手持豆子", this);
            }
        }

        /// <summary>
        /// 工作杯拖入垃圾桶时尝试倒掉整杯咖啡。
        /// </summary>
        /// <returns>整杯咖啡倒掉成功时返回 true。</returns>
        public bool TryDiscardCup()
        {
            if (NewCoffeeCraftManager.Instance == null)
            {
                Debug.LogWarning("[NewCoffeeCraft] 场景中没有新咖啡制作管理器", this);
                return false;
            }

            LiquidAddPanelController panelController = FindObjectOfType<LiquidAddPanelController>();
            panelController?.ResetForDiscard();
            return NewCoffeeCraftManager.Instance.TryDiscardCurrentCup();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中显示工作杯拖入垃圾桶后的倒杯判定范围。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            float radius = Mathf.Max(0.01f, _cupDiscardRadius);
            Gizmos.color = new Color(0.2f, 0.9f, 0.35f, 0.18f);
            Gizmos.DrawSphere(transform.position, radius);

            Gizmos.color = new Color(0.15f, 0.8f, 0.3f, 1f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
