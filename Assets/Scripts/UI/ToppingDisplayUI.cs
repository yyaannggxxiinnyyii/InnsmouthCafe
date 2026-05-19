using UnityEngine;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 小料显示管理器
    /// 管理20个小料锚点的显示和刷新
    /// </summary>
    public class ToppingDisplayUI : MonoBehaviour
    {
        [Header("锚点引用")]
        [SerializeField] [Tooltip("20个小料锚点")]
        private ToppingAnchorUI[] _anchors = new ToppingAnchorUI[20];

        [Header("小料图标配置")]
        [SerializeField] [Tooltip("焦糖碎图标")]
        private Sprite _caramelCrispSprite;

        [SerializeField] [Tooltip("巧克力粉图标")]
        private Sprite _chocolatePowderSprite;

        [SerializeField] [Tooltip("海星糖图标")]
        private Sprite _starfishCandySprite;

        [SerializeField] [Tooltip("眼球爆珠图标")]
        private Sprite _eyeballPoppingBobaSprite;

        [SerializeField] [Tooltip("月尘粉图标")]
        private Sprite _moonDustSprite;

        [SerializeField] [Tooltip("黑盐图标")]
        private Sprite _blackSaltSprite;

        [Header("预制体")]
        [SerializeField] [Tooltip("锚点预制体（可选）")]
        private GameObject _anchorPrefab;

        [SerializeField] [Tooltip("锚点容器")]
        private Transform _anchorContainer;

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 如果没有手动配置锚点，尝试自动查找或创建
            if (_anchors == null || _anchors.Length == 0 || _anchors[0] == null)
            {
                InitializeAnchors();
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
            }
        }

        /// <summary>
        /// 初始化锚点
        /// </summary>
        private void InitializeAnchors()
        {
            if (_anchorContainer == null)
            {
                _anchorContainer = transform;
            }

            // 查找现有锚点
            ToppingAnchorUI[] existingAnchors = _anchorContainer.GetComponentsInChildren<ToppingAnchorUI>(true);

            if (existingAnchors.Length >= 20)
            {
                _anchors = new ToppingAnchorUI[20];
                for (int i = 0; i < 20; i++)
                {
                    _anchors[i] = existingAnchors[i];
                    _anchors[i].AnchorIndex = i;
                }
                return;
            }

            // 如果没有足够的锚点，创建新的
            _anchors = new ToppingAnchorUI[20];
            for (int i = 0; i < 20; i++)
            {
                GameObject anchorObj;

                if (_anchorPrefab != null)
                {
                    anchorObj = Instantiate(_anchorPrefab, _anchorContainer);
                }
                else
                {
                    anchorObj = new GameObject($"Anchor_{i:00}");
                    anchorObj.transform.SetParent(_anchorContainer, false);
                    anchorObj.AddComponent<ToppingAnchorUI>();
                }

                anchorObj.name = $"Anchor_{i:00}";
                _anchors[i] = anchorObj.GetComponent<ToppingAnchorUI>();
                _anchors[i].AnchorIndex = i;
                _anchors[i].Clear();
            }
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData data)
        {
            RefreshToppingAnchors(data);
        }

        /// <summary>
        /// 刷新所有锚点显示
        /// </summary>
        public void RefreshToppingAnchors(CoffeeData data)
        {
            if (data == null || _anchors == null)
            {
                return;
            }

            // 清空所有锚点
            for (int i = 0; i < _anchors.Length; i++)
            {
                if (_anchors[i] != null)
                {
                    _anchors[i].Clear();
                }
            }

            // 按顺序填充小料
            for (int i = 0; i < data.toppings.Count && i < _anchors.Length; i++)
            {
                ToppingInstanceData topping = data.toppings[i];
                Sprite sprite = GetToppingSprite(topping.topping);

                if (_anchors[i] != null)
                {
                    _anchors[i].SetTopping(topping.topping, sprite);
                }
            }
        }

        /// <summary>
        /// 根据小料配置获取图标
        /// </summary>
        private Sprite GetToppingSprite(ToppingSO topping)
        {
            if (topping == null)
            {
                return null;
            }

            // 根据 toppingId 来获取对应的精灵
            switch (topping.toppingId)
            {
                case "caramel_crisp":
                    return _caramelCrispSprite;

                case "chocolate_powder":
                    return _chocolatePowderSprite;

                case "starfish_candy":
                    return _starfishCandySprite;

                case "eyeball_popping_boba":
                    return _eyeballPoppingBobaSprite;

                case "moon_dust":
                    return _moonDustSprite;

                case "black_salt":
                    return _blackSaltSprite;

                default:
                    Debug.LogWarning($"[ToppingDisplayUI] 未配置小料图标：{topping.toppingName}");
                    return null;
            }
        }

        /// <summary>
        /// 手动刷新显示（用于测试）
        /// </summary>
        public void ManualRefresh()
        {
            if (_manager != null && _manager.CurrentCoffeeData != null)
            {
                RefreshToppingAnchors(_manager.CurrentCoffeeData);
            }
        }
    }
}
