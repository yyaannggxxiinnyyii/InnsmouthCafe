using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Persistence;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 主菜单存档槽位面板。
    /// 负责展示三个固定槽位、创建新存档、载入存档和删除存档。
    /// </summary>
    public class SaveSlotPanelUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] [Tooltip("存档面板的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [Header("存档槽位")]
        [SerializeField] [Tooltip("三个固定存档槽位按钮")]
        private Button[] _slotButtons = new Button[SaveSlotService.MaxSaveSlots];

        [SerializeField] [Tooltip("三个槽位按钮中的文本组件")]
        private TextMeshProUGUI[] _slotLabels = new TextMeshProUGUI[SaveSlotService.MaxSaveSlots];

        [Header("底部操作")]
        [SerializeField] [Tooltip("返回主菜单按钮")]
        private Button _backButton;

        [SerializeField] [Tooltip("载入或创建当前选中槽位的按钮")]
        private Button _loadButton;

        [SerializeField] [Tooltip("载入或创建按钮中的文本组件")]
        private TextMeshProUGUI _loadLabel;

        [SerializeField] [Tooltip("删除当前选中槽位的按钮")]
        private Button _deleteButton;

        [SerializeField] [Tooltip("删除按钮中的文本组件")]
        private TextMeshProUGUI _deleteLabel;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("面板淡入淡出时长")]
        private float _fadeDuration = 0.25f;

        private MainMenuUI _mainMenu;
        private int _selectedSlotIndex = -1;
        private bool _deleteConfirmationPending;

        private void Awake()
        {
            _mainMenu = FindObjectOfType<MainMenuUI>();

            EnsureSlotArrays();
            for (int slotIndex = 0; slotIndex < _slotButtons.Length; slotIndex++)
            {
                int capturedSlotIndex = slotIndex;
                _slotButtons[slotIndex]?.onClick.AddListener(
                    () => OnSlotClicked(capturedSlotIndex));
            }

            _backButton?.onClick.AddListener(OnBackClicked);
            _loadButton?.onClick.AddListener(OnLoadOrCreateClicked);
            _deleteButton?.onClick.AddListener(OnDeleteClicked);
            RefreshSlots();
        }

        /// <summary>
        /// 显示存档槽位面板并刷新所有预览。
        /// </summary>
        public void Show()
        {
            RefreshSlots();
            FadePanel(true);
        }

        /// <summary>
        /// 隐藏存档槽位面板。
        /// </summary>
        public void Hide(bool immediate = false)
        {
            if (immediate)
            {
                SetGroupState(false);
                return;
            }

            FadePanel(false);
        }

        private void EnsureSlotArrays()
        {
            if (_slotButtons == null || _slotButtons.Length != SaveSlotService.MaxSaveSlots)
            {
                Array.Resize(ref _slotButtons, SaveSlotService.MaxSaveSlots);
            }

            if (_slotLabels == null || _slotLabels.Length != SaveSlotService.MaxSaveSlots)
            {
                Array.Resize(ref _slotLabels, SaveSlotService.MaxSaveSlots);
            }

            for (int slotIndex = 0; slotIndex < _slotButtons.Length; slotIndex++)
            {
                if (_slotLabels[slotIndex] == null && _slotButtons[slotIndex] != null)
                {
                    _slotLabels[slotIndex] =
                        _slotButtons[slotIndex].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        private void RefreshSlots()
        {
            EnsureSlotArrays();
            var previews = SaveSlotService.Instance.GetAllSlotPreviews();

            for (int slotIndex = 0; slotIndex < SaveSlotService.MaxSaveSlots; slotIndex++)
            {
                SaveSlotPreviewData preview = previews[slotIndex];
                bool isSelected = slotIndex == _selectedSlotIndex;
                string label = BuildSlotLabel(slotIndex, preview, isSelected);
                SetButtonLabel(_slotButtons[slotIndex], _slotLabels[slotIndex], label);

                if (_slotButtons[slotIndex] != null)
                {
                    _slotButtons[slotIndex].interactable = !preview.isCorrupted;
                }
            }

            RefreshActionButtons(previews);
        }

        private string BuildSlotLabel(
            int slotIndex,
            SaveSlotPreviewData preview,
            bool isSelected)
        {
            string prefix = isSelected ? "> " : string.Empty;
            if (preview.isCorrupted)
            {
                return $"{prefix}槽位 {slotIndex + 1}\n存档损坏";
            }

            if (!preview.hasSave)
            {
                return $"{prefix}槽位 {slotIndex + 1}\n空槽位";
            }

            string saveName = string.IsNullOrWhiteSpace(preview.saveName)
                ? $"我的咖啡馆 {slotIndex + 1}"
                : preview.saveName;
            string lastPlayedAt = FormatDate(preview.lastPlayedAtUtc);
            return $"{prefix}槽位 {slotIndex + 1}\n{saveName}\n第 {preview.currentDay} 天 · 资产 {preview.totalAssets}\n最后游玩 {lastPlayedAt}";
        }

        private void RefreshActionButtons(System.Collections.Generic.List<SaveSlotPreviewData> previews)
        {
            bool hasSelection = _selectedSlotIndex >= 0
                && _selectedSlotIndex < previews.Count;
            bool hasSave = hasSelection && previews[_selectedSlotIndex].hasSave;
            bool isCorrupted = hasSelection && previews[_selectedSlotIndex].isCorrupted;

            if (_loadButton != null)
            {
                _loadButton.interactable = hasSelection && !isCorrupted;
            }

            if (_loadLabel != null)
            {
                _loadLabel.text = hasSave ? "载入存档" : "创建存档";
            }

            if (_deleteButton != null)
            {
                _deleteButton.interactable = hasSave;
            }

            if (_deleteLabel != null)
            {
                _deleteLabel.text = _deleteConfirmationPending ? "再次确认删除" : "删除存档";
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (_selectedSlotIndex != slotIndex)
            {
                _selectedSlotIndex = slotIndex;
                _deleteConfirmationPending = false;
                RefreshSlots();
                return;
            }

            OnLoadOrCreateClicked();
        }

        private void OnLoadOrCreateClicked()
        {
            if (_selectedSlotIndex < 0)
            {
                return;
            }

            SaveSlotService saveSlotService = SaveSlotService.Instance;
            SaveSlotPreviewData preview = saveSlotService.GetSlotPreview(_selectedSlotIndex);
            GameSaveData saveData;
            bool success = preview.hasSave
                ? saveSlotService.TryLoadSave(_selectedSlotIndex, out saveData)
                : saveSlotService.TryCreateNewSave(
                    _selectedSlotIndex,
                    $"我的咖啡馆 {_selectedSlotIndex + 1}",
                    out saveData);

            if (!success)
            {
                RefreshSlots();
                return;
            }

            _mainMenu?.OnSaveSelected(saveData);
            if (_mainMenu == null)
            {
                SceneManager.LoadScene("GameScene");
            }
        }

        private void OnDeleteClicked()
        {
            if (_selectedSlotIndex < 0)
            {
                return;
            }

            if (!_deleteConfirmationPending)
            {
                _deleteConfirmationPending = true;
                RefreshSlots();
                return;
            }

            SaveSlotService.Instance.DeleteSave(_selectedSlotIndex);
            _selectedSlotIndex = -1;
            _deleteConfirmationPending = false;
            RefreshSlots();
        }

        private void OnBackClicked()
        {
            _mainMenu?.OnSaveSlotClosed();
        }

        private void SetButtonLabel(Button button, TextMeshProUGUI label, string text)
        {
            if (label == null && button != null)
            {
                label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (label != null)
            {
                label.text = text;
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 32f;
            }
        }

        private string FormatDate(string value)
        {
            if (!DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime dateTime))
            {
                return "暂无记录";
            }

            return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        private void FadePanel(bool fadeIn)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.DOKill();
            if (fadeIn)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => _canvasGroup.interactable = true);
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => _canvasGroup.blocksRaycasts = false);
            }
        }

        private void SetGroupState(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
