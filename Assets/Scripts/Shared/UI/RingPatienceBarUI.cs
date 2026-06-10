using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 环形耐心条UI
    /// 在 CraftBase / CraftMix 界面显示，由 filled radial 360 圆形进度图 + 顾客头像组成
    /// 用 CanvasGroup 淡入淡出，在视图切换开始时就同步过渡
    /// </summary>
    public class RingPatienceBarUI : MonoBehaviour
    {
        [Header("UI 引用")]
        [SerializeField] [Tooltip("环形进度图（Image，Fill Method = Radial 360，顺时针）")]
        private Image _ringFill;

        [SerializeField] [Tooltip("顾客头像 Image（位于环形中心）")]
        private Image _avatarImage;

        [SerializeField] [Tooltip("整个环形条的 CanvasGroup（用于淡入淡出）")]
        private CanvasGroup _canvasGroup;

        [Header("颜色配置")]
        [SerializeField] [Tooltip("阶段1颜色（有耐心）")]
        private Color _stageOneColor = new Color(0.3f, 0.85f, 0.3f);

        [SerializeField] [Tooltip("阶段2颜色（有点等不及）")]
        private Color _stageTwoColor = new Color(1f, 0.75f, 0f);

        [SerializeField] [Tooltip("阶段3颜色（不耐烦）")]
        private Color _stageThreeColor = new Color(1f, 0.25f, 0.1f);

        [Header("过渡配置")]
        [SerializeField] [Tooltip("淡入淡出时长（秒），建议与场景切换动画时长一致")]
        [Range(0.1f, 0.8f)]
        private float _fadeDuration = 0.3f;

        [Header("默认头像")]
        [SerializeField] [Tooltip("无顾客时显示的默认头像（可为空）")]
        private Sprite _defaultAvatar;

        // ── 私有状态 ──────────────────────────────────────────

        /// <summary>当前是否处于需要显示环形条的视图</summary>
        private bool _isOnCraftView = false;

        /// <summary>当前是否有顾客在等待（耐心条激活中）</summary>
        private bool _isPatienceActive = false;

        // ── 生命周期 ──────────────────────────────────────────

        private void Start()
        {
            // 初始完全透明，不拦截射线
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerSpawned      += OnCustomerSpawned;
                CustomerManager.Instance.OnCustomerLeft         += OnCustomerLeft;
                CustomerManager.Instance.OnCustomerStateChanged += OnCustomerStateChanged;
                CustomerManager.Instance.OnPatienceChanged      += OnPatienceChanged;
            }

            if (ViewSwitchManager.Instance != null)
            {
                // OnViewSwitchStarted：切换动画开始时触发，同步开始淡入淡出
                ViewSwitchManager.Instance.OnViewSwitchStarted += OnViewSwitchStarted;
                // OnViewSwitched：切换完成时兜底校正（直接跳转时无动画）
                ViewSwitchManager.Instance.OnViewSwitched      += OnViewSwitched;
            }
        }

        private void OnDestroy()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerSpawned      -= OnCustomerSpawned;
                CustomerManager.Instance.OnCustomerLeft         -= OnCustomerLeft;
                CustomerManager.Instance.OnCustomerStateChanged -= OnCustomerStateChanged;
                CustomerManager.Instance.OnPatienceChanged      -= OnPatienceChanged;
            }

            if (ViewSwitchManager.Instance != null)
            {
                ViewSwitchManager.Instance.OnViewSwitchStarted -= OnViewSwitchStarted;
                ViewSwitchManager.Instance.OnViewSwitched      -= OnViewSwitched;
            }

            _canvasGroup?.DOKill();
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCustomerSpawned(CustomerSO customer)
        {
            RefreshAvatar(customer);
            _isPatienceActive = false;
            RefreshFill(1f, 1);
            // 顾客刚到，耐心条还未激活，不显示
        }

        private void OnCustomerLeft(CustomerSO customer)
        {
            _isPatienceActive = false;
            FadeTo(0f);
        }

        private void OnCustomerStateChanged(CustomerState state)
        {
            if (state == CustomerState.Waiting)
            {
                _isPatienceActive = true;
                UpdateVisibility();
            }
            else if (state == CustomerState.Angry)
            {
                RefreshFill(0f, 3);
            }
            else if (state == CustomerState.Feedback
                  || state == CustomerState.Happy
                  || state == CustomerState.Leaving)
            {
                _isPatienceActive = false;
                FadeTo(0f);
            }
        }

        private void OnPatienceChanged(float remainingRatio)
        {
            int stage = CustomerManager.Instance != null
                ? CustomerManager.Instance.GetCurrentPatienceStage()
                : 1;
            RefreshFill(remainingRatio, stage);
        }

        /// <summary>
        /// 视图切换动画开始时触发（from → to）
        /// 此时场景容器刚开始滑动，同步开始淡入淡出
        /// </summary>
        private void OnViewSwitchStarted(GameViewType from, GameViewType to, bool isNext)
        {
            bool toIsCraft = to == GameViewType.CraftBase || to == GameViewType.CraftMix;
            bool fromIsBar = from == GameViewType.Bar;
            bool toIsBar   = to   == GameViewType.Bar;

            if (!_isPatienceActive) return;

            if (toIsCraft && fromIsBar)
            {
                // Bar → Craft：淡入环形条
                _isOnCraftView = true;
                FadeTo(1f);
            }
            else if (toIsBar)
            {
                // Craft → Bar：淡出环形条
                _isOnCraftView = false;
                FadeTo(0f);
            }
        }

        /// <summary>
        /// 视图切换完成时兜底（ShowView 直接跳转时不会触发 OnViewSwitchStarted）
        /// </summary>
        private void OnViewSwitched(GameViewType viewType)
        {
            _isOnCraftView = viewType == GameViewType.CraftBase
                          || viewType == GameViewType.CraftMix;
            // 直接跳转时无动画，立即设置目标 alpha
            float targetAlpha = (_isOnCraftView && _isPatienceActive) ? 1f : 0f;
            if (_canvasGroup != null && !DOTween.IsTweening(_canvasGroup))
            {
                _canvasGroup.alpha          = targetAlpha;
                _canvasGroup.blocksRaycasts = targetAlpha > 0f;
            }
        }

        // ── 显隐逻辑 ──────────────────────────────────────────

        /// <summary>
        /// 根据当前视图和耐心激活状态决定是否显示
        /// </summary>
        private void UpdateVisibility()
        {
            bool shouldShow = _isOnCraftView && _isPatienceActive;
            FadeTo(shouldShow ? 1f : 0f);
        }

        /// <summary>
        /// 用 DOTween 淡入淡出到目标 alpha
        /// </summary>
        private void FadeTo(float targetAlpha)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.DOKill();
            _canvasGroup.DOFade(targetAlpha, _fadeDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = targetAlpha > 0f;
                    _canvasGroup.interactable   = targetAlpha > 0f;
                });
        }

        // ── 刷新内容 ──────────────────────────────────────────

        private void RefreshFill(float ratio, int stage)
        {
            if (_ringFill == null) return;

            _ringFill.fillAmount = Mathf.Clamp01(ratio);
            _ringFill.color = stage switch
            {
                1    => _stageOneColor,
                2    => _stageTwoColor,
                >= 3 => _stageThreeColor,
                _    => _stageOneColor
            };
        }

        private void RefreshAvatar(CustomerSO customer)
        {
            if (_avatarImage == null) return;

            if (customer != null && customer.avatarSprite != null)
                _avatarImage.sprite = customer.avatarSprite;
            else if (_defaultAvatar != null)
                _avatarImage.sprite = _defaultAvatar;
        }
    }
}
