using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;  

public class ResultSceneController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI centerTitleText; // 「結果発表」
    public TextMeshProUGUI rank1Text;
    public TextMeshProUGUI rank2Text;
    public TextMeshProUGUI rank3Text;

    [Header("Input")]
    public KeyCode proceedKey = KeyCode.Space;


    [Header("Result BGM")]
    public AudioClip resultBgm;

    [Header("Rank Reveal SFX")]
    public AudioSource sfxSource;     // 太鼓を鳴らすAudioSource（PlayOnAwake OFF推奨）
    public AudioClip taikoClip;       // 太鼓SE
    public float taikoVolume = 1.0f;  // 音量

    [Header("Celebrate FX")]
public AudioSource sfxSourceFinal;
public AudioClip applauseClip;
public AudioClip cheerClip;
public TextMeshProUGUI congratsText;

[Header("Confetti")]
public GameObject confettiPrefab;
public Transform confettiSpawnPoint;
public float confettiAutoDestroy = 6f;

[Header("Celebrate Timing")]
public float cheerDelay = 0.4f;
public float celebrateDuration = 4.0f;

bool celebrated = false; // 1回だけ発動ガード

    // 表示ステップ
    // 0: 結果発表のみ
    // 1: Rank3表示
    // 2: Rank2表示
    // 3: Rank1表示
    int step = 0;

    // 表示する内容を事前に作っておく
    string rank1Str, rank2Str, rank3Str;

    void Start()
    {
        if (BGMManager.I != null && resultBgm != null)
        {
            BGMManager.I.FadeToBgm(resultBgm, 0.8f);
        }
        var gs = GameState.I;

        if (centerTitleText) centerTitleText.text = "結果発表";

        // 最初はタイトルだけ
        if (centerTitleText) centerTitleText.gameObject.SetActive(true);
        if (rank1Text) rank1Text.gameObject.SetActive(false);
        if (rank2Text) rank2Text.gameObject.SetActive(false);
        if (rank3Text) rank3Text.gameObject.SetActive(false);

        if (gs == null)
        {
            // GameStateがない場合でも動作はする（表示だけ）
            rank1Str = "GameState が見つかりません";
            rank2Str = "";
            rank3Str = "";
            return;
        }

        // スコア順に並べる
        List<int> teams = new List<int>();
        for (int i = 0; i < GameState.TeamCount; i++)
            teams.Add(i);

        teams.Sort((a, b) =>
        {
            int diff = gs.teamScore[b].CompareTo(gs.teamScore[a]);
            if (diff != 0) return diff;
            return a.CompareTo(b);
        });

        // 同点対応順位（1位・1位・3位）
        List<(int team, int rank)> rankedList = new();
        int currentRank = 1;

        for (int i = 0; i < teams.Count; i++)
        {
            if (i > 0)
            {
                int prevScore = gs.teamScore[teams[i - 1]];
                int curScore = gs.teamScore[teams[i]];
                if (curScore < prevScore) currentRank = i + 1;
            }
            rankedList.Add((teams[i], currentRank));
        }

        // 上位3件（表示文字だけ準備）
        rank1Str = MakeRankLine(rankedList, gs, index: 0);
        rank2Str = MakeRankLine(rankedList, gs, index: 1);
        rank3Str = MakeRankLine(rankedList, gs, index: 2);
    }

    void Update()
    {
        if (Input.GetKeyDown(proceedKey))
        {
            // ★ Space押下ごとに太鼓
            if (sfxSource != null && taikoClip != null)
                sfxSource.PlayOneShot(taikoClip, taikoVolume);
            step++;
            ApplyStep();
        }
    }

    void ApplyStep()
    {
        // step 0はStart時の状態なので、ここでは1以降を扱う
        if (step == 1)
        {
            // Rank3
            if (rank3Text)
            {
                rank3Text.text = rank3Str;
                rank3Text.gameObject.SetActive(true);
            }
        }
        else if (step == 2)
        {
            // Rank2
            if (rank2Text)
            {
                rank2Text.text = rank2Str;
                rank2Text.gameObject.SetActive(true);
            }
        }
        else if (step == 3)
        {
            // Rank1
            if (rank1Text)
            {
                rank1Text.text = rank1Str;
                rank1Text.gameObject.SetActive(true);
            }

            // タイトルは消す（残したいならこの行を消す）
            if (centerTitleText) centerTitleText.gameObject.SetActive(false);
            if (!celebrated)
            {
                celebrated = true;
                StartCoroutine(CelebrationRoutine());
            }
        }
        else
        {
            // さらにSpaceが押されたら何もしない（必要ならリスタート等）
            // 例：SceneManager.LoadScene("RunScene");
        }
    }

    string MakeRankLine(List<(int team, int rank)> rankedList, GameState gs, int index)
    {
        if (rankedList == null || rankedList.Count <= index) return "";
        int team = rankedList[index].team;
        int rank = rankedList[index].rank;
        int score = gs.teamScore[team];
        string teamS = "";

        if(rankedList[index].team == 0) teamS = "緑色";
        if(rankedList[index].team == 1) teamS = "ピンク色";
        if(rankedList[index].team == 2) teamS = "橙色";
        if(rankedList[index].team == 3) teamS = "青色";
        if(rankedList[index].team == 4) teamS = "水色";
        if(rankedList[index].team == 5) teamS = "赤色";
        if(rankedList[index].team == 6) teamS = "黄色";
        if(rankedList[index].team == 7) teamS = "紫色";


        return $"{rank}位： {teamS}（{score}点）";
    }

    IEnumerator CelebrationRoutine()
{
    // ① Congrats表示（ポップ）
    if (congratsText)
    {
        congratsText.gameObject.SetActive(true);
        congratsText.alpha = 0f;
        congratsText.transform.localScale = Vector3.one * 0.8f;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float k = t / 0.25f;
            congratsText.alpha = Mathf.Lerp(0f, 1f, k);
            congratsText.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 1.08f, k);
            yield return null;
        }

        // 少し戻す
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float k = t / 0.12f;
            congratsText.transform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, k);
            yield return null;
        }
        congratsText.alpha = 1f;
        congratsText.transform.localScale = Vector3.one;
    }

    // ② 拍手
    if (sfxSourceFinal && applauseClip) sfxSourceFinal.PlayOneShot(applauseClip);

    // ③ 紙吹雪（ここでPrefab生成）
    if (confettiPrefab != null)
    {
        Vector3 pos = confettiSpawnPoint ? confettiSpawnPoint.position : Vector3.zero;
        Quaternion rot = confettiSpawnPoint ? confettiSpawnPoint.rotation : Quaternion.identity;

        var fx = Instantiate(confettiPrefab, pos, rot);

        if (confettiAutoDestroy > 0f)
            Destroy(fx, confettiAutoDestroy);
    }

    // ④ 少し遅れて歓声（テレビっぽい）
    yield return new WaitForSeconds(cheerDelay);
    if (sfxSourceFinal && cheerClip) sfxSourceFinal.PlayOneShot(cheerClip);

    // ⑤ しばらく祝福を見せる
    yield return new WaitForSeconds(celebrateDuration);

    // ⑥ テキストを消す（残したければこのブロック削除）
    if (congratsText)
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            congratsText.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
            yield return null;
        }
        congratsText.alpha = 0f;
        congratsText.gameObject.SetActive(false);
    }
}
}