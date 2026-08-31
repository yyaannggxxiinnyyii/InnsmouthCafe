using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>统计工作杯内有效粒子，并按当前杯型配置换算液体容量。</summary>
    public class CoffeeCupLiquidVolume : MonoBehaviour
    {
        private const float ParticleLifetime = 99999f;

        [Header("液体引用")]
        [Tooltip("用于统计的 LiquidFun 粒子系统")]
        [SerializeField] private LPParticleSystem _liquidParticleSystem;

        [Header("容量显示")]
        [Tooltip("可选的容量调试文本")]
        [SerializeField] private TMP_Text _volumeDebugText;

        [Header("杯外粒子清理")]
        [Min(0f)]
        [Tooltip("粒子生成后仍未进入杯内有效区域时允许存活的时间，超过后会被删除")]
        [SerializeField] private float _outsideParticleLifetimeSeconds = 10f;

        private Collider2D _cupInteriorArea;
        private float _maximumCapacityMilliliters;
        private int _fullParticleCount = 1;
        private int _particleCount;
        private float _lastFillRatio = -1f;
        private readonly Dictionary<LPParticle, float> _outsideParticles =
            new Dictionary<LPParticle, float>();

        /// <summary>
        /// 杯内容量比例发生变化时通知外部显示组件。
        /// </summary>
        public event Action<float> OnFillRatioChanged;

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

        /// <summary>
        /// 将已萃取的咖啡容量按当前杯型标定转换为粒子，并从固定出口点生成。
        /// </summary>
        public int CreateCoffeeParticles(
            float volumeMilliliters,
            Transform outlet,
            Color particleColor,
            int particleFlags = 0)
        {
            if (_liquidParticleSystem == null || outlet == null || volumeMilliliters <= 0f)
            {
                return 0;
            }

            int particleCount = Mathf.FloorToInt(
                volumeMilliliters / Mathf.Max(0.0001f, SingleParticleCapacityMilliliters));
            return CreateCoffeeParticles(particleCount, outlet, particleColor, particleFlags);
        }

        /// <summary>从固定出口点生成指定数量的咖啡粒子。</summary>
        public int CreateCoffeeParticles(
            int particleCount,
            Transform outlet,
            Color particleColor,
            int particleFlags = 0)
        {
            if (_liquidParticleSystem == null || outlet == null || particleCount <= 0)
            {
                return 0;
            }

            Color32 color = particleColor;
            for (int index = 0; index < particleCount; index++)
            {
                LPAPIParticles.CreateParticleInSystem(
                    _liquidParticleSystem.GetPtr(),
                    particleFlags,
                    outlet.position.x,
                    outlet.position.y,
                    0f,
                    -0.5f,
                    color.r,
                    color.g,
                    color.b,
                    color.a,
                    ParticleLifetime);
            }

            return particleCount;
        }

        /// <summary>立即刷新粒子数量和容量显示。</summary>
        public void RefreshMeasurement()
        {
            RefreshParticleCount();
            RefreshVolumeDisplay();

            float fillRatio = FillRatio;
            if (Mathf.Approximately(_lastFillRatio, fillRatio))
            {
                return;
            }

            _lastFillRatio = fillRatio;
            OnFillRatioChanged?.Invoke(fillRatio);
        }

        private void Update()
        {
            CleanupOutsideParticles();
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
                else if (!_outsideParticles.ContainsKey(particle))
                {
                    _outsideParticles.Add(particle, Time.time);
                }
            }
        }

        /// <summary>
        /// 删除超过允许存活时间且仍未进入杯内有效区域的粒子。
        /// </summary>
        private void CleanupOutsideParticles()
        {
            if (_liquidParticleSystem == null
                || _liquidParticleSystem.Particles == null
                || _cupInteriorArea == null
                || _outsideParticles.Count == 0)
            {
                return;
            }

            float lifetime = Mathf.Max(0f, _outsideParticleLifetimeSeconds);
            List<int> particleIndices = new List<int>();
            List<LPParticle> particlesToRemove = new List<LPParticle>();

            foreach (KeyValuePair<LPParticle, float> entry in _outsideParticles)
            {
                LPParticle particle = entry.Key;
                int particleIndex = _liquidParticleSystem.Particles.IndexOf(particle);
                if (particleIndex < 0)
                {
                    particlesToRemove.Add(particle);
                    continue;
                }

                if (_cupInteriorArea.OverlapPoint(particle.Position))
                {
                    if (Time.time - entry.Value >= lifetime)
                    {
                        particlesToRemove.Add(particle);
                    }
                    continue;
                }

                if (Time.time - entry.Value >= lifetime)
                {
                    particleIndices.Add(particleIndex);
                    particlesToRemove.Add(particle);
                }
            }

            foreach (LPParticle particle in particlesToRemove)
            {
                _outsideParticles.Remove(particle);
            }

            if (particleIndices.Count == 0)
            {
                return;
            }

            particleIndices.Sort();
            int[] indices = new int[particleIndices.Count + 1];
            indices[0] = particleIndices.Count;
            for (int index = 0; index < particleIndices.Count; index++)
            {
                indices[index + 1] = particleIndices[index];
            }

            LPAPIParticles.DestroySelectedParticles(
                _liquidParticleSystem.GetPtr(), indices);
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
            _outsideParticles.Clear();
            _lastFillRatio = -1f;
            RefreshVolumeDisplay();
            OnFillRatioChanged?.Invoke(0f);
        }
    }
}
