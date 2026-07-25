using UnityEngine;
using InnsmouthCafe.Data;

/// <summary>
/// 游戏基础平衡配置访问器。
/// 负责加载并缓存唯一的GameplayBalanceConfigSO资源。
/// </summary>
public sealed class GameplayBalanceManager
{
    private const string ResourcePath = "SO/基础平衡配置SO/GameplayBalanceConfig";

    private static readonly GameplayBalanceManager _instance = new GameplayBalanceManager();

    private GameplayBalanceConfigSO _config;
    private bool _hasReportedMissingConfig;

    /// <summary>游戏基础平衡配置访问器实例。</summary>
    public static GameplayBalanceManager Instance => _instance;

    /// <summary>唯一的游戏基础平衡配置资源。</summary>
    public GameplayBalanceConfigSO Config
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<GameplayBalanceConfigSO>(ResourcePath);
            }

            if (_config == null && !_hasReportedMissingConfig)
            {
                _hasReportedMissingConfig = true;
                Debug.LogError($"[GameplayBalance] 未找到基础平衡配置: Resources/{ResourcePath}");
            }

            return _config;
        }
    }

    private GameplayBalanceManager()
    {
    }
}
