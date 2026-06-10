using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 开局CG模式
    /// </summary>
    public enum OpeningCGMode
    {
        /// <summary>四分镜漫画分镜</summary>
        ComicPanels,
        /// <summary>单图+对话框演出</summary>
        ImageDialogue
    }

    /// <summary>
    /// 单个漫画分镜配置
    /// </summary>
    [Serializable]
    public class ComicPanelConfig
    {
        [Tooltip("分镜贴图（全屏大小，仅对应角落有内容，其余透明）")]
        public Sprite sprite;

        [Tooltip("滑入动画时长（秒）")]
        public float slideInDuration = 0.5f;

        [Tooltip("进入后自动推进等待时间（秒），0 = 等待手动点击才推进下一格")]
        public float autoAdvanceDelay = 1.5f;
    }

    /// <summary>
    /// 单条对话配置（用于 ImageDialogue 模式）
    /// </summary>
    [Serializable]
    public class CGDialogueEntry
    {
        [Tooltip("角色立绘（留空则对话框内无立绘）")]
        public Sprite characterSprite;

        [Tooltip("说话人名字（留空则隐藏名字栏）")]
        public string characterName;

        [TextArea(2, 5)]
        [Tooltip("对话文本内容")]
        public string text;

        [Tooltip("打字机每个字符间隔（秒），越小越快")]
        public float typewriterInterval = 0.05f;

        [Tooltip("打字完成后自动推进等待时间（秒），0 = 等待手动点击")]
        public float autoAdvanceDelay = 0f;

        [Tooltip("打字完成后显示的确认按钮文字（留空则不显示按钮，用全屏点击推进）")]
        public string confirmButtonText;

        [Tooltip("播放本条对话前先淡入黑幕（背景图消失，对话框浮在黑幕上）")]
        public bool blackoutBefore = false;

        [Tooltip("blackoutBefore 为 true 时，黑幕完全不透明后播放的音效")]
        public AudioClip blackoutSfx;
    }

    /// <summary>
    /// 开局CG配置SO
    /// </summary>
    [CreateAssetMenu(fileName = "OpeningCGConfig", menuName = "InnsmouthCafe/Config/OpeningCGConfig", order = 12)]
    public class OpeningCGConfigSO : ScriptableObject
    {
        [Header("模式选择")]
        [Tooltip("ComicPanels = 四分镜漫画；ImageDialogue = 单图+对话框演出")]
        public OpeningCGMode cgMode = OpeningCGMode.ComicPanels;

        // ── 四分镜模式 ────────────────────────────────────────

        [Header("【ComicPanels】分镜配置（按出场顺序）")]
        [Tooltip("第1分镜（左上角，从左侧滑入）")]
        public ComicPanelConfig panel1 = new ComicPanelConfig();

        [Tooltip("第2分镜（右上角，从右侧滑入）")]
        public ComicPanelConfig panel2 = new ComicPanelConfig();

        [Tooltip("第3分镜（左下角，从左侧滑入）")]
        public ComicPanelConfig panel3 = new ComicPanelConfig();

        [Tooltip("第4分镜（右下角，从右侧滑入）")]
        public ComicPanelConfig panel4 = new ComicPanelConfig();

        // ── 单图对话模式 ──────────────────────────────────────

        [Header("【ImageDialogue】背景图")]
        [Tooltip("全屏背景图片")]
        public Sprite backgroundSprite;

        [Header("【ImageDialogue】对话列表（按顺序播放）")]
        public List<CGDialogueEntry> dialogues = new List<CGDialogueEntry>();

        // ── 通用 ──────────────────────────────────────────────

        [Header("过渡时长（两种模式通用）")]
        [Tooltip("开场黑幕淡出时长（秒）")]
        public float fadeInDuration = 0.5f;

        [Tooltip("结尾黑幕淡入时长（秒）")]
        public float fadeOutDuration = 0.8f;

        [Header("BGM（两种模式通用）")]
        [Tooltip("开局CG BGM（留空则不切换BGM）")]
        public AudioClip bgm;
    }
}
