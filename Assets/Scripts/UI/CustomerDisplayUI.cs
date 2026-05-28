using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 顾客立绘显示UI
    /// 负责顾客进场/退场动画（剪影摆动+颜色恢复）以及耐心阶段立绘切换
    /// 仅在 Bar 视图下可见
    /// </summary>
    public class CustomerDisplayUI : MonoBehaviour
    {
        [Header("显示组件")]
        [SerializeField] [Tooltip("顾客立绘 Image 组件")]
        private Image _customerImage;

        [SerializeField] [Tooltip("顾客立绘 RectTransform")]
        private RectTransform _customerRect;

        [Header("进场锚点（按顺序：最右→中间→站立位）")]
        [SerializeField] [Tooltip("进场路径锚点，至少2个，最后一个为站立位置")]
        private RectTransform[] _enterWaypoints = new RectTransform[3];

        [Header("退场锚点（从站立位向左依次排列）")]
        [SerializeField] [Tooltip("退场路径锚点，从当前位置依次向左移动")]
        private RectTransform[] _exitWaypoints = new RectTransform[2];

        [Header("动画配置")]
        [SerializeField] [Tooltip("进场每步时长（秒）")]
        private float _enterStepDuration = 0.28f;

        [SerializeField] [Tooltip("退场每步时长（秒）")]
        private float _exitStepDuration = 0.22f;

        [SerializeField] [Tooltip("摆动角度（度）")]
        private float _swingAngle = 10f;

        [SerializeField] [Tooltip("进场动画进行到多少比例时开始恢复颜色（0~1）")]
        [Range(0f, 1f)]
        private float _colorRestoreStartRatio = 0.5f;

        private static readonly Color SilhouetteColor = Color.black;
        private static readonly Color NormalColor    = Color.white;

        /// <summary>进场动画播放完毕时触发</summary>
        public event Action OnEnterAnimationComplete;

        /// <summary>退场动画播放完毕时触发</summary>
        public event Action OnExitAnimationComplete;

        private CustomerSO _currentCustomer;
        private Sequence   _animSequence;
        private int        _lastPatienceStage = 1;
        private bool       _isAnimating       = false;

        // ── 生命周期 ──────────────────────────────────────────

        private void Start()
        {
            SetVisible(false);

            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerSpawned      += OnCustomerSpawned;
                CustomerManager.Instance.OnCustomerLeft         += OnCustomerLeft;
                CustomerManager.Instance.OnCustomerStateChanged += OnCustomerStateChanged;
            }

            if (ViewSwitchManager.Instance != null)
            {
                ViewSwitchManager.Instance.OnViewSwitched += OnViewSwitched;
            }
        }

        private void OnDestroy()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerSpawned      -= OnCustomerSpawned;
                CustomerManager.Instance.OnCustomerLeft         -= OnCustomerLeft;
                CustomerManager.Instance.OnCustomerStateChanged -= OnCustomerStateChanged;
            }

            if (ViewSwitchManager.Instance != null)
            {
                ViewSwitchManager.Instance.OnViewSwitched -= OnViewSwitched;
            }

            _animSequence?.Kill();
        }

        private void Update()
        {
            // 等待/愤怒阶段实时轮询耐心阶段，切换立绘
            if (_currentCustomer == null || _isAnimating) return;
            if (CustomerManager.Instance == null) return;

            var state = CustomerManager.Instance.CurrentState;
            if (state != CustomerState.Waiting && state != CustomerState.Angry) return;

            int stage = CustomerManager.Instance.GetCurrentPatienceStage();
            if (stage == _lastPatienceStage) return;

            _lastPatienceStage = stage;
            UpdateSpriteForStage(stage);
        }

        // ── 事件回调 ──────────────────────────────────────────

        private void OnCustomerSpawned(CustomerSO customer)
        {
            _currentCustomer   = customer;
            _lastPatienceStage = 1;
            PlayEnterAnimation(customer);
        }

        private void OnCustomerLeft(CustomerSO customer)
        {
            PlayExitAnimation(() =>
            {
                _currentCustomer = null;
                SetVisible(false);
            });
        }

        private void OnCustomerStateChanged(CustomerState state)
        {
            if (_currentCustomer == null) return;

            if (state == CustomerState.Angry)
            {
                _lastPatienceStage = 4;
                UpdateSpriteForStage(4);
            }
            else if (state == CustomerState.Happy)
            {
                UpdateSpriteForHappy();
            }
        }

        private void OnViewSwitched(GameViewType viewType)
        {
            if (_customerImage == null) return;

            // 仅在 Bar 视图显示顾客立绘
            bool inBar = viewType == GameViewType.Bar;
            _customerImage.enabled = inBar && _currentCustomer != null;
        }

        // ── 进场动画 ──────────────────────────────────────────

        /// <summary>
        /// 进场：从最右锚点出发，摆动经过中间锚点，到达站立位，颜色从黑色剪影恢复正常
        /// </summary>
        private void PlayEnterAnimation(CustomerSO customer)
        {
            if (_enterWaypoints == null || _enterWaypoints.Length < 2) return;

            _animSequence?.Kill();
            _animSequence = DOTween.Sequence();

            _customerImage.sprite          = GetSpriteForStage(customer, 1);
            _customerImage.color           = SilhouetteColor;
            _customerRect.anchoredPosition = _enterWaypoints[0].anchoredPosition;
            _customerRect.localEulerAngles = Vector3.zero;
            SetVisible(true);
            _isAnimating = true;

            int   steps         = _enterWaypoints.Length - 1;
            float totalDuration = _enterStepDuration * steps;
            float colorStart    = totalDuration * _colorRestoreStartRatio;
            float colorDuration = totalDuration - colorStart;

            for (int i = 1; i < _enterWaypoints.Length; i++)
            {
                RectTransform waypoint      = _enterWaypoints[i];
                float         tilt          = (i % 2 == 1) ? -_swingAngle : _swingAngle;
                float         halfStep      = _enterStepDuration * 0.5f;
                float         stepStartTime = _enterStepDuration * (i - 1);

                _animSequence.Append(
                    _customerRect.DOAnchorPos(waypoint.anchoredPosition, _enterStepDuration)
                        .SetEase(Ease.InOutSine)
                );
                _animSequence.Join(
                    _customerRect.DOLocalRotate(new Vector3(0f, 0f, tilt), halfStep)
                        .SetEase(Ease.OutSine)
                );
                _animSequence.Insert(
                    stepStartTime + halfStep,
                    _customerRect.DOLocalRotate(Vector3.zero, halfStep).SetEase(Ease.InSine)
                );
            }

            // 颜色在后半段恢复
            _animSequence.Insert(colorStart,
                _customerImage.DOColor(NormalColor, colorDuration).SetEase(Ease.InQuad)
            );

            _animSequence.OnComplete(() =>
            {
                _customerRect.localEulerAngles = Vector3.zero;
                _customerImage.color           = NormalColor;
                _isAnimating                   = false;
                OnEnterAnimationComplete?.Invoke();
            });
        }

        // ── 退场动画 ──────────────────────────────────────────

        /// <summary>
        /// 退场：从当前位置摆动向左离开，颜色从正常逐步变回黑色剪影
        /// </summary>
        private void PlayExitAnimation(Action onComplete)
        {
            if (_exitWaypoints == null || _exitWaypoints.Length == 0)
            {
                SetVisible(false);
                onComplete?.Invoke();
                return;
            }

            _animSequence?.Kill();
            _animSequence = DOTween.Sequence();
            _isAnimating  = true;

            _customerImage.color = NormalColor;

            float stepCount    = _exitWaypoints.Length;
            float stepDuration = _exitStepDuration;

            for (int i = 0; i < _exitWaypoints.Length; i++)
            {
                RectTransform waypoint   = _exitWaypoints[i];
                float         tilt       = (i % 2 == 0) ? _swingAngle : -_swingAngle;
                float         halfStep   = stepDuration * 0.5f;
                float         stepStart  = stepDuration * i;
                float         colorRatio = (i + 1f) / stepCount;
                Color         stepColor  = Color.Lerp(NormalColor, SilhouetteColor, colorRatio);

                _animSequence.Insert(stepStart,
                    _customerRect.DOAnchorPos(waypoint.anchoredPosition, stepDuration)
                        .SetEase(Ease.InOutSine)
                );
                _animSequence.Insert(stepStart,
                    _customerRect.DOLocalRotate(new Vector3(0f, 0f, tilt), halfStep)
                        .SetEase(Ease.OutSine)
                );
                _animSequence.Insert(stepStart + halfStep,
                    _customerRect.DOLocalRotate(Vector3.zero, halfStep).SetEase(Ease.InSine)
                );
                _animSequence.Insert(stepStart,
                    _customerImage.DOColor(stepColor, stepDuration).SetEase(Ease.Linear)
                );
            }

            _animSequence.OnComplete(() =>
            {
                _customerImage.color = SilhouetteColor;
                _isAnimating = false;
                OnExitAnimationComplete?.Invoke();
                onComplete?.Invoke();
            });
        }

        // ── 立绘更新 ──────────────────────────────────────────

        private void UpdateSpriteForStage(int stage)
        {
            if (_customerImage == null || _currentCustomer == null) return;
            _customerImage.sprite = GetSpriteForStage(_currentCustomer, stage);
        }

        /// <summary>
        /// 根据耐心阶段返回对应立绘，未配置时回退到 normalSprite
        /// 阶段1=正常, 2=不耐烦, 3/4=愤怒
        /// </summary>
        private Sprite GetSpriteForStage(CustomerSO customer, int stage)
        {
            Sprite sprite = stage switch
            {
                >= 3 => customer.angrySprite    != null ? customer.angrySprite    : customer.normalSprite,
                2    => customer.impatientSprite != null ? customer.impatientSprite : customer.normalSprite,
                _    => customer.normalSprite
            };
            return sprite != null ? sprite : customer.normalSprite;
        }

        private void UpdateSpriteForHappy()
        {
            if (_customerImage == null || _currentCustomer == null) return;
            if (_currentCustomer.happySprite != null)
                _customerImage.sprite = _currentCustomer.happySprite;
        }

        private void SetVisible(bool visible)
        {
            if (_customerImage != null)
                _customerImage.enabled = visible;
        }
    }
}
