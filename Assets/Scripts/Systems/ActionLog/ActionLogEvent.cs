using UnityEngine;

/// <summary>
/// 操作日志事件
/// 用于发布游戏中的各类日志信息（普通、警告、错误）
/// </summary>
public struct ActionLogEvent : IEvent
{
    /// <summary>日志消息文本</summary>
    public string message;

    /// <summary>文字颜色（null 表示使用 ActionLogUI 的默认颜色）</summary>
    public Color? color;
}
