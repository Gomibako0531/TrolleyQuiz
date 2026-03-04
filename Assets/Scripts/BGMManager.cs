using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class BGMManager : MonoBehaviour
{
    public static BGMManager I { get; private set; }

    [Header("Audio")]
    public AudioSource bgmSource;

    [Tooltip("最初に流すBGM。AudioSource側に入れていてもOK")]
    public AudioClip defaultBgm;


    void Awake()
    {
        // 二重生成防止
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource自動取得
        if (bgmSource == null) bgmSource = GetComponent<AudioSource>();
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;

        // 初回再生
        if (bgmSource.clip == null && defaultBgm != null)
            bgmSource.clip = defaultBgm;

        if (bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    // 途中でBGMを変えたいとき用（必要になったら使う）
    public void PlayBgm(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (bgmSource == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void FadeToBgm(AudioClip newClip, float fadeTime = 1f)
    {
        StartCoroutine(FadeRoutine(newClip, fadeTime));
    }

    IEnumerator FadeRoutine(AudioClip newClip, float fadeTime)
    {
        float startVol = bgmSource.volume;
        float t = 0f;

        // フェードアウト
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // フェードイン
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, startVol, t / fadeTime);
            yield return null;
        }
    }

    public void FadeVolume(float targetVolume, float time = 0.5f)
    {
        if (bgmSource == null) return;
        StartCoroutine(FadeVolumeRoutine(targetVolume, time));
    }

    IEnumerator FadeVolumeRoutine(float target, float time)
    {
        float start = bgmSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        bgmSource.volume = target;
    }
}