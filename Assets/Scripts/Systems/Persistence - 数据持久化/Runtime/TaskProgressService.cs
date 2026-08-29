using System;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Persistence;
using UnityEngine;

namespace InnsmouthCafe.Progression
{
    /// <summary>
    /// 管理当前长期经营存档中的任务激活、进度和完成状态。
    /// 任务配置来自 TaskDefinitionSO，运行时数据只写入 GameSaveData。
    /// </summary>
    [DisallowMultipleComponent]
    public class TaskProgressService : MonoBehaviour
    {
        private static TaskProgressService _instance;

        /// <summary>
        /// 获取任务进度服务单例。
        /// </summary>
        public static TaskProgressService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(TaskProgressService));
                    _instance = serviceObject.AddComponent<TaskProgressService>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 任务进度发生变化后触发，参数为最新进度数据。
        /// </summary>
        public event Action<TaskProgressData> OnTaskProgressChanged;

        /// <summary>
        /// 任务首次完成后触发，参数为完成的任务配置。
        /// </summary>
        public event Action<TaskDefinitionSO, TaskProgressData> OnTaskCompleted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 激活章节中的全部任务，并为累计任务记录当前统计基线。
        /// 已存在的任务进度不会被重置，保证重复初始化不会污染存档。
        /// </summary>
        public bool ActivateChapter(MapConfigSO mapConfig, ChapterConfigSO chapter)
        {
            if (!CanUseSave() || mapConfig == null || chapter == null)
            {
                return false;
            }

            if (chapter.Tasks == null || chapter.Tasks.Count == 0)
            {
                return false;
            }

            bool changed = false;
            foreach (TaskDefinitionSO task in chapter.Tasks)
            {
                if (!IsUsableTask(task))
                {
                    continue;
                }

                if (FindProgress(mapConfig.mapId, chapter.ChapterId, task.TaskId) != null)
                {
                    continue;
                }

                GameSaveData saveData = GetSave();
                int historicalValue = GetMetricValue(task);
                bool completedFromHistory = !task.CountOnlyAfterActivation
                    && IsHistoricalTaskComplete(task);
                int initialValue = task.CountOnlyAfterActivation
                    ? 0
                    : Mathf.Min(task.TargetValue, IsInstantTask(task.Type)
                        ? (completedFromHistory ? task.TargetValue : 0)
                        : historicalValue);
                saveData.taskProgresses.Add(new TaskProgressData
                {
                    mapId = mapConfig.mapId,
                    chapterId = chapter.ChapterId,
                    taskId = task.TaskId,
                    activationBaseline = task.CountOnlyAfterActivation
                        && UsesActivationBaseline(task.Type)
                        ? historicalValue
                        : 0,
                    currentValue = initialValue,
                    isCompleted = initialValue >= task.TargetValue
                });
                changed = true;
            }

            if (changed)
            {
                SaveCurrent();
            }

            return true;
        }

        /// <summary>
        /// 获取指定任务的当前进度；任务未激活或不存在时返回空。
        /// </summary>
        public TaskProgressData GetProgress(string mapId, string chapterId, string taskId)
        {
            if (!CanUseSave())
            {
                return null;
            }

            return FindProgress(mapId, chapterId, taskId);
        }

        /// <summary>
        /// 获取指定章节中已经激活的全部任务进度，返回顺序与存档记录无关。
        /// </summary>
        public IReadOnlyList<TaskProgressData> GetChapterProgress(string mapId, string chapterId)
        {
            List<TaskProgressData> progresses = new List<TaskProgressData>();
            if (!CanUseSave() || string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(chapterId))
            {
                return progresses;
            }

            foreach (TaskProgressData progress in GetSave().taskProgresses)
            {
                if (progress != null && progress.mapId == mapId && progress.chapterId == chapterId)
                {
                    progresses.Add(progress);
                }
            }

            return progresses;
        }

        /// <summary>
        /// 判断指定任务是否已完成。
        /// </summary>
        public bool IsTaskCompleted(string mapId, string chapterId, string taskId)
        {
            TaskProgressData progress = GetProgress(mapId, chapterId, taskId);
            return progress != null && progress.isCompleted;
        }

        /// <summary>
        /// 记录服务顾客数量，并更新匹配的顾客任务。
        /// </summary>
        public bool RecordCustomersServed(string customerId = null, int count = 1)
        {
            if (count <= 0 || !CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            saveData.totalCustomersServed += count;
            if (!string.IsNullOrEmpty(customerId))
            {
                AddCount(saveData.servedCustomerCounts, customerId, count);
            }

            return UpdateActiveTasks(TaskDefinitionSO.TaskType.ServeCustomers, customerId);
        }

        /// <summary>
        /// 记录营业收入，并更新收入任务。
        /// </summary>
        public bool RecordRevenue(int amount)
        {
            if (amount <= 0 || !CanUseSave())
            {
                return false;
            }

            GetSave().totalIncome += amount;
            return UpdateActiveTasks(TaskDefinitionSO.TaskType.EarnRevenue);
        }

        /// <summary>
        /// 记录完成订单数量，并更新订单任务。
        /// </summary>
        public bool RecordOrders(int count = 1)
        {
            if (count <= 0 || !CanUseSave())
            {
                return false;
            }

            GetSave().totalOrders += count;
            return UpdateActiveTasks(TaskDefinitionSO.TaskType.CompleteOrders);
        }

        /// <summary>
        /// 记录完美订单数量，并更新完美订单任务。
        /// </summary>
        public bool RecordPerfectOrders(int count = 1)
        {
            if (count <= 0 || !CanUseSave())
            {
                return false;
            }

            GetSave().fiveStarOrders += count;
            return UpdateActiveTasks(TaskDefinitionSO.TaskType.CompletePerfectOrders);
        }

        /// <summary>
        /// 记录好评次数，并更新匹配的好评任务。
        /// </summary>
        public bool RecordSatisfiedReview(int count = 1)
        {
            if (count <= 0 || !CanUseSave())
            {
                return false;
            }

            GetSave().totalSatisfiedReviews += count;
            return UpdateActiveTasks(TaskDefinitionSO.TaskType.CountSatisfiedReviews);
        }

        /// <summary>
        /// 设置当前美观度，并判定美观度阈值任务。
        /// </summary>
        public bool RecordBeautyScore(int score)
        {
            if (!CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            int normalizedScore = Mathf.Max(0, score);
            bool valueChanged = saveData.beautyScore != normalizedScore;
            saveData.beautyScore = normalizedScore;
            bool taskChanged = UpdateActiveTasks(TaskDefinitionSO.TaskType.ReachBeautyScore);
            return valueChanged || taskChanged;
        }

        /// <summary>
        /// 设置当前总资产，并判定总资产阈值任务。
        /// </summary>
        public bool RecordTotalAssets(int assets)
        {
            if (!CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            int normalizedAssets = Mathf.Max(0, assets);
            bool valueChanged = saveData.totalAssets != normalizedAssets;
            saveData.totalAssets = normalizedAssets;
            bool taskChanged = UpdateActiveTasks(TaskDefinitionSO.TaskType.ReachTotalAssets);
            return valueChanged || taskChanged;
        }

        /// <summary>
        /// 记录一个已解锁资源，并更新资源解锁数量任务。
        /// </summary>
        public bool RecordResourceUnlocked(string resourceId, bool isLiquid)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || !CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            List<string> unlockedIds = isLiquid
                ? saveData.unlockedLiquidIds
                : saveData.unlockedToppingIds;
            bool isNewResource = !unlockedIds.Contains(resourceId);
            if (isNewResource)
            {
                unlockedIds.Add(resourceId);
            }

            bool taskChanged = UpdateActiveTasks(TaskDefinitionSO.TaskType.UnlockResources);
            return isNewResource || taskChanged;
        }

        /// <summary>
        /// 记录采集材料数量，并更新指定材料或任意材料任务。
        /// </summary>
        public bool RecordCollectedMaterials(string materialId, int count = 1)
        {
            if (count <= 0 || !CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            saveData.totalCollectedMaterials += count;
            if (!string.IsNullOrEmpty(materialId))
            {
                AddCount(saveData.collectedMaterialCounts, materialId, count);
            }

            return UpdateActiveTasks(TaskDefinitionSO.TaskType.CollectMaterials, materialId);
        }

        /// <summary>
        /// 记录区域到访；同一存档中重复到访不会重复增加进度。
        /// </summary>
        public bool RecordAreaVisited(string areaId)
        {
            if (string.IsNullOrEmpty(areaId) || !CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            bool isFirstVisit = !saveData.visitedAreaIds.Contains(areaId);
            if (isFirstVisit)
            {
                saveData.visitedAreaIds.Add(areaId);
            }

            bool changed = UpdateActiveTasks(TaskDefinitionSO.TaskType.ExploreArea, areaId);
            return isFirstVisit || changed;
        }

        /// <summary>
        /// 记录特殊来宾已接待，并更新匹配的来宾任务。
        /// </summary>
        public bool RecordSpecialGuestMet(string customerId)
        {
            if (string.IsNullOrEmpty(customerId) || !CanUseSave())
            {
                return false;
            }

            SpecialGuestProgressData progress = FindSpecialGuestProgress(customerId);
            if (progress == null)
            {
                progress = new SpecialGuestProgressData
                {
                    specialCustomerId = customerId
                };
                GetSave().specialGuestProgresses.Add(progress);
            }

            bool isFirstMeeting = !progress.hasMet;
            progress.hasMet = true;
            bool changed = UpdateActiveTasks(TaskDefinitionSO.TaskType.MeetSpecialGuest, customerId);
            return isFirstMeeting || changed;
        }

        /// <summary>
        /// 记录剧情节点已完成，并更新匹配的剧情任务。
        /// </summary>
        public bool RecordStoryCompleted(string storyId)
        {
            if (string.IsNullOrEmpty(storyId) || !CanUseSave())
            {
                return false;
            }

            GameSaveData saveData = GetSave();
            bool isFirstCompletion = !saveData.completedStoryIds.Contains(storyId);
            if (isFirstCompletion)
            {
                saveData.completedStoryIds.Add(storyId);
            }

            bool changed = UpdateActiveTasks(TaskDefinitionSO.TaskType.CompleteStory, storyId);
            return isFirstCompletion || changed;
        }

        /// <summary>
        /// 更新当前存档中所有已激活且匹配事件的任务。
        /// </summary>
        private bool UpdateActiveTasks(TaskDefinitionSO.TaskType taskType, string targetId = null)
        {
            ChapterProgressService chapterProgressService = ChapterProgressService.Instance;
            ChapterConfigSO chapter = chapterProgressService.GetCurrentChapter();
            MapConfigSO mapConfig = chapterProgressService.CurrentMap;
            if (chapter == null || mapConfig == null || string.IsNullOrEmpty(mapConfig.mapId))
            {
                SaveCurrent();
                return false;
            }

            bool changed = false;
            if (chapter.Tasks == null)
            {
                SaveCurrent();
                return false;
            }

            foreach (TaskDefinitionSO task in chapter.Tasks)
            {
                if (!IsUsableTask(task) || task.Type != taskType || !MatchesTarget(task, targetId))
                {
                    continue;
                }

                TaskProgressData progress = FindProgress(mapConfig.mapId, chapter.ChapterId, task.TaskId);
                if (progress == null || progress.isCompleted)
                {
                    continue;
                }

                int previousValue = progress.currentValue;
                progress.currentValue = GetCurrentTaskValue(task, progress);
                if (progress.currentValue >= task.TargetValue)
                {
                    progress.currentValue = task.TargetValue;
                    progress.isCompleted = true;
                    OnTaskCompleted?.Invoke(task, progress);
                }

                changed |= previousValue != progress.currentValue || progress.isCompleted;
                if (previousValue != progress.currentValue || progress.isCompleted)
                {
                    OnTaskProgressChanged?.Invoke(progress);
                }
            }

            SaveCurrent();
            return changed;
        }

        /// <summary>
        /// 计算指定任务在激活基线之后的当前值。
        /// </summary>
        private int GetCurrentTaskValue(TaskDefinitionSO task, TaskProgressData progress)
        {
            if (IsInstantTask(task.Type))
            {
                return 1;
            }

            int metricValue = GetMetricValue(task);
            if (!UsesActivationBaseline(task.Type))
            {
                return metricValue;
            }

            return Mathf.Max(0, metricValue - progress.activationBaseline);
        }

        /// <summary>
        /// 获取任务对应的累计统计值。
        /// </summary>
        private int GetMetricValue(TaskDefinitionSO task)
        {
            GameSaveData saveData = GetSave();
            if (!string.IsNullOrEmpty(task.TargetId) && task.Type == TaskDefinitionSO.TaskType.ServeCustomers)
            {
                return GetCount(saveData.servedCustomerCounts, task.TargetId);
            }

            if (!string.IsNullOrEmpty(task.TargetId) && task.Type == TaskDefinitionSO.TaskType.CollectMaterials)
            {
                return GetCount(saveData.collectedMaterialCounts, task.TargetId);
            }

            switch (task.Type)
            {
                case TaskDefinitionSO.TaskType.ServeCustomers:
                    return saveData.totalCustomersServed;
                case TaskDefinitionSO.TaskType.EarnRevenue:
                    return saveData.totalIncome;
                case TaskDefinitionSO.TaskType.CompleteOrders:
                    return saveData.totalOrders;
                case TaskDefinitionSO.TaskType.CompletePerfectOrders:
                    return saveData.fiveStarOrders;
                case TaskDefinitionSO.TaskType.CountSatisfiedReviews:
                    return saveData.totalSatisfiedReviews;
                case TaskDefinitionSO.TaskType.CollectMaterials:
                    return saveData.totalCollectedMaterials;
                case TaskDefinitionSO.TaskType.ReachBeautyScore:
                    return saveData.beautyScore;
                case TaskDefinitionSO.TaskType.ReachTotalAssets:
                    return saveData.totalAssets;
                case TaskDefinitionSO.TaskType.UnlockResources:
                    return CountUnlockedResources(saveData);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 检查一次性目标或历史累计目标是否已经在当前存档中达成。
        /// </summary>
        private bool IsHistoricalTaskComplete(TaskDefinitionSO task)
        {
            if (IsInstantTask(task.Type))
            {
                GameSaveData saveData = GetSave();
                switch (task.Type)
                {
                    case TaskDefinitionSO.TaskType.ExploreArea:
                        return !string.IsNullOrEmpty(task.TargetId)
                            && saveData.visitedAreaIds.Contains(task.TargetId);
                    case TaskDefinitionSO.TaskType.MeetSpecialGuest:
                        SpecialGuestProgressData guestProgress = FindSpecialGuestProgress(task.TargetId);
                        return guestProgress != null && guestProgress.hasMet;
                    case TaskDefinitionSO.TaskType.CompleteStory:
                        return !string.IsNullOrEmpty(task.TargetId)
                            && saveData.completedStoryIds.Contains(task.TargetId);
                    default:
                        return false;
                }
            }

            return GetMetricValue(task) >= task.TargetValue;
        }

        /// <summary>
        /// 判断任务是否属于一次性事件目标。
        /// </summary>
        private bool IsInstantTask(TaskDefinitionSO.TaskType taskType)
        {
            return taskType == TaskDefinitionSO.TaskType.ExploreArea
                || taskType == TaskDefinitionSO.TaskType.MeetSpecialGuest
                || taskType == TaskDefinitionSO.TaskType.CompleteStory;
        }

        /// <summary>
        /// 判断任务是否需要用激活时累计值作为进度基线。
        /// </summary>
        private bool UsesActivationBaseline(TaskDefinitionSO.TaskType taskType)
        {
            return taskType != TaskDefinitionSO.TaskType.ReachBeautyScore
                && taskType != TaskDefinitionSO.TaskType.ReachTotalAssets;
        }

        /// <summary>
        /// 统计当前存档中去重后的已解锁资源种类数。
        /// </summary>
        private int CountUnlockedResources(GameSaveData saveData)
        {
            HashSet<string> resourceIds = new HashSet<string>();
            AddResourceIds(resourceIds, saveData.unlockedLiquidIds);
            AddResourceIds(resourceIds, saveData.unlockedToppingIds);
            return resourceIds.Count;
        }

        /// <summary>
        /// 将有效资源 ID 加入去重集合。
        /// </summary>
        private void AddResourceIds(HashSet<string> resourceIds, List<string> resourceIdsToAdd)
        {
            if (resourceIdsToAdd == null)
            {
                return;
            }

            foreach (string resourceId in resourceIdsToAdd)
            {
                if (!string.IsNullOrWhiteSpace(resourceId))
                {
                    resourceIds.Add(resourceId);
                }
            }
        }

        /// <summary>
        /// 判断事件对象是否匹配任务对象；空目标表示不限定对象。
        /// </summary>
        private bool MatchesTarget(TaskDefinitionSO task, string targetId)
        {
            return string.IsNullOrEmpty(task.TargetId) || task.TargetId == targetId;
        }

        /// <summary>
        /// 判断任务配置是否具备创建和更新进度所需的有效字段。
        /// </summary>
        private bool IsUsableTask(TaskDefinitionSO task)
        {
            return task != null && task.Validate(out _);
        }

        private TaskProgressData FindProgress(string mapId, string chapterId, string taskId)
        {
            GameSaveData saveData = GetSave();
            foreach (TaskProgressData progress in saveData.taskProgresses)
            {
                if (progress != null && progress.mapId == mapId
                    && progress.chapterId == chapterId && progress.taskId == taskId)
                {
                    return progress;
                }
            }

            return null;
        }

        private SpecialGuestProgressData FindSpecialGuestProgress(string customerId)
        {
            foreach (SpecialGuestProgressData progress in GetSave().specialGuestProgresses)
            {
                if (progress != null && progress.specialCustomerId == customerId)
                {
                    return progress;
                }
            }

            return null;
        }

        private int GetCount(List<IdCountData> entries, string id)
        {
            foreach (IdCountData entry in entries)
            {
                if (entry != null && entry.id == id)
                {
                    return entry.count;
                }
            }

            return 0;
        }

        private void AddCount(List<IdCountData> entries, string id, int count)
        {
            foreach (IdCountData entry in entries)
            {
                if (entry != null && entry.id == id)
                {
                    entry.count += count;
                    return;
                }
            }

            entries.Add(new IdCountData
            {
                id = id,
                count = count
            });
        }

        private GameSaveData GetSave()
        {
            return SaveSlotService.Instance.CurrentSave;
        }

        private bool CanUseSave()
        {
            GameSaveData saveData = GetSave();
            return saveData != null && saveData.taskProgresses != null;
        }

        private void SaveCurrent()
        {
            SaveSlotService.Instance.SaveCurrent();
        }
    }
}
