using System;
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
    /// 开局CG面板UI
    /// 支持两种模式：
    ///   ComicPanels    — 四分镜漫画分镜效果
    ///   ImageDialogue  — 单图+对话框演出效果
    /// 挂在 Canvas 上，初始隐藏
    /// </summary>
    public class OpeningCGPanelUI : MonoBehaviour
    {
        // ── 通用节点 ──────────────────────────────────────────

        [Header("根容器")]
        [SerializeField] [Tooltip("整个CG面板的 CanvasGroup")]
        private CanvasGroup _rootGroup;

        [Header("黑幕（两种模式通用，位于最顶层）")]
        [SerializeField] [Tooltip("全屏黑幕 CanvasGroup")]
        private CanvasGroup _blackOverlayGroup;

        [Header("点击按钮（两种模式通用）")]
        [SerializeField] [Tooltip("全屏透明按钮，用于点击推进")]
        private Button _clickButton;

        [Header("音效播放器（ImageDialogue 模式黑幕音效专用）")]
        [SerializeField] [Tooltip("用于播放 blackoutSfx 的 AudioSource，不走 AudioManager 池")]
        private AudioSource _sfxSource;

        [Header("配置")]
        [SerializeField] [Tooltip("开局CG配置SO")]
        private OpeningCGConfigSO _config;

        // ── ComicPanels 专用节点 ──────────────────────────────

        [Header("【ComicPanels】白背景")]
        [SerializeField] private CanvasGroup _whiteBackgroundGroup;

        [Header("【ComicPanels】四个分镜")]
        [SerializeField] private RectTransform _panel1Rect;
        [SerializeField] private Image         _panel1Image;
        [SerializeField] private RectTransform _panel2Rect;
        [SerializeField] private Image         _panel2Image;
        [SerializeField] private RectTransform _panel3Rect;
        [SerializeField] private Image         _panel3Image;
        [SerializeField] private RectTransform _panel4Rect;
        [SerializeField] private Image         _panel4Image;

        // ── ImageDialogue 专用节点 ────────────────────────────

        [Header("【ImageDialogue】背景图")]
        [SerializeField] [Tooltip("全屏背景 Image")]
        private Image _backgroundImage;

        [Header("【ImageDialogue】对话框")]
        [SerializeField] [Tooltip("对话框根节点 CanvasGroup（控制整体显隐）")]
        private CanvasGroup _dialogueBoxGroup;

        [SerializeField] [Tooltip("角色立绘 Image（留空时隐藏）")]
        private Image _characterImage;

        [SerializeField] [Tooltip("说话人名字 TextMeshProUGUI（留空时隐藏名字栏）")]
        private TextMeshProUGUI _characterNameText;

        [SerializeField] [Tooltip("对话文本 TextMeshProUGUI")]
        private TextMeshProUGUI _dialogueText;

        [SerializeField] [Tooltip("确认按钮（如'上船'），打字完成后按需显示，初始隐藏）")]
        private Button _confirmButton;

        [SerializeField] [Tooltip("确认按钮上的文字 TextMeshProUGUI")]
        private TextMeshProUGUI _confirmButtonText;

        // ── 运行时状态 ────────────────────────────────────────

        private RectTransform _canvasRect;
        private Action        _onComplete;
        private bool          _waitingForAdvance;
        private bool          _typewriterSkipRequested;
        private Coroutine     _sequenceCoroutine;

        // ── 生命周期 ──────────────────────────────────────────

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _canvasRect = canvas.GetComponent<RectTransform>();

            SetGroupState(_rootGroup, false);
            _clickButton?.onClick.AddListener(OnClicked);
            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            if (_confirmButton != null)
                _confirmButton.gameObject.SetActive(false);
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>播放开局CG序列</summary>
        public void Play(OpeningCGConfigSO config, Action onComplete)
        {
            if (config != null) _config = config;
            _onComplete = onComplete;

            if (_config == null)
            {
                Debug.LogError("[OpeningCG] 配置为空，直接执行回调");
                onComplete?.Invoke();
                return;
            }

            SetGroupState(_rootGroup, true);

            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = _config.cgMode == OpeningCGMode.ComicPanels
                ? StartCoroutine(PlayComicSequence())
                : StartCoroutine(PlayImageDialogueSequence());
        }

        // ── 点击处理 ──────────────────────────────────────────

        private void OnClicked()
        {
            // 当前条目有确认按钮时，全屏点击只负责跳过打字机，不推进
            if (_confirmButton != null && _confirmButton.gameObject.activeSelf)
            {
                if (!_typewriterSkipRequested)
                    _typewriterSkipRequested = true;
                return;
            }

            // 普通模式：第一次点击跳过打字机，第二次推进
            if (!_typewriterSkipRequested)
            {
                _typewriterSkipRequested = true;
                return;
            }
            if (_waitingForAdvance)
                _waitingForAdvance = false;
        }

        private void OnConfirmClicked()
        {
            if (_waitingForAdvance)
                _waitingForAdvance = false;
        }

        // ════════════════════════════════════════════════════════
        // ComicPanels 序列
        // ════════════════════════════════════════════════════════

        private IEnumerator PlayComicSequence()
        {
            if (_config.bgm != null)
                AudioManager.Instance?.PlayBgm(_config.bgm);

            // 初始化
            SetGroupAlpha(_blackOverlayGroup, 1f);
            SetGroupAlpha(_whiteBackgroundGroup, 0f);

            float canvasW = GetCanvasWidth();
            MoveOffScreen(_panel1Rect, true,  canvasW);
            MoveOffScreen(_panel2Rect, false, canvasW);
            MoveOffScreen(_panel3Rect, true,  canvasW);
            MoveOffScreen(_panel4Rect, false, canvasW);
            SetPanelActive(_panel1Rect, false);
            SetPanelActive(_panel2Rect, false);
            SetPanelActive(_panel3Rect, false);
            SetPanelActive(_panel4Rect, false);

            // 黑幕淡出 → 白背景
            yield return FadeBlackOverlay(0f, _config.fadeInDuration, fadeWhiteBgIn: true);

            // 四个分镜依次滑入
            ComicPanelConfig[] configs = { _config.panel1, _config.panel2, _config.panel3, _config.panel4 };
            RectTransform[]    rects   = { _panel1Rect, _panel2Rect, _panel3Rect, _panel4Rect };
            Image[]            images  = { _panel1Image, _panel2Image, _panel3Image, _panel4Image };

            for (int i = 0; i < 4; i++)
            {
                if (images[i] != null && configs[i] != null)
                    images[i].sprite = configs[i].sprite;

                yield return SlideInPanel(i, rects[i], configs[i]);

                if (i < 3)
                    yield return WaitForAdvance(configs[i].autoAdvanceDelay);
            }

            // 全部显示后等待手动点击
            _waitingForAdvance = true;
            while (_waitingForAdvance) yield return null;

            // 白背景转黑幕
            yield return FadeBlackOverlay(1f, _config.fadeOutDuration, fadeWhiteBgIn: false);

            FinishSequence();
        }

        // ════════════════════════════════════════════════════════
        // ImageDialogue 序列
        // ════════════════════════════════════════════════════════

        private IEnumerator PlayImageDialogueSequence()
        {
            if (_config.bgm != null)
                AudioManager.Instance?.PlayBgm(_config.bgm);

            // 初始化：黑幕全黑，背景图和对话框隐藏
            SetGroupAlpha(_blackOverlayGroup, 1f);

            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = _config.backgroundSprite;
                _backgroundImage.gameObject.SetActive(_config.backgroundSprite != null);
            }

            SetGroupState(_dialogueBoxGroup, false);

            // 黑幕淡出 → 背景图显现
            yield return FadeBlackOverlay(0f, _config.fadeInDuration);

            // 逐条播放对话
            bool backgroundVisible = true;
            foreach (var entry in _config.dialogues)
            {
                // 需要先拉黑幕
                if (entry.blackoutBefore && backgroundVisible)
                {
                    // 对话框先隐藏
                    SetGroupState(_dialogueBoxGroup, false);

                    // 黑幕淡入（盖住背景图）
                    yield return FadeBlackOverlay(1f, _config.fadeOutDuration);
                    backgroundVisible = false;

                    // 播放音效
                    if (entry.blackoutSfx != null && _sfxSource != null)
                    {
                        _sfxSource.clip = entry.blackoutSfx;
                        _sfxSource.Play();
                    }

                    // 短暂停顿后对话框淡入（浮在黑幕上）
                    yield return new WaitForSecondsRealtime(0.3f);
                }

                // 显示对话框
                yield return ShowDialogueEntry(entry);
            }

            // 所有对话结束，对话框淡出
            if (_dialogueBoxGroup != null)
            {
                _dialogueBoxGroup.DOKill();
                _dialogueBoxGroup.DOFade(0f, 0.4f).SetUpdate(true);
                yield return new WaitForSecondsRealtime(0.4f);
            }

            // 确保黑幕完全不透明（若最后一条没有 blackoutBefore，需要淡入黑幕）
            if (backgroundVisible)
                yield return FadeBlackOverlay(1f, _config.fadeOutDuration);

            FinishSequence();
        }

        /// <summary>显示单条对话：立绘切换 + 打字机文本 + 等待推进</summary>
        private IEnumerator ShowDialogueEntry(CGDialogueEntry entry)
        {
            // 切换立绘
            if (_characterImage != null)
            {
                _characterImage.sprite  = entry.characterSprite;
                _characterImage.enabled = entry.characterSprite != null;
            }

            // 切换名字
            if (_characterNameText != null)
            {
                bool hasName = !string.IsNullOrEmpty(entry.characterName);
                _characterNameText.text    = hasName ? entry.characterName : "";
                _characterNameText.enabled = hasName;
            }

            // 清空文本，显示对话框
            if (_dialogueText != null) _dialogueText.text = "";
            SetGroupState(_dialogueBoxGroup, true);

            // 打字机效果
            _typewriterSkipRequested = false;
            yield return TypewriterEffect(entry.text, entry.typewriterInterval);

            // 打字完成后：有确认按钮文字则显示按钮等待点击，否则普通等待
            bool hasConfirmButton = !string.IsNullOrEmpty(entry.confirmButtonText);
            if (hasConfirmButton && _confirmButton != null)
            {
                if (_confirmButtonText != null)
                    _confirmButtonText.text = entry.confirmButtonText;
                _confirmButton.gameObject.SetActive(true);
                yield return WaitForAdvance(0f);  // 纯等点击（只有确认按钮能触发）
                _confirmButton.gameObject.SetActive(false);
            }
            else
            {
                yield return WaitForAdvance(entry.autoAdvanceDelay);
            }
        }

        /// <summary>打字机效果：逐字显示，点击可跳过直接显示全文。自然跑完后标记 skip=true 避免下次点击被误判为跳过</summary>
        private IEnumerator TypewriterEffect(string fullText, float interval)
        {
            if (_dialogueText == null) yield break;

            _dialogueText.text = "";
            float timer = 0f;

            for (int i = 0; i < fullText.Length; i++)
            {
                if (_typewriterSkipRequested)
                {
                    _dialogueText.text = fullText;
                    yield break;
                }

                _dialogueText.text += fullText[i];
                timer = 0f;
                while (timer < interval)
                {
                    if (_typewriterSkipRequested)
                    {
                        _dialogueText.text = fullText;
                        yield break;
                    }
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            // 自然跑完：标记为已完成，下次点击直接推进而不是误触发"跳过"
            _typewriterSkipRequested = true;
        }

        // ════════════════════════════════════════════════════════
        // 通用动画工具
        // ════════════════════════════════════════════════════════

        /// <summary>黑幕淡入/淡出，可选同时控制白背景</summary>
        private IEnumerator FadeBlackOverlay(float targetAlpha, float duration, bool fadeWhiteBgIn = false)
        {
            _blackOverlayGroup?.DOKill();
            _blackOverlayGroup?.DOFade(targetAlpha, duration).SetUpdate(true);

            if (fadeWhiteBgIn && _whiteBackgroundGroup != null)
            {
                _whiteBackgroundGroup.DOKill();
                _whiteBackgroundGroup.DOFade(1f, duration).SetUpdate(true);
            }

            yield return new WaitForSecondsRealtime(duration);
        }

        /// <summary>等待推进：delay > 0 时计时（可点击提前），delay == 0 时纯等点击</summary>
        private IEnumerator WaitForAdvance(float delay)
        {
            _waitingForAdvance = true;

            if (delay > 0f)
            {
                float elapsed = 0f;
                while (elapsed < delay && _waitingForAdvance)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            else
            {
                while (_waitingForAdvance) yield return null;
            }

            _waitingForAdvance = false;
        }

        /// <summary>单个分镜滑入动画</summary>
        private IEnumerator SlideInPanel(int index, RectTransform rect, ComicPanelConfig config)
        {
            if (rect == null || config == null) yield break;

            SetPanelActive(rect, true);
            float duration = Mathf.Max(0.01f, config.slideInDuration);

            rect.DOKill();
            rect.DOAnchorPosX(0f, duration).SetEase(Ease.OutCubic).SetUpdate(true);
            yield return new WaitForSecondsRealtime(duration);

            SoundId sfx = index switch
            {
                0 => SoundId.OpeningCGPanel1,
                1 => SoundId.OpeningCGPanel2,
                2 => SoundId.OpeningCGPanel3,
                _ => SoundId.OpeningCGPanel4,
            };
            AudioManager.Instance?.PlaySfx(sfx);
        }

        /// <summary>序列结束：隐藏面板，执行回调</summary>
        private void FinishSequence()
        {
            SetGroupState(_rootGroup, false);
            _sequenceCoroutine = null;

            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        // ── 工具方法 ──────────────────────────────────────────

        private void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            group.alpha = alpha;
        }

        private void SetGroupState(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha          = visible ? 1f : 0f;
            group.interactable   = visible;
            group.blocksRaycasts = visible;
        }

        private float GetCanvasWidth()
        {
            return _canvasRect != null ? _canvasRect.rect.width : Screen.width;
        }

        private void MoveOffScreen(RectTransform rect, bool fromLeft, float canvasW)
        {
            if (rect == null) return;
            rect.anchoredPosition = new Vector2(fromLeft ? -canvasW : canvasW, 0f);
        }

        private void SetPanelActive(RectTransform rect, bool active)
        {
            if (rect != null) rect.gameObject.SetActive(active);
        }
    }
}
