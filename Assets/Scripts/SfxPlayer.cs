using UnityEngine;

// 효과음 재생기. 2D(공간감 없음)로 재생해 거리와 무관하게 항상 들리게 한다.
//  - 클립을 지정하면 그 사운드를, 비워두면 자동 생성 톤을 재생 (에셋 없이도 소리 보장)
//  - 노트 성공 / 노트 실패 / 보너스 공 3종
[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Header("효과음 클립 (비우면 자동 생성 톤 사용)")]
    public AudioClip hitSuccess;   // 노트 성공(선공)
    public AudioClip hitFail;      // 노트 실패
    public AudioClip ballHit;      // 보너스 공 타격

    [Range(0f, 1f)] public float volume = 0.8f;

    AudioSource _src;

    void Awake()
    {
        Instance = this;
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f; // 2D → 항상 동일 음량
        _src.loop = false;

        EnsureListener();
    }

    public void PlaySuccess() => Play(hitSuccess, 660f, 0.12f);
    public void PlayFail()    => Play(hitFail,    150f, 0.20f);
    public void PlayBall()    => Play(ballHit,    880f, 0.12f);

    void Play(AudioClip clip, float freq, float dur)
    {
        if (_src == null) return;
        _src.PlayOneShot(clip != null ? clip : Tone(freq, dur), volume);
    }

    // 씬에 AudioListener가 하나도 없으면 소리가 안 나므로 카메라(또는 자신)에 추가
    static void EnsureListener()
    {
        if (FindObjectOfType<AudioListener>() != null) return;
        var cam = Camera.main;
        var target = cam != null ? cam.gameObject : Instance.gameObject;
        target.AddComponent<AudioListener>();
        Debug.LogWarning("[BeatHeal] 씬에 AudioListener가 없어 자동 추가했습니다 (" + target.name + ").");
    }

    static AudioClip Tone(float freq, float dur)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.CeilToInt(sampleRate * dur));
        var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Clamp01(1f - t / dur);   // 감쇠 엔벨로프
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
