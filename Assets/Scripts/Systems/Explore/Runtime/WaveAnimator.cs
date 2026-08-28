using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 浪花位置漂移控制器。
    /// 用于饥荒原生动画预制体（已有 Animator 控制帧动画）。
    /// 只负责在 XZ 平面漂移，不管理帧动画播放。
    /// </summary>
    public class WaveAnimator : MonoBehaviour
    {
        [Header("位置漂移")]
        public bool enableDrift = true;
        public float driftSpeed = 0.2f;
        public float driftRange = 0.3f;

        private Vector3 _originPos;
        private float _seedX, _seedZ;

        private void Start()
        {
            // 记录初始位置
            _originPos = transform.position;

            // 随机种子（每个浪花漂移轨迹不同）
            _seedX = Random.Range(0f, 1000f);
            _seedZ = Random.Range(0f, 1000f);
        }

        private void Update()
        {
            UpdateDrift();
        }

        private void UpdateDrift()
        {
            // 位置漂移（Perlin Noise 制造自然漂移）
            // 只在 XZ 平面漂移，Y 轴保持不变
            if (enableDrift)
            {
                float noiseX = Mathf.PerlinNoise(Time.time * driftSpeed + _seedX, 0f) - 0.5f;
                float noiseZ = Mathf.PerlinNoise(0f, Time.time * driftSpeed + _seedZ) - 0.5f;

                Vector3 drift = new Vector3(noiseX * 2f, 0f, noiseZ * 2f) * driftRange;
                transform.position = new Vector3(
                    _originPos.x + drift.x,
                    _originPos.y,  // Y 保持不变
                    _originPos.z + drift.z
                );
            }
        }

        /// <summary>重置到新位置（对象池复用时调用）</summary>
        public void ResetPosition(Vector3 newOrigin)
        {
            _originPos = newOrigin;
            transform.position = newOrigin;

            // 重新随机种子
            _seedX = Random.Range(0f, 1000f);
            _seedZ = Random.Range(0f, 1000f);
        }
    }
}
