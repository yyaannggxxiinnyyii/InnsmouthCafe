using System;
using UnityEngine;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 操作日志事件总线
    /// 任何系统调用 Log() 发布一条日志，ActionLogUI 订阅后负责显示
    /// </summary>
    public static class ActionLogBus
    {
        /// <summary>
        /// 日志条目数据
        /// </summary>
        public struct LogEntry
        {
            /// <summary>显示文本</summary>
            public string message;

            /// <summary>文字颜色（null 表示使用 ActionLogUI 的默认颜色）</summary>
            public Color? color;
        }

        /// <summary>
        /// 日志发布事件，ActionLogUI 订阅此事件
        /// </summary>
        public static event Action<LogEntry> OnLog;

        /// <summary>
        /// 发布一条日志（使用默认颜色）
        /// </summary>
        public static void Log(string message)
        {
            OnLog?.Invoke(new LogEntry { message = message, color = null });
        }

        /// <summary>
        /// 发布一条日志（指定颜色）
        /// </summary>
        public static void Log(string message, Color color)
        {
            OnLog?.Invoke(new LogEntry { message = message, color = color });
        }
    }
}
