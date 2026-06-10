using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 对话气泡UI管理器
    /// 负责控制对话气泡的显示/隐藏和文本内容
    /// 对话气泡预先放置在场景中，通过CanvasGroup控制显示/隐藏
    /// </summary>
    public class DialogueUIManager : Singleton<DialogueUIManager>
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("对话气泡的CanvasGroup组件")]
        private CanvasGroup _dialogueBubbleCanvasGroup;

        [SerializeField]
        [Tooltip("对话文本组件")]
        private TextMeshProUGUI _dialogueText;

        [SerializeField]
        [Tooltip("气泡容器RectTransform（挂有ContentSizeFitter的那个对象）")]
        private RectTransform _bubbleRect;

        [Header("动画配置")]
        [SerializeField]
        [Tooltip("打字机效果速度（字符/秒）")]
        [Range(10, 100)]
        private float _typewriterSpeed = 30f;

        [SerializeField]
        [Tooltip("气泡淡入淡出时长（秒）")]
        [Range(0.1f, 1f)]
        private float _fadeDuration = 0.3f;

        [SerializeField]
        [Tooltip("对话完成后自动隐藏等待时间（秒）")]
        [Range(0.5f, 3f)]
        private float _autoHideDelay = 1f;

        [SerializeField]
        [Tooltip("多段对话时，每句之间的间隔时间（秒）")]
        [Range(0.3f, 2f)]
        private float _dialogueInterval = 0.5f;

        [SerializeField]
        [Tooltip("是否启用打字机效果")]
        private bool _enableTypewriter = true;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        /// <summary>
        /// 对话队列
        /// </summary>
        private Queue<string> _dialogueQueue = new Queue<string>();

        /// <summary>
        /// 当前对话完成回调
        /// </summary>
        private Action _currentOnComplete;

        /// <summary>
        /// 是否正在显示对话
        /// </summary>
        private bool _isShowingDialogue = false;

        /// <summary>
        /// 是否正在播放打字机效果
        /// </summary>
        private bool _isTyping = false;

        /// <summary>
        /// 当前显示的完整文本（用于跳过打字机效果）
        /// </summary>
        private string _currentFullText = "";

        /// <summary>
        /// 打字机协程引用
        /// </summary>
        private Coroutine _typewriterCoroutine;

        /// <summary>
        /// 自动隐藏协程引用
        /// </summary>
        private Coroutine _autoHideCoroutine;

        /// <summary>
        /// 自动继续协程引用
        /// </summary>
        private Coroutine _autoContinueCoroutine;

        /// <summary>
        /// 是否可以点击继续
        /// </summary>
        private bool _canClickToContinue = false;

        /// <summary>
        /// 对话开始事件
        /// </summary>
        public event Action OnDialogueStart;

        /// <summary>
        /// 单行对话显示完成事件（打字机效果结束）
        /// </summary>
        public event Action<string> OnDialogueLineComplete;

        /// <summary>
        /// 所有对话完成事件
        /// </summary>
        public event Action OnDialogueComplete;

        /// <summary>
        /// 对话被跳过事件
        /// </summary>
        public event Action OnDialogueSkipped;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                return;
            }

            // 初始化时隐藏对话气泡
            if (_dialogueBubbleCanvasGroup != null)
            {
                _dialogueBubbleCanvasGroup.alpha = 0f;
                _dialogueBubbleCanvasGroup.interactable = false;
                _dialogueBubbleCanvasGroup.blocksRaycasts = false;
            }

            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 对话UI管理器初始化完成");
            }
        }

        private void Update()
        {
            // 检测全屏点击
            if (_isShowingDialogue && Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        /// <summary>
        /// 显示单句对话
        /// </summary>
        /// <param name="dialogue">对话文本</param>
        /// <param name="onComplete">对话完成回调</param>
        public void ShowDialogue(string dialogue, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(dialogue))
            {
                Debug.LogWarning("[DialogueUI] 对话文本为空");
                onComplete?.Invoke();
                return;
            }

            // 清空队列，显示新对话
            _dialogueQueue.Clear();
            _dialogueQueue.Enqueue(dialogue);
            _currentOnComplete = onComplete;

            StartDialogueSequence();
        }

        /// <summary>
        /// 显示多段对话序列
        /// </summary>
        /// <param name="dialogues">对话文本列表</param>
        /// <param name="onComplete">所有对话完成回调</param>
        public void ShowDialogueSequence(List<string> dialogues, Action onComplete = null)
        {
            if (dialogues == null || dialogues.Count == 0)
            {
                Debug.LogWarning("[DialogueUI] 对话列表为空");
                onComplete?.Invoke();
                return;
            }

            // 清空队列，添加新对话
            _dialogueQueue.Clear();
            foreach (var dialogue in dialogues)
            {
                if (!string.IsNullOrEmpty(dialogue))
                {
                    _dialogueQueue.Enqueue(dialogue);
                }
            }

            _currentOnComplete = onComplete;

            StartDialogueSequence();
        }

        /// <summary>
        /// 立即隐藏对话气泡
        /// </summary>
        public void HideDialogue()
        {
            StopDialogueRoutine();

            // 清空队列
            _dialogueQueue.Clear();

            // 隐藏气泡
            HideBubble();

            // 重置状态
            _isShowingDialogue = false;
            _isTyping = false;
            _canClickToContinue = false;

            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 对话已隐藏");
            }
        }

        /// <summary>
        /// 强制打断并立即清空当前对话
        /// </summary>
        public void ForceClearDialogue()
        {
            StopDialogueRoutine();

            _dialogueQueue.Clear();
            _currentOnComplete = null;
            _currentFullText = string.Empty;
            _isShowingDialogue = false;
            _isTyping = false;
            _canClickToContinue = false;

            if (_dialogueBubbleCanvasGroup != null)
            {
                _dialogueBubbleCanvasGroup.DOKill();
                _dialogueBubbleCanvasGroup.alpha = 0f;
                _dialogueBubbleCanvasGroup.interactable = false;
                _dialogueBubbleCanvasGroup.blocksRaycasts = false;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
            }

            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 对话已强制清空");
            }
        }

        /// <summary>
        /// 停止所有对话相关协程和回调状态
        /// </summary>
        private void StopDialogueRoutine()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            if (_autoContinueCoroutine != null)
            {
                StopCoroutine(_autoContinueCoroutine);
                _autoContinueCoroutine = null;
            }
        }

        /// <summary>
        /// 开始对话序列
        /// </summary>
        private void StartDialogueSequence()
        {
            if (_dialogueQueue.Count == 0)
            {
                return;
            }

            _isShowingDialogue = true;
            OnDialogueStart?.Invoke();

            // 显示气泡
            ShowBubble(() =>
            {
                // 气泡显示完成后，开始显示第一句对话
                ShowNextDialogue();
            });

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 开始对话序列，共 {_dialogueQueue.Count} 句");
            }
        }

        /// <summary>
        /// 显示下一句对话
        /// </summary>
        private void ShowNextDialogue()
        {
            if (_dialogueQueue.Count == 0)
            {
                // 所有对话完成，启动自动隐藏
                StartAutoHide();
                return;
            }

            string dialogue = _dialogueQueue.Dequeue();
            _currentFullText = dialogue;
            _canClickToContinue = false;

            // 停止之前的自动隐藏协程（如果有）
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }

            // 停止之前的自动继续协程（如果有）
            if (_autoContinueCoroutine != null)
            {
                StopCoroutine(_autoContinueCoroutine);
                _autoContinueCoroutine = null;
            }

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 显示对话: {dialogue}");
            }

            // 开始打字机效果
            if (_enableTypewriter)
            {
                if (_typewriterCoroutine != null)
                {
                    StopCoroutine(_typewriterCoroutine);
                }
                _typewriterCoroutine = StartCoroutine(TypewriterEffect(dialogue));
            }
            else
            {
                // 直接显示完整文本
                _dialogueText.text = dialogue;
                _isTyping = false;
                _canClickToContinue = true;
                OnDialogueLineComplete?.Invoke(dialogue);

                // 判断是否还有下一句
                if (_dialogueQueue.Count > 0)
                {
                    // 还有下一句，启动自动继续
                    StartAutoContinue();
                }
                else
                {
                    // 最后一句，启动自动隐藏
                    StartAutoHide();
                }
            }
        }

        /// <summary>
        /// 打字机效果协程
        /// </summary>
        private IEnumerator TypewriterEffect(string fullText)
        {
            _isTyping = true;
            _dialogueText.text = "";

            float delay = 1f / _typewriterSpeed;

            for (int i = 0; i < fullText.Length; i++)
            {
                _dialogueText.text += fullText[i];
                yield return new WaitForSeconds(delay);
            }

            // 打字机效果完成
            _isTyping = false;
            _canClickToContinue = true;
            _typewriterCoroutine = null;

            OnDialogueLineComplete?.Invoke(fullText);

            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 打字机效果完成");
            }

            // 判断是否还有下一句
            if (_dialogueQueue.Count > 0)
            {
                // 还有下一句，启动自动继续
                StartAutoContinue();
            }
            // 最后一句：等待玩家点击，不自动隐藏
        }

        /// <summary>
        /// 处理点击事件
        /// </summary>
        private void HandleClick()
        {
            if (_isTyping)
            {
                // 打字机播放中：跳过打字机效果
                SkipTypewriter();
            }
            else if (_canClickToContinue)
            {
                // 打字机完成后：点击可以加速
                if (_dialogueQueue.Count > 0)
                {
                    // 还有下一句：立即显示下一句（跳过间隔等待）
                    if (_autoContinueCoroutine != null)
                    {
                        StopCoroutine(_autoContinueCoroutine);
                        _autoContinueCoroutine = null;
                    }
                    ShowNextDialogue();
                }
                else
                {
                    // 最后一句：立即隐藏（跳过自动隐藏等待）
                    if (_autoHideCoroutine != null)
                    {
                        StopCoroutine(_autoHideCoroutine);
                        _autoHideCoroutine = null;
                    }
                    CompleteDialogue();
                }
            }
        }

        /// <summary>
        /// 跳过打字机效果
        /// </summary>
        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            // 立即显示完整文本
            _dialogueText.text = _currentFullText;
            _isTyping = false;
            _canClickToContinue = true;

            // 强制刷新布局，确保 ContentSizeFitter 立即更新气泡大小
            if (_bubbleRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_bubbleRect);

            OnDialogueSkipped?.Invoke();
            OnDialogueLineComplete?.Invoke(_currentFullText);

            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 跳过打字机效果");
            }
        }

        /// <summary>
        /// 启动自动继续（多段对话时）
        /// </summary>
        private void StartAutoContinue()
        {
            if (_autoContinueCoroutine != null)
            {
                StopCoroutine(_autoContinueCoroutine);
            }

            _autoContinueCoroutine = StartCoroutine(AutoContinueCoroutine());

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 启动自动继续，{_dialogueInterval}秒后显示下一句");
            }
        }

        /// <summary>
        /// 自动继续协程
        /// </summary>
        private IEnumerator AutoContinueCoroutine()
        {
            yield return new WaitForSeconds(_dialogueInterval);

            _autoContinueCoroutine = null;
            ShowNextDialogue();
        }

        /// <summary>
        /// 启动自动隐藏
        /// </summary>
        private void StartAutoHide()
        {
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
            }

            _autoHideCoroutine = StartCoroutine(AutoHideCoroutine());

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 启动自动隐藏，{_autoHideDelay}秒后隐藏");
            }
        }

        /// <summary>
        /// 自动隐藏协程
        /// </summary>
        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSeconds(_autoHideDelay);

            _autoHideCoroutine = null;
            CompleteDialogue();
        }

        /// <summary>
        /// 完成所有对话
        /// </summary>
        private void CompleteDialogue()
        {
            if (_showDebugLog)
            {
                Debug.Log("[DialogueUI] 所有对话完成");
            }

            // 隐藏气泡
            HideBubble(() =>
            {
                // 先缓存并清空回调，避免回调中再次 ShowDialogue 时被后续代码覆盖
                Action onComplete = _currentOnComplete;
                _currentOnComplete = null;

                // 气泡隐藏完成后，触发回调
                _isShowingDialogue = false;
                OnDialogueComplete?.Invoke();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 显示气泡（淡入）
        /// </summary>
        private void ShowBubble(Action onComplete = null)
        {
            if (_dialogueBubbleCanvasGroup == null)
            {
                Debug.LogError("[DialogueUI] CanvasGroup引用为空，请在Inspector中设置");
                onComplete?.Invoke();
                return;
            }

            _dialogueBubbleCanvasGroup.interactable = true;
            _dialogueBubbleCanvasGroup.blocksRaycasts = true;

            _dialogueBubbleCanvasGroup.DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 隐藏气泡（淡出）
        /// </summary>
        private void HideBubble(Action onComplete = null)
        {
            if (_dialogueBubbleCanvasGroup == null)
            {
                Debug.LogError("[DialogueUI] CanvasGroup引用为空，请在Inspector中设置");
                onComplete?.Invoke();
                return;
            }

            _dialogueBubbleCanvasGroup.DOFade(0f, _fadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    _dialogueBubbleCanvasGroup.interactable = false;
                    _dialogueBubbleCanvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// 设置打字机速度
        /// </summary>
        public void SetTypewriterSpeed(float speed)
        {
            _typewriterSpeed = Mathf.Clamp(speed, 10f, 100f);

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 打字机速度设置为: {_typewriterSpeed} 字符/秒");
            }
        }

        /// <summary>
        /// 设置是否启用打字机效果
        /// </summary>
        public void SetTypewriterEnabled(bool enabled)
        {
            _enableTypewriter = enabled;

            if (_showDebugLog)
            {
                Debug.Log($"[DialogueUI] 打字机效果: {(enabled ? "启用" : "禁用")}");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器调试方法：测试单句对话
        /// </summary>
        [ContextMenu("测试：显示单句对话")]
        private void DebugShowSingleDialogue()
        {
            ShowDialogue("这是一句测试对话，用于验证对话系统是否正常工作。", () =>
            {
                Debug.Log("[DialogueUI] 测试对话完成");
            });
        }

        /// <summary>
        /// 编辑器调试方法：测试多段对话
        /// </summary>
        [ContextMenu("测试：显示多段对话")]
        private void DebugShowMultipleDialogues()
        {
            var dialogues = new List<string>
            {
                "欢迎光临！",
                "我想要一杯拿铁咖啡。",
                "谢谢，期待您的作品！"
            };

            ShowDialogueSequence(dialogues, () =>
            {
                Debug.Log("[DialogueUI] 测试多段对话完成");
            });
        }

        /// <summary>
        /// 编辑器调试方法：立即隐藏对话
        /// </summary>
        [ContextMenu("测试：隐藏对话")]
        private void DebugHideDialogue()
        {
            HideDialogue();
        }
#endif
    }
}
