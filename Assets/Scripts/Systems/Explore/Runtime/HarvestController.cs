using UnityEngine;

namespace InnsmouthCafe.Explore
{
    /// <summary>
    /// 采集交互控制器（设计文档 §10）。
    /// 玩家走到采集点交互范围内 → 显示提示 → 按住采集键 → 播进度 → 消耗理智 + 获得材料。
    /// 工具条件不满足时提示「需要 XX 工具（等级 N）」，不进入采集、不消耗理智。
    ///
    /// 占位说明：玩家背包/工具等级系统尚未接入，本控制器用「工具状态」临时模拟。
    /// 后续接入真实背包后，把 CheckTool 改为读取玩家工具数据即可。
    /// </summary>
    public class HarvestController : MonoBehaviour
    {
        [Header("交互范围")]
        [Tooltip("能采集的最远距离（世界单位）")]
        public float interactRadius = 1.6f;

        [Header("引用")]
        [Tooltip("输入读取器；为空时自动查找")]
        public PlayerMotor playerMotor;

        [Tooltip("理智管理器；为空时自动查找")]
        public ExploreSanityManager sanity;

        [Tooltip("生成器（用于遍历候选采集点）；为空时自动查找")]
        public ResourceNodeSpawner spawner;

        [Header("直接拾取")]
        [SerializeField] [Min(0.01f)]
        [Tooltip("直接拾取的锁定时长，同时作为拾取动画的播放时长（秒）")]
        private float _pickupDuration = 0.8f;

        [Header("工具持有状态 [占位]")]
        [Tooltip("占位：玩家目前持有的每类工具最高等级。后续替换为真实背包数据。")]
        public ToolState toolState = new ToolState();

        /// <summary>当前交互范围内的采集点（可能为 null）。</summary>
        public ResourceNode FocusedNode { get; private set; }

        private float _progress;
        private ResourceNode _harvesting;
        private ResourceNode _pickingUp;
        private float _pickupElapsed;
        private bool _pickupAnimationRequested;

        /// <summary>是否处于采集过程中（按住键且目标有效）。</summary>
        public bool IsHarvesting => _harvesting != null;

        /// <summary>当前采集进度 0-1。</summary>
        public float HarvestProgress => _progress;

        /// <summary>直接拾取是否正在进行。</summary>
        public bool IsPickingUp => _pickingUp != null;

        /// <summary>直接拾取的当前进度 0-1。</summary>
        public float PickupProgress => IsPickingUp
            ? Mathf.Clamp01(_pickupElapsed / Mathf.Max(0.01f, _pickupDuration))
            : 0f;

        /// <summary>直接拾取的锁定时长，同时作为拾取动画时长。</summary>
        public float PickupDuration => Mathf.Max(0.01f, _pickupDuration);

        [System.Serializable]
        public class ToolState
        {
            public ToolType type = ToolType.Pickaxe;
            public int level = 0;
        }

        private void Awake()
        {
            if (playerMotor == null) playerMotor = GetComponent<PlayerMotor>();
            if (playerMotor == null) playerMotor = FindObjectOfType<PlayerMotor>();
            if (sanity == null) sanity = FindObjectOfType<ExploreSanityManager>();
            if (spawner == null) spawner = FindObjectOfType<ResourceNodeSpawner>();
        }

        private void Update()
        {
            if (playerMotor == null) return;

            if (_pickingUp != null)
            {
                UpdateDirectPickup();
            }
            else if (_harvesting != null)
            {
                UpdateHarvest();
            }
            else
            {
                FocusNode();
                // 靠近有效采集点且按下采集键 → 起手采集（按住生效）
                if (FocusedNode != null && playerMotor.IsHarvestPressedThisFrame)
                {
                    TryBeginHarvest();
                }
            }
        }

        /// <summary>更新直接拾取进度，并在完成后结算资源。</summary>
        private void UpdateDirectPickup()
        {
            if (_pickingUp == null || _pickingUp.IsDepleted)
            {
                CancelDirectPickup();
                return;
            }

            _pickupElapsed += Time.deltaTime;
            if (_pickupElapsed < PickupDuration)
            {
                return;
            }

            ResourceNode node = _pickingUp;
            _pickingUp = null;
            _pickupElapsed = 0f;
            playerMotor.SetMovementLocked(false);

            _harvesting = node;
            _progress = 1f;
            CompleteHarvest();
        }

        /// <summary>取消失效目标的直接拾取并解除玩家移动锁定。</summary>
        private void CancelDirectPickup()
        {
            _pickingUp = null;
            _pickupElapsed = 0f;
            if (playerMotor != null)
            {
                playerMotor.SetMovementLocked(false);
            }
        }

        /// <summary>在当前场景所有采集点里找最近的且在交互半径内的有效点。</summary>
        private void FocusNode()
        {
            FocusedNode = null;
            if (spawner == null) return;

            float bestSqr = interactRadius * interactRadius;
            ResourceNode best = null;

            foreach (var node in spawner.Nodes)
            {
                if (node == null || node.IsDepleted) continue;

                // 修正：使用 3D 距离（XZ 平面游戏中需要完整的 3D 距离）
                Vector3 to = node.transform.position - transform.position;
                float sqr = to.sqrMagnitude;

                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }

            FocusedNode = best;
        }

        /// <summary>采集过程：起手/持续/完成。</summary>
        private void UpdateHarvest()
        {
            // 目标已失效（被采完/销毁）则终止
            if (_harvesting == null || _harvesting.IsDepleted)
            {
                EndHarvest();
                return;
            }

            // 松开采集键 → 重置进度（进度条退回）
            if (playerMotor.IsHarvestReleasedThisFrame)
            {
                ResetHarvest();
                return;
            }

            if (playerMotor.IsHarvestHeld)
            {
                _progress += Time.deltaTime / Mathf.Max(0.01f, _harvesting.HarvestDuration);
                if (_progress >= 1f)
                {
                    CompleteHarvest();
                }
            }
        }

        /// <summary>检测并进入采集。返回是否允许起手（用于外部提示）。</summary>
        public bool TryBeginHarvest()
        {
            if (FocusedNode == null) return false;
            if (FocusedNode.IsDepleted) return false;

            if (FocusedNode.IsDirectPickup)
            {
                _pickingUp = FocusedNode;
                _pickupElapsed = 0f;
                _pickupAnimationRequested = true;
                playerMotor.SetMovementLocked(true);
                return true;
            }

            if (!CheckTool(FocusedNode)) return false;

            _harvesting = FocusedNode;
            _progress = 0f;
            playerMotor.SetMovementLocked(true);
            return true;
        }

        /// <summary>工具等级检测：是否持有匹配工具且等级达标。</summary>
        public bool CheckTool(ResourceNode node)
        {
            if (node == null) return false;
            if (toolState.type != node.HarvestToolType) return false;
            return toolState.level >= node.HarvestToolLevel;
        }

        /// <summary>
        /// 消费一次直接拾取动画请求，避免同一次拾取重复触发动画。
        /// </summary>
        public bool ConsumePickupAnimationRequest()
        {
            if (!_pickupAnimationRequested)
            {
                return false;
            }

            _pickupAnimationRequested = false;
            return true;
        }

        /// <summary>采集完成：一次性扣理智 + 获得材料，标记并销毁节点。</summary>
        private void CompleteHarvest()
        {
            if (_harvesting == null) return;

            var node = _harvesting;

            // 理智不足以支撑本次采集消耗 → 不给材料、发出提示
            if (sanity != null && !sanity.TrySpend(node.HarvestSanityCost))
            {
                // 理智不足，终止采集
                ResetHarvest();
                return;
            }

            // 产出：MVP 先 Debug.Log，后续接入材料库存
            if (node.Yields != null)
            {
                foreach (var y in node.Yields)
                {
                    string msg = $"[采集] 获得 {y.resourceId} ×{y.amount}";
                    Debug.Log(msg);
                }
            }

            // 采集点立即消失（本次探索不再出现）
            node.MarkDepleted();
            Destroy(node.gameObject);
            ResetHarvest();
            FocusedNode = null;
        }

        private void ResetHarvest()
        {
            _progress = 0f;
            _harvesting = null;
            if (playerMotor != null)
            {
                playerMotor.SetMovementLocked(false);
            }
        }

        private void EndHarvest()
        {
            _progress = 0f;
            _harvesting = null;
            if (playerMotor != null)
            {
                playerMotor.SetMovementLocked(false);
            }
        }

        /// <summary>编辑器下可视化交互范围。</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
