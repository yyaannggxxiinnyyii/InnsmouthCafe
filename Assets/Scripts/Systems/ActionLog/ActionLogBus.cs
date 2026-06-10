using UnityEngine;

/// <summary>
/// 操作日志总线。
/// 对外提供统一的日志发布入口，底层通过 EventBus<ActionLogEvent> 分发事件。
/// </summary>
public static class ActionLogBus
{
    /// <summary>
    /// 发布一条普通日志，使用默认颜色。
    /// </summary>
    /// <param name="message">日志文本。</param>
    public static void Log(string message)
    {
        EventBus<ActionLogEvent>.Raise(new ActionLogEvent
        {
            message = message,
            color = null
        });
    }

    /// <summary>
    /// 发布一条警告日志，使用黄色。
    /// </summary>
    /// <param name="message">日志文本。</param>
    public static void LogWarning(string message)
    {
        Log(message, Color.yellow);
    }

    /// <summary>
    /// 发布一条错误日志，使用红色。
    /// </summary>
    /// <param name="message">日志文本。</param>
    public static void LogError(string message)
    {
        Log(message, Color.red);
    }

    /// <summary>
    /// 发布一条指定颜色的日志。
    /// </summary>
    /// <param name="message">日志文本。</param>
    /// <param name="color">日志颜色。</param>
    public static void Log(string message, Color color)
    {
        EventBus<ActionLogEvent>.Raise(new ActionLogEvent
        {
            message = message,
            color = color
        });
    }
}