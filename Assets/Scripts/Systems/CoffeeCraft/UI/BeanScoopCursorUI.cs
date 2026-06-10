using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 取豆勺光标UI
    /// 鼠标进入指定区域时隐藏系统光标，显示取豆勺跟随鼠标
    /// 根据当前取豆进度切换勺子贴图
    /// </summary>
    public class BeanScoopCursorUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("勺子图片")]
        [SerializeField] [Tooltip("勺子Image组件（放在Canvas最上层）")]
        private Image _scoopImage;

        [SerializeField] [Tooltip("勺子RectTransform")]
        private RectTransform _scoopRect;

        [Header("进度贴图（按顺序：0g, 5g, 10g, 15g, 20g）")]
        [SerializeField] [Tooltip("空勺 (0g)")]
        private Sprite _scoopEmpty;

        [SerializeField] [Tooltip("少量 (5g)")]
        private Sprite _scoopLight;

        [SerializeField] [Tooltip("半勺 (10g)")]
        private Sprite _scoopMedium;

        [SerializeField] [Tooltip("大半 (15g)")]
        private Sprite _scoopHeavy;

        [SerializeField] [Tooltip("满勺 (20g)")]
        private Sprite _scoopFull;

        [Header("跟随设置")]
        [SerializeField] [Tooltip("勺子偏移量（相对鼠标位置）")]
        private Vector2 _offset = new Vector2(16f, -16f);

        [SerializeField] [Tooltip("跟随平滑速度（越大越快，0为无平滑）")]
        private float _followSpeed = 25f;

        [SerializeField] [Tooltip("所属Canvas（用于坐标转换）")]
        private Canvas _parentCanvas;

        [Header("动效设置")]
        [SerializeField] [Tooltip("进度切换时的缩放弹跳")]
        private bool _enableScaleBounce = true;

        [SerializeField] [Tooltip("点击取豆时的舀动画")]
        private bool _enableScoopAnimation = true;

        private CoffeeCraftManager _manager;
        private bool _isInArea = false;
        private int _lastSpriteIndex = -1;
        private Camera _uiCamera;
        private Vector2 _targetPosition;
        private Tween _scoopTween;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }

            // 初始隐藏勺子
            SetScoopVisible(false);
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
            }

            _scoopRect?.DOKill();
        }

        private void Update()
        {
            if (!_isInArea || _scoopRect == null) return;

            // 直接用屏幕坐标转世界坐标，不受anchor/pivot影响
            Vector3 screenPos = Input.mousePosition;
            Vector3 worldPos;

            if (_uiCamera != null)
            {
                // Screen Space - Camera 模式
                screenPos.z = _parentCanvas.planeDistance;
                worldPos = _uiCamera.ScreenToWorldPoint(screenPos);
            }
            else
            {
                // Screen Space - Overlay 模式，position直接等于屏幕坐标
                worldPos = screenPos;
            }

            // 加上偏移（转换为世界空间的偏移）
            Vector3 offsetWorld = _scoopRect.TransformVector(_offset);
            Vector3 targetWorld = worldPos + offsetWorld;

            // 平滑跟随
            if (_followSpeed > 0f)
            {
                _scoopRect.position = Vector3.Lerp(
                    _scoopRect.position,
                    targetWorld,
                    Time.unscaledDeltaTime * _followSpeed
                );
            }
            else
            {
                _scoopRect.position = targetWorld;
            }
        }

        /// <summary>
        /// 鼠标进入取豆区域
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isInArea = true;
            Cursor.visible = false;
            UpdateScoopSprite();
            SetScoopVisible(true);
        }

        /// <summary>
        /// 鼠标离开取豆区域
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isInArea = false;
            Cursor.visible = true;
            SetScoopVisible(false);
        }

        /// <summary>
        /// 播放舀豆动画（取豆按钮点击时外部调用）
        /// </summary>
        public void PlayScoopAnimation()
        {
            if (!_enableScoopAnimation || _scoopRect == null) return;

            // 杀掉上一次的动画
            if (_scoopTween != null && _scoopTween.IsActive())
            {
                _scoopTween.Kill();
            }

            _scoopRect.localEulerAngles = Vector3.zero;

            _scoopTween = DOTween.Sequence()
                .Append(_scoopRect.DOLocalRotate(new Vector3(0f, 0f, -15f), 0.12f).SetEase(Ease.OutQuad))
                .Append(_scoopRect.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.InOutQuad));
        }

        /// <summary>
        /// 批次数据变化回调
        /// </summary>
        private void OnBatchDataChanged(CurrentBeanBatchData batch)
        {
            if (_isInArea)
            {
                UpdateScoopSprite();
            }
        }

        /// <summary>
        /// 根据当前取豆克数更新勺子贴图
        /// </summary>
        private void UpdateScoopSprite()
        {
            if (_scoopImage == null || _manager == null) return;

            float gram = _manager.CurrentBatch.beanGram;
            int spriteIndex = GetSpriteIndex(gram);

            if (spriteIndex == _lastSpriteIndex) return;

            _lastSpriteIndex = spriteIndex;
            _scoopImage.sprite = GetSpriteByIndex(spriteIndex);

            // 切换时缩放弹跳
            if (_enableScaleBounce && spriteIndex > 0)
            {
                _scoopRect.DOKill(true);
                _scoopRect.localScale = Vector3.one;
                _scoopRect.DOScale(1.15f, 0.08f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _scoopRect.DOScale(1f, 0.1f)
                            .SetEase(Ease.InOutQuad);
                    });
            }
        }

        /// <summary>
        /// 根据克数获取贴图索引
        /// 0g→0, 1-5g→1, 6-10g→2, 11-15g→3, 16-20g→4
        /// </summary>
        private int GetSpriteIndex(float gram)
        {
            if (gram <= 0f)  return 0;
            if (gram <= 5f)  return 1;
            if (gram <= 10f) return 2;
            if (gram <= 15f) return 3;
            return 4;
        }

        /// <summary>
        /// 根据索引获取对应贴图
        /// </summary>
        private Sprite GetSpriteByIndex(int index)
        {
            switch (index)
            {
                case 0: return _scoopEmpty;
                case 1: return _scoopLight;
                case 2: return _scoopMedium;
                case 3: return _scoopHeavy;
                case 4: return _scoopFull;
                default: return _scoopEmpty;
            }
        }

        /// <summary>
        /// 设置勺子显隐
        /// </summary>
        private void SetScoopVisible(bool visible)
        {
            if (_scoopImage != null)
            {
                _scoopImage.enabled = visible;
            }
        }
    }
}
