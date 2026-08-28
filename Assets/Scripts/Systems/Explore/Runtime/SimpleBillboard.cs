using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 简单 Billboard（只同步相机 Y 轴旋转）。
    /// 挂在单个海浪子物体上，只改变其世界 Y 轴朝向并保留原有倾斜姿态。
    /// </summary>
    public class SimpleBillboard : MonoBehaviour
    {
        private Camera _camera;
        private Quaternion _baseWorldRotation;

        private void Awake()
        {
            _baseWorldRotation = transform.rotation;
        }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                // 获取相机的 Y 轴旋转角度
                float cameraYaw = _camera.transform.eulerAngles.y;

                // 绕世界 Y 轴旋转基础姿态，不修改 Transform 的位置。
                transform.rotation = Quaternion.AngleAxis(cameraYaw, Vector3.up) * _baseWorldRotation;
            }
        }
    }
}
