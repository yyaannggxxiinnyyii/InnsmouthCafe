using System;
using UnityEngine;

/// <summary>
/// Lens Distortion 效果配置（持续层 + 波动 + 两种脉冲）
/// </summary>
[Serializable]
public class LensDistortionConfig
{
    [Tooltip("是否启用此效果的理智值控制")]
    public bool enabled = true;

    [Tooltip("理智值满（100）时的畸变强度")]
    [Range(-1f, 1f)]
    public float sustainedMin = 0f;

    [Tooltip("理智值空（0）时的畸变强度（负值向内收缩）")]
    [Range(-1f, 1f)]
    public float sustainedMax = -0.2f;

    [Header("波动设置")]
    [Tooltip("Perlin Noise 波动幅度（理智越低波动越强）")]
    [Range(0f, 0.3f)]
    public float noiseAmplitude = 0.06f;

    [Tooltip("Perlin Noise 波动速度")]
    [Range(0.01f, 2f)]
    public float noiseSpeed = 0.3f;

    [Header("扣理智脉冲")]
    [Tooltip("扣理智时额外叠加的畸变强度（负值加重收缩）")]
    [Range(-1f, 1f)]
    public float damageExtra = -0.12f;

    [Header("恢复理智脉冲")]
    [Tooltip("恢复理智时额外叠加的畸变强度（正值短暂膨胀，给松了口气的感觉）")]
    [Range(-1f, 1f)]
    public float healExtra = 0.06f;
}

/// <summary>
/// Chromatic Aberration 效果配置（持续层 + 两种脉冲）
/// </summary>
[Serializable]
public class ChromaticAberrationConfig
{
    [Tooltip("是否启用此效果的理智值控制")]
    public bool enabled = true;

    [Tooltip("理智值满（100）时的色差强度")]
    [Range(0f, 1f)]
    public float sustainedMin = 0f;

    [Tooltip("理智值空（0）时的色差强度")]
    [Range(0f, 1f)]
    public float sustainedMax = 0.6f;

    [Header("扣理智脉冲")]
    [Tooltip("扣理智时额外叠加的色差强度")]
    [Range(0f, 1f)]
    public float damageExtra = 0.3f;

    [Header("恢复理智脉冲")]
    [Tooltip("恢复理智时额外叠加的色差强度（负值短暂减轻）")]
    [Range(-1f, 0f)]
    public float healExtra = -0.1f;
}

/// <summary>
/// Vignette 效果配置（持续层 + 两种脉冲）
/// </summary>
[Serializable]
public class VignetteConfig
{
    [Tooltip("是否启用此效果的理智值控制")]
    public bool enabled = true;

    [Tooltip("理智值满（100）时的暗角强度")]
    [Range(0f, 1f)]
    public float sustainedMin = 0.2f;

    [Tooltip("理智值空（0）时的暗角强度")]
    [Range(0f, 1f)]
    public float sustainedMax = 0.55f;

    [Header("扣理智脉冲")]
    [Tooltip("扣理智时额外叠加的暗角强度")]
    [Range(0f, 1f)]
    public float damageExtra = 0.15f;

    [Header("恢复理智脉冲")]
    [Tooltip("恢复理智时额外叠加的暗角强度（负值短暂减轻）")]
    [Range(-1f, 0f)]
    public float healExtra = -0.08f;
}

/// <summary>
/// Film Grain 效果配置（仅持续层）
/// </summary>
[Serializable]
public class FilmGrainConfig
{
    [Tooltip("是否启用此效果的理智值控制")]
    public bool enabled = true;

    [Tooltip("理智值满（100）时的颗粒强度")]
    [Range(0f, 1f)]
    public float sustainedMin = 0f;

    [Tooltip("理智值空（0）时的颗粒强度")]
    [Range(0f, 1f)]
    public float sustainedMax = 0.5f;
}

/// <summary>
/// Color Adjustments 效果配置
/// 持续层控制饱和度/曝光；脉冲层通过 Color Filter 闪红/绿
/// </summary>
[Serializable]
public class ColorAdjustmentsConfig
{
    [Tooltip("是否启用此效果的理智值控制")]
    public bool enabled = true;

    [Header("持续层 - 饱和度")]
    [Tooltip("理智值满（100）时的饱和度（0 = 不变）")]
    [Range(-100f, 100f)]
    public float saturationMin = 0f;

    [Tooltip("理智值空（0）时的饱和度（负值去色）")]
    [Range(-100f, 100f)]
    public float saturationMax = -60f;

    [Header("持续层 - 曝光")]
    [Tooltip("理智值满（100）时的曝光补偿")]
    public float postExposureMin = 0f;

    [Tooltip("理智值空（0）时的曝光补偿（负值变暗）")]
    public float postExposureMax = -0.3f;

    [Header("扣理智脉冲 - Color Filter")]
    [Tooltip("扣理智时 Color Filter 闪烁的颜色（建议偏红）")]
    public Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("恢复理智脉冲 - Color Filter")]
    [Tooltip("恢复理智时 Color Filter 闪烁的颜色（建议偏绿）")]
    public Color healFlashColor = new Color(0.2f, 1f, 0.4f, 1f);
}
