using System.Collections;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using UnityEngine;

/// <summary>
/// 教学流程门控节点，用于让正常业务流程在教学首日等待引导放行。
/// </summary>
public enum TutorialGateKey
{
    BeforeFirstCustomerEnter,
    BeforeFirstCustomerDialogue,
    BeforeFirstTicketAutoCollapse
}

/// <summary>
/// 教学门控运行时状态，普通模式直接通过，教学首日按节点等待 Release。
/// </summary>
public static class TutorialGate
{
    /// <summary>已放行的教学门控节点。</summary>
    private static readonly HashSet<TutorialGateKey> _releasedGates = new HashSet<TutorialGateKey>();

    /// <summary>
    /// 清空所有门控状态，开始新一局或重新播放教学时调用。
    /// </summary>
    public static void Reset()
    {
        _releasedGates.Clear();
    }

    /// <summary>
    /// 放行指定教学门控节点。
    /// </summary>
    public static void Release(TutorialGateKey gateKey)
    {
        _releasedGates.Add(gateKey);
    }

    /// <summary>
    /// 等待指定教学门控节点放行；非教学首日时立即通过。
    /// </summary>
    public static IEnumerator WaitForRelease(TutorialGateKey gateKey)
    {
        if (!ShouldUseTutorialGate())
            yield break;

        while (!_releasedGates.Contains(gateKey))
            yield return null;
    }

    /// <summary>
    /// 判断当前是否需要启用教学门控。
    /// </summary>
    public static bool ShouldUseTutorialGate()
    {
        if (GameManager.Instance == null || GameManager.Instance.SelectedModeConfig == null)
            return false;

        if (GameManager.Instance.SelectedModeConfig.gameMode != GameMode.Tutorial)
            return false;

        if (GameManager.Instance.IsTutorialCompleted())
            return false;

        return GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentDay == 1;
    }
}
