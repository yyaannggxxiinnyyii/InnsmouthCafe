using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 操作日志字幕UI
    /// 订阅 ActionLogBus，在右下角显示堆叠的日志条目。
    /// 新条目出现在底部，旧条目向上移动，超时后渐隐消失。
    /// 容器锚点应设为右下角（anchorMin/Max = (1,0)），pivot = (1,0)。
    /// </summary>
    public class ActionLogUI : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] [Tooltip("日志条目预制体（含 TextMeshProUGUI + CanvasGroup）")]
        private GameObject _entryPrefab;

        [SerializeField] [Tooltip("条目容器（RectTransform，锚点右下，不需要 LayoutGroup）")]
        private RectTransform _container;

        [Header("显示设置")]
        [SerializeField] [Tooltip("条目停留时长（秒）")]
        private float _stayDuration = 2.5f;

        [SerializeField] [Tooltip("渐隐时长（秒）")]
        private float _fadeDuration = 0.5f;

        [SerializeField] [Tooltip("弹入时长（秒）")]
        private float _slideInDuration = 0.2f;

        [SerializeField] [Tooltip("条目间距（像素）")]
        private float _entrySpacing = 4f;

        [SerializeField] [Tooltip("最大同时显示条目数，超出后立即移除最旧的")]
        private int _maxEntries = 6;

        [Header("颜色设置")]
        [SerializeField] [Tooltip("默认文字颜色")]
        private Color _defaultColor = Color.white;

        // ── 运行时 ────────────────────────────────────────────

        private class EntryData
        {
            public GameObject go;
            public RectTransform rt;
            public CanvasGroup cg;
            public float height;
            /// <summary>当前目标 anchoredPosition.y（从容器底部算起）</summary>
            public float targetY;
        }

        private readonly List<EntryData> _entries = new List<EntryData>();

        private void OnEnable()  => ActionLogBus.OnLog += OnLog;
        private void OnDisable() => ActionLogBus.OnLog -= OnLog;

        private void OnLog(ActionLogBus.LogEntry entry)
        {
            // 超出最大数量时立即销毁最旧的（最顶部的）
            while (_entries.Count >= _maxEntries)
                RemoveEntry(_entries[0], immediate: true);

            StartCoroutine(SpawnEntry(entry));
        }

        private IEnumerator SpawnEntry(ActionLogBus.LogEntry entry)
        {
            if (_entryPrefab == null || _container == null) yield break;

            // 实例化
            GameObject go = Instantiate(_entryPrefab, _container);
            var rt = go.GetComponent<RectTransform>();
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            // 设置文本
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = entry.message;
                tmp.color = entry.color ?? _defaultColor;
            }

            // 锚点设为右下，pivot 右下，这样 anchoredPosition.y=0 就是容器底部
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);

            // 等一帧让 Canvas 重建，获取真实高度
            cg.alpha = 0f;
            rt.anchoredPosition = new Vector2(0f, -_slideInDuration * 60f); // 先放到屏幕外
            yield return null;

            float h = rt.rect.height;
            if (h <= 0f) h = 30f; // 保底高度

            // 把所有现有条目向上移动
            float shift = h + _entrySpacing;
            foreach (var e in _entries)
            {
                e.targetY += shift;
                e.rt.DOKill(false);
                e.rt.DOAnchorPosY(e.targetY, _slideInDuration).SetEase(Ease.OutQuad);
            }

            // 新条目从底部偏下弹入
            var data = new EntryData { go = go, rt = rt, cg = cg, height = h, targetY = 0f };
            _entries.Add(data);

            rt.anchoredPosition = new Vector2(0f, -(h * 0.5f)); // 从半个身位下方弹入
            cg.alpha = 0f;

            rt.DOKill(false);
            cg.DOKill(false);
            rt.DOAnchorPosY(0f, _slideInDuration).SetEase(Ease.OutQuad);
            cg.DOFade(1f, _slideInDuration);

            // 停留
            yield return new WaitForSeconds(_slideInDuration + _stayDuration);

            // 渐隐
            if (go != null)
            {
                cg.DOKill(false);
                cg.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad);
                yield return new WaitForSeconds(_fadeDuration);
            }

            RemoveEntry(data, immediate: false);
        }

        /// <summary>从列表中移除并销毁一个条目</summary>
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
