using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 杯子动画管理器
    /// 负责管理杯子的所有动画效果
    /// </summary>
    public class CupAnimationManager : MonoBehaviour
    {
        [Header("杯子实例")]
        [SerializeField] [Tooltip("当前杯子的Image组件")]
        private Image _cupImage;

        [SerializeField] [Tooltip("杯子的RectTransform")]
        private RectTransform _cupRect;

        [Header("锚点配置")]
        [SerializeField] [Tooltip("杯子锚点配置")]
        private CupAnchorPoints _anchorPoints;

        [Header("动画配置")]
        [SerializeField] [Tooltip("选择杯子飞行时长")]
        private float _selectionFlyDuration = 0.5f;

        [SerializeField] [Tooltip("场景切换-杯子退出时长")]
        private float _sceneExitDuration = 0.3f;

        [SerializeField] [Tooltip("场景切换-杯子进入时长")]
        private float _sceneEnterDuration = 0.3f;

        [Header("调试")]
        [SerializeField] [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        private CoffeeCraftManager _craftManager;
        private ViewSwitchManager _viewManager;
        private CupContainerData _currentCupData;
        private bool _isAnimating = false;

        private void Start()
        {
            _craftManager = CoffeeCraftManager.Instance;
            _viewManager = ViewSwitchManager.Instance;

            if (_craftManager != null)
            {
                _craftManager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }

            // 初始透明（无杯子）
            if (_cupImage != null)
            {
                SetCupAlpha(0f);
            }
        }

        private void OnDestroy()
        {
            if (_craftManager != null)
            {
                _craftManager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
            }

            // 清理所有动画
            _cupRect?.DOKill();
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData coffeeData)
        {
            // 检查杯子是否变化
            if (coffeeData.selectedCup != _currentCupData)
            {
                OnCupChanged(coffeeData.selectedCup);
            }

            // 检查是否需要显示杯子（萃取后才显示）
            bool shouldShow = coffeeData.coffeeSegments.Count > 0;
            if (_cupImage != null)
            {
                SetCupAlpha(shouldShow ? 1f : 0f);
            }
        }

        /// <summary>
        /// 杯子变化处理
        /// </summary>
        private void OnCupChanged(CupContainerData newCup)
        {
            if (newCup == null)
            {
                _currentCupData = null;
                if (_cupImage != null)
                {
                    SetCupAlpha(0f);
                }
                return;
            }

            CupContainerData oldCup = _currentCupData;
            _currentCupData = newCup;

            // 更新杯子图片
            if (_cupImage != null && newCup.cupSprite != null)
            {
                _cupImage.sprite = newCup.cupSprite;
                // 选择杯子时显示（透明度=1）
                SetCupAlpha(1f);
            }

            // 执行飞行动画
            if (oldCup != null)
            {
                // 有旧杯子，执行交换动画
                AnimateCupSwap(oldCup, newCup);
            }
            else
            {
                // 第一次选择杯子，直接飞到萃取机
                AnimateCupToExtraction(newCup);
            }
        }

        /// <summary>
        /// 杯子飞到萃取机动画
        /// </summary>
        private void AnimateCupToExtraction(CupContainerData cup)
        {
            if (_cupRect == null || _anchorPoints == null)
            {
                return;
            }

            RectTransform startAnchor = _anchorPoints.GetSelectionAnchor(cup.cupId);
            RectTransform endAnchor = _anchorPoints.GetExtractionAnchor();

            if (startAnchor == null || endAnchor == null)
            {
                Debug.LogWarning("[CupAnimationManager] 锚点未配置");
                return;
            }

            _isAnimating = true;
            _cupRect.DOKill();

            // 设置起始位置
            _cupRect.anchoredPosition = startAnchor.anchoredPosition;

            // 飞到萃取机
            _cupRect.DOAnchorPos(endAnchor.anchoredPosition, _selectionFlyDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    if (_showDebugLog)
                    {
                        Debug.Log($"[CupAnimationManager] 杯子飞到萃取机：{cup.cupName}");
                    }
                });
        }

        /// <summary>
        /// 杯子交换动画（旧杯子飞回，新杯子飞来）
        /// </summary>
        private void AnimateCupSwap(CupContainerData oldCup, CupContainerData newCup)
        {
            if (_cupRect == null || _anchorPoints == null)
            {
                return;
            }

            RectTransform oldAnchor = _anchorPoints.GetSelectionAnchor(oldCup.cupId);
            RectTransform newAnchor = _anchorPoints.GetSelectionAnchor(newCup.cupId);
            RectTransform extractionAnchor = _anchorPoints.GetExtractionAnchor();

            if (oldAnchor == null || newAnchor == null || extractionAnchor == null)
            {
                Debug.LogWarning("[CupAnimationManager] 锚点未配置");
                return;
            }

            _isAnimating = true;
            _cupRect.DOKill();

            // 阶段1：旧杯子飞回选择区
            _cupRect.DOAnchorPos(oldAnchor.anchoredPosition, _selectionFlyDuration * 0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    // 阶段2：切换到新杯子图片
                    if (_cupImage != null && newCup.cupSprite != null)
                    {
                        _cupImage.sprite = newCup.cupSprite;
                    }

                    // 设置新杯子起始位置
                    _cupRect.anchoredPosition = newAnchor.anchoredPosition;

                    // 阶段3：新杯子飞到萃取机
                    _cupRect.DOAnchorPos(extractionAnchor.anchoredPosition, _selectionFlyDuration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;
                            if (_showDebugLog)
                            {
                                Debug.Log($"[CupAnimationManager] 杯子交换完成：{oldCup.cupName} → {newCup.cupName}");
                            }
                        });
                });
        }

        /// <summary>
        /// 场景切换时的杯子移动动画
        /// </summary>
        /// <param name="fromView">起始场景</param>
        /// <param name="toView">目标场景</param>
        /// <param name="isNext">是否向下一个场景切换</param>
        public void AnimateCupSceneTransition(GameViewType fromView, GameViewType toView, bool isNext)
        {
            if (_cupRect == null || _anchorPoints == null || _cupImage == null)
            {
                return;
            }

            // 检查杯子是否可见（透明度>0）
            if (_cupImage.color.a <= 0f)
            {
                return;
            }

            RectTransform targetAnchor = _anchorPoints.GetSceneAnchor(toView);
            if (targetAnchor == null)
            {
                Debug.LogWarning($"[CupAnimationManager] 目标场景锚点未配置：{toView}");
                return;
            }

            _isAnimating = true;
            _cupRect.DOKill();

            // 计算退出和进入位置
            float screenWidth = ((RectTransform)transform.parent).rect.width;
            Vector2 currentPos = _cupRect.anchoredPosition;
            Vector2 exitPos;
            Vector2 enterPos;

            if (isNext)
            {
                // 向右切换：杯子向右退出，从左侧进入
                exitPos = new Vector2(screenWidth + 200f, currentPos.y);
                enterPos = new Vector2(-200f, targetAnchor.anchoredPosition.y);
            }
            else
            {
                // 向左切换：杯子向左退出，从右侧进入
                exitPos = new Vector2(-200f, currentPos.y);
                enterPos = new Vector2(screenWidth + 200f, targetAnchor.anchoredPosition.y);
            }

            // 创建动画序列
            Sequence seq = DOTween.Sequence();

            // 阶段1：杯子退出
            seq.Append(_cupRect.DOAnchorPos(exitPos, _sceneExitDuration).SetEase(Ease.InBack));

            // 阶段2：等待场景切换（这里可以添加回调通知场景开始切换）
            seq.AppendCallback(() =>
            {
                if (_showDebugLog)
                {
                    Debug.Log("[CupAnimationManager] 杯子退出完成，场景可以切换");
                }
            });

            // 阶段3：设置进入位置
            seq.AppendCallback(() =>
            {
                _cupRect.anchoredPosition = enterPos;
            });

            // 阶段4：杯子进入
            seq.Append(_cupRect.DOAnchorPos(targetAnchor.anchoredPosition, _sceneEnterDuration).SetEase(Ease.OutBack));

            seq.OnComplete(() =>
            {
                _isAnimating = false;
                if (_showDebugLog)
                {
                    Debug.Log($"[CupAnimationManager] 场景切换动画完成：{fromView} → {toView}");
                }
            });
        }

        /// <summary>
        /// 检查是否正在动画中
        /// </summary>
        public bool IsAnimating()
        {
            return _isAnimating;
        }

        /// <summary>
        /// 设置杯子透明度
        /// </summary>
        private void SetCupAlpha(float alpha)
        {
            if (_cupImage == null) return;

            Color color = _cupImage.color;
            color.a = alpha;
            _cupImage.color = color;
        }
    }
}
