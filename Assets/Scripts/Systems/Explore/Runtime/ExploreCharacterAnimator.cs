using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 将探索阶段玩家状态同步到角色 Animator。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ExploreCharacterAnimator : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] [Tooltip("玩家移动组件，为空时自动从当前对象获取")]
        private PlayerMotor _playerMotor;

        [SerializeField] [Tooltip("角色视觉子对象上的 Animator，为空时自动从子对象获取")]
        private Animator _animator;

        [SerializeField] [Tooltip("采集控制器，为空时自动从当前对象获取")]
        private HarvestController _harvestController;

        [Header("Animator 参数")]
        [SerializeField] [Tooltip("控制 Idle 与 Walk 切换的 Bool 参数名")]
        private string _isMovingParameter = "IsMoving";

        [SerializeField] [Tooltip("控制采集状态的 Bool 参数名")]
        private string _isHarvestingParameter = "IsHarvesting";

        [SerializeField] [Tooltip("控制采集动画进度的 Float 参数名")]
        private string _harvestProgressParameter = "HarvestProgress";

        [SerializeField] [Tooltip("控制直接拾取状态的 Bool 参数名")]
        private string _isPickingUpParameter = "IsPickingUp";

        [SerializeField] [Tooltip("控制直接拾取动画进度的 Float 参数名")]
        private string _pickupProgressParameter = "PickupProgress";

        [SerializeField] [Tooltip("触发直接拾取动画的 Trigger 参数名")]
        private string _pickupTriggerParameter = "Pickup";

        [SerializeField] [Tooltip("控制角色 X 方向的 Float 参数名，左为 -1，右为 1")]
        private string _moveXParameter = "MoveX";

        [SerializeField] [Tooltip("控制角色 Z 方向的 Float 参数名，后方为 -1，前方为 1")]
        private string _moveZParameter = "MoveZ";

        private int _isMovingParameterHash;
        private int _isHarvestingParameterHash;
        private int _harvestProgressParameterHash;
        private int _isPickingUpParameterHash;
        private int _pickupProgressParameterHash;
        private int _pickupTriggerParameterHash;
        private int _moveXParameterHash;
        private int _moveZParameterHash;
        private bool _lastIsMoving;
        private bool _lastIsHarvesting;
        private float _lastHarvestProgress;
        private bool _lastIsPickingUp;
        private float _lastPickupProgress;
        private Vector3 _lastMoveDirection = Vector3.back;
        private Vector2 _previousRawMoveInput;
        private bool _hasAppliedState;

        private void Awake()
        {
            if (_playerMotor == null)
            {
                _playerMotor = GetComponent<PlayerMotor>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_harvestController == null)
            {
                _harvestController = GetComponent<HarvestController>();
            }

            _isMovingParameterHash = Animator.StringToHash(_isMovingParameter);
            _isHarvestingParameterHash = Animator.StringToHash(_isHarvestingParameter);
            _harvestProgressParameterHash = Animator.StringToHash(_harvestProgressParameter);
            _isPickingUpParameterHash = Animator.StringToHash(_isPickingUpParameter);
            _pickupProgressParameterHash = Animator.StringToHash(_pickupProgressParameter);
            _pickupTriggerParameterHash = Animator.StringToHash(_pickupTriggerParameter);
            _moveXParameterHash = Animator.StringToHash(_moveXParameter);
            _moveZParameterHash = Animator.StringToHash(_moveZParameter);
        }

        private void Update()
        {
            if (_playerMotor == null || _animator == null || !_animator.isActiveAndEnabled)
            {
                return;
            }

            bool isMoving = _playerMotor.IsMoving;
            bool isHarvesting = _harvestController != null && _harvestController.IsHarvesting;
            float harvestProgress = isHarvesting && _harvestController != null
                ? Mathf.Clamp01(_harvestController.HarvestProgress)
                : 0f;
            bool isPickingUp = _harvestController != null && _harvestController.IsPickingUp;
            float pickupProgress = isPickingUp && _harvestController != null
                ? Mathf.Clamp01(_harvestController.PickupProgress)
                : 0f;
            Vector2 rawMoveInput = _playerMotor.RawMoveInput;
            Vector3 moveDirection = ResolveMoveDirection(isMoving, rawMoveInput);
            _previousRawMoveInput = rawMoveInput;

            if (_harvestController != null && _harvestController.ConsumePickupAnimationRequest())
            {
                _animator.SetTrigger(_pickupTriggerParameterHash);
            }

            if (_hasAppliedState && isMoving == _lastIsMoving
                && isHarvesting == _lastIsHarvesting
                && Mathf.Approximately(harvestProgress, _lastHarvestProgress)
                && isPickingUp == _lastIsPickingUp
                && Mathf.Approximately(pickupProgress, _lastPickupProgress)
                && Vector3.SqrMagnitude(moveDirection - _lastMoveDirection) <= 0.0001f)
            {
                return;
            }

            _animator.SetBool(_isMovingParameterHash, isMoving);
            _animator.SetBool(_isHarvestingParameterHash, isHarvesting);
            _animator.SetFloat(_harvestProgressParameterHash, harvestProgress);
            _animator.SetBool(_isPickingUpParameterHash, isPickingUp);
            _animator.SetFloat(_pickupProgressParameterHash, pickupProgress);
            _animator.SetFloat(_moveXParameterHash, moveDirection.x);
            _animator.SetFloat(_moveZParameterHash, moveDirection.z);
            _lastIsMoving = isMoving;
            _lastIsHarvesting = isHarvesting;
            _lastHarvestProgress = harvestProgress;
            _lastIsPickingUp = isPickingUp;
            _lastPickupProgress = pickupProgress;
            _lastMoveDirection = moveDirection;
            _hasAppliedState = true;
        }

        /// <summary>
        /// 获取当前或最近一次有效的视角空间移动方向，并量化为现有的四个角色视角。
        /// </summary>
        private Vector3 ResolveMoveDirection(bool isMoving, Vector2 rawInput)
        {
            if (!isMoving)
            {
                return _lastMoveDirection;
            }

            bool hasHorizontal = Mathf.Abs(rawInput.x) > 0.01f;
            bool hasVertical = Mathf.Abs(rawInput.y) > 0.01f;
            bool horizontalStarted = hasHorizontal
                && (!HasAxis(_previousRawMoveInput.x)
                    || Mathf.Sign(rawInput.x) != Mathf.Sign(_previousRawMoveInput.x));
            bool verticalStarted = hasVertical
                && (!HasAxis(_previousRawMoveInput.y)
                    || Mathf.Sign(rawInput.y) != Mathf.Sign(_previousRawMoveInput.y));

            if (!hasHorizontal)
            {
                return hasVertical ? VerticalDirection(rawInput.y) : _lastMoveDirection;
            }

            if (!hasVertical)
            {
                return HorizontalDirection(rawInput.x);
            }

            if (verticalStarted)
            {
                return VerticalDirection(rawInput.y);
            }

            if (horizontalStarted)
            {
                return HorizontalDirection(rawInput.x);
            }

            return _lastMoveDirection;
        }

        /// <summary>判断一个输入轴是否超过死区。</summary>
        private static bool HasAxis(float value)
        {
            return Mathf.Abs(value) > 0.01f;
        }

        /// <summary>将水平输入转换为左或右的动画方向。</summary>
        private static Vector3 HorizontalDirection(float value)
        {
            return value < 0f ? Vector3.left : Vector3.right;
        }

        /// <summary>将垂直输入转换为上或下的动画方向。</summary>
        private static Vector3 VerticalDirection(float value)
        {
            return value < 0f ? Vector3.back : Vector3.forward;
        }

        /// <summary>
        /// 在编辑器中检查必要引用，减少运行时配置错误。
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_isMovingParameter))
            {
                _isMovingParameter = "IsMoving";
            }

            _isMovingParameterHash = Animator.StringToHash(_isMovingParameter);
            _isHarvestingParameterHash = Animator.StringToHash(_isHarvestingParameter);
            _harvestProgressParameterHash = Animator.StringToHash(_harvestProgressParameter);
            _isPickingUpParameterHash = Animator.StringToHash(_isPickingUpParameter);
            _pickupProgressParameterHash = Animator.StringToHash(_pickupProgressParameter);
            _pickupTriggerParameterHash = Animator.StringToHash(_pickupTriggerParameter);
            _moveXParameterHash = Animator.StringToHash(_moveXParameter);
            _moveZParameterHash = Animator.StringToHash(_moveZParameter);
        }
    }
}
