using UnityEngine;

/// <summary>
/// 字符串载荷事件频道，用于广播文本消息（如日志条目、状态提示）。
/// MessageLog 订阅此频道接收并显示游戏日志。
/// </summary>
[CreateAssetMenu(menuName = "Events/String Event Channel", fileName = "StringEventChannel")]
public class StringEventChannel : EventChannel<string> { }
