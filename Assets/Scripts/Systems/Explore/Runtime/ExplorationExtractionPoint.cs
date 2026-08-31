using UnityEngine;
using InnsmouthCafe.GameFlow;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索撤离点。
    /// 玩家进入 3D 触发器后，通过全局流程管理器返回店铺场景。
    /// </summary>
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class ExplorationExtractionPoint : MonoBehaviour
    {
        [Header("撤离配置")]
        [Tooltip("是否允许玩家进入后立即撤离")]
        [SerializeField] private bool _isEnabled = true;

        private bool _hasTriggered;

        private void Awake()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isEnabled || _hasTriggered)
            {
                return;
            }

            PlayerMotor player = other.GetComponentInParent<PlayerMotor>();
            if (player == null)
            {
                return;
            }

            NewGameFlowManager flow = NewGameFlowManager.Instance;
            if (flow == null || flow.CurrentState != GameFlowState.Exploring)
            {
                return;
            }

            _hasTriggered = true;
            FindObjectOfType<ExplorationSceneController>()?.MarkExtracting();
            flow.CompleteExploration();
        }

        /// <summary>
        /// 设置撤离点是否可用。
        /// </summary>
        /// <param name="enabled">是否启用。</param>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (enabled)
            {
                _hasTriggered = false;
            }
        }
    }
}
