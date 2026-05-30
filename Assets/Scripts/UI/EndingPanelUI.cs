using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 结局系统UI
    /// 管理过场Panel（图片+打字机文本，点击推进）和结局Panel（图片+标题+描述）
    /// 监听 GameFlowManager.OnGameEnding 触发
    /// </summary>
    public class EndingPanelUI : MonoBehaviour
    {
        [Header("根容器")]
        [SerializeField] [Tooltip("结局系统根 CanvasGroup（控制整体显隐和射线阻挡）")]
        private CanvasGroup _rootCanvasGroup;

        [Header("黑幕过渡")]
        [SerializeField] [Tooltip("全屏黑幕 CanvasGroup")]
        private CanvasGroup _blackOverlay;

        [SerializeField] [Tooltip("黑幕淡入时长")]
        private float _blackFadeDuration = 1f;

        [Header("过场Panel")]
        [SerializeField] [Tooltip("过场面板 CanvasGroup")]
        private CanvasGroup _cutscenePanel;

        [SerializeField] [Tooltip("过场图片")]
        private Image _cutsceneImage;

        [SerializeField] [Tooltip("过场文本")]
        private TextMeshProUGUI _cutsceneText;

        [SerializeField] [Tooltip("过场全屏点击按钮（推进下一帧）")]
        private Button _cutsceneClickButton;

        [Header("结局Panel")]
        [SerializeField] [Tooltip("结局面板 CanvasGroup")]
        private CanvasGroup _endingPanel;

        [SerializeField] [Tooltip("结局图片")]
        private Image _endingImage;

        [SerializeField] [Tooltip("结局名称文本")]
        private TextMeshProUGUI _endingTitleText;

        [SerializeField] [Tooltip("结局描述文本")]
        private TextMeshProUGUI _endingDescText;

        [SerializeField] [Tooltip("结局全屏点击按钮（返回主菜单）")]
        private Button _endingClickButton;

        [Header("结局配置")]
        [SerializeField] [Tooltip("结局配置SO")]
        private EndingConfigSO _endingConfigSO;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("过场图片切换淡入淡出时长")]
        private float _slideFadeDuration = 0.5f;

        [SerializeField] [Tooltip("打字机速度（字符/秒）")]
        private float _typewriterSpeed = 25f;

        [SerializeField] [Tooltip("过场开始前的等待时间")]
        private float _cutsceneStartDelay = 0.5f;

        /// <summary>当前结局配置</summary>
        private EndingConfig _currentConfig;

        /// <summary>当前过场帧索引</summary>
        private int _currentSlideIndex;

        /// <summary>打字机协程引用</summary>
        private Coroutine _typewriterCoroutine;

        /// <summary>是否正在播放打字机效果</summary>
        private bool _isTyping;

        /// <summary>当前正在切换过场帧（防止快速点击）</summary>
        private bool _isTransitioning;

        private void Awake()
        {
            // 初始状态：整个结局系统不可见、不阻挡射线
            SetGroupState(_rootCanvasGroup, false);
            SetGroupState(_blackOverlay, false);
            SetGroupState(_cutscenePanel, false);
            SetGroupState(_endingPanel, false);

            _cutsceneClickButton?.onClick.AddListener(OnCutsceneClicked);
            _endingClickButton?.onClick.AddListener(OnEndingClicked);
        }

        private void Start()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.OnGameEnding += OnGameEnding;
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.OnGameEnding -= OnGameEnding;
        }

        // ── 结局触发 ──────────────────────────────────────────

        private void OnGameEnding(GameEnding ending)
        {
            if (_endingConfigSO == null)
            {
                Debug.LogError("[Ending] EndingConfigSO 未配置");
                return;
            }

            _currentConfig = _endingConfigSO.GetEndingConfig(ending);
            if (_currentConfig == null)
            {
                Debug.LogError($"[Ending] 结局配置为空: {ending}");
                return;
            }

            StartCoroutine(PlayEndingSequence());
        }

        /// <summary>
        /// 完整结局流程：黑幕 → 过场序列 → 结局面板
        /// </summary>
        private IEnumerator PlayEndingSequence()
        {
            // 启用根容器（开始阻挡射线）
            if (_rootCanvasGroup != null)
            {
                _rootCanvasGroup.alpha = 1f;
                _rootCanvasGroup.interactable = true;
                _rootCanvasGroup.blocksRaycasts = true;
            }

            // 1. 黑幕淡入
            yield return FadeInBlackOverlay();

            // 2. 切换结局BGM
            if (_currentConfig.bgm != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayBgm(_currentConfig.bgm);

            // 3. 等待一小段时间
            yield return new WaitForSecondsRealtime(_cutsceneStartDelay);

            // 3. 播放过场序列
            if (_currentConfig.cutsceneSlides != null && _currentConfig.cutsceneSlides.Count > 0)
            {
                _currentSlideIndex = 0;
                ShowCutsceneSlide(_currentSlideIndex);
            }
            else
            {
                // 没有过场帧，直接显示结局面板
                ShowEndingPanel();
            }
        }

        // ── 过场Panel ─────────────────────────────────────────

        /// <summary>显示指定索引的过场帧</summary>
        private void ShowCutsceneSlide(int index)
        {
            if (_currentConfig.cutsceneSlides == null || index >= _currentConfig.cutsceneSlides.Count)
            {
                // 所有过场帧播完，切换到结局面板
                StartCoroutine(TransitionToEndingPanel());
                return;
            }

            var slide = _currentConfig.cutsceneSlides[index];

            // 设置图片
            if (_cutsceneImage != null && slide.image != null)
                _cutsceneImage.sprite = slide.image;

            // 清空文本
            if (_cutsceneText != null)
                _cutsceneText.text = "";

            // 显示过场面板
            if (index == 0)
            {
                // 第一帧渐入显示
                _cutscenePanel.alpha = 0f;
                _cutscenePanel.blocksRaycasts = true;
                _cutscenePanel.interactable = false;
                _isTransitioning = true;
                _cutscenePanel.DOKill();
                _cutscenePanel.DOFade(1f, _slideFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _cutscenePanel.interactable = true;
                        _isTransitioning = false;
                        StartTypewriter(_cutsceneText, slide.text);
                    });
                return;
            }

            // 非第一帧：开始打字机效果
            _isTransitioning = false;
            StartTypewriter(_cutsceneText, slide.text);
        }

        /// <summary>过场面板点击事件</summary>
        private void OnCutsceneClicked()
        {
            if (_isTransitioning) return;

            if (_isTyping)
            {
                // 打字机播放中：跳过打字机，直接显示完整文本
                SkipTypewriter();
            }
            else
            {
                // 打字机已完成：切换到下一帧
                _currentSlideIndex++;
                if (_currentSlideIndex < _currentConfig.cutsceneSlides.Count)
                {
                    StartCoroutine(TransitionToNextSlide(_currentSlideIndex));
                }
                else
                {
                    StartCoroutine(TransitionToEndingPanel());
                }
            }
        }

        /// <summary>过渡到下一帧（旧的渐隐，新的渐出）</summary>
        private IEnumerator TransitionToNextSlide(int nextIndex)
        {
            _isTransitioning = true;

            // 淡出当前内容
            _cutscenePanel.DOKill();
            _cutscenePanel.DOFade(0f, _slideFadeDuration)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(_slideFadeDuration);

            // 更新内容
            var slide = _currentConfig.cutsceneSlides[nextIndex];
            if (_cutsceneImage != null && slide.image != null)
                _cutsceneImage.sprite = slide.image;
            if (_cutsceneText != null)
                _cutsceneText.text = "";

            // 淡入新内容
            _cutscenePanel.DOKill();
            _cutscenePanel.DOFade(1f, _slideFadeDuration)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(_slideFadeDuration);

            _cutscenePanel.interactable = true;

            // 开始新帧的打字机
            _isTransitioning = false;
            StartTypewriter(_cutsceneText, slide.text);
        }

        // ── 结局Panel ─────────────────────────────────────────

        /// <summary>从过场过渡到结局面板</summary>
        private IEnumerator TransitionToEndingPanel()
        {
            _isTransitioning = true;

            // 淡出过场面板
            if (_cutscenePanel != null)
            {
                _cutscenePanel.DOKill();
                _cutscenePanel.DOFade(0f, _slideFadeDuration)
                    .SetUpdate(true);
                yield return new WaitForSecondsRealtime(_slideFadeDuration);
                SetGroupState(_cutscenePanel, false);
            }

            yield return new WaitForSecondsRealtime(0.3f);

            ShowEndingPanel();
        }

        /// <summary>显示结局面板</summary>
        private void ShowEndingPanel()
        {
            _isTransitioning = false;

            // 设置结局图片
            if (_endingImage != null && _currentConfig.endingImage != null)
                _endingImage.sprite = _currentConfig.endingImage;

            // 清空文本（打字机会逐步填充）
            if (_endingTitleText != null)
                _endingTitleText.text = "";
            if (_endingDescText != null)
                _endingDescText.text = "";

            // 淡入结局面板
            if (_endingPanel != null)
            {
                _endingPanel.alpha = 0f;
                _endingPanel.blocksRaycasts = true;
                _endingPanel.interactable = false;
                _endingPanel.DOKill();
                _endingPanel.DOFade(1f, _slideFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _endingPanel.interactable = true;
                        // 开始结局标题打字机
                        StartCoroutine(PlayEndingTexts());
                    });
            }
        }

        /// <summary>依次播放结局标题和描述的打字机效果</summary>
        private IEnumerator PlayEndingTexts()
        {
            // 播放标题
            _isTyping = true;
            yield return StartCoroutine(TypewriterCoroutine(_endingTitleText, _currentConfig.endingTitle));

            yield return new WaitForSecondsRealtime(0.3f);

            // 播放描述
            _isTyping = true;
            yield return StartCoroutine(TypewriterCoroutine(_endingDescText, _currentConfig.endingDescription));

            _isTyping = false;
        }

        /// <summary>结局面板点击事件（返回主菜单）</summary>
        private void OnEndingClicked()
        {
            if (_isTyping)
            {
                // 打字机播放中：跳过，直接显示完整文本
                StopAllCoroutines();
                _isTyping = false;
                if (_endingTitleText != null)
                    _endingTitleText.text = _currentConfig.endingTitle ?? "";
                if (_endingDescText != null)
                    _endingDescText.text = _currentConfig.endingDescription ?? "";
                return;
            }

            // 禁止重复点击
            if (_endingClickButton != null)
                _endingClickButton.interactable = false;

            // 渐黑后返回主菜单
            StartCoroutine(FadeOutToMainMenu());
        }

        /// <summary>渐黑过渡后加载主菜单</summary>
        private IEnumerator FadeOutToMainMenu()
        {
            // 淡出结局面板
            if (_endingPanel != null)
            {
                _endingPanel.DOKill();
                _endingPanel.interactable = false;
                _endingPanel.DOFade(0f, _slideFadeDuration).SetUpdate(true);
                yield return new WaitForSecondsRealtime(_slideFadeDuration);
            }

            // 确保黑幕完全不透明（此时黑幕应该已经是全黑的，但做个保底）
            if (_blackOverlay != null)
            {
                _blackOverlay.DOKill();
                _blackOverlay.alpha = 1f;
            }

            // 切回默认BGM
            AudioManager.Instance?.PlayDefaultBgm();

            yield return new WaitForSecondsRealtime(0.3f);

            // 加载主菜单
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }

        // ── 黑幕 ─────────────────────────────────────────────

        private IEnumerator FadeInBlackOverlay()
        {
            if (_blackOverlay == null) yield break;

            _blackOverlay.DOKill();
            _blackOverlay.alpha = 0f;
            _blackOverlay.blocksRaycasts = true;
            _blackOverlay.DOFade(1f, _blackFadeDuration)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(_blackFadeDuration);
        }

        // ── 打字机效果 ────────────────────────────────────────

        private void StartTypewriter(TextMeshProUGUI label, string text)
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _isTyping = true;
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(label, text));
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isTyping = false;

            // 显示当前帧的完整文本
            if (_currentSlideIndex < _currentConfig.cutsceneSlides.Count)
            {
                var slide = _currentConfig.cutsceneSlides[_currentSlideIndex];
                if (_cutsceneText != null)
                    _cutsceneText.text = slide.text ?? "";
            }
        }

        private IEnumerator TypewriterCoroutine(TextMeshProUGUI label, string fullText)
        {
            if (label == null || string.IsNullOrEmpty(fullText))
            {
                _isTyping = false;
                _typewriterCoroutine = null;
                yield break;
            }

            label.text = "";
            float delay = 1f / _typewriterSpeed;

            for (int i = 0; i < fullText.Length; i++)
            {
                label.text = fullText.Substring(0, i + 1);
                yield return new WaitForSecondsRealtime(delay);
            }

            _isTyping = false;
            _typewriterCoroutine = null;
        }

        // ── 工具 ──────────────────────────────────────────────

        private void SetGroupState(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
