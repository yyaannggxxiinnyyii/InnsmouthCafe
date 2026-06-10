using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using InnsmouthCafe.Managers;
using TMPro;
using UnityEngine;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 操作日志字幕 UI。
    /// 订阅 ActionLogEvent，在右下角显示堆叠的日志条目。
    /// </summary>
    public class ActionLogUI : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("日志条目预制体，需包含 TextMeshProUGUI 和 CanvasGroup。")]
        private GameObject _entryPrefab;

        [SerializeField]
        [Tooltip("日志条目容器，锚点和 Pivot 应设置为右下角。")]
        private RectTransform _container;

        [Header("显示设置")]
        [SerializeField]
        [Tooltip("单条日志停留时长，单位为秒。")]
        private float _stayDuration = 2.5f;

        [SerializeField]
        [Tooltip("日志淡出时长，单位为秒。")]
        private float _fadeDuration = 0.5f;

        [SerializeField]
        [Tooltip("日志弹入时长，单位为秒。")]
        private float _slideInDuration = 0.2f;

        [SerializeField]
        [Tooltip("日志条目之间的垂直间距。")]
        private float _entrySpacing = 4f;

        [SerializeField]
        [Tooltip("同时显示的最大日志数量。")]
        private int _maxEntries = 6;

        [Header("颜色设置")]
        [SerializeField]
        [Tooltip("未指定颜色时使用的默认文本颜色。")]
        private Color _defaultColor = Color.white;

        private readonly List<EntryData> _entries = new List<EntryData>();
        private EventBinding<ActionLogEvent> _logBinding;

        /// <summary>
        /// 单条日志 UI 运行时数据。
        /// </summary>
        private class EntryData
        {
            public GameObject go;
            public RectTransform rt;
            public CanvasGroup cg;
            public float height;
            public float targetY;
        }

        /// <summary>
        /// 初始化日志事件绑定。
        /// </summary>
        private void Awake()
        {
            _logBinding = new EventBinding<ActionLogEvent>(OnLog);
        }

        /// <summary>
        /// 注册操作日志事件监听。
        /// </summary>
        private void OnEnable()
        {
            EventBus<ActionLogEvent>.Register(_logBinding);
        }

        /// <summary>
        /// 注销操作日志事件监听。
        /// </summary>
        private void OnDisable()
        {
            EventBus<ActionLogEvent>.Deregister(_logBinding);
        }

        /// <summary>
        /// 处理收到的操作日志事件。
        /// </summary>
        /// <param name="entry">日志事件数据。</param>
        private void OnLog(ActionLogEvent entry)
        {
            while (_entries.Count >= _maxEntries)
            {
                RemoveEntry(_entries[0], true);
            }

            StartCoroutine(SpawnEntry(entry));
        }

        /// <summary>
        /// 生成并播放单条日志的显示动画。
        /// </summary>
        /// <param name="entry">日志事件数据。</param>
        private IEnumerator SpawnEntry(ActionLogEvent entry)
        {
            if (_entryPrefab == null || _container == null)
            {
                yield break;
            }

            GameObject go = Instantiate(_entryPrefab, _container);
            RectTransform rt = go.GetComponent<RectTransform>();
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = go.AddComponent<CanvasGroup>();
            }

            TextMeshProUGUI tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = entry.message;
                tmp.color = entry.color ?? _defaultColor;
            }

            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);

            cg.alpha = 0f;
            rt.anchoredPosition = new Vector2(0f, -_slideInDuration * 60f);
            yield return null;

            float height = rt.rect.height;
            if (height <= 0f)
            {
                height = 30f;
            }

            float shift = height + _entrySpacing;
            foreach (EntryData currentEntry in _entries)
            {
                currentEntry.targetY += shift;
                currentEntry.rt.DOKill(false);
                currentEntry.rt.DOAnchorPosY(currentEntry.targetY, _slideInDuration).SetEase(Ease.OutQuad);
            }

            EntryData data = new EntryData
            {
                go = go,
                rt = rt,
                cg = cg,
                height = height,
                targetY = 0f
            };
            _entries.Add(data);

            rt.anchoredPosition = new Vector2(0f, -(height * 0.5f));
            cg.alpha = 0f;

            rt.DOKill(false);
            cg.DOKill(false);
            rt.DOAnchorPosY(0f, _slideInDuration).SetEase(Ease.OutQuad);
            cg.DOFade(1f, _slideInDuration);

            yield return new WaitForSeconds(_slideInDuration + _stayDuration);

            if (go != null)
            {
                cg.DOKill(false);
                cg.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad);
                yield return new WaitForSeconds(_fadeDuration);
            }

            RemoveEntry(data, false);
        }

        /// <summary>
        /// 从列表中移除并销毁一条日志条目。
        /// </summary>
        /// <param name="data">要移除的条目数据。</param>
        /// <param name="immediate">是否立即终止动画。</param>
        private void RemoveEntry(EntryData data, bool immediate)
        {
            _entries.Remove(data);

            if (data.go != null)
            {
                if (immediate)
                {
                    data.rt.DOKill(false);
                    data.cg.DOKill(false);
                }

                Destroy(data.go);
            }
        }
    }
}
