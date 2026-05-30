using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 通用悬停提示系统入口
    /// </summary>
    public class TooltipSystem : MonoBehaviour
    {
        private static TooltipSystem _instance;

        [Header("提示面板")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("显示设置")]
        [SerializeField] private Vector2 _mouseOffset = new Vector2(24f, -24f);
        [SerializeField] private float _screenPadding = 24f;

        private Canvas _canvas;
        private Camera _uiCamera;
        private bool _isShown;

        public static TooltipSystem Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _canvas.worldCamera;
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (_isShown)
            {
                FollowMouse();
            }
        }

        public void Show(IItemTooltipSource source)
        {
            if (source == null)
            {
                Hide();
                return;
            }

            if (_titleText != null) _titleText.text = source.TooltipTitle ?? string.Empty;
            if (_descriptionText != null) _descriptionText.text = source.TooltipDescription ?? string.Empty;

            if (_panelRect != null)
            {
                _panelRect.gameObject.SetActive(true);
            }

            SetCanvasVisible(true);
            _isShown = true;
            FollowMouse();
        }

        public void Hide()
        {
            _isShown = false;
            HideImmediate();
        }

        private void HideImmediate()
        {
            SetCanvasVisible(false);
            if (_panelRect != null)
            {
                _panelRect.gameObject.SetActive(false);
            }
        }

        private void SetCanvasVisible(bool visible)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void FollowMouse()
        {
            if (_panelRect == null || _canvas == null) return;

            Vector2 screenPos = (Vector2)Input.mousePosition + _mouseOffset;
            Vector2 clampedPos = ClampToScreen(screenPos, _panelRect);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                clampedPos,
                _uiCamera,
                out Vector2 localPoint);

            _panelRect.anchoredPosition = localPoint;
        }

        private Vector2 ClampToScreen(Vector2 screenPos, RectTransform panel)
        {
            if (_canvas == null || panel == null) return screenPos;

            Vector2 size = panel.rect.size * panel.lossyScale;
            float minX = _screenPadding + size.x * panel.pivot.x;
            float maxX = Screen.width - _screenPadding - size.x * (1f - panel.pivot.x);
            float minY = _screenPadding + size.y * panel.pivot.y;
            float maxY = Screen.height - _screenPadding - size.y * (1f - panel.pivot.y);

            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
            screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);
            return screenPos;
        }
    }

    public class HoverTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ScriptableObject _tooltipSource;
        [SerializeField] private float _delay = 0.5f;

        private IItemTooltipSource _source;
        private Coroutine _showCoroutine;
        private bool _isPointerOver;

        private void Awake()
        {
            _source = _tooltipSource as IItemTooltipSource;
        }

        public void Configure(ScriptableObject tooltipSource, float delay = 0.5f)
        {
            _tooltipSource = tooltipSource;
            _source = _tooltipSource as IItemTooltipSource;
            _delay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// 用纯文本配置 Tooltip（用于运行时动态内容，不依赖 SO）
        /// </summary>
        public void ConfigureText(string title, string description, float delay = 0f)
        {
            _tooltipSource = null;
            _source = new InlineTooltipSource(title, description);
            _delay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// 更新已配置的 description 文本（用于实时数值变化）
        /// </summary>
        public void UpdateDescription(string description)
        {
            if (_source is InlineTooltipSource inline)
                inline.Description = description;
        }

        private class InlineTooltipSource : IItemTooltipSource
        {
            public string TooltipTitle { get; }
            public string TooltipDescription => Description;
            public string Description { get; set; }

            public InlineTooltipSource(string title, string description)
            {
                TooltipTitle = title;
                Description  = description;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
            }
            _showCoroutine = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            TooltipSystem.Instance?.Hide();
        }

        private IEnumerator ShowAfterDelay()
        {
            if (_delay > 0f)
            {
                yield return new WaitForSecondsRealtime(_delay);
            }

            _showCoroutine = null;
            if (_isPointerOver)
            {
                TooltipSystem.Instance?.Show(_source);
            }
        }
    }
}
