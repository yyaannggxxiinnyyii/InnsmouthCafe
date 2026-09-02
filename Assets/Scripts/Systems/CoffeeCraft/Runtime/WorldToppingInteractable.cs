using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>场景小料对象，点击后在当前杯型萃取出口生成一个物理小料粒子。</summary>
    [RequireComponent(typeof(Collider))]
    public class WorldToppingInteractable : MonoBehaviour
    {
        [Header("小料配置")]
        [SerializeField] private ToppingSO _topping;
        [SerializeField] private LiquidAddPanelController _panelController;
        [SerializeField] private LPParticleMaterial _particleMaterial;
        [SerializeField] private LPParticleGroupMaterial _particleGroupMaterial;
        [SerializeField] private int _userData;

        /// <summary>当前小料配置。</summary>
        public ToppingSO Topping => _topping;

        /// <summary>当前小料粒子的用户数据。</summary>
        public int UserData => _userData;

        /// <summary>点击小料并生成一个物理粒子。</summary>
        public bool AddTopping()
        {
            if (_topping == null || _panelController == null)
            {
                return false;
            }

            if (!_panelController.BeginToppingMode(this))
            {
                return false;
            }

            _panelController.SpawnToppingParticleAtOutlet(
                _topping, _particleMaterial, _particleGroupMaterial, _userData);
            return true;
        }
    }
}
