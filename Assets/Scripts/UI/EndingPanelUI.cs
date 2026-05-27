using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 结局面板UI
    /// 监听 GameFlowManager.OnGameEnding，根据结局类型显示对应内容
    /// </summary>
    public class EndingPanelUI : MonoBehaviour
    {
        [Header("CanvasGroup")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("结局标题")]
        [SerializeField] private TextMeshProUGUI _endingTitleText;

        [Header("结局描述")]
        [SerializeField] private TextMeshProUGUI _endingDescText;

        [Header("理智值显示")]
        [SerializeField] private TextMeshProUGUI _sanityText;

        [Header("结局配置")]
        [SerializeField] [Tooltip("迷失结局标题")]
        private string _lostTitle = "迷失";

        [SerializeField] [Tooltip("迷失结局描述")]
        [TextArea(2, 4)]
        private string _lostDesc = "深渊凝视着你，你也凝视着深渊。\n理智的光芒已经熄灭。";

        [SerializeField] [Tooltip("回归结局标题")]
        private string _returnTitle = "回归";

        [SerializeField] [Tooltip("回归结局描述")]
        [TextArea(2, 4)]
        private string _returnDesc = "你在混沌的边缘保住了自我，\n带着伤痕，回到了人间。";

        [SerializeField] [Tooltip("好结局标题")]
        private string _goodTitle = "光明";

        [SerializeField] [Tooltip("好结局描述")]
        [TextArea(2, 4)]
        private string _goodDesc = "你看透了印斯茅斯的秘密，\n却没有被它吞噬。";

        [Header("按钮")]
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _restartButton;

        [Header("过渡设置")]
        [SerializeField] private float _fadeDuration = 0.8f;
        [SerializeField] private float _showDelay = 1f;

        private void Awake()
        {
            SetGroupState(false);

            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
            _restartButton?.onClick.AddListener(OnRestartClicked);
        }

        private void Start()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.OnGameEnding += OnGameEnding;
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.OnGameEnding -= OnGameEnding;
        }

        // ── 结局显示 ──────────────────────────────────────────

        private void OnGameEnding(GameEnding ending)
        {
            ApplyEndingContent(ending);
            DOVirtual.DelayedCall(_showDelay, () => FadeIn(), ignoreTimeScale: true);
        }

        private void ApplyEndingContent(GameEnding ending)
        {
            string title, desc;

            switch (ending)
            {
                case GameEnding.Good:
                    title = _goodTitle;
                    desc  = _goodDesc;
                    break;
                case GameEnding.Return:
                    title = _returnTitle;
                    desc  = _returnDesc;
                    break;
                default:
                    title = _lostTitle;
                    desc  = _lostDesc;
                    break;
            }

            if (_endingTitleText != null) _endingTitleText.text = title;
            if (_endingDescText  != null) _endingDescText.text  = desc;

            if (_sanityText != null && SanityManager.Instance != null)
                _sanityText.text = $"最终理智值：{SanityManager.Instance.CurrentSanity:F0}";
        }

        private void FadeIn()
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha          = 0f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable   = false;
            _canvasGroup.DOFade(1f, _fadeDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() => _canvasGroup.interactable = true);
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null && GameManager.Instance.SelectedModeConfig != null)
                GameManager.Instance.StartGameWithConfig(GameManager.Instance.SelectedModeConfig);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // ── 工具 ──────────────────────────────────────────────

        private void SetGroupState(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha          = visible ? 1f : 0f;
            _canvasGroup.interactable   = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
