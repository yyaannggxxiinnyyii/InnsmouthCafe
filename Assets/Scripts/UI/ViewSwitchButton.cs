using UnityEngine;
using UnityEngine.UI;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 界面切换按钮
    /// 用于触发界面的左右切换
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ViewSwitchButton : MonoBehaviour
    {
        #region 序列化字段

        [Header("切换方向")]
        [SerializeField]
        [Tooltip("是否为下一个界面按钮（false为上一个界面按钮）")]
        private bool _isNextButton = true;

        [Header("调试")]
        [SerializeField]
        [Tooltip("是否显示调试日志")]
        private bool _showDebugLog = true;

        #endregion

        #region 私有字段

        /// <summary>
        /// 按钮组件
        /// </summary>
        private Button _button;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
            else
            {
                Debug.LogError("[ViewSwitchButton] 未找到Button组件");
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }

        #endregion

        #region 按钮事件

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClick()
        {
            if (ViewSwitchManager.Instance == null)
            {
                Debug.LogError("[ViewSwitchButton] ViewSwitchManager实例不存在");
                return;
            }

            if (_isNextButton)
            {
                ViewSwitchManager.Instance.SwitchNextView();

                if (_showDebugLog)
                {
                    Debug.Log("[ViewSwitchButton] 点击下一个界面按钮");
                }
            }
            else
            {
                ViewSwitchManager.Instance.SwitchPreviousView();

                if (_showDebugLog)
                {
                    Debug.Log("[ViewSwitchButton] 点击上一个界面按钮");
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置按钮是否可交互
        /// </summary>
        /// <param name="interactable">是否可交互</param>
        public void SetInteractable(bool interactable)
        {
            if (_button != null)
            {
                _button.interactable = interactable;
            }
        }

        #endregion
    }
}
