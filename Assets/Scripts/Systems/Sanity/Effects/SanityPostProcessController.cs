using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using InnsmouthCafe.Managers;

/// <summary>
/// 理智值后处理控制器
/// 监听 SanityManager 的理智值变化，驱动 URP Volume 的后处理效果。
///
/// 两层效果：
///   持续层 — 根据当前理智值百分比平滑映射各效果强度（理智越低效果越强）
///             涉及：LensDistortion / ChromaticAberration / Vignette / FilmGrain / ColorAdjustments(饱和度+曝光)
///   脉冲层 — 理智值变化时触发短暂脉冲，分扣理智和恢复理智两种
///             涉及：LensDistortion / ChromaticAberration / Vignette / ColorAdjustments(ColorFilter闪色)
/// </summary>
public class SanityPostProcessController : MonoBehaviour
{
    // ── Inspector 配置 ────────────────────────────────────────

    [Header("Volume 引用")]
    [SerializeField] [Tooltip("要控制的 URP Global Volume")]
    private Volume _volume;

    [Header("持续层设置")]
    [SerializeField] [Tooltip("持续层平滑过渡速度（理智值变化后效果追上目标值的速度）")]
    private float _sustainedLerpSpeed = 2f;

    [Header("脉冲设置")]
    [SerializeField] [Tooltip("脉冲持续时长（秒）")]
    private float _pulseDuration = 0.5f;

    [SerializeField] [Tooltip("脉冲衰减曲线（X=归一化时间 0-1，Y=脉冲强度系数 0-1）")]
    private AnimationCurve _pulseDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [SerializeField] [Tooltip("触发脉冲所需的最小理智值变化量（绝对值）")]
    private float _pulseMinDelta = 1f;

    [SerializeField] [Tooltip("脉冲强度随变化量的缩放系数（变化量 × 此系数 = 脉冲强度倍率）")]
    private float _pulseIntensityScale = 0.05f;

    [SerializeField] [Tooltip("脉冲强度上限倍率")]
    private float _pulseMaxIntensity = 1f;

    // ── 各效果配置 ────────────────────────────────────────────

    [Header("Lens Distortion（镜头畸变）")]
    [SerializeField] private LensDistortionConfig _lensDistortion = new LensDistortionConfig();

    [Header("Chromatic Aberration（色差）")]
    [SerializeField] private ChromaticAberrationConfig _chromaticAberration = new ChromaticAberrationConfig();

    [Header("Vignette（暗角）")]
    [SerializeField] private VignetteConfig _vignette = new VignetteConfig();

    [Header("Film Grain（胶片颗粒）")]
    [SerializeField] private FilmGrainConfig _filmGrain = new FilmGrainConfig();

    [Header("Color Adjustments（色彩调整）")]
    [SerializeField] private ColorAdjustmentsConfig _colorAdjustments = new ColorAdjustmentsConfig();

    // ── 运行时字段 ────────────────────────────────────────────

    private LensDistortion      _lensDistortionEffect;
    private ChromaticAberration _chromaticAberrationEffect;
    private Vignette            _vignetteEffect;
    private FilmGrain           _filmGrainEffect;
    private ColorAdjustments    _colorAdjustmentsEffect;

    /// <summary>持续层目标值（0=理智满，1=理智空）</summary>
    private float _sustainedTarget;

    /// <summary>持续层当前平滑值</summary>
    private float _sustainedCurrent;

    /// <summary>当前脉冲强度（0-1）</summary>
    private float _pulseValue;

    /// <summary>当前脉冲方向：true=扣理智，false=恢复理智</summary>
    private bool _isDamagePulse;

    /// <summary>Color Filter 当前插值（0=白色，1=目标闪色）</summary>
    private float _colorFilterValue;

    private Coroutine _pulseCoroutine;

    // ── 生命周期 ──────────────────────────────────────────────

    private void Awake()
    {
        if (_volume == null)
        {
            Debug.LogError("[SanityPP] 未指定 Volume，请在 Inspector 中赋值。");
            enabled = false;
            return;
        }
        TryGetEffects();
    }

    private void Start()
    {
        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[SanityPP] 未找到 SanityManager，后处理控制器将不工作。");
            return;
        }

        _sustainedTarget  = 1f - SanityManager.Instance.SanityRatio;
        _sustainedCurrent = _sustainedTarget;
        _colorFilterValue = 0f;
        ApplyAll(_sustainedCurrent, 0f, false, 0f);

        SanityManager.Instance.OnSanityChanged += OnSanityChanged;
    }

    private void OnDestroy()
    {
        if (SanityManager.Instance != null)
            SanityManager.Instance.OnSanityChanged -= OnSanityChanged;
    }

    private void Update()
    {
        _sustainedCurrent = Mathf.Lerp(_sustainedCurrent, _sustainedTarget,
                                        Time.deltaTime * _sustainedLerpSpeed);
        ApplyAll(_sustainedCurrent, _pulseValue, _isDamagePulse, _colorFilterValue);
    }

    // ── 事件处理 ──────────────────────────────────────────────

    private void OnSanityChanged(float oldValue, float newValue, string reason)
    {
        _sustainedTarget = 1f - SanityManager.Instance.SanityRatio;

        float delta = newValue - oldValue;
        if (Mathf.Abs(delta) < _pulseMinDelta) return;

        bool isDamage = delta < 0f;
        float intensity = Mathf.Clamp(Mathf.Abs(delta) * _pulseIntensityScale,
                                       0f, _pulseMaxIntensity);
        TriggerPulse(isDamage, intensity);
    }

    // ── 脉冲 ──────────────────────────────────────────────────

    private void TriggerPulse(bool isDamage, float intensity)
    {
        if (_pulseCoroutine != null)
            StopCoroutine(_pulseCoroutine);
        _isDamagePulse = isDamage;
        _pulseCoroutine = StartCoroutine(PulseCoroutine(intensity));
    }

    private IEnumerator PulseCoroutine(float intensity)
    {
        float elapsed = 0f;
        while (elapsed < _pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _pulseDuration);
            float curve = _pulseDecayCurve.Evaluate(t);
            _pulseValue       = curve * intensity;
            _colorFilterValue = curve * intensity;
            yield return null;
        }
        _pulseValue       = 0f;
        _colorFilterValue = 0f;
        _pulseCoroutine   = null;
    }

    // ── 效果应用 ──────────────────────────────────────────────

    /// <summary>
    /// 将持续层和脉冲层合并后写入所有后处理效果。
    /// t          = 持续层（0=理智满，1=理智空）
    /// pulse      = 脉冲强度（0-1）
    /// isDamage   = 脉冲方向（true=扣理智，false=恢复）
    /// colorPulse = Color Filter 脉冲强度（0-1）
    /// </summary>
    private void ApplyAll(float t, float pulse, bool isDamage, float colorPulse)
    {
        ApplyLensDistortion(t, pulse, isDamage);
        ApplyChromaticAberration(t, pulse, isDamage);
        ApplyVignette(t, pulse, isDamage);
        ApplyFilmGrain(t);
        ApplyColorAdjustments(t, isDamage, colorPulse);
    }

    private void ApplyLensDistortion(float t, float pulse, bool isDamage)
    {
        if (!_lensDistortion.enabled || _lensDistortionEffect == null) return;

        float sustained = Mathf.Lerp(_lensDistortion.sustainedMin, _lensDistortion.sustainedMax, t);

        // Perlin Noise 波动：理智越低波动越强
        float noise = (Mathf.PerlinNoise(Time.time * _lensDistortion.noiseSpeed, 0.5f) * 2f - 1f)
                      * _lensDistortion.noiseAmplitude * t;

        float extra = isDamage
            ? _lensDistortion.damageExtra * pulse
            : _lensDistortion.healExtra   * pulse;

        _lensDistortionEffect.intensity.value = Mathf.Clamp(sustained + noise + extra, -1f, 1f);
    }

    private void ApplyChromaticAberration(float t, float pulse, bool isDamage)
    {
        if (!_chromaticAberration.enabled || _chromaticAberrationEffect == null) return;

        float sustained = Mathf.Lerp(_chromaticAberration.sustainedMin, _chromaticAberration.sustainedMax, t);
        float extra = isDamage
            ? _chromaticAberration.damageExtra * pulse
            : _chromaticAberration.healExtra   * pulse;

        _chromaticAberrationEffect.intensity.value = Mathf.Clamp01(sustained + extra);
    }

    private void ApplyVignette(float t, float pulse, bool isDamage)
    {
        if (!_vignette.enabled || _vignetteEffect == null) return;

        float sustained = Mathf.Lerp(_vignette.sustainedMin, _vignette.sustainedMax, t);
        float extra = isDamage
            ? _vignette.damageExtra * pulse
            : _vignette.healExtra   * pulse;

        _vignetteEffect.intensity.value = Mathf.Clamp01(sustained + extra);
    }

    private void ApplyFilmGrain(float t)
    {
        if (!_filmGrain.enabled || _filmGrainEffect == null) return;
        _filmGrainEffect.intensity.value = Mathf.Lerp(_filmGrain.sustainedMin, _filmGrain.sustainedMax, t);
    }

    private void ApplyColorAdjustments(float t, bool isDamage, float colorPulse)
    {
        if (!_colorAdjustments.enabled || _colorAdjustmentsEffect == null) return;

        // 持续层：饱和度 + 曝光
        _colorAdjustmentsEffect.saturation.value =
            Mathf.Clamp(Mathf.Lerp(_colorAdjustments.saturationMin, _colorAdjustments.saturationMax, t),
                        -100f, 100f);
        _colorAdjustmentsEffect.postExposure.value =
            Mathf.Lerp(_colorAdjustments.postExposureMin, _colorAdjustments.postExposureMax, t);

        // 脉冲层：Color Filter 从白色插值到闪色，再随脉冲衰减回白色
        Color targetFlash = isDamage
            ? _colorAdjustments.damageFlashColor
            : _colorAdjustments.healFlashColor;
        _colorAdjustmentsEffect.colorFilter.value = Color.Lerp(Color.white, targetFlash, colorPulse);
    }

    // ── 初始化 ────────────────────────────────────────────────

    private void TryGetEffects()
    {
        var profile = _volume.sharedProfile != null ? _volume.sharedProfile : _volume.profile;
        profile.TryGet(out _lensDistortionEffect);
        profile.TryGet(out _chromaticAberrationEffect);
        profile.TryGet(out _vignetteEffect);
        profile.TryGet(out _filmGrainEffect);
        profile.TryGet(out _colorAdjustmentsEffect);
    }

#if UNITY_EDITOR
    [ContextMenu("测试：模拟理智值 100")]
    private void DebugSanityFull()  { _sustainedTarget = 0f; }

    [ContextMenu("测试：模拟理智值 50")]
    private void DebugSanityHalf()  { _sustainedTarget = 0.5f; }

    [ContextMenu("测试：模拟理智值 0")]
    private void DebugSanityEmpty() { _sustainedTarget = 1f; }

    [ContextMenu("测试：触发扣理智脉冲")]
    private void DebugDamagePulse() { TriggerPulse(true,  1f); }

    [ContextMenu("测试：触发恢复理智脉冲")]
    private void DebugHealPulse()   { TriggerPulse(false, 1f); }
#endif
}
