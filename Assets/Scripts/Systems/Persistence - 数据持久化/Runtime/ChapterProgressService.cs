using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InnsmouthCafe.Data;
using InnsmouthCafe.Persistence;
using UnityEngine;

namespace InnsmouthCafe.Progression
{
    /// <summary>
    /// 管理当前地图的章节激活与完成状态。
    /// 章节完成由本章节全部任务完成推导，不单独保存章节完成标记。
    /// </summary>
    [DisallowMultipleComponent]
    public class ChapterProgressService : MonoBehaviour
    {
        private static ChapterProgressService _instance;

        /// <summary>
        /// 获取章节进度服务单例。
        /// </summary>
        public static ChapterProgressService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject serviceObject = new GameObject(nameof(ChapterProgressService));
                    _instance = serviceObject.AddComponent<ChapterProgressService>();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 当前地图切换成功后触发。
        /// </summary>
        public event Action<MapConfigSO> OnMapInitialized;

        /// <summary>
        /// 章节完成后触发，参数为刚完成的章节配置。
        /// </summary>
        public event Action<ChapterConfigSO> OnChapterCompleted;

        /// <summary>
        /// 当前已初始化的地图配置。
        /// </summary>
        public MapConfigSO CurrentMap { get; private set; }

        private bool _advanceCheckScheduled;
        private string _pendingCompletedChapterId;

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

        private void OnEnable()
        {
            TaskProgressService.Instance.OnTaskCompleted += HandleTaskCompleted;
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                TaskProgressService.Instance.OnTaskCompleted -= HandleTaskCompleted;
            }
        }

        /// <summary>
        /// 初始化当前地图并激活当前章节的全部任务。
        /// </summary>
        public bool InitializeMap(MapConfigSO mapConfig)
        {
            if (mapConfig == null || string.IsNullOrEmpty(mapConfig.mapId))
            {
                Debug.LogWarning("[ChapterProgress] 地图配置为空或缺少地图 ID，无法初始化章节。");
                return false;
            }

            ValidateMapConfiguration(mapConfig);

            if (SaveSlotService.Instance.CurrentSave == null)
            {
                Debug.LogWarning("[ChapterProgress] 当前没有已加载的存档，无法初始化章节。");
                return false;
            }

            CurrentMap = mapConfig;
            SaveSlotService.Instance.CurrentSave.currentMapId = mapConfig.mapId;
            GetCurrentChapter();
            SaveSlotService.Instance.SaveCurrent();
            OnMapInitialized?.Invoke(mapConfig);
            return true;
        }

        /// <summary>
        /// 获取当前地图第一个未完成的章节。
        /// </summary>
        public ChapterConfigSO GetCurrentChapter()
        {
            if (CurrentMap == null || CurrentMap.chapters == null)
            {
                return null;
            }

            ChapterConfigSO currentChapter = FindFirstIncompleteChapter();
            if (currentChapter != null)
            {
                TaskProgressService.Instance.ActivateChapter(CurrentMap, currentChapter);

                if (IsChapterCompleted(currentChapter))
                {
                    TryAdvanceChapter(currentChapter.ChapterId);
                    return GetCurrentChapter();
                }
            }

            return currentChapter;
        }

        /// <summary>
        /// 判断指定章节的全部任务是否已完成。
        /// 没有任务的章节不会自动完成。
        /// </summary>
        public bool IsChapterCompleted(ChapterConfigSO chapter)
        {
            if (chapter == null || CurrentMap == null || SaveSlotService.Instance.CurrentSave == null)
            {
                return false;
            }

            if (chapter.Tasks == null || chapter.Tasks.Count == 0)
            {
                return false;
            }

            foreach (TaskDefinitionSO task in chapter.Tasks)
            {
                if (task == null || string.IsNullOrEmpty(task.TaskId)
                    || !TaskProgressService.Instance.IsTaskCompleted(
                        CurrentMap.mapId,
                        chapter.ChapterId,
                        task.TaskId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查当前章节是否完成，并激活完成后的下一章节。
        /// </summary>
        /// <summary>
        /// 获取当前章节的任务进度，供章节界面和其他运行时展示系统读取。
        /// </summary>
        public IReadOnlyList<TaskProgressData> GetCurrentChapterTaskProgress()
        {
            ChapterConfigSO currentChapter = GetCurrentChapter();
            if (CurrentMap == null || currentChapter == null)
            {
                return new List<TaskProgressData>();
            }

            return TaskProgressService.Instance.GetChapterProgress(
                CurrentMap.mapId,
                currentChapter.ChapterId);
        }

        public bool TryAdvanceChapter(string completedChapterId)
        {
            ChapterConfigSO completedChapter = FindChapter(completedChapterId);
            if (completedChapter == null || !IsChapterCompleted(completedChapter))
            {
                return false;
            }

            OnChapterCompleted?.Invoke(completedChapter);
            ActivateCurrentChapter();
            SaveSlotService.Instance.SaveCurrent();
            return true;
        }

        /// <summary>
        /// 激活当前地图第一个未完成章节的任务。
        /// </summary>
        private void ActivateCurrentChapter()
        {
            ChapterConfigSO currentChapter = FindFirstIncompleteChapter();
            if (currentChapter != null)
            {
                TaskProgressService.Instance.ActivateChapter(CurrentMap, currentChapter);
            }
        }

        /// <summary>
        /// 查找当前地图中排序最靠前的未完成章节。
        /// </summary>
        private ChapterConfigSO FindFirstIncompleteChapter()
        {
            if (CurrentMap == null || CurrentMap.chapters == null)
            {
                return null;
            }

            HashSet<string> seenChapterIds = new HashSet<string>();
            foreach (ChapterConfigSO chapter in CurrentMap.chapters
                .Where(chapter => chapter != null)
                .OrderBy(chapter => chapter.SortOrder))
            {
                if (!IsUsableChapter(chapter) || !seenChapterIds.Add(chapter.ChapterId))
                {
                    continue;
                }

                if (!IsChapterCompleted(chapter))
                {
                    return chapter;
                }
            }

            return null;
        }

        /// <summary>
        /// 任务完成后检查当前章节是否可以推进。
        /// </summary>
        private void HandleTaskCompleted(TaskDefinitionSO task, TaskProgressData progress)
        {
            if (CurrentMap == null || task == null || progress == null)
            {
                return;
            }

            if (progress.mapId != CurrentMap.mapId)
            {
                return;
            }

            _pendingCompletedChapterId = progress.chapterId;
            if (_advanceCheckScheduled)
            {
                return;
            }

            _advanceCheckScheduled = true;
            StartCoroutine(AdvanceChapterAfterCurrentEvent());
        }

        /// <summary>
        /// 等待当前游戏事件的所有任务判定完成后，再检查章节是否可以推进。
        /// </summary>
        private IEnumerator AdvanceChapterAfterCurrentEvent()
        {
            yield return null;

            _advanceCheckScheduled = false;
            string completedChapterId = _pendingCompletedChapterId;
            _pendingCompletedChapterId = null;
            TryAdvanceChapter(completedChapterId);
        }

        /// <summary>
        /// 校验地图所引用的章节配置，并报告会被运行时跳过的无效项。
        /// </summary>
        private void ValidateMapConfiguration(MapConfigSO mapConfig)
        {
            if (mapConfig.chapters == null)
            {
                Debug.LogWarning($"[ChapterProgress] 地图 {mapConfig.mapId} 未配置章节列表。");
                return;
            }

            HashSet<string> chapterIds = new HashSet<string>();
            foreach (ChapterConfigSO chapter in mapConfig.chapters)
            {
                if (chapter == null)
                {
                    Debug.LogWarning($"[ChapterProgress] 地图 {mapConfig.mapId} 包含空章节引用。");
                    continue;
                }

                if (string.IsNullOrEmpty(chapter.ChapterId))
                {
                    Debug.LogWarning($"[ChapterProgress] 地图 {mapConfig.mapId} 包含缺少 ChapterId 的章节。");
                    continue;
                }

                if (!chapterIds.Add(chapter.ChapterId))
                {
                    Debug.LogWarning($"[ChapterProgress] 地图 {mapConfig.mapId} 存在重复 ChapterId：{chapter.ChapterId}。");
                }

                if (!IsUsableChapter(chapter))
                {
                    Debug.LogWarning($"[ChapterProgress] 地图 {mapConfig.mapId} 的章节 {chapter.ChapterId} 未配置有效任务，将被跳过。");
                }
            }
        }

        /// <summary>
        /// 判断章节是否具备参与运行时推进所需的有效任务配置。
        /// </summary>
        private bool IsUsableChapter(ChapterConfigSO chapter)
        {
            if (chapter == null || string.IsNullOrEmpty(chapter.ChapterId)
                || chapter.Tasks == null || chapter.Tasks.Count == 0)
            {
                return false;
            }

            HashSet<string> taskIds = new HashSet<string>();
            foreach (TaskDefinitionSO task in chapter.Tasks)
            {
                if (task == null || !task.Validate(out _)
                    || !taskIds.Add(task.TaskId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断任务类型是否必须绑定明确的目标 ID。
        /// </summary>
        /// <summary>
        /// 按章节 ID 查找当前地图中的章节配置。
        /// </summary>
        private ChapterConfigSO FindChapter(string chapterId)
        {
            if (CurrentMap == null || CurrentMap.chapters == null || string.IsNullOrEmpty(chapterId))
            {
                return null;
            }

            return CurrentMap.chapters.FirstOrDefault(chapter =>
                chapter != null && chapter.ChapterId == chapterId);
        }
    }
}
