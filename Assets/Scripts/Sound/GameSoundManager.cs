using UnityEngine;

/// <summary>
/// 게임 사운드 효과 관리자
/// </summary>
public class GameSoundManager : MonoBehaviour
{
    public static GameSoundManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource SfxSource;           // 효과음 재생용

    [Header("Buff Sounds")]
    public AudioClip BuffAcquiredClip;      // 버프 획득 사운드
    public AudioClip LevelUpClip;           // 레벨업 사운드

    [Header("Item Sounds")]
    public AudioClip StarCollectClip;       // 스타 수집 사운드

    [Header("Combat Sounds")]
    public AudioClip ShootClip;             // 발사 사운드
    public AudioClip HitClip;               // 피격 사운드
    public AudioClip EnemyDeathClip;        // 적 사망 사운드
    public AudioClip PlayerDeathClip;       // 플레이어 사망 사운드

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float SfxVolume = 1f;

    [Header("Fallback (클립 없을 때)")]
    public bool UseFallbackSounds = true;

    private void Awake()
    {
        Instance = this;

        // AudioSource가 없으면 생성
        if (SfxSource == null)
        {
            SfxSource = gameObject.AddComponent<AudioSource>();
            SfxSource.playOnAwake = false;
        }

        // 폴백 사운드 생성
        if (UseFallbackSounds)
        {
            CreateFallbackSounds();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 기본 효과음 생성 (클립이 없을 때)
    /// </summary>
    private void CreateFallbackSounds()
    {
        if (BuffAcquiredClip == null)
        {
            BuffAcquiredClip = CreateBeepSound(880f, 0.15f); // 높은 음
        }
        if (StarCollectClip == null)
        {
            StarCollectClip = CreateBeepSound(660f, 0.1f);
        }
        if (LevelUpClip == null)
        {
            LevelUpClip = CreateBeepSound(1046f, 0.2f); // C6
        }
    }

    /// <summary>
    /// 간단한 비프음 생성
    /// </summary>
    private AudioClip CreateBeepSound(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleLength = Mathf.RoundToInt(sampleRate * duration);
        var clip = AudioClip.Create("Beep", sampleLength, 1, sampleRate, false);

        float[] samples = new float[sampleLength];
        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration); // 페이드 아웃
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.3f;
        }
        clip.SetData(samples, 0);

        return clip;
    }

    /// <summary>
    /// 버프 획득 사운드 재생
    /// </summary>
    public void PlayBuffAcquired(BuffType buffType)
    {
        if (BuffAcquiredClip != null)
        {
            PlaySfx(BuffAcquiredClip);
        }
        Debug.Log($"[GameSoundManager] 버프 사운드 재생: {buffType}");
    }

    /// <summary>
    /// 스타 수집 사운드 재생
    /// </summary>
    public void PlayStarCollect()
    {
        if (StarCollectClip != null)
        {
            PlaySfx(StarCollectClip);
        }
    }

    /// <summary>
    /// 레벨업 사운드 재생
    /// </summary>
    public void PlayLevelUp()
    {
        if (LevelUpClip != null)
        {
            PlaySfx(LevelUpClip);
        }
    }

    /// <summary>
    /// 발사 사운드 재생
    /// </summary>
    public void PlayShoot()
    {
        if (ShootClip != null)
        {
            PlaySfx(ShootClip, 0.5f); // 발사음은 볼륨 낮춤
        }
    }

    /// <summary>
    /// 피격 사운드 재생
    /// </summary>
    public void PlayHit()
    {
        if (HitClip != null)
        {
            PlaySfx(HitClip);
        }
    }

    /// <summary>
    /// 적 사망 사운드 재생
    /// </summary>
    public void PlayEnemyDeath()
    {
        if (EnemyDeathClip != null)
        {
            PlaySfx(EnemyDeathClip);
        }
    }

    /// <summary>
    /// 플레이어 사망 사운드 재생
    /// </summary>
    public void PlayPlayerDeath()
    {
        if (PlayerDeathClip != null)
        {
            PlaySfx(PlayerDeathClip);
        }
    }

    /// <summary>
    /// 효과음 재생
    /// </summary>
    public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (SfxSource != null && clip != null)
        {
            SfxSource.PlayOneShot(clip, SfxVolume * volumeMultiplier);
        }
    }

    /// <summary>
    /// 특정 위치에서 효과음 재생
    /// </summary>
    public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, SfxVolume * volumeMultiplier);
        }
    }
}
