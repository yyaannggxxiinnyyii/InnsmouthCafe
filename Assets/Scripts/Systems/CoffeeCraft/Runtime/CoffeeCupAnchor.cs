using UnityEngine;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 咖啡制作工位的杯子吸附锚点。
    /// </summary>
    public class CoffeeCupAnchor : MonoBehaviour
    {
        [SerializeField] private float _snapRadius = 0.8f;

        /// <summary>杯子吸附半径。</summary>
        public float SnapRadius => Mathf.Max(0.01f, _snapRadius);

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中显示工作杯可吸附到此锚点的范围。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.65f, 1f, 0.18f);
            Gizmos.DrawSphere(transform.position, SnapRadius);

            Gizmos.color = new Color(0.15f, 0.55f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, SnapRadius);
        }
#endif
    }
}
