using UnityEngine;
using System.Collections.Generic;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 界面切换管理器
    /// 负责管理三个主要游戏界面的循环切换
    /// </summary>
    public class ViewSwitchManager : MonoBehaviour
    {
        #region 单例

        private static ViewSwitchManager _instance;

        /// <summary>
        /// 单例实例
        /// </summary>
        public static ViewSwitchManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ViewSwitchManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ViewSwitchManager");
                        _instance = go.AddComponent<ViewSwitchManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 序列化字段

        [Header("界面面板")]
        [SerializeField]
        [Tooltip("吧台接单界面")]
        private GameObject _barPanel;

        [SerializeField]
        [Tooltip("制作界面1：基础咖啡制作")]
        private GameObject _craftBasePanel;

        [SerializeField]
        [Tooltip("制作界面2：调味与完成")]
        private GameObject _craftMixPanel;

        [Header("切换按钮")]
        [SerializeField]
        [Tooltip("左切换按钮")]
        private GameObject _leftSwitchButton;

        [SerializeField]
        [Tooltip("右切换按钮")]
        private GameObject _rightSwitchButton;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        #endregion

        #region 私有字段

        /// <summary>
        /// 界面列表（按顺序）
        /// </summary>
        private List<GameViewType> _viewList;

        /// <summary>
        /// 当前界面索引
        /// </summary>
        private int _currentViewIndex;

        /// <summary>
        /// 当前界面类型
        /// </summary>
        private GameViewType _currentViewType;

        /// <summary>
        /// 是否允许切换
        /// </summary>
        private bool _canSwitch = true;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeViewList();
        }

        private void Start()
        {
            // 默认显示吧台接单界面
            ShowView(GameViewType.Bar);
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化界面列表
        /// </summary>
        private void InitializeViewList()
        {
            _viewList = new List<GameViewType>
            {
                GameViewType.Bar,
                GameViewType.CraftBase,
                GameViewType.CraftMix
            };

            _currentViewIndex = 0;
            _currentViewType = GameViewType.Bar;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 界面列表初始化完成");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 切换到下一个界面
        /// </summary>
        public void SwitchNextView()
        {
            if (!_canSwitch)
            {
                if (_showDebugLog)
                {
                    Debug.LogWarning("[ViewSwitchManager] 当前不允许切换界面");
                }
                return;
            }

            _currentViewIndex++;
            if (_currentViewIndex >= _viewList.Count)
            {
                _currentViewIndex = 0;
            }

            ShowView(_viewList[_currentViewIndex]);

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 切换到下一个界面: {_currentViewType}");
            }
        }

        /// <summary>
        /// 切换到上一个界面
        /// </summary>
        public void SwitchPreviousView()
        {
            if (!_canSwitch)
            {
                if (_showDebugLog)
                {
                    Debug.LogWarning("[ViewSwitchManager] 当前不允许切换界面");
                }
                return;
            }

            _currentViewIndex--;
            if (_currentViewIndex < 0)
            {
                _currentViewIndex = _viewList.Count - 1;
            }

            ShowView(_viewList[_currentViewIndex]);

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 切换到上一个界面: {_currentViewType}");
            }
        }

        /// <summary>
        /// 显示指定界面
        /// </summary>
        /// <param name="viewType">界面类型</param>
        public void ShowView(GameViewType viewType)
        {
            // 隐藏所有界面
            if (_barPanel != null) _barPanel.SetActive(false);
            if (_craftBasePanel != null) _craftBasePanel.SetActive(false);
            if (_craftMixPanel != null) _craftMixPanel.SetActive(false);

            // 显示指定界面
            switch (viewType)
            {
                case GameViewType.Bar:
                    if (_barPanel != null) _barPanel.SetActive(true);
                    break;

                case GameViewType.CraftBase:
                    if (_craftBasePanel != null) _craftBasePanel.SetActive(true);
                    break;

                case GameViewType.CraftMix:
                    if (_craftMixPanel != null) _craftMixPanel.SetActive(true);
                    break;
            }

            // 更新当前界面
            _currentViewType = viewType;
            _currentViewIndex = _viewList.IndexOf(viewType);

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 显示界面: {_currentViewType}");
            }
        }

        /// <summary>
        /// 设置是否允许切换
        /// </summary>
        /// <param name="canSwitch">是否允许切换</param>
        public void SetCanSwitch(bool canSwitch)
        {
            _canSwitch = canSwitch;

            // 更新按钮状态
            if (_leftSwitchButton != null)
            {
                _leftSwitchButton.SetActive(canSwitch);
            }

            if (_rightSwitchButton != null)
            {
                _rightSwitchButton.SetActive(canSwitch);
            }

            if (_showDebugLog)
            {
                Debug.Log($"[ViewSwitchManager] 设置切换状态: {canSwitch}");
            }
        }

        /// <summary>
        /// 获取当前界面类型
        /// </summary>
        /// <returns>当前界面类型</returns>
        public GameViewType GetCurrentViewType()
        {
            return _currentViewType;
        }

        /// <summary>
        /// 设置界面面板引用（用于运行时动态设置）
        /// </summary>
        public void SetPanelReferences(GameObject barPanel, GameObject craftBasePanel, GameObject craftMixPanel)
        {
            _barPanel = barPanel;
            _craftBasePanel = craftBasePanel;
            _craftMixPanel = craftMixPanel;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 界面面板引用已设置");
            }
        }

        /// <summary>
        /// 设置切换按钮引用（用于运行时动态设置）
        /// </summary>
        public void SetButtonReferences(GameObject leftButton, GameObject rightButton)
        {
            _leftSwitchButton = leftButton;
            _rightSwitchButton = rightButton;

            if (_showDebugLog)
            {
                Debug.Log("[ViewSwitchManager] 切换按钮引用已设置");
            }
        }

        #endregion
    }
}
