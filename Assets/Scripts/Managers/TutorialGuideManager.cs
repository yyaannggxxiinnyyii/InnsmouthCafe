using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 教学引导管理器
    /// 负责在教学模式中触发 TutorialMark 引导、显示提示、驱动引导序列
    /// </summary>
    public class TutorialGuideManager : Singleton<TutorialGuideManager>
    {
        [Header("引导UI引用")]
        [SerializeField] [Tooltip("引导UI组件")]
        private UI.TutorialGuideUI _guideUI;

        [Header("设置")]
        [SerializeField] [Tooltip("Mark序列中步骤间最小间隔（秒）")]
        private float _stepInterval = 0.3f;

        /// <summary>引导是否正在进行</summary>
        public bool IsGuideActive { get; private set; }

        /// <summary>当前Mark序列</summary>
        private UI.TutorialMark[] _currentMarks;

        /// <summary>当前Mark序列索引</summary>
        private int _currentMarkIndex;

        /// <summary>序列完成回调</summary>
        private Action _onSequenceComplete;

        /// <summary>自动推进协程</summary>
        private Coroutine _autoAdvanceCoroutine;

        /// <summary>用于支持暂停计数的计数器</summary>
        private int _pauseCount = 0;

        /// <summary>暂停前保存的 timeScale</summary>
        private float _prevTimeScale = 1f;

        /// <summary>单个Mark完成事件</summary>
        public event Action<UI.TutorialMark> OnMarkCompleted;

        /// <summary>序列完成事件</summary>
        public event Action OnSequenceCompleted;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            if (_guideUI == null)
                _guideUI = FindObjectOfType<UI.TutorialGuideUI>();
        }

        // ── TutorialMark 驱动接口（主要使用方式）─────────────

        /// <summary>
        /// 触发单个 TutorialMark 引导
        /// 高亮Mark所在区域，TipPanel显示在旁边
        /// </summary>
        /// <param name="mark">场景中的TutorialMark组件</param>
        /// <param name="onComplete">引导完成回调</param>
        public void TriggerMark(UI.TutorialMark mark, Action onComplete = null)
        {
            if (_guideUI == null || mark == null) return;

            PauseGameForGuide();
            IsGuideActive = true;

            _guideUI.ShowMark(mark, () =>
            {
                // 完成时恢复游戏时间并触发回调
                ResumeGameForGuide();
                IsGuideActive = false;
                OnMarkCompleted?.Invoke(mark);
                onComplete?.Invoke();
            });

            // 自动推进（使用实时等待，暂停游戏时也能自动推进）
            if (mark.AutoAdvanceDelay > 0f)
            {
                if (_autoAdvanceCoroutine != null)
                {
                    StopCoroutine(_autoAdvanceCoroutine);
                    _autoAdvanceCoroutine = null;
                }
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(mark.AutoAdvanceDelay));
            }
        }

        /// <summary>
        /// 按顺序触发一组 TutorialMark 引导序列
        /// </summary>
        /// <param name="marks">有序Mark数组</param>
        /// <param name="onAllComplete">全部完成回调</param>
        public void TriggerMarkSequence(UI.TutorialMark[] marks, Action onAllComplete = null)
        {
            if (marks == null || marks.Length == 0)
            {
                onAllComplete?.Invoke();
                return;
            }

            _currentMarks = marks;
            _currentMarkIndex = 0;
            _onSequenceComplete = onAllComplete;
            PauseGameForGuide();
            IsGuideActive = true;

            TriggerCurrentMark();
        }

        /// <summary>
        /// 按顺序触发 List 形式的 Mark 序列
        /// </summary>
        public void TriggerMarkSequence(List<UI.TutorialMark> marks, Action onAllComplete = null)
        {
            TriggerMarkSequence(marks?.ToArray(), onAllComplete);
        }

        // ── 简易接口 ─────────────────────────────────────────

        /// <summary>
        /// 显示纯文本提示（不高亮，点击或超时消失）
        /// </summary>
        public void ShowTip(string text, float duration = 0f)
        {
            if (_guideUI == null) return;

            PauseGameForGuide();
            IsGuideActive = true;
            _guideUI.ShowTip(text);

            if (duration > 0f)
            {
                if (_autoAdvanceCoroutine != null)
                {
                    StopCoroutine(_autoAdvanceCoroutine);
                    _autoAdvanceCoroutine = null;
                }
                _autoAdvanceCoroutine = StartCoroutine(AutoHideTip(duration));
            }
        }

        /// <summary>
        /// 隐藏当前提示
        /// </summary>
        public void HideTip()
        {
            if (_guideUI == null) return;

            _guideUI.HideTip();
            IsGuideActive = false;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            ResumeGameForGuide();
        }

        /// <summary>
        /// 强制跳过当前引导
        /// </summary>
        public void SkipCurrent()
        {
            if (!IsGuideActive) return;

            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }

            _guideUI?.HideAll();

            // 如果在序列中，推进到下一步
            if (_currentMarks != null)
            {
                _currentMarkIndex++;
                StartCoroutine(DelayedTriggerNext());
            }
            else
            {
                IsGuideActive = false;
                ResumeGameForGuide();
            }
        }

        // ── 内部逻辑 ─────────────────────────────────────────

        private void TriggerCurrentMark()
        {
            if (_currentMarks == null || _currentMarkIndex >= _currentMarks.Length)
            {
                // 序列完成
                IsGuideActive = false;
                _currentMarks = null;
                OnSequenceCompleted?.Invoke();
                _onSequenceComplete?.Invoke();
                ResumeGameForGuide();
                return;
            }

            var mark = _currentMarks[_currentMarkIndex];

            // 跳过null或已销毁的Mark
            if (mark == null)
            {
                _currentMarkIndex++;
                TriggerCurrentMark();
                return;
            }

            TriggerMark(mark, () =>
            {
                _currentMarkIndex++;
                StartCoroutine(DelayedTriggerNext());
            });
        }

        private IEnumerator DelayedTriggerNext()
        {
            yield return new WaitForSecondsRealtime(_stepInterval);
            TriggerCurrentMark();
        }

        private IEnumerator AutoHideTip(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _autoAdvanceCoroutine = null;
            HideTip();
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _autoAdvanceCoroutine = null;
            _guideUI?.CompleteCurrentStep();
        }

        /// <summary>
        /// 暂停游戏时间以显示引导（支持嵌套计数）
        /// </summary>
        private void PauseGameForGuide()
        {
            if (_pauseCount == 0)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            _pauseCount++;
        }

        /// <summary>
        /// 恢复游戏时间（仅在所有暂停请求都解除后恢复）
        /// </summary>
        private void ResumeGameForGuide()
        {
            _pauseCount = Mathf.Max(0, _pauseCount - 1);
            if (_pauseCount == 0)
            {
                Time.timeScale = _prevTimeScale;
            }
        }
    }
}
