using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BranchSceneController : MonoBehaviour
{
    [Header("Track scrollers (optional)")]
    public MonoBehaviour leftTrackScroller;
    public MonoBehaviour rightTrackScroller;

    [Header("Trolley Visual Angle")]
    public float trolleyYaw = 0f;

    [Header("Spawn")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public GameObject trolleyPrefab;

    [Tooltip("同じ側に全員来ても見えるように、Z方向に並べる間隔")]
    public float trolleySpacingZ = 2.0f;

    [Header("UI")]
    public TextMeshProUGUI leftJudgeText;
    public TextMeshProUGUI rightJudgeText;

    [Header("Timing")]
    public float runDuration = 3.0f;

    [Header("Rule")]
    [Tooltip("不正解チームを脱落扱いにするならON（次の問題で表示しなくなる）")]
    public bool eliminateWrongTeams = false;

    [Header("Lightning (Wrong side effect)")]
    public GameObject lightningPrefab;         // ParticleSystem prefab など
    public Transform leftLightningPoint;       // 左線路上（上空）の空Object
    public Transform rightLightningPoint;      // 右線路上（上空）の空Object
    public AudioSource lightningAudio;         // 雷の音（PlayOnAwake OFF 推奨）
    public float lightningAutoDestroy = 2.0f;  // 生成した雷を消す秒数（0なら消さない）

    [Header("Fireworks (All-correct effect)")]
    public GameObject fireworksPrefab;         // 花火VFX prefab
    public Transform leftFireworksPoint;       // 左線路上（上空）の空Object
    public Transform rightFireworksPoint;      // 右線路上（上空）の空Object
    public AudioSource fireworksAudio;         // 花火の音（PlayOnAwake OFF 推奨）
    public float fireworksAutoDestroy = 3.0f;

    [Header("TV Show: Audio")]
    public AudioSource sfxSource;              // SFX_Audio の AudioSource
    public AudioClip whooshClip;               // 溜めのヒュン…（任意）
    public AudioClip drumRollClip;             // ドラムロール（任意）
    public AudioClip revealClip;               // ドン！（必須級）
    public AudioClip correctClip;              // 正解SE（任意）
    public AudioClip wrongClip;                // 不正解SE（任意）

    [Header("TV Show: Timing")]
    public float suspenseFadeDownTime = 1.0f;  // BGM下げる時間
    public float suspenseHoldTime = 1.5f;      // スロー中の溜め
    public float silenceTime = 0.5f;           // 静寂
    public float textDelayAfterReveal = 0.4f; // ドン後に文字を出す遅延
    public float bgmReturnTime = 1.0f;         // BGM戻す時間

    [Header("TV Show: BGM Volume")]
    public float normalBgmVolume = 1.0f;
    public float suspenseBgmVolume = 0.15f;

    [Header("TV Show: Screen FX")]
    public CanvasGroup flashPanel;             // FlashPanel の CanvasGroup
    public float flashTime = 0.12f;

    public Transform cameraRoot;               // Main Camera（Transform）
    public float shakeDuration = 0.3f;
    public float shakeStrength = 0.3f;

    readonly List<GameObject> leftTrolleys = new();
    readonly List<GameObject> rightTrolleys = new();

    void Start()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        var gs = GameState.I;
        if (gs == null) yield break;

        // その回の正解側（ランダム左右入れ替え後）
        int correctSide = gs.runtimeCorrect;   // 0=Left(A), 1=Right(D)
        int wrongSide = 1 - correctSide;

        // ① スコア加算
        gs.ApplyScoringForCurrentQuestion();

        // ② 左右にトロッコ生成（スコア順）
        SpawnSortedTrolleysByAnswer();

        // ③ 結果テキストはまだ出さない（テレビの溜め）
        ClearJudgeText();

        // ===== TV演出：溜め =====
        // ④ BGMを下げる
        if (BGMManager.I != null)
            BGMManager.I.FadeVolume(suspenseBgmVolume, suspenseFadeDownTime);

        // ⑤ 溜めSFX
        if (sfxSource && whooshClip) sfxSource.PlayOneShot(whooshClip);

        // ⑥ スロー（WaitForSecondsRealtimeで待つ）
        Time.timeScale = 0.35f;
        yield return new WaitForSecondsRealtime(suspenseHoldTime);
        Time.timeScale = 1f;

        // ⑦ ドラムロール（任意）
        if (sfxSource && drumRollClip) sfxSource.PlayOneShot(drumRollClip);

        // ⑧ 静寂（ここが一番大事）
        yield return new WaitForSecondsRealtime(silenceTime);

        // ===== ドン！ =====
        if (sfxSource && revealClip) sfxSource.PlayOneShot(revealClip);
        StartCoroutine(Flash());
        StartCoroutine(CameraShake());

        bool hasWrong = gs.AnyWrong();

        if (hasWrong)
        {      
            PlayLightning(wrongSide);
            if (sfxSource && wrongClip) sfxSource.PlayOneShot(wrongClip);
        }
        else
        {
            // 全員正解：花火
            PlayFireworks(correctSide);
            if (sfxSource && correctClip) sfxSource.PlayOneShot(correctClip);
        }

        // ⑪ ドンの後、少し遅れて結果テキストを出す
        yield return new WaitForSecondsRealtime(textDelayAfterReveal);
        ShowJudgeText(correctSide);

        if (hasWrong)
        {
            // 表示された“不正解”を粉砕する（表示直後が一番効く）
            yield return new WaitForEndOfFrame();
            PlayShatterOnWrongSide(wrongSide);
        }

        // ⑫ BGMを戻す
        if (BGMManager.I != null)
            BGMManager.I.FadeVolume(normalBgmVolume, bgmReturnTime);

        // ⑬ 走行演出（線路スクロールがあるならON）
        if (leftTrackScroller) leftTrackScroller.enabled = true;
        if (rightTrackScroller) rightTrackScroller.enabled = true;

        yield return new WaitForSeconds(runDuration);

        // ⑭ 走行停止（任意）
        if (leftTrackScroller) leftTrackScroller.enabled = false;
        if (rightTrackScroller) rightTrackScroller.enabled = false;

        // ⑮ 不正解チームを脱落（必要なら）
        if (eliminateWrongTeams && hasWrong)
        {
            gs.EliminateWrongTeams();
        }

        // ⑯ 次へ
        gs.NextQuestion();

        if (gs.quizFinished)
            SceneManager.LoadScene("ResultScene");
        else
            SceneManager.LoadScene("RunScene");
    }

    void ClearJudgeText()
    {
        if (leftJudgeText) leftJudgeText.text = "";
        if (rightJudgeText) rightJudgeText.text = "";
    }

    void ShowJudgeText(int correctSide)
    {
        if (leftJudgeText)
        {
            bool isCorrect = (correctSide == 0);
            leftJudgeText.text = isCorrect ? "正解！" : "不正解";
            leftJudgeText.color = isCorrect ? Color.red : Color.blue;
        }

        if (rightJudgeText)
        {
            bool isCorrect = (correctSide == 1);
            rightJudgeText.text = isCorrect ? "正解！" : "不正解";
            rightJudgeText.color = isCorrect ? Color.red : Color.blue;
        }
    }

    void PlayShatterOnWrongSide(int wrongSide)
    {
        // 不正解テキストを粉砕（出すなら、テキスト表示後にPlayでもOK）
        // 今は「ドン！」と同時に粉砕したいので、あえて表示前でもPlayしている
        // もし表示後に粉砕したいなら、ShowJudgeText() の後に呼ぶ
        if (wrongSide == 0 && leftJudgeText)
        {
            var sh = leftJudgeText.GetComponent<TextShatterTMP>();
            if (sh) sh.Play();
        }
        if (wrongSide == 1 && rightJudgeText)
        {
            var sh = rightJudgeText.GetComponent<TextShatterTMP>();
            if (sh) sh.Play();
        }
    }

    void SpawnSortedTrolleysByAnswer()
    {
        var gs = GameState.I;
        if (gs == null) return;

        // 既存削除
        foreach (var t in leftTrolleys) if (t) Destroy(t);
        foreach (var t in rightTrolleys) if (t) Destroy(t);
        leftTrolleys.Clear();
        rightTrolleys.Clear();

        // 左右に分類（生存チームのみ）
        List<int> leftTeams = new();
        List<int> rightTeams = new();

        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (!gs.teamAlive[team]) continue;

            // teamAnswers: 0=Left(A), 1=Right(D)
            if (gs.teamAnswers[team] == 0) leftTeams.Add(team);
            else rightTeams.Add(team);
        }

        // スコア降順、同点はチーム番号昇順
        leftTeams.Sort((a, b) =>
        {
            int diff = gs.teamScore[b].CompareTo(gs.teamScore[a]);
            if (diff != 0) return diff;
            return a.CompareTo(b);
        });

        rightTeams.Sort((a, b) =>
        {
            int diff = gs.teamScore[b].CompareTo(gs.teamScore[a]);
            if (diff != 0) return diff;
            return a.CompareTo(b);
        });

        // 中央寄せで並べる
        SpawnLane(leftTeams, leftSpawnPoint, "Left", leftTrolleys, flagLeft: true);
        SpawnLane(rightTeams, rightSpawnPoint, "Right", rightTrolleys, flagLeft: false);
    }

    void SpawnLane(List<int> teams, Transform spawnPoint, string laneName, List<GameObject> store, bool flagLeft)
    {
        var gs = GameState.I;
        if (gs == null) return;
        if (spawnPoint == null || trolleyPrefab == null) return;

        int count = teams.Count;
        if (count <= 0) return;

        float total = (count - 1) * trolleySpacingZ;
        float startOffset = total / 2f;

        for (int i = 0; i < count; i++)
        {
            int team = teams[i];

            float offsetZ = startOffset - i * trolleySpacingZ;

            var go = Instantiate(trolleyPrefab);
            go.name = $"Team{team + 1}_{laneName}";
            go.transform.position = spawnPoint.position + new Vector3(0f, 0f, offsetZ);

            var tv = go.GetComponent<TrolleyVisual>();
            if (tv != null) tv.SetVisualYaw(trolleyYaw);

            var ms = go.GetComponent<TrolleyModelSwitch>();
            if (ms != null) ms.SetFlagLeft(flagLeft);

            PrepareStatic(go);
            ApplyTeamColor(go, gs.teamColors[team]);

            store.Add(go);
        }
    }

    void PlayLightning(int wrongSide)
    {
        Transform p = (wrongSide == 0) ? leftLightningPoint : rightLightningPoint;

        if (lightningPrefab != null && p != null)
        {
            var fx = Instantiate(lightningPrefab, p.position, p.rotation);
            if (lightningAutoDestroy > 0f) Destroy(fx, lightningAutoDestroy);
        }

        if (lightningAudio != null)
        {
            lightningAudio.Play();
        }
    }

    void PlayFireworks(int correctSide)
    {
        Transform p = (correctSide == 0) ? leftFireworksPoint : rightFireworksPoint;

        if (fireworksPrefab != null && p != null)
        {
            var fx = Instantiate(fireworksPrefab, p.position, p.rotation);
            if (fireworksAutoDestroy > 0f) Destroy(fx, fireworksAutoDestroy);
        }

        if (fireworksAudio != null)
        {
            fireworksAudio.Play();
        }
    }

    void ApplyTeamColor(GameObject go, Color c)
    {
        var visual = go.GetComponent<TrolleyVisual>();
        if (visual != null)
        {
            visual.SetColor(c);
            return;
        }

        var r = go.GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = c;
    }

    void PrepareStatic(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    IEnumerator Flash()
    {
        if (flashPanel == null) yield break;

        flashPanel.alpha = 0f;

        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            flashPanel.alpha = Mathf.Lerp(0f, 1f, t / flashTime);
            yield return null;
        }

        t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            flashPanel.alpha = Mathf.Lerp(1f, 0f, t / flashTime);
            yield return null;
        }

        flashPanel.alpha = 0f;
    }

    IEnumerator CameraShake()
    {
        if (cameraRoot == null) yield break;

        Vector3 basePos = cameraRoot.localPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            cameraRoot.localPosition = basePos + (Random.insideUnitSphere * shakeStrength);
            yield return null;
        }

        cameraRoot.localPosition = basePos;
    }
}