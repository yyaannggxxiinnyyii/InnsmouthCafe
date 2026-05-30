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
    /// 小章鱼NPC控制器
    /// 每天开门前执行开场序列，说完台词后通过回调通知外部继续流程。
    /// 首次进入场景时播放入场动画，之后每天复用（已在场景中，不再滑入）。
    ///
    /// 场景结构：
    ///   OctopusNPC (RectTransform + OctopusNPCController)
    ///     ├── OctopusImage (Image, 小章鱼立绘)
    ///     └── DialogueBubble (CanvasGroup)
    ///           └── DialogueText (TextMeshProUGUI)
    /// </summary>
    public class OctopusNPCController : MonoBehaviour
    {
        [Header("入场动画")]
        [SerializeField] [Tooltip("小章鱼的 RectTransform（用于位移动画，留空则取自身）")]
        private RectTransform _npcRect;

        [SerializeField] [Tooltip("入场起始偏移X（正值=从右侧进入，负值=从左侧进入）")]
        private float _enterOffsetX = 400f;

        [SerializeField] [Tooltip("入场动画时长（秒）")]
        private float _enterDuration = 0.6f;

        [SerializeField] [Tooltip("入场动画缓动")]
        private Ease _enterEase = Ease.OutCubic;

        [Header("对话气泡")]
        [SerializeField] [Tooltip("对话气泡根节点的 CanvasGroup")]
        private CanvasGroup _bubbleGroup;

        [SerializeField] [Tooltip("对话文本组件")]
        private TextMeshProUGUI _dialogueText;

        [SerializeField] [Tooltip("气泡淡入淡出时长（秒）")]
        private float _bubbleFadeDuration = 0.25f;

        [SerializeField] [Tooltip("打字机每字间隔（秒）")]
        private float _typeCharDelay = 0.05f;

        [SerializeField] [Tooltip("每句台词打完后停留时长（秒）")]
        private float _lineHoldDuration = 1.5f;

        [Header("教学模式台词")]
        [SerializeField] [Tooltip("教学模式固定台词（按顺序显示）")]
        private string[] _tutorialLines = new string[]
        {
            "来新人了，老板快来教！"
        };

        [Header("普通模式台词模板")]
        [SerializeField] [Tooltip("顾客数量播报模板，{0}=顾客总数")]
        private string _customerCountTemplate = "今天一共有{0}名顾客";

        [SerializeField] [Tooltip("有特殊顾客时追加的台词")]
        private string _specialCustomerLine = "其中貌似有一位很特殊的客人哦";

        [Header("拍扁互动")]
        [SerializeField] [Tooltip("点击触发拍扁效果的按钮")]
        private Button _squishButton;

        [SerializeField] [Tooltip("拍扁时作用的 RectTransform（留空则用 _npcRect）")]
        private RectTransform _squishRect;

        [SerializeField] [Tooltip("拍扁压缩比例（X轴拉伸，Y轴压缩）")]
        private Vector2 _squishScale = new Vector2(1.3f, 0.6f);

        [SerializeField] [Tooltip("压缩动画时长（秒）")]
        private float _squishDuration = 0.08f;

        [SerializeField] [Tooltip("回弹动画时长（秒）")]
        private float _bounceDuration = 0.35f;

        [Header("时序")]
        [SerializeField] [Tooltip("首次入场前的等待时长（秒）")]
        private float _firstEnterDelay = 0.5f;

        [SerializeField] [Tooltip("每日开场前的等待时长（秒）")]
        private float _dailyStartDelay = 0.3f;

        [SerializeField] [Tooltip("所有台词结束后延迟多少秒再回调（秒）")]
        private float _afterDialogueDelay = 0.4f;

        // ── 运行时 ────────────────────────────────────────────

        /// <summary>停留位置（Awake时记录）</summary>
        private Vector2 _restPosition;

        /// <summary>是否已完成首次入场</summary>
        private bool _hasEntered;

        private Coroutine _sequenceCoroutine;

        /// <summary>拍扁动画是否正在播放（防止连点叠加）</summary>
        private bool _isSquishing;

        // ── 生命周期 ──────────────────────────────────────────

        private void Awake()
        {
            if (_npcRect == null)
                _npcRect = GetComponent<RectTransform>();

            _restPosition = _npcRect.anchoredPosition;

            // 初始移到屏幕外等待入场
            _npcRect.anchoredPosition = _restPosition + new Vector2(_enterOffsetX, 0f);

            if (_bubbleGroup != null)
            {
                _bubbleGroup.alpha = 0f;
                _bubbleGroup.interactable = false;
                _bubbleGroup.blocksRaycasts = false;
            }

            if (_dialogueText != null)
                _dialogueText.text = string.Empty;

            if (_squishButton != null)
                _squishButton.onClick.AddListener(OnSquishClicked);

            if (_squishRect == null)
                _squishRect = _npcRect;
        }

        // ── 公开接口 ──────────────────────────────────────────

        /// <summary>
        /// 播放每日开场序列。
        /// 首次调用时执行入场动画，之后直接显示台词。
        /// 台词结束后调用 onComplete。
        /// </summary>
        /// <param name="isTutorial">是否教学模式</param>
        /// <param name="dayConfig">当天顾客配置（普通模式用于生成台词）</param>
        /// <param name="onComplete">台词结束后的回调</param>
        public void PlayDailyOpening(bool isTutorial, DayCustomerConfigSO dayConfig, Action onComplete)
        {
            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = StartCoroutine(DailyOpeningSequence(isTutorial, dayConfig, onComplete));
        }

        // ── 主序列 ────────────────────────────────────────────

        private IEnumerator DailyOpeningSequence(bool isTutorial, DayCustomerConfigSO dayConfig, Action onComplete)
        {
            // 1. 首次入场：滑入动画
            if (!_hasEntered)
            {
                if (_firstEnterDelay > 0f)
                    yield return new WaitForSeconds(_firstEnterDelay);

                yield return SlideIn();
                _hasEntered = true;
            }
            else
            {
                if (_dailyStartDelay > 0f)
                    yield return new WaitForSeconds(_dailyStartDelay);
            }

            // 2. 生成台词列表
            string[] lines = isTutorial
                ? _tutorialLines
                : BuildNormalLines(dayConfig);

            // 3. 依次显示台词（每句开始前清空，避免上一天内容残留）
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                yield return ShowLine(lines[i], clearFirst: true);
            }

            // 4. 气泡淡出
            yield return HideBubble();

            // 5. 教学模式：发布开场完成事件，供引导系统监听
            if (isTutorial)
                TutorialEventBus.Publish(TutorialEvents.OctopusOpeningComplete);

            // 6. 延迟后回调
            if (_afterDialogueDelay > 0f)
                yield return new WaitForSeconds(_afterDialogueDelay);

            _sequenceCoroutine = null;
            onComplete?.Invoke();
        }

        // ── 台词生成 ──────────────────────────────────────────

        private string[] BuildNormalLines(DayCustomerConfigSO dayConfig)
        {
            if (dayConfig == null)
                return Array.Empty<string>();

            int total = dayConfig.fixedCustomers != null && dayConfig.fixedCustomers.Count > 0
                ? dayConfig.fixedCustomers.Count + (dayConfig.extraCustomers?.Count ?? 0)
                : dayConfig.baseCustomerCount + (dayConfig.extraCustomers?.Count ?? 0);

            bool hasSpecial = dayConfig.extraCustomers != null && dayConfig.extraCustomers.Count > 0;

            string countLine = string.Format(_customerCountTemplate, total);

            return hasSpecial
                ? new[] { countLine, _specialCustomerLine }
                : new[] { countLine };
        }

        // ── 动画步骤 ──────────────────────────────────────────

        private IEnumerator SlideIn()
        {
            _npcRect.DOKill();
            _npcRect.DOAnchorPos(_restPosition, _enterDuration).SetEase(_enterEase);
            yield return new WaitForSeconds(_enterDuration);
        }

        /// <summary>显示一句台词（气泡淡入 + 打字机 + 停留）</summary>
        private IEnumerator ShowLine(string line, bool clearFirst = false)
        {
            // 气泡淡入（首句或已隐藏时）
            if (_bubbleGroup != null && _bubbleGroup.alpha < 1f)
            {
                _bubbleGroup.DOKill();
                _bubbleGroup.DOFade(1f, _bubbleFadeDuration);
                yield return new WaitForSeconds(_bubbleFadeDuration);
            }

            if (_dialogueText != null)
            {
                if (clearFirst)
                    _dialogueText.text = string.Empty;

                foreach (char c in line)
                {
                    _dialogueText.text += c;
                    yield return new WaitForSeconds(_typeCharDelay);
                }
            }

            yield return new WaitForSeconds(_lineHoldDuration);
        }

        private IEnumerator HideBubble()
        {
            if (_bubbleGroup == null) yield break;

            _bubbleGroup.DOKill();
            _bubbleGroup.DOFade(0f, _bubbleFadeDuration);
            yield return new WaitForSeconds(_bubbleFadeDuration);

            _bubbleGroup.interactable = false;
            _bubbleGroup.blocksRaycasts = false;

            if (_dialogueText != null)
                _dialogueText.text = string.Empty;
        }

        // ── 拍扁互动 ──────────────────────────────────────────

        private void OnSquishClicked()
        {
            if (_isSquishing) return;
            AudioManager.Instance?.PlaySfx(SoundId.OctopusSquish);
            ActionLogBus.Log("拍拍小章鱼");
            StartCoroutine(SquishSequence());
        }

        private IEnumerator SquishSequence()
        {
            _isSquishing = true;

            // 压缩：快速拍扁
            _squishRect.DOKill(false);
            _squishRect.DOScale(new Vector3(_squishScale.x, _squishScale.y, 1f), _squishDuration)
                       .SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(_squishDuration);

            // 回弹：弹回原始比例，带过冲
            _squishRect.DOScale(Vector3.one, _bounceDuration)
                       .SetEase(Ease.OutElastic);
            yield return new WaitForSeconds(_bounceDuration);

            _isSquishing = false;
        }
    }
}
