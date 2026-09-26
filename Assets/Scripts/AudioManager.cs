using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region 오디오 열거형 정의
public enum BGMType
{
    Main,
    Hunt,
    Shop,
    Boss
}

public enum SFXType
{
    ButtonClick,  // 버튼 클릭
    Shoot_Fire,   // 불 속성 발사음 (여러 클립 등록 가능)
    Shoot_Water,  // 물 속성 발사음 (여러 클립 등록 가능)
    Shoot_Grass,  // 풀 속성 발사음 (여러 클립 등록 가능)
    Hit,          // 적 타격음
    Upgrade,      // 캐릭터 강화/진화
    Buy,          // 상점 구매
    Equip,        // 캐릭터 장착
    BossAppear,   // 보스 등장
    BossFail,      // 보스전 실패
    EquipFail   // 구매 실패
}
#endregion

#region 인스펙터 직렬화 구조체
[System.Serializable]
public struct BGMData
{
    public BGMType bgmType;
    public AudioClip clip;
}

[System.Serializable]
public struct SFXData
{
    public SFXType sfxType;
    public AudioClip[] clips; //  단일 클립에서 배열로 변경 (변주용 클립 여러 개 등록)
}
#endregion

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("컴포넌트 참조")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX 오디오 풀 설정")]
    [SerializeField] private int sfxPoolSize = 20;
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int currentSfxIndex = 0;

    [Header("오디오 클립 데이터베이스")]
    [SerializeField] private BGMData[] bgmList;
    [SerializeField] private SFXData[] sfxList;

    [Header("BGM 설정")]
    [SerializeField][Range(0f, 1f)] private float bgmMasterVolume = 1f;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    [Header("SFX 피치 랜덤화 설정")]
    [SerializeField][Range(0f, 1f)] private float sfxMasterVolume = 1f;
    [SerializeField][Range(0.5f, 1.5f)] private float minPitch = 0.85f;
    [SerializeField][Range(0.5f, 1.5f)] private float maxPitch = 1.15f;

    private Dictionary<BGMType, AudioClip> bgmDictionary = new Dictionary<BGMType, AudioClip>();
    private Dictionary<SFXType, AudioClip[]> sfxDictionary = new Dictionary<SFXType, AudioClip[]>(); //  AudioClip[] 저장

    private Coroutine bgmFadeCoroutine;
    private BGMType currentBGMType = (BGMType)(-1);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitAudioSources();
        InitDictionaries();
    }

    private void InitAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.spatialBlend = 0f;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmMasterVolume;

        GameObject sfxGroup = new GameObject("SFX_Pool");
        sfxGroup.transform.SetParent(transform);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource source = sfxGroup.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.loop = false;
            sfxPool.Add(source);
        }
    }

    private void InitDictionaries()
    {
        if (bgmList != null)
        {
            foreach (var bgm in bgmList)
            {
                if (!bgmDictionary.ContainsKey(bgm.bgmType))
                    bgmDictionary.Add(bgm.bgmType, bgm.clip);
            }
        }

        if (sfxList != null)
        {
            foreach (var sfx in sfxList)
            {
                if (!sfxDictionary.ContainsKey(sfx.sfxType))
                    sfxDictionary.Add(sfx.sfxType, sfx.clips);
            }
        }
    }

    #region BGM 시스템
    public void PlayBGM(BGMType type, float fadeDuration = -1f)
    {
        if (!bgmDictionary.TryGetValue(type, out AudioClip clip) || clip == null) return;
        if (currentBGMType == type && bgmSource.isPlaying) return;

        currentBGMType = type;
        float duration = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(FadeBGMCoroutine(clip, duration));
    }

    private IEnumerator FadeBGMCoroutine(AudioClip newClip, float duration)
    {
        float targetVolume = bgmMasterVolume;

        if (bgmSource.isPlaying && bgmSource.volume > 0f)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        float fadeInTimer = 0f;
        while (fadeInTimer < duration)
        {
            fadeInTimer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, fadeInTimer / duration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }
    #endregion

    #region SFX 풀링 및 변주 재생 시스템
    private AudioSource GetAvailableSFXSource()
    {
        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (!sfxPool[i].isPlaying)
                return sfxPool[i];
        }

        AudioSource source = sfxPool[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxPoolSize;
        return source;
    }

    /// <summary>
    /// 지정된 SFXType의 음원 목록 중 하나를 무작위 선택하여 피치 변형과 함께 재생합니다.
    /// </summary>
    public void PlaySFX(SFXType type, bool useRandomPitch = false)
    {
        // 1. 해당 SFXType에 등록된 클립 배열 탐색
        if (!sfxDictionary.TryGetValue(type, out AudioClip[] clips) || clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"[AudioManager] 등록되지 않거나 비어있는 SFX입니다: {type}");
            return;
        }

        // 2. 클립 배열 중 무작위 1개 선택 (변주)
        int randomIndex = Random.Range(0, clips.Length);
        AudioClip selectedClip = clips[randomIndex];

        if (selectedClip == null) return;

        // 3. 오디오 소스 할당 및 피치 변형 후 재생
        AudioSource source = GetAvailableSFXSource();
        source.clip = selectedClip;
        source.volume = sfxMasterVolume;
        source.pitch = useRandomPitch ? Random.Range(minPitch, maxPitch) : 1f;
        source.Play();
    }
    #endregion
}