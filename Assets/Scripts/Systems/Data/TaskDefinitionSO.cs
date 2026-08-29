using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 任务静态配置（TaskDefinitionSO）。
    /// 只描述任务目标和展示内容，不保存玩家运行时进度。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Chapter/Task Definition", fileName = "TaskDefinition")]
    public class TaskDefinitionSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("任务唯一标识符")]
        [SerializeField] private string taskId;

        [Tooltip("任务显示名称")]
        [SerializeField] private string taskName;

        [Tooltip("任务描述")]
        [TextArea(3, 5)]
        [SerializeField] private string description;

        [Header("目标")]
        [Tooltip("任务目标类型")]
        [SerializeField] private TaskType taskType;

        [Tooltip("目标对象 ID；不需要对象时留空")]
        [SerializeField] private string targetId;

        [Tooltip("目标数量或目标数值")]
        [Min(1)]
        [SerializeField] private int targetValue = 1;

        [Tooltip("开启后只统计任务激活之后的事件；关闭后会读取已有历史记录")]
        [SerializeField] private bool countOnlyAfterActivation;

        /// <summary>任务唯一标识符。</summary>
        public string TaskId => taskId;

        /// <summary>任务显示名称。</summary>
        public string TaskName => taskName;

        /// <summary>任务描述。</summary>
        public string Description => description;

        /// <summary>任务目标类型。</summary>
        public TaskType Type => taskType;

        /// <summary>任务目标对象 ID。</summary>
        public string TargetId => targetId;

        /// <summary>任务目标数量或数值。</summary>
        public int TargetValue => targetValue;

        /// <summary>
        /// 获取任务是否只接受激活后的判定结果。
        /// </summary>
        public bool CountOnlyAfterActivation => countOnlyAfterActivation;

        /// <summary>
        /// 校验任务配置是否具备参与运行时进度管理的必要字段。
        /// </summary>
        public bool Validate(out string validationMessage)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                validationMessage = "缺少 TaskId。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                validationMessage = $"任务 {taskId} 缺少任务名称。";
                return false;
            }

            if (!System.Enum.IsDefined(typeof(TaskType), taskType))
            {
                validationMessage = $"任务 {taskId} 的任务类型无效。";
                return false;
            }

            if (targetValue <= 0)
            {
                validationMessage = $"任务 {taskId} 的目标值必须大于 0。";
                return false;
            }

            if (RequiresTarget(taskType) && string.IsNullOrWhiteSpace(targetId))
            {
                validationMessage = $"任务 {taskId} 的 {taskType} 必须配置 TargetId。";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 判断任务类型是否必须绑定具体目标 ID。
        /// </summary>
        private bool RequiresTarget(TaskType type)
        {
            return type == TaskType.ExploreArea
                || type == TaskType.MeetSpecialGuest
                || type == TaskType.CompleteStory;
        }

        /// <summary>
        /// 可配置的任务目标类型。
        /// </summary>
        public enum TaskType
        {
            ServeCustomers = 0, // 服务一定数量顾客
            EarnRevenue = 1,    // 获得一定数量收入
            CompleteOrders = 2, // 完成一定数量订单
            CompletePerfectOrders = 3,  // 完成一定数量完美订单
            CollectMaterials = 4,   // 收集一定数量材料
            ExploreArea = 5,    // 探索指定区域
            MeetSpecialGuest = 6,   // 招待指定顾客特殊顾客
            CompleteStory = 7,  // 完成指定剧情
            CountSatisfiedReviews = 8, // 获得一定次数好评
            ReachBeautyScore = 9, // 美观度达到指定数值
            ReachTotalAssets = 10, // 总资产达到指定数值
            UnlockResources = 11 // 累计解锁指定数量的资源
        }
    }
}
