using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 采集点运行时实例。由 ResourceNodeSpawner 克隆模板后生成，
    /// 自带采集条件与产出。采集完成后由交互控制器负责销毁。
    /// Sprite 始终面向相机（Billboard）。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ResourceNode : MonoBehaviour
    {
        /// <summary>所属地区（仅用于程序生成归属/显示，不参与采集判定）。</summary>
        public AreaConfigSO BelongArea { get; private set; }

        /// <summary>采集条件：所需工具类型。</summary>
        public ToolType HarvestToolType => _config != null ? _config.harvestToolType : ToolType.None;

        /// <summary>采集条件：所需工具等级。</summary>
        public int HarvestToolLevel => _config != null ? _config.harvestToolLevel : -1;

        /// <summary>单次采集理智消耗。</summary>
        public float HarvestSanityCost => _config != null ? _config.harvestSanityCost : 0f;

        /// <summary>按住采集到完成的耗时。</summary>
        public float HarvestDuration => _config != null ? _config.harvestDuration : 1f;

        /// <summary>当前资源点是否可以直接按 F 拾取。</summary>
        public bool IsDirectPickup => _config != null && _config.nodeType == ResourceNodeType.Pickup;

        /// <summary>采集完成后的总产出。</summary>
        public HarvestYield[] Yields => _config != null ? _config.yields : null;

        /// <summary>本采集点是否已被采完（用于交互逻辑跳过）。</summary>
        public bool IsDepleted { get; private set; }

        private ResourceNodeConfig _config;
        private SpriteRenderer _spriteRenderer;
        private Camera _mainCamera;

        // 共享的占位精灵，避免每个节点都创建新的 Texture2D
        private static Sprite _sharedPlaceholderSprite;

        /// <summary>
        /// 用模板配置初始化本实例。必须在 Awake/Start 前调用（生成时立即调用）。
        /// </summary>
        public void Initialize(ResourceNodeConfig config, AreaConfigSO belongArea)
        {
            _config = config;
            BelongArea = belongArea;
            IsDepleted = false;

            // 立即应用外观（此时 SpriteRenderer 可能还没获取，需要先获取）
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            ApplyAppearance();
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            // 如果 Initialize 还没调用，Awake 时再应用一次外观
            if (_config != null)
            {
                ApplyAppearance();
            }
        }

        private void LateUpdate()
        {
            // Billboard：Sprite 始终面向相机（2.5D 斜俯视角）
            if (_mainCamera != null)
            {
                // 与相机旋转一致，让 Sprite 始终正对相机
                transform.rotation = _mainCamera.transform.rotation;
            }
        }

        /// <summary>生成时根据模板套用外观。</summary>
        private void ApplyAppearance()
        {
            if (_spriteRenderer == null || _config == null) return;

            if (_config.sprite != null)
            {
                // 使用配置的精灵
                _spriteRenderer.sprite = _config.sprite;
            }
            else
            {
                // 无精灵时按露头类型给个占位色块，便于早期验证
                _spriteRenderer.color = NodeTypeColor(_config.nodeType);

                // 用共享的占位精灵（避免每个节点都创建新 Texture2D）
                if (_sharedPlaceholderSprite == null)
                {
                    _sharedPlaceholderSprite = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0, 0, 4, 4),
                        new Vector2(0.5f, 0.5f),
                        8f);
                }
                _spriteRenderer.sprite = _sharedPlaceholderSprite;
            }
        }

        /// <summary>标记为已采完（交互控制器在采集完成时调用）。</summary>
        public void MarkDepleted()
        {
            IsDepleted = true;
            _spriteRenderer.color = new Color(1f, 1f, 1f, 0.25f); // 变淡示意
        }

        /// <summary>销毁自己。由交互控制器在采集完成动画/延迟后调用。</summary>
        public void DestroyNode()
        {
            Destroy(gameObject);
        }

        /// <summary>露头类型 → 占位颜色。</summary>
        private static Color NodeTypeColor(ResourceNodeType type)
        {
            switch (type)
            {
                case ResourceNodeType.Harvest: return new Color(0.6f, 0.5f, 0.3f);  // 开采资源棕色
                case ResourceNodeType.Pickup: return new Color(0.4f, 0.75f, 0.9f);  // 直接拾取青色
                default: return Color.gray;
            }
        }
    }
}
