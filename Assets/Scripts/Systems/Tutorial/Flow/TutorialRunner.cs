using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教学流程运行器，负责按顺序展示步骤、拦截输入并等待业务事件推进。
/// </summary>
[DisallowMultipleComponent]
public class TutorialRunner : Singleton<TutorialRunner>
{
    [Header("组件引用")]
    [SerializeField]
    [Tooltip("教学引导 UI")]
    private TutorialGuideUI _guideUI;

    [SerializeField]
    [Tooltip("教学目标注册表")]
    private TutorialTargetRegistry _registry;

    /// <summary>当前运行的教学协程。</summary>
    private Coroutine _runCoroutine;

    /// <summary>当前步骤是否已经满足推进条件。</summary>
    private bool _stepAdvanced;

    /// <summary>是否正在等待业务事件。</summary>
    private bool _waitingForEvent;

    /// <summary>当前等待的事件 Key。</summary>
    private string _expectedEventKey;

    /// <summary>是否正在播放教学流程。</summary>
    public bool IsRunning => _runCoroutine != null;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        CacheReferences();
    }

    /// <summary>
    /// 播放咖啡制作主流程教学，可用于自动触发或调试重播。
    /// </summary>
    public void PlayCoffeeTutorial(bool forceRepeat = false)
    {
        Play(CoffeeTutorialFlow.Build(), forceRepeat);
    }

    /// <summary>
    /// 播放指定教学步骤列表；已有流程运行时会先取消旧流程。
    /// </summary>
    public void Play(IReadOnlyList<TutorialStep> steps, bool forceRepeat = false)
    {
        if (steps == null || steps.Count == 0)
            return;

        if (IsRunning)
            Cancel();

        _runCoroutine = StartCoroutine(Run(steps));
    }

    /// <summary>
    /// 取消当前教学流程，并恢复 UI 状态。
    /// </summary>
    public void Cancel()
    {
        if (_runCoroutine != null)
        {
            StopCoroutine(_runCoroutine);
            _runCoroutine = null;
        }

        UnsubscribeExpectedEvent();
        _guideUI?.HideAll(true);
    }

    /// <summary>
    /// 缓存运行所需组件，避免步骤运行时频繁查找。
    /// </summary>
    private void CacheReferences()
    {
        if (_guideUI == null)
            _guideUI = FindObjectOfType<TutorialGuideUI>(true);

        if (_registry == null)
            _registry = TutorialTargetRegistry.Instance;
    }

    /// <summary>
    /// 顺序执行步骤列表，全部完成后记录教学完成状态。
    /// </summary>
    private IEnumerator Run(IReadOnlyList<TutorialStep> steps)
    {
        for (int i = 0; i < steps.Count; i++)
            yield return RunStep(steps[i]);

        GameManager.Instance?.MarkTutorialCompleted();
        _runCoroutine = null;
        _guideUI?.HideAll(true);
    }

    /// <summary>
    /// 执行单个教学步骤，并等待对应点击或业务事件满足。
    /// </summary>
    private IEnumerator RunStep(TutorialStep step)
    {
        if (step == null)
            yield break;

        _stepAdvanced = false;
        switch (step.Kind)
        {
            case TutorialStepKind.Say:
                ShowStep(step.Text, null, 0f, TutorialGuideInteraction.AnyClick);
                break;
            case TutorialStepKind.Highlight:
                if (!TryResolve(step.TargetId, out TutorialTarget highlightTarget))
                    yield break;
                ShowStep(step.Text, highlightTarget.HighlightRect, highlightTarget.Padding, TutorialGuideInteraction.AnyClick);
                break;
            case TutorialStepKind.RequireAreaClick:
                if (!TryResolve(step.TargetId, out TutorialTarget areaTarget))
                    yield break;
                ShowStep(step.Text, areaTarget.HighlightRect, areaTarget.Padding, TutorialGuideInteraction.AreaClick);
                break;
            case TutorialStepKind.RequireClick:
                if (!TryResolve(step.TargetId, out TutorialTarget clickTarget))
                    yield break;
                RunRequireClickStep(step, clickTarget);
                break;
            case TutorialStepKind.WaitForEvent:
                SubscribeExpectedEvent(step.EventKey);
                break;
            case TutorialStepKind.FreePlayUntilEvent:
                _guideUI?.HideAll();
                SubscribeExpectedEvent(step.EventKey);
                break;
            case TutorialStepKind.ReleaseGate:
                TutorialGate.Release(step.GateKey);
                AdvanceStep();
                break;
        }

        while (!_stepAdvanced)
            yield return null;

        UnsubscribeExpectedEvent();
    }

    /// <summary>
    /// 展示强制点击步骤；如果配置了事件 Key，则等待真实业务事件确认。
    /// </summary>
    private void RunRequireClickStep(TutorialStep step, TutorialTarget target)
    {
        if (string.IsNullOrEmpty(step.EventKey))
        {
            Debug.LogError($"[TutorialRunner] RequireClick must wait for a business event. Target: {step.TargetId}");
            AdvanceStep();
            return;
        }

        SubscribeExpectedEvent(step.EventKey);

        ShowStep(
            step.Text,
            target.HighlightRect,
            target.Padding,
            TutorialGuideInteraction.TargetPassThrough);
    }

    /// <summary>
    /// 调用教学 UI 展示步骤；UI 缺失时记录错误并让流程安全推进。
    /// </summary>
    private void ShowStep(
        string text,
        RectTransform rect,
        float padding,
        TutorialGuideInteraction interaction,
        Action areaClick = null)
    {
        if (_guideUI == null)
        {
            Debug.LogError("[TutorialRunner] TutorialGuideUI not found.");
            _stepAdvanced = true;
            return;
        }

        _guideUI.ShowStep(text, rect, padding, interaction, AdvanceStep, areaClick ?? AdvanceStep);
    }

    /// <summary>
    /// 根据目标 ID 获取场景目标，缺失时输出明确错误并结束当前步骤。
    /// </summary>
    private bool TryResolve(string targetId, out TutorialTarget target)
    {
        if (_registry == null)
            _registry = TutorialTargetRegistry.Instance;

        if (_registry != null && _registry.TryGet(targetId, out target))
            return true;

        target = null;
        Debug.LogError($"[TutorialRunner] Tutorial target not found: {targetId}");
        _stepAdvanced = true;
        return false;
    }

    /// <summary>
    /// 订阅当前步骤需要等待的业务事件。
    /// </summary>
    private void SubscribeExpectedEvent(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            AdvanceStep();
            return;
        }

        UnsubscribeExpectedEvent();
        _expectedEventKey = eventKey;
        _waitingForEvent = true;
        TutorialEventBus.Subscribe(eventKey, OnExpectedEvent);
    }

    /// <summary>
    /// 取消当前业务事件订阅，防止后续步骤收到旧事件。
    /// </summary>
    private void UnsubscribeExpectedEvent()
    {
        if (_waitingForEvent && !string.IsNullOrEmpty(_expectedEventKey))
            TutorialEventBus.Unsubscribe(_expectedEventKey, OnExpectedEvent);

        _waitingForEvent = false;
        _expectedEventKey = null;
    }

    /// <summary>
    /// 处理等待中的业务事件，事件到达即推进当前步骤。
    /// </summary>
    private void OnExpectedEvent()
    {
        AdvanceStep();
    }

    /// <summary>
    /// 标记当前步骤已完成，等待协程在下一帧继续。
    /// </summary>
    private void AdvanceStep()
    {
        _stepAdvanced = true;
    }

}
