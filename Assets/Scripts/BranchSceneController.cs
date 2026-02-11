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

    [Header("Spawn")]
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    public GameObject trolleyPrefab;
    public float trolleySpacingZ = 2.0f;

    [Header("UI")]
    public TextMeshProUGUI leftJudgeText;
    public TextMeshProUGUI rightJudgeText;

    [Header("Timing")]
    public float runDuration = 3.0f;

    [Header("Rule")]
    [Tooltip("不正解チームを脱落扱いにするならON（次の問題で表示しなくなる）")]
    public bool eliminateWrongTeams = false;

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

        // ★その回の正解側（ランダム入れ替え後）を見る
        int correctSide = gs.runtimeCorrect; // 0=Left(A), 1=Right(D)

        // ① 正解チームのスコア+1（runtimeCorrectで判定される前提）
        gs.ApplyScoringForCurrentQuestion();

        // ② 左右にトロッコ生成（生存チームのみ・スコア順）
        SpawnSortedTrolleysByAnswer();

        // ③ 正解/不正解 表示（ランダム左右に合わせる）
        if (leftJudgeText)  leftJudgeText.text  = (correctSide == 0) ? "正解！" : "不正解";
        if (rightJudgeText) rightJudgeText.text = (correctSide == 1) ? "正解！" : "不正解";

        // ④ 走行演出（線路スクロールがあるならON）
        if (leftTrackScroller) leftTrackScroller.enabled = true;
        if (rightTrackScroller) rightTrackScroller.enabled = true;

        yield return new WaitForSeconds(runDuration);

        // ⑤ 走行停止（任意）
        if (leftTrackScroller) leftTrackScroller.enabled = false;
        if (rightTrackScroller) rightTrackScroller.enabled = false;

        // ⑥ 不正解チームを脱落（必要なら）
        if (eliminateWrongTeams && gs.AnyWrong())
        {
            gs.EliminateWrongTeams();
        }

        // ⑦ 次の問題へ戻る
        gs.NextQuestion();
        SceneManager.LoadScene("RunScene");
    }

    void SpawnSortedTrolleysByAnswer()
    {
        var gs = GameState.I;

        foreach (var t in leftTrolleys) if (t) Destroy(t);
        foreach (var t in rightTrolleys) if (t) Destroy(t);
        leftTrolleys.Clear();
        rightTrolleys.Clear();

        List<int> leftTeams = new();
        List<int> rightTeams = new();

        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (!gs.teamAlive[team]) continue;

            // teamAnswers は「その回の表示のLeft/Right」(0/1)
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

        // 左生成
        for (int i = 0; i < leftTeams.Count; i++)
        {
            int team = leftTeams[i];
            var go = Instantiate(trolleyPrefab);
            go.name = $"Team{team + 1}_Left";
            go.transform.position = leftSpawnPoint.position + new Vector3(0f, 0f, -i * trolleySpacingZ);
            PrepareStatic(go);
            ApplyTeamColor(go, gs.teamColors[team]);
            leftTrolleys.Add(go);
        }

        // 右生成
        for (int i = 0; i < rightTeams.Count; i++)
        {
            int team = rightTeams[i];
            var go = Instantiate(trolleyPrefab);
            go.name = $"Team{team + 1}_Right";
            go.transform.position = rightSpawnPoint.position + new Vector3(0f, 0f, -i * trolleySpacingZ);
            PrepareStatic(go);
            ApplyTeamColor(go, gs.teamColors[team]);
            rightTrolleys.Add(go);
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
}
