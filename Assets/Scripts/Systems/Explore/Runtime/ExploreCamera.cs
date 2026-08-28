using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 探索相机（跟随角色 · 2.5D 斜视角）。
    /// 相机是角色的"挂件"：始终贴在角色固定方位+固定角度，故转视角时相机跟着角色走，永远照得到角色。
    /// 朝向（yaw）由 PlayerMotor 持有（Q/E 步进转身），本类每帧读角色朝向，把自身 offset 绕 yaw 旋转后定位。
    ///   - rotation = Euler(tiltAngle, yaw, 0)：与角色一致，维持贴图正对相机。
    ///   - position = 角色位置 + RotY(yaw)·offset ：offset 随角色转，相机绕角色旋转。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ExploreCamera : MonoBehaviour
    {
        [Header("目标")]
        [Tooltip("目标玩家；为空时自动查找 PlayerMotor 所在对象")]
        public PlayerMotor target;

        [Header("视角")]
        [Tooltip("相机相对角色的偏移（角色本地朝向 0° 时）。斜俯视 XZ 地面时相机应在角色 -Z 侧上方。")]
        public Vector3 offset = new Vector3(0f, 9f, -9f);

        [Header("跟随")]
        [Tooltip("玩家移动时的位置平滑时间；数值越小跟随越紧，0 为直接吸附")]
        [Min(0f)]
        public float smoothTime = 0.2f;

        [Header("缩放")]
        [Tooltip("鼠标滚轮每格改变的相机距离")]
        [Min(0f)]
        public float zoomSpeed = 1f;

        [Tooltip("相机距离的最小值")]
        [Min(0.01f)]
        public float minZoomDistance = 6f;

        [Tooltip("相机距离的最大值")]
        [Min(0.01f)]
        public float maxZoomDistance = 18f;

        private Camera _camera;
        private Vector3 _followPosition;
        private Vector3 _followVelocity;
        private float _currentYaw;
        private float _currentZoomDistance;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                target = FindObjectOfType<PlayerMotor>();
            }

            // 起始直接吸附，避免开场飞入
            if (target != null)
            {
                NormalizeZoomLimits();
                _currentZoomDistance = Mathf.Clamp(
                    offset.magnitude,
                    minZoomDistance,
                    maxZoomDistance);
                _followPosition = target.transform.position;
                _currentYaw = target.Yaw;
                ApplyCameraTransform(true);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            ApplyCameraTransform(false);
        }

        private void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f)) return;

            NormalizeZoomLimits();
            _currentZoomDistance = Mathf.Clamp(
                _currentZoomDistance - scroll * zoomSpeed,
                minZoomDistance,
                maxZoomDistance);
        }

        /// <summary>
        /// 按角色朝向把相机定位到角色固定方位。
        ///   - 位置：角色位置 + RotY(yaw)·offset（offset 绕角色 yaw 转，相机始终在角色固定相对方位）
        ///   - 朝向：Euler(tiltAngle, yaw, 0) 与角色一致
        /// </summary>
        private void ApplyCameraTransform(bool snap)
        {
            float targetYaw = target.Yaw;
            float tilt = target.TiltAngle;

            _currentYaw = targetYaw;
            transform.rotation = Quaternion.Euler(tilt, _currentYaw, 0f);

            if (snap)
            {
                _followPosition = target.transform.position;
                _followVelocity = Vector3.zero;
            }
            else if (smoothTime <= 0f)
            {
                _followPosition = target.transform.position;
                _followVelocity = Vector3.zero;
            }
            else
            {
                // 只平滑玩家世界位置，避免 QE 改变相机轨道时产生位置滞后。
                _followPosition = Vector3.SmoothDamp(
                    _followPosition,
                    target.transform.position,
                    ref _followVelocity,
                    smoothTime);
            }

            // 当前 Yaw 直接作用于轨道偏移，旋转时不经过 SmoothDamp。
            Vector3 zoomedOffset = offset;
            if (offset.sqrMagnitude > 0.0001f)
            {
                zoomedOffset = offset.normalized * _currentZoomDistance;
            }

            transform.position = _followPosition + RotateY(zoomedOffset, _currentYaw);
        }

        private void NormalizeZoomLimits()
        {
            minZoomDistance = Mathf.Max(0.01f, minZoomDistance);
            maxZoomDistance = Mathf.Max(minZoomDistance, maxZoomDistance);
        }

        /// <summary>绕世界 Y 轴旋转向量。</summary>
        private static Vector3 RotateY(Vector3 v, float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector3(v.x * cos + v.z * sin, v.y, -v.x * sin + v.z * cos);
        }
    }
}
