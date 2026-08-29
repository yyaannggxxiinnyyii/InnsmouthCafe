using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 章节配置（ChapterConfigSO）。
    /// 地图内的阶段性内容容器，负责提供章节身份和内容编排元数据。
    /// 具体任务配置与运行时进度由后续任务系统负责。
    /// </summary>
    [CreateAssetMenu(menuName = "InnsmouthCafe/Map/Chapter Config", fileName = "ChapterConfig")]
    public class ChapterConfigSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("章节唯一标识符")]
        [SerializeField] private string chapterId;

        [Tooltip("章节显示名称")]
        [SerializeField] private string chapterName;

        [Tooltip("章节描述")]
        [TextArea(3, 5)]
        [SerializeField] private string description;

        [Tooltip("章节排序（数字越小越靠前）")]
        [SerializeField] private int sortOrder;

        [Header("任务编排")]
        [Tooltip("本章节包含的任务，列表顺序用于内容编排；任务系统接入后将据此判断章节完成")]
        [SerializeField] private List<TaskDefinitionSO> tasks = new List<TaskDefinitionSO>();

        /// <summary>章节唯一标识符。</summary>
        public string ChapterId => chapterId;

        /// <summary>章节显示名称。</summary>
        public string ChapterName => chapterName;

        /// <summary>章节描述。</summary>
        public string Description => description;

        /// <summary>章节排序值。</summary>
        public int SortOrder => sortOrder;

        /// <summary>本章节的有序任务配置列表。</summary>
        public IReadOnlyList<TaskDefinitionSO> Tasks => tasks;
    }
}
