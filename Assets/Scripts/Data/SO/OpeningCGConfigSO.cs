using System;
using UnityEngine;

namespace InnsmouthCafe.Data
{
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
    /// 开局CG配置SO
    /// 四分镜漫画分镜效果：黑幕 → 白背景 → 四个分镜依次滑入 → 点击继续 → 黑幕 → 进入游戏
    /// 触发条件：教学模式未通关时每次点击开始游戏都会播放
    /// </summary>
    [CreateAssetMenu(fileName = "OpeningCGConfig", menuName = "InnsmouthCafe/Config/OpeningCGConfig", order = 12)]
    public class OpeningCGConfigSO : ScriptableObject
    {
        [Header("分镜配置（按出场顺序）")]
        [Tooltip("第1分镜（左上角，从左侧滑入）")]
        public ComicPanelConfig panel1 = new ComicPanelConfig();

        [Tooltip("第2分镜（右上角，从右侧滑入）")]
        public ComicPanelConfig panel2 = new ComicPanelConfig();

        [Tooltip("第3分镜（左下角，从左侧滑入）")]
        public ComicPanelConfig panel3 = new ComicPanelConfig();

        [Tooltip("第4分镜（右下角，从右侧滑入）")]
        public ComicPanelConfig panel4 = new ComicPanelConfig();

        [Header("过渡时长")]
        [Tooltip("黑幕转白背景时长（秒）")]
        public float blackToWhiteDuration = 0.5f;

        [Tooltip("白背景转黑幕时长（秒）")]
        public float whiteToBlackDuration = 0.8f;

        [Header("BGM")]
        [Tooltip("开局CG BGM（留空则不切换BGM）")]
        public AudioClip bgm;
    }
}
