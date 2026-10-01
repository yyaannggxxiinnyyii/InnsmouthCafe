using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 将 LiquidFun 粒子交给 GPU 绘制为密度图，并使用 Shader 形成融合液体。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LiquidMetaballRenderer : MonoBehaviour
{
    [Header("粒子来源")]
    [SerializeField] private LPParticleSystem particleSystem;
    [Header("渲染范围")]
    [SerializeField] private Vector2 boundsCenter;
    [SerializeField] private Vector2 boundsSize = new Vector2(8f, 5f);
    [SerializeField] private int textureSize = 256;
    [SerializeField] private float particleInfluence = 1.65f;
    [Header("液体外观")]
    [SerializeField] private Color liquidColor = new Color(0.18f, 0.42f, 0.72f, 1f);
    [SerializeField] private float threshold = 0.42f;
    [SerializeField] private float edgeSoftness = 0.12f;
    [SerializeField] private Material displayMaterial;
    [SerializeField] private Material particleMaterial;

    private RenderTexture _densityTexture;
    private Material _runtimeDisplayMaterial;
    private Material _runtimeParticleMaterial;
    private MeshRenderer _meshRenderer;
    private MeshFilter _particleMeshFilter;
    private MeshRenderer _particleMeshRenderer;
    private Camera _densityCamera;
    private Mesh _particleMesh;
    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;

    /// <summary>创建 GPU 密度图、粒子网格、专用相机和输出材质。</summary>
    private void Awake()
    {
        textureSize = Mathf.Clamp(textureSize, 64, 1024);
        _meshRenderer = GetComponent<MeshRenderer>();
        _densityTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8)
        {
            name = "LiquidDensityTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        _densityTexture.Create();
        CreateParticleRenderer();
        BuildQuad();
        _runtimeDisplayMaterial = displayMaterial != null
            ? new Material(displayMaterial)
            : new Material(Shader.Find("InnsmouthCafe/Liquid Metaball"));
        _runtimeDisplayMaterial.SetColor("_LiquidColor", liquidColor);
        _runtimeDisplayMaterial.SetFloat("_Threshold", threshold);
        _runtimeDisplayMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
        _runtimeDisplayMaterial.SetTexture("_DensityTex", _densityTexture);
        _meshRenderer.sharedMaterial = _runtimeDisplayMaterial;
    }

    /// <summary>每帧只更新粒子网格位置，再由 GPU 完成密度绘制。</summary>
    private void LateUpdate()
    {
        if (particleSystem == null || particleSystem.Particles == null)
        {
            _particleMesh.Clear();
            return;
        }
        UpdateParticleMesh(particleSystem.Particles);
    }

    /// <summary>创建粒子四边形网格和只渲染密度图的隐藏相机。</summary>
    private void CreateParticleRenderer()
    {
        GameObject particleObject = new GameObject("GPU Particle Density | GPU粒子密度");
        particleObject.transform.SetParent(transform, false);
        particleObject.layer = 31;
        _particleMeshFilter = particleObject.AddComponent<MeshFilter>();
        _particleMeshRenderer = particleObject.AddComponent<MeshRenderer>();
        _particleMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _particleMeshRenderer.receiveShadows = false;
        _runtimeParticleMaterial = particleMaterial != null
            ? new Material(particleMaterial)
            : new Material(Shader.Find("InnsmouthCafe/Liquid Density Particle"));
        _particleMeshRenderer.sharedMaterial = _runtimeParticleMaterial;
        _particleMesh = new Mesh { name = "LiquidDensityParticleMesh" };
        _particleMesh.MarkDynamic();
        _particleMeshFilter.sharedMesh = _particleMesh;

        GameObject cameraObject = new GameObject("Liquid Density Camera | 密度相机");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
        cameraObject.layer = 31;
        _densityCamera = cameraObject.AddComponent<Camera>();
        _densityCamera.orthographic = true;
        _densityCamera.orthographicSize = boundsSize.y * 0.5f;
        _densityCamera.aspect = boundsSize.x / boundsSize.y;
        _densityCamera.clearFlags = CameraClearFlags.SolidColor;
        _densityCamera.backgroundColor = Color.black;
        _densityCamera.cullingMask = 1 << 31;
        _densityCamera.targetTexture = _densityTexture;
        _densityCamera.depth = -100f;
    }

    /// <summary>将粒子位置转换为局部四边形顶点，粒子重叠由 GPU 叠加。</summary>
    private void UpdateParticleMesh(List<LPParticle> particles)
    {
        int particleCount = particles.Count;
        int vertexCount = particleCount * 4;
        if (_vertices == null || _vertices.Length != vertexCount)
        {
            _vertices = new Vector3[vertexCount];
            _uvs = new Vector2[vertexCount];
            _triangles = new int[particleCount * 6];
        }
        float radius = particleSystem.ParticleRadius * particleInfluence;
        for (int i = 0; i < particleCount; i++)
        {
            Vector3 position = particles[i].Position - (Vector3)boundsCenter;
            int vertex = i * 4;
            _vertices[vertex] = position + new Vector3(-radius, -radius);
            _vertices[vertex + 1] = position + new Vector3(-radius, radius);
            _vertices[vertex + 2] = position + new Vector3(radius, radius);
            _vertices[vertex + 3] = position + new Vector3(radius, -radius);
            _uvs[vertex] = new Vector2(0f, 0f);
            _uvs[vertex + 1] = new Vector2(0f, 1f);
            _uvs[vertex + 2] = new Vector2(1f, 1f);
            _uvs[vertex + 3] = new Vector2(1f, 0f);
            int triangle = i * 6;
            _triangles[triangle] = vertex;
            _triangles[triangle + 1] = vertex + 1;
            _triangles[triangle + 2] = vertex + 2;
            _triangles[triangle + 3] = vertex;
            _triangles[triangle + 4] = vertex + 2;
            _triangles[triangle + 5] = vertex + 3;
        }
        _particleMesh.Clear();
        _particleMesh.vertices = _vertices;
        _particleMesh.uv = _uvs;
        _particleMesh.triangles = _triangles;
        _particleMesh.bounds = new Bounds(Vector3.zero, new Vector3(boundsSize.x, boundsSize.y, 1f));
    }

    /// <summary>创建与世界范围对应的输出平面。</summary>
    private void BuildQuad()
    {
        Mesh mesh = new Mesh { name = "LiquidMetaballQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-boundsSize.x * 0.5f, -boundsSize.y * 0.5f),
            new Vector3(-boundsSize.x * 0.5f, boundsSize.y * 0.5f),
            new Vector3(boundsSize.x * 0.5f, boundsSize.y * 0.5f),
            new Vector3(boundsSize.x * 0.5f, -boundsSize.y * 0.5f)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        transform.position = new Vector3(boundsCenter.x, boundsCenter.y, transform.position.z);
    }

    /// <summary>释放 GPU 资源和运行时创建的对象。</summary>
    private void OnDestroy()
    {
        if (_densityCamera != null) Destroy(_densityCamera.gameObject);
        if (_particleMesh != null) Destroy(_particleMesh);
        if (_densityTexture != null) Destroy(_densityTexture);
        if (_runtimeDisplayMaterial != null) Destroy(_runtimeDisplayMaterial);
        if (_runtimeParticleMaterial != null) Destroy(_runtimeParticleMaterial);
    }
}
