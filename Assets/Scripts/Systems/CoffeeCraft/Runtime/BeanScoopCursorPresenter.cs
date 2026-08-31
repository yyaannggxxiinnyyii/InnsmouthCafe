using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.CoffeeCraft
{
    /// <summary>
    /// 显示场景取豆时的鼠标豆勺，并在显示期间替代系统鼠标指针。
    /// </summary>
    public class BeanScoopCursorPresenter : MonoBehaviour
    {
        [Header("豆勺显示")]
        [Tooltip("显示在鼠标位置的豆勺图像")]
        [SerializeField] private Image _scoopImage;

        [Tooltip("豆勺图像的 RectTransform")]
        [SerializeField] private RectTransform _scoopRect;

        [Tooltip("豆勺相对鼠标点击点的像素偏移")]
        [SerializeField] private Vector2 _cursorOffset = new Vector2(18f, -18f);

        [Header("豆勺贴图")]
        [SerializeField] private Sprite _emptyScoopSprite;
        [SerializeField] private Sprite _lightScoopSprite;
        [SerializeField] private Sprite _mediumScoopSprite;
        [SerializeField] private Sprite _heavyScoopSprite;
        [SerializeField] private Sprite _fullScoopSprite;

        [Header("舀豆动画")]
        [Tooltip("成功取豆时是否播放豆勺下探动画")]
        [SerializeField] private bool _enableScoopAnimation = true;

        [Tooltip("豆勺下探时的旋转角度")]
        [SerializeField] private float _scoopDownAngle = -15f;

        [Tooltip("豆勺下探动画时长（秒）")]
        [SerializeField] private float _scoopDownDuration = 0.12f;

        [Tooltip("豆勺回正动画时长（秒）")]
        [SerializeField] private float _scoopReturnDuration = 0.15f;

        [Tooltip("倒入研磨机后，空豆勺继续显示的时长（秒）")]
        [SerializeField] private float _emptyScoopAfterLoadDuration = 0.45f;

        private NewCoffeeCraftManager _manager;
        private WorldBeanBarrelInteractable _hoveredBeanBarrel;
        private bool _isScoopVisible;
        private Vector3 _defaultScoopEulerAngles;
        private Tween _scoopTween;
        private float _emptyScoopVisibleUntil;

        private void Awake()
        {
            if (_scoopImage == null)
            {
                _scoopImage = GetComponent<Image>();
            }

            if (_scoopRect == null && _scoopImage != null)
            {
                _scoopRect = _scoopImage.rectTransform;
            }

            if (_scoopRect != null)
            {
                _defaultScoopEulerAngles = _scoopRect.localEulerAngles;
            }

            if (_scoopImage != null)
            {
                _scoopImage.raycastTarget = false;
            }

            SetScoopVisible(false);
        }

        private void Start()
        {
            _manager = NewCoffeeCraftManager.Instance;
            SubscribeManagerEvents();
            RefreshScoopPresentation();
        }

        private void OnEnable()
        {
            if (_manager == null)
            {
                _manager = NewCoffeeCraftManager.Instance;
            }

            SubscribeManagerEvents();
        }

        private void Update()
        {
            if (_manager == null)
            {
                _manager = NewCoffeeCraftManager.Instance;
                SubscribeManagerEvents();
            }

            RefreshScoopPresentation();
            if (_isScoopVisible && _scoopRect != null)
            {
                _scoopRect.position = (Vector2)Input.mousePosition + _cursorOffset;
            }
        }

        private void OnDisable()
        {
            UnsubscribeManagerEvents();
            _scoopTween?.Kill();
            SetScoopVisible(false);
            Cursor.visible = true;
        }

        /// <summary>
        /// 更新当前鼠标悬停的场景豆桶。
        /// </summary>
        /// <param name="beanBarrel">鼠标当前悬停的豆桶，离开时传入 null。</param>
        public void SetHoveredBeanBarrel(WorldBeanBarrelInteractable beanBarrel)
        {
            if (_hoveredBeanBarrel == beanBarrel)
            {
                return;
            }

            _hoveredBeanBarrel = beanBarrel;
            RefreshScoopPresentation();
        }

        /// <summary>
        /// 播放一次成功取豆时的豆勺下探和回正动画。
        /// </summary>
        public void PlayScoopAnimation()
        {
            if (!_enableScoopAnimation || _scoopRect == null)
            {
                return;
            }

            _scoopTween?.Kill();
            _scoopRect.localEulerAngles = _defaultScoopEulerAngles;

            Vector3 scoopDownEulerAngles = _defaultScoopEulerAngles
                + new Vector3(0f, 0f, _scoopDownAngle);
            _scoopTween = DOTween.Sequence()
                .Append(_scoopRect.DOLocalRotate(scoopDownEulerAngles, _scoopDownDuration)
                    .SetEase(Ease.OutQuad))
                .Append(_scoopRect.DOLocalRotate(_defaultScoopEulerAngles, _scoopReturnDuration)
                    .SetEase(Ease.InOutQuad));
        }

        /// <summary>
        /// 在成功倒入研磨机后播放空豆勺动画，并在短暂停留后隐藏豆勺。
        /// </summary>
        public void PlayEmptyScoopAfterLoadAnimation()
        {
            if (_scoopImage == null)
            {
                return;
            }

            _emptyScoopVisibleUntil = Time.unscaledTime
                + Mathf.Max(0f, _emptyScoopAfterLoadDuration);
            _scoopImage.sprite = _emptyScoopSprite;
            SetScoopVisible(true);
            PlayScoopAnimation();
        }

        /// <summary>
        /// 订阅制作管理器的豆勺状态变化事件。
        /// </summary>
        private void SubscribeManagerEvents()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.OnHeldBeansChanged -= OnHeldBeansChanged;
            _manager.OnHeldBeansChanged += OnHeldBeansChanged;
            _manager.OnCraftReset -= OnCraftReset;
            _manager.OnCraftReset += OnCraftReset;
        }

        /// <summary>
        /// 取消订阅制作管理器事件，避免对象销毁后继续收到回调。
        /// </summary>
        private void UnsubscribeManagerEvents()
        {
            if (_manager == null)
            {
                return;
            }

            _manager.OnHeldBeansChanged -= OnHeldBeansChanged;
            _manager.OnCraftReset -= OnCraftReset;
        }

        /// <summary>
        /// 响应手持豆勺内容变化。
        /// </summary>
        private void OnHeldBeansChanged(BeanSO bean, float beanGrams)
        {
            RefreshScoopPresentation();
        }

        /// <summary>
        /// 响应制作流程重置并清除悬停显示状态。
        /// </summary>
        private void OnCraftReset()
        {
            _hoveredBeanBarrel = null;
            _emptyScoopVisibleUntil = 0f;
            RefreshScoopPresentation();
        }

        /// <summary>
        /// 根据悬停和手持状态更新豆勺显示、贴图及系统鼠标可见性。
        /// </summary>
        private void RefreshScoopPresentation()
        {
            if (Time.unscaledTime < _emptyScoopVisibleUntil)
            {
                SetScoopVisible(true);
                if (_scoopImage != null)
                {
                    _scoopImage.sprite = _emptyScoopSprite;
                }

                return;
            }

            bool hasHeldBeans = _manager != null && _manager.HasHeldBeans;
            bool canShowEmptyScoop = _manager != null
                && _hoveredBeanBarrel != null
                && _manager.CanTakeBeans();
            bool shouldShowScoop = hasHeldBeans || canShowEmptyScoop;

            SetScoopVisible(shouldShowScoop);
            if (!shouldShowScoop || _scoopImage == null)
            {
                return;
            }

            _scoopImage.sprite = hasHeldBeans
                ? GetHeldScoopSprite(_manager.HeldBeanGrams, _manager.MaxHeldBeanGrams)
                : _emptyScoopSprite;
        }

        /// <summary>
        /// 根据当前手持豆量选择对应的豆勺贴图。
        /// </summary>
        private Sprite GetHeldScoopSprite(float beanGrams, float maxBeanGrams)
        {
            float fillRatio = maxBeanGrams > 0f ? beanGrams / maxBeanGrams : 0f;
            if (fillRatio <= 0.25f)
            {
                return _lightScoopSprite;
            }

            if (fillRatio <= 0.5f)
            {
                return _mediumScoopSprite;
            }

            if (fillRatio <= 0.75f)
            {
                return _heavyScoopSprite;
            }

            return _fullScoopSprite;
        }

        /// <summary>
        /// 设置豆勺图标与系统鼠标的显示状态。
        /// </summary>
        private void SetScoopVisible(bool visible)
        {
            if (_isScoopVisible == visible
                && (_scoopImage == null || _scoopImage.enabled == visible))
            {
                return;
            }

            _isScoopVisible = visible;
            if (_scoopImage != null)
            {
                _scoopImage.enabled = visible;
            }

            Cursor.visible = !visible;
        }
    }
}
