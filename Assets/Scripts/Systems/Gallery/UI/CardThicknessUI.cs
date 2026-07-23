using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 为透视相机下的图鉴卡牌创建圆角实体侧壁，使卡牌倾斜时呈现厚度。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CardThicknessUI : MonoBehaviour
    {
        [Header("厚度外观")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("卡牌从正面向背面延伸的厚度，单位与当前 Canvas UI 尺寸一致")]
        private float _thickness = 8f;

        [SerializeField]
        [Tooltip("卡牌四个侧面的基础颜色")]
        private Color _edgeColor = new Color(0.18f, 0.07f, 0.08f, 1f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("厚度侧壁的圆角半径，需要与卡牌正面贴图的圆角轮廓匹配")]
        private float _cornerRadius = 10f;

        [SerializeField]
        [Range(1, 16)]
        [Tooltip("每个圆角使用的分段数量；数值越高轮廓越平滑，推荐 4 到 8")]
        private int _cornerSegments = 6;

        [SerializeField]
        [Tooltip("侧面使用的可选纹理；为空时使用纯色 UI 纹理")]
        private Sprite _edgeSprite;

        [SerializeField]
        [Tooltip("侧面使用的可选材质；为空时使用默认 UI 材质")]
        private Material _edgeMaterial;

        private RectTransform _rectTransform;
        private CardRoundedEdgeGraphic _roundedEdgeGraphic;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            EnsureEdges();
            RefreshGeometry();
        }

        private void OnEnable()
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            EnsureEdges();
            RefreshGeometry();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!Application.isPlaying || _rectTransform == null)
            {
                return;
            }

            RefreshGeometry();
        }

        private void OnValidate()
        {
            _thickness = Mathf.Max(0f, _thickness);
            _cornerRadius = Mathf.Max(0f, _cornerRadius);
            _cornerSegments = Mathf.Clamp(_cornerSegments, 1, 16);
            if (Application.isPlaying)
            {
                RefreshGeometry();
            }
        }

        /// <summary>
        /// 根据当前卡牌尺寸和厚度参数刷新四个侧面的三维位置与外观。
        /// </summary>
        public void RefreshGeometry()
        {
            if (_rectTransform == null)
            {
                return;
            }

            EnsureEdges();

            float thickness = Mathf.Max(0f, _thickness);
            bool showEdges = thickness > 0.001f;
            _roundedEdgeGraphic.gameObject.SetActive(showEdges);
            if (!showEdges)
            {
                return;
            }

            Rect cardRect = _rectTransform.rect;
            RectTransform edgeRectTransform = _roundedEdgeGraphic.rectTransform;
            edgeRectTransform.sizeDelta = cardRect.size;
            edgeRectTransform.anchoredPosition3D = new Vector3(
                cardRect.center.x,
                cardRect.center.y,
                0f);
            edgeRectTransform.localRotation = Quaternion.identity;
            edgeRectTransform.localScale = Vector3.one;
            _roundedEdgeGraphic.Configure(
                thickness,
                _cornerRadius,
                _cornerSegments,
                _edgeSprite,
                _edgeMaterial,
                _edgeColor);
        }

        /// <summary>
        /// 确保圆角侧壁节点只创建一次，并放在其他卡面子节点之前渲染。
        /// </summary>
        private void EnsureEdges()
        {
            if (_rectTransform == null)
            {
                return;
            }

            if (_roundedEdgeGraphic == null)
            {
                Transform existingEdge = transform.Find("卡牌厚度-圆角侧壁");
                _roundedEdgeGraphic = existingEdge != null
                    ? existingEdge.GetComponent<CardRoundedEdgeGraphic>()
                    : null;
            }

            _roundedEdgeGraphic ??= CreateRoundedEdge();
            RemoveLegacyEdges();
        }

        /// <summary>
        /// 创建一个不参与射线检测的圆角侧壁 Graphic。
        /// </summary>
        private CardRoundedEdgeGraphic CreateRoundedEdge()
        {
            GameObject edgeObject = new(
                "卡牌厚度-圆角侧壁",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CardRoundedEdgeGraphic));
            edgeObject.layer = gameObject.layer;
            edgeObject.transform.SetParent(transform, false);
            edgeObject.transform.SetAsFirstSibling();

            RectTransform edgeRectTransform = edgeObject.GetComponent<RectTransform>();
            edgeRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            edgeRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            edgeRectTransform.pivot = new Vector2(0.5f, 0.5f);

            CardRoundedEdgeGraphic roundedEdgeGraphic = edgeObject.GetComponent<CardRoundedEdgeGraphic>();
            roundedEdgeGraphic.raycastTarget = false;
            roundedEdgeGraphic.maskable = true;
            return roundedEdgeGraphic;
        }

        /// <summary>
        /// 清理脚本热重载前可能残留的旧版四面厚度节点。
        /// </summary>
        private void RemoveLegacyEdges()
        {
            string[] legacyEdgeNames =
            {
                "卡牌厚度-左侧",
                "卡牌厚度-右侧",
                "卡牌厚度-顶部",
                "卡牌厚度-底部"
            };

            foreach (string edgeName in legacyEdgeNames)
            {
                Transform legacyEdge = transform.Find(edgeName);
                if (legacyEdge != null)
                {
                    Destroy(legacyEdge.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 沿圆角矩形轮廓生成卡牌厚度侧壁，并兼容 UGUI 的材质、遮罩与裁剪流程。
    /// </summary>
    [DisallowMultipleComponent]
    internal class CardRoundedEdgeGraphic : MaskableGraphic
    {
        private readonly List<Vector2> _perimeterPoints = new();
        private readonly List<float> _perimeterDistances = new();

        private Sprite _sprite;
        private float _thickness;
        private float _cornerRadius;
        private int _cornerSegments = 6;

        public override Texture mainTexture => _sprite != null
            ? _sprite.texture
            : Texture2D.whiteTexture;

        /// <summary>
        /// 更新圆角侧壁的几何与外观参数，并请求 UGUI 重建网格。
        /// </summary>
        public void Configure(
            float thickness,
            float cornerRadius,
            int cornerSegments,
            Sprite sprite,
            Material edgeMaterial,
            Color edgeColor)
        {
            _thickness = Mathf.Max(0f, thickness);
            _cornerRadius = Mathf.Max(0f, cornerRadius);
            _cornerSegments = Mathf.Max(1, cornerSegments);
            _sprite = sprite;
            material = edgeMaterial;
            color = edgeColor;
            raycastTarget = false;
            maskable = true;
            SetVerticesDirty();
            SetMaterialDirty();
        }

        /// <summary>
        /// 根据当前矩形尺寸生成圆角轮廓，并沿厚度方向连接为连续侧壁。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_thickness <= 0.001f)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float maximumRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float radius = Mathf.Clamp(_cornerRadius, 0f, maximumRadius);
            BuildPerimeter(rect, radius);
            if (_perimeterPoints.Count < 2)
            {
                return;
            }

            BuildPerimeterDistances();
            Vector4 outerUv = _sprite != null
                ? DataUtility.GetOuterUV(_sprite)
                : new Vector4(0f, 0f, 1f, 1f);
            Color32 vertexColor = color;
            float perimeterLength = _perimeterDistances[^1];

            for (int index = 0; index < _perimeterPoints.Count; index++)
            {
                int nextIndex = (index + 1) % _perimeterPoints.Count;
                float startDistance = _perimeterDistances[index];
                float endDistance = nextIndex == 0
                    ? perimeterLength
                    : _perimeterDistances[nextIndex];
                float startU = Mathf.Lerp(outerUv.x, outerUv.z, startDistance / perimeterLength);
                float endU = Mathf.Lerp(outerUv.x, outerUv.z, endDistance / perimeterLength);
                AddSideQuad(
                    vertexHelper,
                    _perimeterPoints[index],
                    _perimeterPoints[nextIndex],
                    startU,
                    endU,
                    outerUv,
                    vertexColor);
            }
        }

        /// <summary>
        /// 按逆时针方向生成圆角矩形周长采样点。
        /// </summary>
        private void BuildPerimeter(Rect rect, float radius)
        {
            _perimeterPoints.Clear();
            if (radius <= 0.001f)
            {
                _perimeterPoints.Add(new Vector2(rect.xMax, rect.yMax));
                _perimeterPoints.Add(new Vector2(rect.xMin, rect.yMax));
                _perimeterPoints.Add(new Vector2(rect.xMin, rect.yMin));
                _perimeterPoints.Add(new Vector2(rect.xMax, rect.yMin));
                return;
            }

            AddCornerPoints(new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f);
            AddCornerPoints(new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f);
            AddCornerPoints(new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f);
            AddCornerPoints(new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f);
        }

        /// <summary>
        /// 为单个圆角追加指定数量的圆弧采样点。
        /// </summary>
        private void AddCornerPoints(Vector2 center, float radius, float startAngle)
        {
            for (int segment = 0; segment <= _cornerSegments; segment++)
            {
                float angle = (startAngle + segment * 90f / _cornerSegments) * Mathf.Deg2Rad;
                _perimeterPoints.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        /// <summary>
        /// 计算每个轮廓点沿周长的累计距离，用于连续铺设侧面纹理。
        /// </summary>
        private void BuildPerimeterDistances()
        {
            _perimeterDistances.Clear();
            float distance = 0f;
            for (int index = 0; index < _perimeterPoints.Count; index++)
            {
                _perimeterDistances.Add(distance);
                int nextIndex = (index + 1) % _perimeterPoints.Count;
                distance += Vector2.Distance(
                    _perimeterPoints[index],
                    _perimeterPoints[nextIndex]);
            }

            _perimeterDistances.Add(Mathf.Max(distance, 0.0001f));
        }

        /// <summary>
        /// 在相邻轮廓点之间生成一块从卡牌正面延伸到背面的侧壁四边形。
        /// </summary>
        private void AddSideQuad(
            VertexHelper vertexHelper,
            Vector2 start,
            Vector2 end,
            float startU,
            float endU,
            Vector4 outerUv,
            Color32 vertexColor)
        {
            int startVertexIndex = vertexHelper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = vertexColor;

            vertex.position = new Vector3(start.x, start.y, 0f);
            vertex.uv0 = new Vector2(startU, outerUv.y);
            vertexHelper.AddVert(vertex);

            vertex.position = new Vector3(end.x, end.y, 0f);
            vertex.uv0 = new Vector2(endU, outerUv.y);
            vertexHelper.AddVert(vertex);

            vertex.position = new Vector3(end.x, end.y, _thickness);
            vertex.uv0 = new Vector2(endU, outerUv.w);
            vertexHelper.AddVert(vertex);

            vertex.position = new Vector3(start.x, start.y, _thickness);
            vertex.uv0 = new Vector2(startU, outerUv.w);
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(startVertexIndex, startVertexIndex + 1, startVertexIndex + 2);
            vertexHelper.AddTriangle(startVertexIndex, startVertexIndex + 2, startVertexIndex + 3);
        }
    }
}
