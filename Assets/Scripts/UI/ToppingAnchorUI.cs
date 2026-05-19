using UnityEngine;
using UnityEngine.UI;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 小料锚点UI组件
    /// 用于显示单个小料图标
    /// </summary>
    public class ToppingAnchorUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("小料图标")]
        private Image _toppingIcon;

        [Header("状态")]
        [Tooltip("当前小料类型")]
        private ToppingType? _currentTopping;

        [Tooltip("锚点索引")]
        private int _anchorIndex;

        /// <summary>
        /// 锚点索引
        /// </summary>
        public int AnchorIndex
        {
            get => _anchorIndex;
            set => _anchorIndex = value;
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty => !_currentTopping.HasValue;

        private void Awake()
        {
            if (_toppingIcon == null)
            {
                _toppingIcon = GetComponent<Image>();
            }

            Clear();
        }

        /// <summary>
        /// 设置小料并显示
        /// </summary>
        public void SetTopping(ToppingType toppingType, Sprite sprite)
        {
            _currentTopping = toppingType;

            if (_toppingIcon != null)
            {
                _toppingIcon.sprite = sprite;
                _toppingIcon.enabled = true;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 清空锚点
        /// </summary>
        public void Clear()
        {
            _currentTopping = null;

            if (_toppingIcon != null)
            {
                _toppingIcon.sprite = null;
                _toppingIcon.enabled = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 播放飞入动画（可选，需要DOTween）
        /// </summary>
        public void PlayFlyInAnimation()
        {
            // TODO: 使用DOTween实现飞入动画
            // transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack);
        }
    }
}
