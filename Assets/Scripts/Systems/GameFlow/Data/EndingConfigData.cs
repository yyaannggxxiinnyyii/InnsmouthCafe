using System;
using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 单个过场帧数据（一张图+一段台词）
    /// </summary>
    [Serializable]
    public class EndingCutsceneSlide
    {
        [Tooltip("过场图片")]
        public Sprite image;

        [TextArea(3, 6)]
        [Tooltip("过场台词")]
        public string text;
    }

    /// <summary>
    /// 单个结局的完整配置（过场序列 + 最终结局面板）
    /// </summary>
    [Serializable]
    public class EndingConfig
    {
        [Header("过场序列")]
        [Tooltip("过场帧列表（按顺序播放）")]
        public List<EndingCutsceneSlide> cutsceneSlides = new List<EndingCutsceneSlide>();

        [Header("BGM")]
        [Tooltip("结局BGM（进入过场时切换）")]
        public AudioClip bgm;

        [Header("结局面板")]
        [Tooltip("结局面板图片")]
        public Sprite endingImage;

        [Tooltip("结局图片的叠加颜色（可用于染色，默认白色=不变色）")]
        public Color endingImageColor = Color.white;

        [Tooltip("结局名称（如：A结局、B结局）")]
        public string endingTitle;

        [Tooltip("结局名称的文字颜色")]
        public Color endingTitleColor = Color.white;

        [TextArea(3, 6)]
        [Tooltip("结局描述文本")]
        public string endingDescription;
    }
}
