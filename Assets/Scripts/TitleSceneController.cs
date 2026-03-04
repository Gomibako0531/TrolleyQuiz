using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [Header("Scene")]
    public string runSceneName = "RunScene";

    [Header("UI")]
    public CanvasGroup fadePanel;         // 黒フェード用（CanvasGroup）
    public TextMeshProUGUI titleText;     // タイトル
    public Button startButton;            // Startボタン

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip titleWhoosh;
    public AudioClip clickBeep;
    public AudioClip startImpact;

    [Header("Timing")]
    public float titlePopTime = 0.6f;
    public float fadeOutTime = 0.6f;

    public GameObject gameStatePrefab;

    void Start()
    {
        if (fadePanel) fadePanel.alpha = 1f;
        if (startButton) startButton.interactable = false;

        // タイトル登場演出
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        // フェードイン
        yield return Fade(1f, 0f, 0.6f);

        // タイトルを一旦小さくしてポップ
        if (titleText)
        {
            titleText.transform.localScale = Vector3.one * 0.7f;
            if (sfxSource && titleWhoosh) sfxSource.PlayOneShot(titleWhoosh);

            float t = 0f;
            while (t < titlePopTime)
            {
                t += Time.deltaTime;
                float k = t / titlePopTime;
                titleText.transform.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one, EaseOutBack(k));
                yield return null;
            }
            titleText.transform.localScale = Vector3.one;
        }

        // ボタン解放
        if (startButton) startButton.interactable = true;
    }

    // ボタンから呼ぶ
    public void OnClickStart()
    {
        if (startButton) startButton.interactable = false;
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
    // クリック音は「ホバー」に使うので、クリックでは鳴らさない（好みでOK）
    // if (sfxSource && clickBeep) sfxSource.PlayOneShot(clickBeep);

    // クリックした瞬間に「ドン！」
    if (sfxSource && startImpact) sfxSource.PlayOneShot(startImpact);

    // フェードアウトして遷移
    yield return Fade(0f, 1f, fadeOutTime);
    SceneManager.LoadScene(runSceneName);
    }

    IEnumerator Fade(float from, float to, float time)
    {
        if (!fadePanel) yield break;

        float t = 0f;
        fadePanel.alpha = from;
        while (t < time)
        {
            t += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        fadePanel.alpha = to;
    }

    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }
}