using System;
using System.Collections;
using System.Collections.Generic;
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

        [SerializeField] [Tooltip("对话气泡 RectTransform；为空时自动从 CanvasGroup 所在节点获取")]
        private RectTransform _bubbleRect;

        [Header("气泡自适应")]
        [SerializeField] [Tooltip("气泡宽度无效时用于计算换行的兜底文本宽度；正常情况下使用气泡当前宽度")]
        private float _maxTextWidth = 420f;

        [SerializeField] [Tooltip("气泡相对文本内容的额外留白，X=左右总留白，Y=上下总留白")]
        private Vector2 _bubblePadding = new Vector2(72f, 48f);

        [SerializeField] [Tooltip("气泡最小尺寸，避免短句气泡过小")]
        private Vector2 _minBubbleSize = new Vector2(180f, 82f);

        [SerializeField] [Tooltip("气泡图片中不放置文本的小尾巴额外高度；只增加气泡高度，不增加文本区域高度")]
        private float _bubbleTailExtraHeight = 0f;

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

        [SerializeField] [Tooltip("有新材料到店时追加的台词，{0}=新小料数量，{1}=新辅助液数量")]
        private string _newIngredientLineTemplate = "今天有{0}种新的小料和{1}种新的辅助液到店了";

        [Header("拍击随机事件")]
        [SerializeField] [Tooltip("随机台词列表（每次拍击从中随机选一条）")]
        private string[] _squishRandomLines = new string[]
        {
            "呜呜呜……",
            "别拍我啦！",
            "再拍我生气了！",
        };

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

            CacheBubbleRect();

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
            {
                _dialogueText.text = string.Empty;
                _dialogueText.maxVisibleCharacters = int.MaxValue;
            }

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
        /// <param name="totalCustomerCount">当天实际顾客数量</param>
        /// <param name="hasSpecialCustomerToday">当天队列中是否存在特殊顾客</param>
        /// <param name="newToppingCount">当天新到店的小料数量</param>
        /// <param name="newLiquidCount">当天新到店的辅助液数量</param>
        /// <param name="onComplete">台词结束后的回调</param>
        public void PlayDailyOpening(
            bool isTutorial,
            DayCustomerConfigSO dayConfig,
            int totalCustomerCount,
            bool hasSpecialCustomerToday,
            int newToppingCount,
            int newLiquidCount,
            Action onComplete)
        {
            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = StartCoroutine(DailyOpeningSequence(
                isTutorial,
                dayConfig,
                totalCustomerCount,
                hasSpecialCustomerToday,
                newToppingCount,
                newLiquidCount,
                onComplete));
        }

        /// <summary>
        /// 播放特殊顾客入场播报，说完后调用 onComplete 继续顾客流程。
        /// </summary>
        public void PlaySpecialCustomerAnnouncement(IReadOnlyList<string> lines, Action onComplete)
        {
            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = StartCoroutine(SpecialCustomerAnnouncementSequence(lines, onComplete));
        }

        // ── 主序列 ────────────────────────────────────────────

        /// <summary>
        /// 特殊顾客入场播报序列。
        /// </summary>
        private IEnumerator SpecialCustomerAnnouncementSequence(IReadOnlyList<string> lines, Action onComplete)
        {
            if (lines == null || lines.Count == 0)
            {
                _sequenceCoroutine = null;
                onComplete?.Invoke();
                yield break;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                {
                    continue;
                }

                yield return ShowLine(lines[i], clearFirst: true);
            }

            yield return HideBubble();

            _sequenceCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator DailyOpeningSequence(
            bool isTutorial,
            DayCustomerConfigSO dayConfig,
            int totalCustomerCount,
            bool hasSpecialCustomerToday,
            int newToppingCount,
            int newLiquidCount,
            Action onComplete)
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
                ? BuildTutorialLines(newToppingCount, newLiquidCount)
                : BuildNormalLines(dayConfig, totalCustomerCount, hasSpecialCustomerToday, newToppingCount, newLiquidCount);

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

        /// <summary>
        /// 构建教学模式开场台词；有新材料时在固定台词后追加新货到店播报。
        /// </summary>
        private string[] BuildTutorialLines(int newToppingCount, int newLiquidCount)
        {
            List<string> lines = new List<string>();
            if (_tutorialLines != null)
            {
                lines.AddRange(_tutorialLines);
            }

            AddNewIngredientLine(lines, newToppingCount, newLiquidCount);
            return lines.ToArray();
        }

        /// <summary>
        /// 构建普通模式开场台词，顺序为顾客数量、特殊顾客、新货到店。
        /// </summary>
        private string[] BuildNormalLines(
            DayCustomerConfigSO dayConfig,
            int totalCustomerCount,
            bool hasSpecialCustomerToday,
            int newToppingCount,
            int newLiquidCount)
        {
            if (dayConfig == null)
                return Array.Empty<string>();

            List<string> lines = new List<string>
            {
                string.Format(_customerCountTemplate, totalCustomerCount)
            };

            if (hasSpecialCustomerToday)
            {
                lines.Add(_specialCustomerLine);
            }

            AddNewIngredientLine(lines, newToppingCount, newLiquidCount);
            return lines.ToArray();
        }

        /// <summary>
        /// 有新材料到店时追加播报台词。
        /// </summary>
        private void AddNewIngredientLine(List<string> lines, int newToppingCount, int newLiquidCount)
        {
            if (lines == null || newToppingCount + newLiquidCount <= 0)
            {
                return;
            }

            lines.Add(string.Format(_newIngredientLineTemplate, newToppingCount, newLiquidCount));
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
            PrepareBubbleLayout(line, 0);

            // 气泡淡入（首句或已隐藏时）
            if (_bubbleGroup != null && _bubbleGroup.alpha < 1f)
            {
                _bubbleGroup.DOKill();
                _bubbleGroup.DOFade(1f, _bubbleFadeDuration);
                yield return new WaitForSeconds(_bubbleFadeDuration);
            }

            if (_dialogueText != null)
            {
                yield return PlayTypewriter();
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
            {
                _dialogueText.text = string.Empty;
                _dialogueText.maxVisibleCharacters = int.MaxValue;
            }
        }

        /// <summary>
        /// 缓存气泡 RectTransform，未手动绑定时从 CanvasGroup 所在节点获取。
        /// </summary>
        private void CacheBubbleRect()
        {
            if (_bubbleRect != null || _bubbleGroup == null)
            {
                return;
            }

            _bubbleRect = _bubbleGroup.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 根据完整台词提前计算气泡尺寸，避免打字机播放过程中触发布局抖动。
        /// </summary>
        /// <param name="line">需要显示的完整台词。</param>
        /// <param name="visibleCharacters">初始可见字符数。</param>
        private void PrepareBubbleLayout(string line, int visibleCharacters)
        {
            if (_dialogueText == null)
            {
                return;
            }

            CacheBubbleRect();

            string safeLine = line ?? string.Empty;
            _dialogueText.enableWordWrapping = true;
            _dialogueText.maxVisibleCharacters = int.MaxValue;
            _dialogueText.text = safeLine;

            ResizeBubbleToText(safeLine);

            _dialogueText.maxVisibleCharacters = visibleCharacters;
            _dialogueText.ForceMeshUpdate();
        }

        /// <summary>
        /// 按当前气泡宽度计算完整台词的换行高度，只调整高度，不覆盖场景中设置的气泡宽度。
        /// </summary>
        /// <param name="line">需要计算尺寸的完整台词。</param>
        private void ResizeBubbleToText(string line)
        {
            if (_bubbleRect == null || _dialogueText == null)
            {
                return;
            }

            float maxTextWidth = Mathf.Max(1f, _maxTextWidth);
            float bubbleWidth = Mathf.Max(_bubbleRect.rect.width, _bubbleRect.sizeDelta.x);
            if (bubbleWidth <= _bubblePadding.x + 1f)
            {
                bubbleWidth = Mathf.Max(_minBubbleSize.x, maxTextWidth + _bubblePadding.x);
            }

            float textWidth = Mathf.Max(1f, bubbleWidth - _bubblePadding.x);
            float minTextHeight = Mathf.Max(1f, _minBubbleSize.y - _bubblePadding.y);

            Vector2 preferredWrap = _dialogueText.GetPreferredValues(
                line,
                textWidth,
                Mathf.Infinity);

            float textHeight = Mathf.Max(preferredWrap.y, minTextHeight);
            float bodyHeight = Mathf.Max(_minBubbleSize.y, textHeight + _bubblePadding.y);
            float bubbleHeight = bodyHeight + Mathf.Max(0f, _bubbleTailExtraHeight);

            _bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bubbleHeight);

            RectTransform textRect = _dialogueText.rectTransform;
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
            textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
        }

        /// <summary>
        /// 推进当前完整文本的可见字符数，实现不改变文本内容的打字机效果。
        /// </summary>
        private IEnumerator PlayTypewriter()
        {
            if (_dialogueText == null)
            {
                yield break;
            }

            int characterCount = GetCurrentTextCharacterCount();
            for (int visibleCount = 1; visibleCount <= characterCount; visibleCount++)
            {
                _dialogueText.maxVisibleCharacters = visibleCount;
                yield return new WaitForSeconds(_typeCharDelay);
            }
        }

        /// <summary>
        /// 获取当前文本的 TMP 字符数量，确保 maxVisibleCharacters 按最终排版推进。
        /// </summary>
        private int GetCurrentTextCharacterCount()
        {
            if (_dialogueText == null)
            {
                return 0;
            }

            _dialogueText.ForceMeshUpdate();
            return _dialogueText.textInfo.characterCount;
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

            // 拍击结束后触发随机事件（各自独立判定）
            TriggerSquishEvents();
        }

        /// <summary>
        /// 拍击随机事件：先判断是否触发，触发后按权重选一个执行
        /// </summary>
        private void TriggerSquishEvents()
        {
            // 先判断这次拍击是否触发任何事件
            OctopusBalanceSettings balance = GameplayBalanceManager.Instance.Config.Octopus;
            if (UnityEngine.Random.value >= balance.squishEventChance) return;

            // 按权重随机选一个事件
            float totalWeight = balance.lineWeight + balance.sanityGainWeight + balance.sanityLossWeight;
            float roll = UnityEngine.Random.value * totalWeight;

            if (roll < balance.lineWeight)
            {
                // 随机台词
                if (_squishRandomLines != null && _squishRandomLines.Length > 0)
                {
                    string line = _squishRandomLines[UnityEngine.Random.Range(0, _squishRandomLines.Length)];
                    if (!string.IsNullOrEmpty(line))
                        StartCoroutine(ShowSquishLine(line));
                }
            }
            else if (roll < balance.lineWeight + balance.sanityGainWeight)
            {
                // 理智增加
                SanityManager.Instance?.AddSanity(balance.sanityGainAmount, "拍拍小章鱼");
                ActionLogBus.Log($"小章鱼很开心，理智 +{balance.sanityGainAmount}", Color.green);
            }
            else
            {
                // 理智减少
                SanityManager.Instance?.ReduceSanity(balance.sanityLossAmount, "拍拍小章鱼");
                ActionLogBus.Log($"小章鱼不高兴了，理智 -{balance.sanityLossAmount}", Color.red);
            }
        }

        /// <summary>
        /// 显示拍击触发的随机台词（不影响开场序列）
        /// </summary>
        private IEnumerator ShowSquishLine(string line)
        {
            if (_bubbleGroup == null || _dialogueText == null) yield break;

            // 如果开场序列正在运行则不打断
            if (_sequenceCoroutine != null) yield break;

            _bubbleGroup.DOKill();
            _bubbleGroup.alpha = 0f;
            _bubbleGroup.blocksRaycasts = false;
            PrepareBubbleLayout(line, 0);

            _bubbleGroup.DOFade(1f, _bubbleFadeDuration);
            yield return new WaitForSeconds(_bubbleFadeDuration);

            yield return PlayTypewriter();

            yield return new WaitForSeconds(_lineHoldDuration);

            _bubbleGroup.DOFade(0f, _bubbleFadeDuration);
            yield return new WaitForSeconds(_bubbleFadeDuration);

            _dialogueText.text = string.Empty;
            _dialogueText.maxVisibleCharacters = int.MaxValue;
        }
    }
}
