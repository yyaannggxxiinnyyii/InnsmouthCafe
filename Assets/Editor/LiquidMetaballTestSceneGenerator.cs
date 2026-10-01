#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 创建独立的 LiquidFun 融合液体测试场景。
/// </summary>
public static class LiquidMetaballTestSceneGenerator
{
    private const string ScenePath = "Assets/Scenes/LiquidMetaballTestScene.unity";
    /// <summary>
    /// 生成并保存液体融合测试场景。
    /// </summary>
    [MenuItem("InnsmouthCafe/Liquid/Generate Metaball Test Scene")]
    public static void Generate()
    {
        EnsureDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject managerObject = new GameObject("LPManager | 液体物理世界");
        LPManager manager = managerObject.AddComponent<LPManager>();
        manager.Gravity = new Vector2(0f, -9.81f);
        manager.DebugMessages = false;

        GameObject systemObject = new GameObject("LiquidParticleSystem | 测试液体");
        LPParticleSystem system = systemObject.AddComponent<LPParticleSystem>();
        system.ParticleRadius = 0.12f;
        system.Damping = 0.85f;
        system.GravityScale = 1f;
        system.SurfaceTensionNormalStrenght = 0.45f;
        system.SurfaceTensionPressureStrenght = 0.35f;
        system.ViscousStrenght = 5f;

        GameObject rendererObject = new GameObject("LiquidMetaballRenderer | 融合显示");
        LiquidMetaballRenderer renderer = rendererObject.AddComponent<LiquidMetaballRenderer>();
        SerializedObject rendererData = new SerializedObject(renderer);
        rendererData.FindProperty("particleSystem").objectReferenceValue = system;
        rendererData.FindProperty("boundsCenter").vector2Value = new Vector2(0f, 0.5f);
        rendererData.FindProperty("boundsSize").vector2Value = new Vector2(8f, 5f);
        rendererData.ApplyModifiedPropertiesWithoutUndo();

        CreateBoundary(new Vector2(0f, -2f), new Vector2(8f, 0.25f), "Floor | 液体底部");
        CreateBoundary(new Vector2(-3.9f, 0.5f), new Vector2(0.25f, 5f), "LeftWall | 左边界");
        CreateBoundary(new Vector2(3.9f, 0.5f), new Vector2(0.25f, 5f), "RightWall | 右边界");

        GameObject groupObject = new GameObject("InitialLiquid | 初始液体");
        groupObject.transform.position = new Vector3(-1.8f, -1.4f, 0f);
        LPParticleGroupCircle group = groupObject.AddComponent<LPParticleGroupCircle>();
        group.Radius = 0.8f;
        group.ParticleSystemImIn = 0;
        group.SpawnOnPlay = true;
        group._Color = new Color(0.16f, 0.38f, 0.72f, 1f);

        GameObject emitterObject = new GameObject("TestEmitter | 测试发射器");
        LiquidMouseEmitter mouseEmitter = emitterObject.AddComponent<LiquidMouseEmitter>();
        SerializedObject emitterData = new SerializedObject(mouseEmitter);
        emitterData.FindProperty("particleSystem").objectReferenceValue = system;
        emitterData.FindProperty("particlesPerSecond").floatValue = 18f;
        emitterData.FindProperty("particleColor").colorValue = group._Color;
        emitterData.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 3.1f;
        camera.transform.position = new Vector3(0f, 0.5f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
        camera.cullingMask = ~(1 << 31);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = rendererObject;
        AssetDatabase.Refresh();
        Debug.Log("[Liquid] 已生成测试场景: " + ScenePath);
    }

    /// <summary>
    /// 创建一个静态 LiquidFun 矩形边界。
    /// </summary>
    private static void CreateBoundary(Vector2 position, Vector2 size, string objectName)
    {
        GameObject boundaryObject = new GameObject(objectName);
        boundaryObject.transform.position = position;
        LPBody body = boundaryObject.AddComponent<LPBody>();
        body.BodyType = LPBodyTypes.Static;
        LPFixtureBox fixture = boundaryObject.AddComponent<LPFixtureBox>();
        fixture.Size = size;
    }

    /// <summary>
    /// 确保编辑器生成目标目录存在。
    /// </summary>
    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
#endif
