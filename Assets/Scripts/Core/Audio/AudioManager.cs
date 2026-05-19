using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 音效管理器（单例）。
/// 职责：
///   - 维护 SFX 对象池（复用 AudioSource，避免高频场景时的 GC）。
///   - 通过 <see cref="SoundId"/> 枚举查找并播放对应 <see cref="AudioClip"/>。
///   - 管理 BGM 专用 AudioSource，支持 DOTween 淡入淡出切换。
///   - 全局音量控制（SFX / BGM），持久化到 PlayerPrefs。
///
/// 使用方法：
///   AudioManager.Instance.PlaySfx(SoundId.ButtonClick);
///   AudioManager.Instance.PlayBgm(bgmClip, fadeDuration: 0.5f);
///   AudioManager.Instance.SetSfxVolume(0.8f);
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    // ── PlayerPrefs 键名 ──────────────────────────────────────

    private const string PrefKeySfxVolume = "SfxVolume";
    private const string PrefKeyBgmVolume = "BgmVolume";

    // ── Inspector 配置 ────────────────────────────────────────

    [Header("音效条目列表")]
    [Tooltip("将每个 SoundId 与对应 AudioClip / 音量 / 音调随机范围绑定。")]
    [SerializeField] private AudioEntry[] _entries;

    [Header("音效池")]
    [Tooltip("预生成的 SFX AudioSource 数量，高频音效场景建议 8–16。")]
    [SerializeField] private int _poolSize = 10;

    [Header("背景音乐")]
    [Tooltip("BGM 专用 AudioSource（挂在同一 GameObject 或子对象上）。")]
    [SerializeField] private AudioSource _bgmSource;
    [Tooltip("BGM 淡出 / 淡入默认时长（秒）。")]
    [SerializeField] private float _bgmFadeDuration = 0.5f;

    [Header("旁白音频")]
    [Tooltip("旁白专用 AudioSource（用于播放线索触发时的旁白语音）。")]
    [SerializeField] private AudioSource _narrationSource;

    // ── 运行时字段 ────────────────────────────────────────────

    /// <summary>SoundId → AudioEntry 快速查找字典，Awake 时构建。</summary>
    private Dictionary<SoundId, AudioEntry> _entryMap;

    /// <summary>SFX 对象池队列。</summary>
    private Queue<AudioSource> _pool;

    /// <summary>全局 SFX 音量系数（0–1）。</summary>
    private float _sfxVolume = 1f;

    /// <summary>全局 BGM 音量上限（0–1）。</summary>
    private float _bgmVolume = 1f;

    /// <summary>全局旁白音量系数（0–1），默认与 SFX 音量相同。</summary>
    private float _narrationVolume = 1f;

    // ── 公开属性 ──────────────────────────────────────────────

    /// <summary>当前全局 SFX 音量（0–1）。</summary>
    public float SfxVolume => _sfxVolume;

    /// <summary>当前全局 BGM 音量（0–1）。</summary>
    public float BgmVolume => _bgmVolume;

    /// <summary>当前全局旁白音量（0–1）。</summary>
    public float NarrationVolume => _narrationVolume;

    // ── 生命周期 ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // 若 base.Awake 检测到重复实例并销毁了自己，就不要继续初始化
        if (Instance != this) return;

        BuildEntryMap();
        BuildPool();
        LoadVolumeSettings();
        DontDestroyOnLoad(gameObject);
    }

    // ── 全局音量 API ──────────────────────────────────────────

    /// <summary>
    /// 设置全局 SFX 音量并持久化到 PlayerPrefs。
    /// </summary>
    /// <param name="v">音量值，会被 Clamp 到 0–1。</param>
    public void SetSfxVolume(float v)
    {
        _sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PrefKeySfxVolume, _sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 设置全局 BGM 音量并持久化到 PlayerPrefs。
    /// 同时实时更新当前正在播放的 BGM 音量。
    /// </summary>
    /// <param name="v">音量值，会被 Clamp 到 0–1。</param>
    public void SetBgmVolume(float v)
    {
        _bgmVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PrefKeyBgmVolume, _bgmVolume);
        PlayerPrefs.Save();

        if (_bgmSource != null && _bgmSource.isPlaying)
        {
            _bgmSource.volume = _bgmVolume;
        }
    }

    /// <summary>
    /// 设置全局旁白音量（当前不持久化，使用 SFX 音量作为默认值）。
    /// </summary>
    /// <param name="v">音量值，会被 Clamp 到 0–1。</param>
    public void SetNarrationVolume(float v)
    {
        _narrationVolume = Mathf.Clamp01(v);
    }

    // ── SFX API ──────────────────────────────────────────────

    /// <summary>
    /// 播放一次性音效（SFX）。
    /// 从对象池中取出空闲的 AudioSource，设置参数后播放，播放完毕自动归还。
    /// </summary>
    /// <param name="id">要播放的音效枚举 ID。</param>
    public void PlaySfx(SoundId id)
    {
        if (!_entryMap.TryGetValue(id, out var entry))
        {
            Debug.LogWarning($"[AudioManager] 找不到 SoundId={id} 的配置，请检查 Inspector 列表。");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"[AudioManager] SoundId={id} 的 AudioClip 为空，请在 Inspector 中赋值。");
            return;
        }

        AudioSource src = RentSource();
        src.clip = entry.clip;
        src.volume = entry.volume * _sfxVolume;
        src.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        src.Play();

        float delay = entry.clip.length / Mathf.Abs(src.pitch) + 0.05f;
        StartCoroutine(ReturnSourceDelayed(src, delay));
    }

    // ── BGM API ──────────────────────────────────────────────

    /// <summary>
    /// 切换背景音乐，先淡出当前 BGM，再淡入新 BGM。
    /// </summary>
    /// <param name="clip">新 BGM 的 AudioClip；传入 <c>null</c> 则只淡出不播放。</param>
    /// <param name="fadeDuration">淡出+淡入各自的时长（秒）；传入负值则使用 Inspector 默认值。</param>
    public void PlayBgm(AudioClip clip, float fadeDuration = -1f)
    {
        float duration = fadeDuration < 0f ? _bgmFadeDuration : fadeDuration;

        _bgmSource.DOKill();

        if (_bgmSource.isPlaying)
        {
            DOTween.To(() => _bgmSource.volume, x => _bgmSource.volume = x, 0f, duration)
                   .OnComplete(() => SwapBgm(clip, duration));
        }
        else
        {
            SwapBgm(clip, duration);
        }
    }

    /// <summary>
    /// 立即停止背景音乐（无淡出）。
    /// </summary>
    public void StopBgm()
    {
        _bgmSource.DOKill();
        _bgmSource.Stop();
    }

    /// <summary>
    /// 暂停 / 恢复背景音乐。
    /// </summary>
    /// <param name="paused"><c>true</c> 暂停，<c>false</c> 恢复。</param>
    public void SetBgmPaused(bool paused)
    {
        if (paused) _bgmSource.Pause();
        else _bgmSource.UnPause();
    }

    // ── 旁白 API ──────────────────────────────────────────────

    /// <summary>
    /// 播放旁白音频（用于线索触发时的语音旁白）。
    /// 如果已有旁白正在播放，会停止当前旁白并播放新的。
    /// </summary>
    /// <param name="clip">旁白音频片段。</param>
    public void PlayNarration(AudioClip clip)
    {
        if (_narrationSource == null)
        {
            Debug.LogWarning("[AudioManager] 旁白 AudioSource 未配置，请在 Inspector 中赋值。");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] 旁白 AudioClip 为空。");
            return;
        }

        _narrationSource.Stop();
        _narrationSource.clip = clip;
        _narrationSource.volume = _narrationVolume;
        _narrationSource.loop = false;
        _narrationSource.Play();

        Debug.Log($"[AudioManager] 播放旁白：{clip.name}");
    }

    /// <summary>
    /// 停止当前正在播放的旁白。
    /// </summary>
    public void StopNarration()
    {
        if (_narrationSource != null)
        {
            _narrationSource.Stop();
        }
    }

    /// <summary>
    /// 检查旁白是否正在播放。
    /// </summary>
    public bool IsNarrationPlaying()
    {
        return _narrationSource != null && _narrationSource.isPlaying;
    }

    // ── 内部方法 ──────────────────────────────────────────────

    /// <summary>从 PlayerPrefs 加载音量设置。</summary>
    private void LoadVolumeSettings()
    {
        _sfxVolume = PlayerPrefs.GetFloat(PrefKeySfxVolume, 1f);
        _bgmVolume = PlayerPrefs.GetFloat(PrefKeyBgmVolume, 1f);
        _narrationVolume = _sfxVolume; // 旁白默认使用 SFX 音量
    }

    /// <summary>根据 _entries 构建 SoundId → AudioEntry 字典。</summary>
    private void BuildEntryMap()
    {
        _entryMap = new Dictionary<SoundId, AudioEntry>(_entries?.Length ?? 0);
        if (_entries == null) return;

        foreach (var entry in _entries)
        {
            if (_entryMap.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[AudioManager] SoundId={entry.id} 重复配置，将使用第一条，忽略后续。");
                continue;
            }
            _entryMap[entry.id] = entry;
        }
    }

    /// <summary>预生成音效池，创建 _poolSize 个 AudioSource 子对象。</summary>
    private void BuildPool()
    {
        _pool = new Queue<AudioSource>(_poolSize);
        for (int i = 0; i < _poolSize; i++)
        {
            _pool.Enqueue(CreatePooledSource(i));
        }
    }

    /// <summary>创建一个挂在子 GameObject 上的 AudioSource 并返回。</summary>
    private AudioSource CreatePooledSource(int index)
    {
        var go = new GameObject($"SFX_Pooled_{index:D2}");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    /// <summary>
    /// 从池中取出一个空闲的 AudioSource。
    /// 若池已耗尽则临时扩容（打印警告提示增大 _poolSize）。
    /// </summary>
    private AudioSource RentSource()
    {
        if (_pool.Count > 0)
            return _pool.Dequeue();

        Debug.LogWarning("[AudioManager] 音效池已耗尽，临时扩容。建议在 Inspector 中增大 Pool Size。");
        return CreatePooledSource(_poolSize++);
    }

    /// <summary>延迟后将 AudioSource 归还到池（停止播放并重置）。</summary>
    private IEnumerator ReturnSourceDelayed(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        src.Stop();
        src.clip = null;
        _pool.Enqueue(src);
    }

    /// <summary>实际执行 BGM 替换与淡入，淡入目标为全局 BGM 音量。</summary>
    private void SwapBgm(AudioClip clip, float fadeDuration)
    {
        if (clip == null)
        {
            _bgmSource.Stop();
            return;
        }

        _bgmSource.clip = clip;
        _bgmSource.volume = 0f;
        _bgmSource.loop = true;
        _bgmSource.Play();
        DOTween.To(() => _bgmSource.volume, x => _bgmSource.volume = x, _bgmVolume, fadeDuration);
    }
}

