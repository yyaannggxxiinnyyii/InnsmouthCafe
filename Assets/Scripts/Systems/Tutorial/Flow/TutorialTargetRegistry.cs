using System.Collections.Generic;
using InnsmouthCafe.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教学目标注册表，负责收集场景中的 TutorialTarget 并按目标 ID 提供查询。
/// </summary>
[DisallowMultipleComponent]
public class TutorialTargetRegistry : Singleton<TutorialTargetRegistry>
{
    /// <summary>目标 ID 到目标组件的运行时缓存。</summary>
    private readonly Dictionary<string, TutorialTarget> _targets = new Dictionary<string, TutorialTarget>();

    protected override void Awake()
    {
        base.Awake();

        if (Instance == this)
            Refresh();
    }

    /// <summary>
    /// 重新收集场景中所有 TutorialTarget，用于目标启用状态变化后的兜底刷新。
    /// </summary>
    public void Refresh()
    {
        _targets.Clear();

        TutorialTarget[] targets = FindObjectsOfType<TutorialTarget>(true);
        foreach (TutorialTarget target in targets)
            Register(target);
    }

    /// <summary>
    /// 将目标注册到缓存中；目标 ID 为空时安全忽略。
    /// </summary>
    public void Register(TutorialTarget target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.TargetId))
            return;

        _targets[target.TargetId] = target;
    }

    /// <summary>
    /// 从缓存中注销目标，只移除当前仍指向该组件的记录。
    /// </summary>
    public void Unregister(TutorialTarget target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.TargetId))
            return;

        if (_targets.TryGetValue(target.TargetId, out TutorialTarget current) && current == target)
            _targets.Remove(target.TargetId);
    }

    /// <summary>
    /// 根据目标 ID 查找教学目标；缓存未命中时会刷新并尝试用现有 UI 组件兜底定位。
    /// </summary>
    public bool TryGet(string targetId, out TutorialTarget target)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            target = null;
            return false;
        }

        if (_targets.TryGetValue(targetId, out target) && target != null && target.isActiveAndEnabled)
            return true;

        Refresh();
        if (_targets.TryGetValue(targetId, out target) && target != null && target.isActiveAndEnabled)
            return true;

        return target != null;
    }
}