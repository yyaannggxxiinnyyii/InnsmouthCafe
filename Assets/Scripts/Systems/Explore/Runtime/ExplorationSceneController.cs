using UnityEngine;
using InnsmouthCafe.GameFlow;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索场景控制器。
    /// 负责设置固定出生点，并协调探索场景的撤离入口。
    /// </summary>
    public class ExplorationSceneController : MonoBehaviour
    {
        [Header("场景点位")]
        [Tooltip("玩家进入探索场景后的固定出生点")]
        [SerializeField] private Transform _playerSpawnPoint;

        [Tooltip("场景中的玩家对象；留空时自动查找")]
        [SerializeField] private PlayerMotor _player;

        private bool _initialized;
        private ExplorationPhaseState _currentState = ExplorationPhaseState.Inactive;

        /// <summary>
        /// 当前探索环节状态。
        /// </summary>
        public ExplorationPhaseState CurrentState => _currentState;

        private void Start()
        {
            InitializePlayer();
        }

        /// <summary>
        /// 在探索场景载入后将玩家放置到固定出生点，仅执行一次。
        /// </summary>
        private void InitializePlayer()
        {
            if (_initialized)
            {
                return;
            }

            if (NewGameFlowManager.Instance == null
                || NewGameFlowManager.Instance.CurrentState != GameFlowState.Exploring)
            {
                return;
            }

            _currentState = ExplorationPhaseState.Initializing;

            if (_player == null)
            {
                _player = FindObjectOfType<PlayerMotor>();
            }

            if (_player == null || _playerSpawnPoint == null)
            {
                Debug.LogWarning("[ExplorationScene] 未配置玩家或固定出生点", this);
                return;
            }

            _player.transform.SetPositionAndRotation(
                _playerSpawnPoint.position,
                _playerSpawnPoint.rotation);
            _initialized = true;
            _currentState = ExplorationPhaseState.Exploring;
        }

        /// <summary>
        /// 标记探索环节已完成。
        /// </summary>
        public void MarkCompleted()
        {
            _currentState = ExplorationPhaseState.Completed;
        }

        /// <summary>
        /// 标记玩家正在执行撤离。
        /// </summary>
        public void MarkExtracting()
        {
            if (_currentState == ExplorationPhaseState.Exploring)
            {
                _currentState = ExplorationPhaseState.Extracting;
            }
        }
    }

    /// <summary>
    /// 探索环节内部状态。
    /// </summary>
    public enum ExplorationPhaseState
    {
        Inactive,
        Initializing,
        Exploring,
        Extracting,
        Completed
    }
}
