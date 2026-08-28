using UnityEngine;
using DG.Tweening;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索阶段玩家移动与输入组件。
    /// 负责读取探索输入、处理 XZ 平面移动和 Q/E 视角步进旋转，并向其他组件提供只读状态。
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        [Header("移动")]
        [SerializeField] [Tooltip("移动速度（单位/秒）")]
        private float _moveSpeed = 6f;

        [SerializeField] [Min(0f)]
        [Tooltip("达到满速所需的时间（秒），同时用于松开按键后的减速")]
        private float _accelerationDuration = 0.2f;

        [SerializeField] [Tooltip("移动死区，输入向量超过此值才视为移动")]
        private float _deadZone = 0.01f;

        [Header("转向（Q/E 步进）")]
        [SerializeField] [Tooltip("每次按下 Q/E 旋转的角度（度）")]
        private float _rotateStepDeg = 45f;

        [SerializeField] [Tooltip("旋转过渡时间（秒）")]
        private float _rotateDuration = 0.3f;

        [SerializeField] [Tooltip("角色和相机的俯视角（绕 X 轴）")]
        private float _tiltAngle = 45f;

        [Header("输入")]
        [SerializeField] [Tooltip("水平移动轴名称")]
        private string _horizontalAxis = "Horizontal";

        [SerializeField] [Tooltip("垂直移动轴名称")]
        private string _verticalAxis = "Vertical";

        [SerializeField] [Tooltip("采集按键")]
        private KeyCode _harvestKey = KeyCode.F;

        [SerializeField] [Tooltip("逆时针旋转按键")]
        private KeyCode _rotateLeftKey = KeyCode.Q;

        [SerializeField] [Tooltip("顺时针旋转按键")]
        private KeyCode _rotateRightKey = KeyCode.E;

        /// <summary>当前角色绕世界 Y 轴的朝向。</summary>
        public float Yaw => _yaw;

        /// <summary>角色和相机使用的俯视角。</summary>
        public float TiltAngle => _tiltAngle;

        /// <summary>当前帧的屏幕空间移动输入。</summary>
        public Vector2 MoveInput => _moveInput;

        /// <summary>最近一次有效移动方向，用于停止后保持角色朝向。</summary>
        public Vector2 LastMoveInput => _lastMoveInput;

        /// <summary>当前帧是否有有效移动输入。</summary>
        public bool IsMoving => !_movementLocked
            && _moveInput.sqrMagnitude > _deadZone * _deadZone;

        /// <summary>当前帧玩家在世界 XZ 平面的移动速度。</summary>
        public Vector3 MoveVelocity => _moveVelocity;

        /// <summary>当前帧未经归一化的视角空间移动输入。</summary>
        public Vector2 RawMoveInput => _rawMoveInput;

        /// <summary>当前玩家是否被交互动作锁定移动。</summary>
        public bool IsMovementLocked => _movementLocked;

        /// <summary>当前帧是否按住采集键。</summary>
        public bool IsHarvestHeld => Input.GetKey(_harvestKey);

        /// <summary>当前帧是否刚按下采集键。</summary>
        public bool IsHarvestPressedThisFrame => Input.GetKeyDown(_harvestKey);

        /// <summary>当前帧是否刚松开采集键。</summary>
        public bool IsHarvestReleasedThisFrame => Input.GetKeyUp(_harvestKey);

        /// <summary>
        /// 获取 Q/E 旋转步进。每次按键只返回一次，左转为 -1，右转为 +1。
        /// </summary>
        public float RotateStep
        {
            get
            {
                bool right = Input.GetKeyDown(_rotateRightKey);
                bool left = Input.GetKeyDown(_rotateLeftKey);
                if (right && left) return 0f;
                return right ? 1f : left ? -1f : 0f;
            }
        }

        private float _yaw;
        private Vector2 _moveInput;
        private Vector2 _rawMoveInput;
        private Vector2 _lastMoveInput;
        private Vector3 _moveVelocity;
        private bool _movementLocked;
        private Tween _rotateTween;

        private void Start()
        {
            ApplyCharacterFacing();
        }

        private void Update()
        {
            _moveInput = ReadMoveInput();

            if (IsMoving)
            {
                _lastMoveInput = _moveInput.normalized;
            }

            float rotateStep = RotateStep;
            if (rotateStep != 0f)
            {
                float targetYaw = _yaw + rotateStep * _rotateStepDeg;
                _rotateTween?.Kill();
                _rotateTween = DOTween.To(() => _yaw, value => _yaw = value, targetYaw, _rotateDuration)
                    .SetEase(Ease.OutCubic)
                    .OnUpdate(ApplyCharacterFacing)
                    .OnComplete(() => _rotateTween = null);
            }

            ApplyMovement();
        }

        /// <summary>
        /// 读取并归一化屏幕空间移动输入，避免斜向移动速度高于直线移动。
        /// </summary>
        private Vector2 ReadMoveInput()
        {
            float x = Input.GetAxisRaw(_horizontalAxis);
            float y = Input.GetAxisRaw(_verticalAxis);
            _rawMoveInput = new Vector2(x, y);
            Vector2 input = _rawMoveInput;
            return input.magnitude > 1f ? input.normalized : input;
        }

        /// <summary>将屏幕空间输入转换为世界 XZ 移动并应用到玩家根节点。</summary>
        private void ApplyMovement()
        {
            Vector3 targetVelocity = Vector3.zero;
            if (IsMoving)
            {
                Vector2 normalized = _moveInput.normalized;
                Vector3 moveDirection = new Vector3(normalized.x, 0f, normalized.y);
                moveDirection = RotateY(moveDirection, _yaw);
                targetVelocity = moveDirection * _moveSpeed;
            }

            if (_accelerationDuration <= 0f)
            {
                _moveVelocity = targetVelocity;
            }
            else
            {
                float acceleration = _moveSpeed / _accelerationDuration;
                _moveVelocity = Vector3.MoveTowards(
                    _moveVelocity,
                    targetVelocity,
                    acceleration * Time.deltaTime);
            }
            transform.position += _moveVelocity * Time.deltaTime;
        }

        /// <summary>
        /// 设置玩家移动锁定状态，锁定时清除当前移动速度但保留输入读取。
        /// </summary>
        public void SetMovementLocked(bool locked)
        {
            _movementLocked = locked;
            if (locked)
            {
                _moveInput = Vector2.zero;
                _moveVelocity = Vector3.zero;
            }
        }

        /// <summary>保持角色贴图与 2.5D 相机正对。</summary>
        private void ApplyCharacterFacing()
        {
            transform.rotation = Quaternion.Euler(_tiltAngle, _yaw, 0f);
        }

        /// <summary>绕世界 Y 轴旋转方向向量。</summary>
        private static Vector3 RotateY(Vector3 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector3(
                vector.x * cos + vector.z * sin,
                vector.y,
                -vector.x * sin + vector.z * cos);
        }

        private void OnDestroy()
        {
            _rotateTween?.Kill();
        }

        /// <summary>显示玩家的交互范围。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
