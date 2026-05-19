using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 萃取进度UI组件
    /// 显示萃取进度条和当前/目标萃取量
    /// </summary>
    public class ExtractionProgressUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("进度条背景")]
        private Image _background;

        [SerializeField] [Tooltip("进度条填充")]
        private Image _fill;

        [SerializeField] [Tooltip("进度文本")]
        private TextMeshProUGUI _progressText;

        [Header("显示配置")]
        [SerializeField] [Tooltip("进度条填充颜色")]
        private Color _fillColor = new Color(0.4f, 0.2f, 0.1f, 1f); // 深棕色

        [SerializeField] [Tooltip("是否在未萃取时隐藏")]
        private bool _hideWhenNotExtracting = true;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = false;

        private CoffeeCraftManager _manager;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 获取或添加CanvasGroup（用于控制显示/隐藏）
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null && _hideWhenNotExtracting)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 设置填充颜色
            if (_fill != null)
            {
                _fill.color = _fillColor;
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnExtractionProgressChanged += OnExtractionProgressChanged;
                _manager.OnExtractionCompleted += OnExtractionCompleted;
            }

            // 初始隐藏
            if (_hideWhenNotExtracting)
            {
                SetVisible(false);
            }

            RefreshDisplay(0f, 0f);
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnExtractionProgressChanged -= OnExtractionProgressChanged;
                _manager.OnExtractionCompleted -= OnExtractionCompleted;
            }
        }

        /// <summary>
        /// 萃取进度变化回调
        /// </summary>
        private void OnExtractionProgressChanged(float currentVolume, float targetVolume)
        {
            if (_hideWhenNotExtracting)
            {
                SetVisible(true);
            }

            RefreshDisplay(currentVolume, targetVolume);

            if (_showDebugLog)
            {
                Debug.Log($"[ExtractionProgressUI] 萃取进度：{currentVolume:F1}/{targetVolume:F1}ml ({(currentVolume / targetVolume * 100f):F0}%)");
            }
        }

        /// <summary>
        /// 萃取完成回调
        /// </summary>
        private void OnExtractionCompleted()
        {
            // TODO: 播放音效
            // AudioManager.Instance.PlaySound("ExtractionComplete");

            if (_showDebugLog)
            {
                Debug.Log("[ExtractionProgressUI] 萃取完成");
            }

            // 延迟隐藏（让用户看到100%的进度）
            if (_hideWhenNotExtracting)
            {
                Invoke(nameof(HideAfterComplete), 0.5f);
            }
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        private void RefreshDisplay(float currentVolume, float targetVolume)
        {
            // 更新进度条
            if (_fill != null)
            {
                float progress = targetVolume > 0 ? currentVolume / targetVolume : 0f;
                _fill.fillAmount = Mathf.Clamp01(progress);
            }

            // 更新文本
            if (_progressText != null)
            {
                _progressText.text = $"{currentVolume:F1} / {targetVolume:F1} ml";
            }
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// 完成后隐藏
        /// </summary>
        private void HideAfterComplete()
        {
            SetVisible(false);
        }
    }
}
