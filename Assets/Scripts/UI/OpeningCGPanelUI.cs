using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 开局CG面板UI
    /// 四分镜漫画分镜效果：黑幕 → 白背景 → 四个分镜依次滑入 → 点击继续 → 黑幕 → 回调
    /// 挂在 MainScene 的 Canvas 上，初始隐藏
    public class OpeningCGPanelUI : MonoBehaviour
    {
        [Header("根容器")]
        [SerializeField] [Tooltip("整个CG面板的 CanvasGroup（控制整体显隐和射线阻挡）")]
        private CanvasGroup _rootGroup;

        [Header("背景与黑幕")]
        [SerializeField] [Tooltip("全屏白背景的 CanvasGroup")]
        private CanvasGroup _whiteBackgroundGroup;

        [SerializeField] [Tooltip("全屏黑幕的 CanvasGroup（位于最顶层）")]
        private CanvasGroup _blackOverlayGroup;

        [Header("四个分镜（RectTransform + Image）")]
        [SerializeField] [Tooltip("第1分镜 RectTransform（左上，从左滑入）")]
        private RectTransform _panel1Rect;

        [SerializeField] [Tooltip("第1分镜 Image")]
        private Image _panel1Image;

        [SerializeField] [Tooltip("第2分镜 RectTransform（右上，从右滑入）")]
        private RectTransform _panel2Rect;

        [SerializeField] [Tooltip("第2分镜 Image")]
        private Image _panel2Image;

        [SerializeField] [Tooltip("第3分镜 RectTransform（左下，从左滑入）")]
        private RectTransform _panel3Rect;

        [SerializeField] [Tooltip("第3分镜 Image")]
        private Image _panel3Image;

        [SerializeField] [Tooltip("第4分镜 RectTransform（右下，从右滑入）")]
        private RectTransform _panel4Rect;

        [SerializeField] [Tooltip("第4分镜 Image")]
        private Image _panel4Image;

        [Header("点击按钮")]
        [SerializeField] [Tooltip("全屏透明按钮，用于点击推进或跳过等待")]
        private Button _clickButton;

        [Header("配置")]
        [SerializeField] [Tooltip("开局CG配置SO")]
        private OpeningCGConfigSO _config;

        // ── 运行时状态 ────────────────────────────────────────

        /// <summary>所在Canvas的RectTransform，用于获取画布尺寸</summary>
        private RectTransform _canvasRect;

        /// <summary>播放完成回调</summary>
        private Action _onComplete;

        /// <summary>当前是否处于等待点击/等待计时状态</summary>
        private bool _waitingForAdvance;

        /// <summary>主序列协程引用</summary>
        private Coroutine _sequenceCoroutine;

        // ── 生命周期 ──────────────────────────────────────────

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _canvasRect = canvas.GetComponent<RectTransform>();

            // 初始完全隐藏
            SetGroupState(_rootGroup, false);

            _clickButton?.onClick.AddListener(OnClicked);
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>
        /// 播放开局CG序列
        /// </summary>
        /// <param name="config">CG配置，传null则使用Inspector中配置的默认值</param>
        /// <param name="onComplete">CG播放完毕（黑幕结束）后的回调</param>
        public void Play(OpeningCGConfigSO config, Action onComplete)
        {
            if (config != null)
                _config = config;

            _onComplete = onComplete;

            if (_config == null)
            {
                Debug.LogError("[OpeningCG] 配置为空，直接执行回调");
                onComplete?.Invoke();
                return;
            }

            // 启用根容器，开始阻挡射线
            if (_rootGroup != null)
            {
                _rootGroup.alpha = 1f;
                _rootGroup.interactable = true;
                _rootGroup.blocksRaycasts = true;
            }

            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = StartCoroutine(PlaySequence());
        }

        // ── 主序列 ────────────────────────────────────────────

        private IEnumerator PlaySequence()
        {
            // 播放BGM
            if (_config.bgm != null)
                AudioManager.Instance?.PlayBgm(_config.bgm);

            // 1. 初始化：黑幕全黑，白背景透明，所有分镜移出屏幕外
            SetupInitialState();

            // 2. 黑幕淡出 → 白背景显现
            yield return FadeBlackToWhite();

            // 3. 依次显示四个分镜
            ComicPanelConfig[] configs = { _config.panel1, _config.panel2, _config.panel3, _config.panel4 };
            RectTransform[]    rects   = { _panel1Rect, _panel2Rect, _panel3Rect, _panel4Rect };
            Image[]            images  = { _panel1Image, _panel2Image, _panel3Image, _panel4Image };

            for (int i = 0; i < 4; i++)
            {
                // 设置贴图并激活
                if (images[i] != null && configs[i] != null)
                    images[i].sprite = configs[i].sprite;

                // 滑入动画
                yield return SlideInPanel(i, rects[i], configs[i]);

                // 最后一格不等待，直接进入"全部显示后等待点击"
                if (i < 3)
                    yield return WaitForAdvance(configs[i].autoAdvanceDelay);
            }

            // 4. 全部分镜显示完毕，等待手动点击继续
            _waitingForAdvance = true;
            while (_waitingForAdvance)
                yield return null;

            // 5. 白背景转黑幕
            yield return FadeWhiteToBlack();

            // 6. 隐藏整个面板，执行回调
            SetGroupState(_rootGroup, false);
            _sequenceCoroutine = null;

            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        // ── 点击处理 ──────────────────────────────────────────

        private void OnClicked()
        {
            if (_waitingForAdvance)
                _waitingForAdvance = false;
        }

        // ── 动画步骤 ──────────────────────────────────────────

        /// <summary>初始化所有元素到起始状态</summary>
        private void SetupInitialState()
        {
            float canvasW = GetCanvasWidth();

            // 黑幕不透明
            SetGroupAlpha(_blackOverlayGroup, 1f);

            // 白背景透明
            SetGroupAlpha(_whiteBackgroundGroup, 0f);

            // 四个分镜移到屏幕外，并隐藏
            MoveOffScreen(_panel1Rect, fromLeft: true,  canvasW);
            MoveOffScreen(_panel2Rect, fromLeft: false, canvasW);
            MoveOffScreen(_panel3Rect, fromLeft: true,  canvasW);
            MoveOffScreen(_panel4Rect, fromLeft: false, canvasW);

            SetPanelActive(_panel1Rect, false);
            SetPanelActive(_panel2Rect, false);
            SetPanelActive(_panel3Rect, false);
            SetPanelActive(_panel4Rect, false);
        }

        /// <summary>黑幕淡出，白背景淡入</summary>
        private IEnumerator FadeBlackToWhite()
        {
            float duration = _config.blackToWhiteDuration;

            _whiteBackgroundGroup?.DOKill();
            _whiteBackgroundGroup?.DOFade(1f, duration).SetUpdate(true);

            _blackOverlayGroup?.DOKill();
            _blackOverlayGroup?.DOFade(0f, duration).SetUpdate(true);

            yield return new WaitForSecondsRealtime(duration);
        }

        /// <summary>白背景转黑幕</summary>
        private IEnumerator FadeWhiteToBlack()
        {
            float duration = _config.whiteToBlackDuration;

            _blackOverlayGroup?.DOKill();
            _blackOverlayGroup?.DOFade(1f, duration).SetUpdate(true);

            yield return new WaitForSecondsRealtime(duration);
        }

        /// <summary>单个分镜滑入动画，完成后播放对应音效</summary>
        private IEnumerator SlideInPanel(int index, RectTransform rect, ComicPanelConfig config)
        {
            if (rect == null || config == null) yield break;

            SetPanelActive(rect, true);

            float duration = Mathf.Max(0.01f, config.slideInDuration);

            rect.DOKill();
            rect.DOAnchorPosX(0f, duration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);

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

        /// <summary>
        /// 等待推进：delay > 0 时计时，同时监听点击提前推进；delay == 0 时纯等待点击
        /// </summary>
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
                while (_waitingForAdvance)
                    yield return null;
            }

            _waitingForAdvance = false;
        }

        // ── 工具方法 ──────────────────────────────────────────

        private void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            group.alpha = alpha;
        }

        private float GetCanvasWidth()
        {
            if (_canvasRect != null)
                return _canvasRect.rect.width;
            return Screen.width;
        }

        /// <summary>将分镜移到屏幕外（左侧或右侧）</summary>
        private void MoveOffScreen(RectTransform rect, bool fromLeft, float canvasW)
        {
            if (rect == null) return;
            float x = fromLeft ? -canvasW : canvasW;
            rect.anchoredPosition = new Vector2(x, 0f);
        }

        private void SetPanelActive(RectTransform rect, bool active)
        {
            if (rect != null)
                rect.gameObject.SetActive(active);
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
