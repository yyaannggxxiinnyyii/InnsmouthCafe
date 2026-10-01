using UnityEngine;

/// <summary>
/// 通过鼠标位置向 LiquidFun 粒子系统持续发射单个液体粒子。
/// </summary>
public class LiquidMouseEmitter : MonoBehaviour
{
    [Header("粒子来源")]
    [SerializeField] private LPParticleSystem particleSystem;
    [SerializeField] private Camera targetCamera;

    [Header("发射设置")]
    [SerializeField] private float particlesPerSecond = 18f;
    [SerializeField] private float minParticlesPerSecond = 1f;
    [SerializeField] private float maxParticlesPerSecond = 60f;
    [SerializeField] private float scrollSensitivity = 5f;
    [SerializeField] private float initialVelocity = 0f;
    [SerializeField] private Color particleColor = new Color(0.16f, 0.38f, 0.72f, 1f);
    [SerializeField] private int userData;

    private float _spawnAccumulator;

    /// <summary>
    /// 缓存相机引用并限制初始发射速度。
    /// </summary>
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        particlesPerSecond = Mathf.Clamp(
            particlesPerSecond,
            minParticlesPerSecond,
            maxParticlesPerSecond);
    }

    /// <summary>
    /// 处理滚轮调速和鼠标按住发射。
    /// </summary>
    private void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scroll, 0f))
        {
            particlesPerSecond = Mathf.Clamp(
                particlesPerSecond + scroll * scrollSensitivity,
                minParticlesPerSecond,
                maxParticlesPerSecond);
        }

        if (!Input.GetMouseButton(0) || particleSystem == null || targetCamera == null)
        {
            _spawnAccumulator = 0f;
            return;
        }

        _spawnAccumulator += Time.deltaTime * particlesPerSecond;
        while (_spawnAccumulator >= 1f)
        {
            SpawnParticleAtMouse();
            _spawnAccumulator -= 1f;
        }
    }

    /// <summary>
    /// 在鼠标所在的世界坐标创建一个 LiquidFun 粒子。
    /// </summary>
    private void SpawnParticleAtMouse()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(screenPosition);
        LPAPIParticles.CreateParticleInSystem(
            particleSystem.GetPtr(),
            0,
            worldPosition.x,
            worldPosition.y,
            0f,
            initialVelocity,
            Mathf.RoundToInt(particleColor.r * 255f),
            Mathf.RoundToInt(particleColor.g * 255f),
            Mathf.RoundToInt(particleColor.b * 255f),
            Mathf.RoundToInt(particleColor.a * 255f),
            0f);
    }
}
