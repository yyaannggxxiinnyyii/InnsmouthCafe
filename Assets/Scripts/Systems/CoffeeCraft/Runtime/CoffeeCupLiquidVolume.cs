using TMPro;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>统计工作杯内有效粒子，并按当前杯型配置换算液体容量。</summary>
    public class CoffeeCupLiquidVolume : MonoBehaviour
    {
        [Header("液体引用")]
        [Tooltip("用于统计的 LiquidFun 粒子系统")]
        [SerializeField] private LPParticleSystem _liquidParticleSystem;

        [Header("容量显示")]
        [Tooltip("可选的容量调试文本")]
        [SerializeField] private TMP_Text _volumeDebugText;

        private Collider2D _cupInteriorArea;
        private float _maximumCapacityMilliliters;
        private int _fullParticleCount = 1;
        private int _particleCount;

        /// <summary>当前粒子数量相对满杯参考数量的比例。</summary>
        public float FillRatio => Mathf.Clamp01((float)_particleCount / Mathf.Max(1, _fullParticleCount));

        /// <summary>单个有效粒子对应的液体容量。</summary>
        public float SingleParticleCapacityMilliliters => Mathf.Max(0f, _maximumCapacityMilliliters)
            / Mathf.Max(1, _fullParticleCount);

        /// <summary>当前杯内有效粒子数量。</summary>
        public int CurrentParticleCount => _particleCount;

        /// <summary>当前根据粒子数量计算出的液体容量。</summary>
        public float CurrentMilliliters => Mathf.Min(_particleCount, Mathf.Max(1, _fullParticleCount))
            * SingleParticleCapacityMilliliters;

        /// <summary>当前粒子数量是否达到满杯参考数量。</summary>
        public bool IsFull => _particleCount >= Mathf.Max(1, _fullParticleCount);

        /// <summary>兼容旧调用方的当前粒子数量属性。</summary>
        public int ParticleCount => _particleCount;

        /// <summary>从当前杯型数据读取最大容量和满杯参考粒子数量。</summary>
        public void Configure(CupContainerData cupData, Collider2D cupInteriorArea)
        {
            if (cupData == null)
            {
                Debug.LogWarning("[CoffeeCupLiquidVolume] 杯型数据为空，无法配置容量。", this);
                return;
            }

            _maximumCapacityMilliliters = Mathf.Max(0f, cupData.capacity);
            _fullParticleCount = Mathf.Max(1, cupData.fullParticleCount);
            _cupInteriorArea = cupInteriorArea;
            RefreshMeasurement();
        }

        /// <summary>设置用于统计的 LiquidFun 粒子系统。</summary>
        public void SetParticleSystem(LPParticleSystem particleSystem)
        {
            _liquidParticleSystem = particleSystem;
        }

        /// <summary>立即刷新粒子数量和容量显示。</summary>
        public void RefreshMeasurement()
        {
            RefreshParticleCount();
            RefreshVolumeDisplay();
        }

        private void Update()
        {
            RefreshMeasurement();
        }

        /// <summary>只统计杯内粒子，飞出杯外的粒子视为浪费。</summary>
        private void RefreshParticleCount()
        {
            _particleCount = 0;
            if (_liquidParticleSystem == null
                || _liquidParticleSystem.Particles == null
                || _cupInteriorArea == null)
            {
                return;
            }

            foreach (LPParticle particle in _liquidParticleSystem.Particles)
            {
                if (_cupInteriorArea.OverlapPoint(particle.Position))
                {
                    _particleCount++;
                }
            }
        }

        /// <summary>更新可选的调试显示文本。</summary>
        private void RefreshVolumeDisplay()
        {
            if (_volumeDebugText == null)
            {
                return;
            }

            _volumeDebugText.text = string.Format(
                "粒子数：{0}\n容量：{1:0.0} ml / {2:0.0} ml\n填充：{3:0}%",
                _particleCount,
                CurrentMilliliters,
                Mathf.Max(0f, _maximumCapacityMilliliters),
                FillRatio * 100f);
        }

        /// <summary>清理全部液体粒子，并重置容量显示。</summary>
        public void ClearParticles()
        {
            if (_liquidParticleSystem != null
                && _liquidParticleSystem.Particles != null
                && _liquidParticleSystem.Particles.Count > 0)
            {
                int particleCount = _liquidParticleSystem.Particles.Count;
                int[] particleIndices = new int[particleCount + 1];
                particleIndices[0] = particleCount;
                for (int index = 0; index < particleCount; index++)
                {
                    particleIndices[index + 1] = index;
                }

                LPAPIParticles.DestroySelectedParticles(
                    _liquidParticleSystem.GetPtr(), particleIndices);
            }

            _particleCount = 0;
            RefreshVolumeDisplay();
        }
    }
}
