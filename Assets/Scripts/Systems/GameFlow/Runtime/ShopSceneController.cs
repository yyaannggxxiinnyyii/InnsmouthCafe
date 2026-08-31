using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InnsmouthCafe.GameFlow
{
    /// <summary>
    /// 店铺场景控制器。
    /// 负责将店铺场景入口绑定到跨场景日流程管理器。
    /// </summary>
    public class ShopSceneController : MonoBehaviour
    {
        [Header("店铺入口")]
        [Tooltip("开始探索按钮")]
        [SerializeField] private Button _exploreButton;

        [Tooltip("开始营业按钮")]
        [SerializeField] private Button _startBusinessButton;

        [Header("流程信息")]
        [SerializeField] private TMP_Text _flowStatusText;
        [SerializeField] private TMP_Text _dayText;
        [SerializeField] private GameObject _businessPreparationPanel;

        private void OnEnable()
        {
            if (NewGameFlowManager.Instance != null)
            {
                NewGameFlowManager.Instance.OnStateChanged += HandleStateChanged;
            }

            if (_exploreButton != null)
            {
                _exploreButton.onClick.AddListener(RequestEnterExploration);
            }

            if (_startBusinessButton != null)
            {
                _startBusinessButton.onClick.AddListener(RequestStartBusiness);
            }

            RefreshButtons();
        }

        private void OnDisable()
        {
            if (NewGameFlowManager.Instance != null)
            {
                NewGameFlowManager.Instance.OnStateChanged -= HandleStateChanged;
            }

            if (_exploreButton != null)
            {
                _exploreButton.onClick.RemoveListener(RequestEnterExploration);
            }

            if (_startBusinessButton != null)
            {
                _startBusinessButton.onClick.RemoveListener(RequestStartBusiness);
            }
        }

        /// <summary>
        /// 流程状态变化时刷新店铺入口按钮。
        /// </summary>
        private void HandleStateChanged(GameFlowState state)
        {
            RefreshButtons();
        }

        /// <summary>
        /// 根据当前流程状态刷新店铺入口按钮。
        /// </summary>
        public void RefreshButtons()
        {
            GameFlowState state = NewGameFlowManager.Instance != null
                ? NewGameFlowManager.Instance.CurrentState
                : GameFlowState.None;

            if (_exploreButton != null)
            {
                _exploreButton.interactable = state == GameFlowState.ShopPreparation;
            }

            if (_startBusinessButton != null)
            {
                _startBusinessButton.interactable = state == GameFlowState.BusinessPreparation;
            }

            if (_businessPreparationPanel != null)
            {
                _businessPreparationPanel.SetActive(state == GameFlowState.BusinessPreparation);
            }

            if (_flowStatusText != null)
            {
                _flowStatusText.text = GetStateLabel(state);
            }

            if (_dayText != null && NewGameFlowManager.Instance != null)
            {
                _dayText.text = $"第 {NewGameFlowManager.Instance.CurrentDay} 天";
            }
        }

        /// <summary>
        /// 将全局流程状态转换为店铺界面文本。
        /// </summary>
        private static string GetStateLabel(GameFlowState state)
        {
            switch (state)
            {
                case GameFlowState.ShopPreparation:
                    return "探索准备";
                case GameFlowState.BusinessPreparation:
                    return "营业准备";
                case GameFlowState.LoadingExploration:
                    return "正在进入探索";
                case GameFlowState.ReturningFromExploration:
                    return "正在返回店铺";
                case GameFlowState.Exploring:
                    return "探索进行中";
                case GameFlowState.Business:
                    return "营业进行中";
                case GameFlowState.Settlement:
                    return "日结算";
                default:
                    return "等待开始";
            }
        }

        /// <summary>
        /// 请求进入探索场景。
        /// </summary>
        public void RequestEnterExploration()
        {
            NewGameFlowManager.Instance?.RequestEnterExploration();
        }

        /// <summary>
        /// 请求开始营业。
        /// </summary>
        public void RequestStartBusiness()
        {
            NewGameFlowManager.Instance?.RequestStartBusiness();
        }
    }
}
