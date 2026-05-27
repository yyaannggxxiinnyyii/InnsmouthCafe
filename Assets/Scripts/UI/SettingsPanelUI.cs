using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 设置面板UI
    /// 控制音效音量、BGM音量、显示模式（下拉）及分辨率（下拉）
    /// 使用 CanvasGroup 淡入淡出
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("CanvasGroup")]
        [SerializeField] [Tooltip("本面板的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [Header("音量设置")]
        [SerializeField] [Tooltip("音效音量滑条（Min=0 Max=1）")]
        private Slider _sfxSlider;

        [SerializeField] [Tooltip("BGM音量滑条（Min=0 Max=1）")]
        private Slider _bgmSlider;

        [SerializeField] [Tooltip("音效音量数值文本（显示百分比）")]
        private TextMeshProUGUI _sfxValueText;

        [SerializeField] [Tooltip("BGM音量数值文本（显示百分比）")]
        private TextMeshProUGUI _bgmValueText;

        [Header("显示模式")]
        [SerializeField] [Tooltip("显示模式下拉框（选项：窗口 / 全屏）")]
        private TMP_Dropdown _displayModeDropdown;

        [SerializeField] [Tooltip("分辨率下拉框（窗口模式时可交互）")]
        private TMP_Dropdown _resolutionDropdown;

        [SerializeField] [Tooltip("分辨率行容器的 CanvasGroup（全屏时淡出禁用）")]
        private CanvasGroup _resolutionGroup;

        [Header("底部按钮")]
        [SerializeField] [Tooltip("关闭/返回按钮")]
        private Button _closeButton;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("面板淡入淡出时长")]
        private float _fadeDuration = 0.25f;

        // 显示模式下拉选项索引
        private const int IndexWindowed    = 0;
        private const int IndexFullscreen  = 1;

        private MainMenuUI _mainMenu;
        private bool _isFullscreen;
        private int  _resolutionIndex;

        /// <summary>关闭按钮回调，由外部设置（MainMenuUI 或 PausePanelUI）</summary>
        public Action OnCloseCallback { get; set; }

        private void Awake()
        {
            _mainMenu = FindObjectOfType<MainMenuUI>();

            _closeButton?.onClick.AddListener(OnCloseClicked);
            _sfxSlider?.onValueChanged.AddListener(OnSfxChanged);
            _bgmSlider?.onValueChanged.AddListener(OnBgmChanged);

            BuildDisplayModeDropdown();
            BuildResolutionDropdown();

            _displayModeDropdown?.onValueChanged.AddListener(OnDisplayModeChanged);
            _resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
        }

        // ── 显示/隐藏 ─────────────────────────────────────────

        /// <summary>显示面板并刷新当前设置值（由 MainMenuUI 调用）</summary>
        public void Show()
        {
            RefreshAll();
            FadePanel(true);
        }

        /// <summary>隐藏面板</summary>
        public void Hide(bool immediate = false)
        {
            if (immediate)
                SetGroupState(_canvasGroup, false);
            else
                FadePanel(false);
        }

        // ── 初始化 ────────────────────────────────────────────

        private void BuildDisplayModeDropdown()
        {
            if (_displayModeDropdown == null) return;
            _displayModeDropdown.ClearOptions();
            _displayModeDropdown.AddOptions(new List<string> { "窗口", "全屏" });
        }

        private void BuildResolutionDropdown()
        {
            if (_resolutionDropdown == null) return;
            _resolutionDropdown.ClearOptions();

            var options = new List<string>();
            foreach (var res in GameManager.WindowedResolutions)
                options.Add($"{res.width} × {res.height}");

            _resolutionDropdown.AddOptions(options);
        }

        /// <summary>从 GameManager / AudioManager 读取当前值并刷新所有控件</summary>
        private void RefreshAll()
        {
            // 音量
            if (AudioManager.Instance != null)
            {
                if (_sfxSlider != null)
                {
                    _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
                    UpdateVolumeText(_sfxValueText, AudioManager.Instance.SfxVolume);
                }
                if (_bgmSlider != null)
                {
                    _bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
                    UpdateVolumeText(_bgmValueText, AudioManager.Instance.BgmVolume);
                }
            }

            // 显示设置
            if (GameManager.Instance != null)
            {
                _isFullscreen    = GameManager.Instance.IsFullscreen;
                _resolutionIndex = GameManager.Instance.ResolutionIndex;
            }
            else
            {
                _isFullscreen    = false;
                _resolutionIndex = 2; // 默认 1920×1080
            }

            _displayModeDropdown?.SetValueWithoutNotify(_isFullscreen ? IndexFullscreen : IndexWindowed);
            _resolutionDropdown?.SetValueWithoutNotify(_resolutionIndex);
            RefreshResolutionGroupState(immediate: true);
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnSfxChanged(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
            UpdateVolumeText(_sfxValueText, value);
        }

        private void OnBgmChanged(float value)
        {
            AudioManager.Instance?.SetBgmVolume(value);
            UpdateVolumeText(_bgmValueText, value);
        }

        private void OnDisplayModeChanged(int index)
        {
            _isFullscreen = (index == IndexFullscreen);
            GameManager.Instance?.ApplyDisplaySettings(_isFullscreen, _resolutionIndex);
            RefreshResolutionGroupState(immediate: false);
        }

        private void OnResolutionChanged(int index)
        {
            _resolutionIndex = index;
            GameManager.Instance?.ApplyDisplaySettings(_isFullscreen, _resolutionIndex);
        }

        private void OnCloseClicked()
        {
            if (OnCloseCallback != null)
                OnCloseCallback.Invoke();
            else
                _mainMenu?.OnSettingsClosed();
        }

        // ── UI 刷新 ───────────────────────────────────────────

        /// <summary>根据当前模式设置分辨率下拉框的可见/可交互状态</summary>
        private void RefreshResolutionGroupState(bool immediate)
        {
            if (_resolutionGroup == null) return;

            float targetAlpha = _isFullscreen ? 0f : 1f;

            _resolutionGroup.DOKill();

            if (immediate)
            {
                _resolutionGroup.alpha          = targetAlpha;
                _resolutionGroup.interactable   = !_isFullscreen;
                _resolutionGroup.blocksRaycasts = !_isFullscreen;
            }
            else
            {
                _resolutionGroup.interactable   = false;
                _resolutionGroup.blocksRaycasts = !_isFullscreen;
                _resolutionGroup.DOFade(targetAlpha, 0.15f).OnComplete(() =>
                {
                    _resolutionGroup.interactable = !_isFullscreen;
                });
            }
        }

        private void UpdateVolumeText(TextMeshProUGUI label, float value)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        // ── CanvasGroup 工具 ──────────────────────────────────

        private void FadePanel(bool fadeIn)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.DOKill();

            if (fadeIn)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => _canvasGroup.interactable = true);
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => _canvasGroup.blocksRaycasts = false);
            }
        }

        private void SetGroupState(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha          = visible ? 1f : 0f;
            group.interactable   = visible;
            group.blocksRaycasts = visible;
        }
    }
}
