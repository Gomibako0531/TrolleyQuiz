using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunSceneController : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI choiceAText; // 任意
    public TextMeshProUGUI choiceBText;
    public TextMeshProUGUI timerText;

    [Header("UI Image Choices (optional)")]
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public Image leftChoiceImage;
    public Image rightChoiceImage;

    [Tooltip("任意：いまどのチーム入力中か表示")]
    public TextMeshProUGUI currentTeamText;

    [Header("Trolleys (Team1..Team7)")]
    public TrolleyTilt[] trolleyTilts = new TrolleyTilt[GameState.TeamCount];
    public TrolleyVisual[] trolleyVisuals = new TrolleyVisual[GameState.TeamCount];

    [Header("Keys")]
    public KeyCode leftKey = KeyCode.A;     // 左
    public KeyCode rightKey = KeyCode.D;    // 右
    public KeyCode proceedKey = KeyCode.Space;

    [Header("Rank Order In RunScene")]
    [Tooltip("RunSceneに入ったらスコア順で見た目を並べ替える")]
    public bool rankOrderByScore = true;

    // RunSceneに最初から置いてある7台の「先頭→最後尾」のスロット位置
    Vector3[] laneSlots;

    // その問題での「入力順（=先頭→最後尾の順）」に並んだチーム番号リスト
    List<int> rankingTeams = new();

    // 入力中のインデックス（rankingTeamsの何番目を入力しているか）
    int inputRankIndex = 0;

    void Start()
    {
        if (GameState.I == null)
        {
            var go = new GameObject("GameState");
            go.AddComponent<GameState>();
        }

        // ① スロット（先頭→最後尾の位置）を記録
        CacheLaneSlotsIfNeeded();

        // ② チーム色適用
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyVisuals != null && i < trolleyVisuals.Length && trolleyVisuals[i] != null)
                trolleyVisuals[i].SetColor(GameState.I.teamColors[i]);
        }

        // ③ 今回の「入力順（ランキング順）」を作る
        BuildRankingTeams();

        // ④ 見た目をランキング順に並べ替え
        if (rankOrderByScore)
            ArrangeTrolleysByRanking();

        // ⑤ 新しい問題を開始（60秒 + 未回答に戻す）
        GameState.I.StartQuestionTimer();

        // ⑥ 問題表示
        ShowQuestion();

        // ⑦ 傾きリセット
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyTilts != null && i < trolleyTilts.Length && trolleyTilts[i] != null)
                trolleyTilts[i].SetChoiceVisual(-1);
        }

        // ⑧ 画像ボタン入力があるなら登録
        if (leftChoiceButton != null) leftChoiceButton.onClick.AddListener(ChooseLeft);
        if (rightChoiceButton != null) rightChoiceButton.onClick.AddListener(ChooseRight);

        // ⑨ 最初の入力対象（ランキング先頭）
        inputRankIndex = FindNextUnansweredRankIndex(0);
        UpdateCurrentTeamUI();
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null) return;

        // タイマー
        if (gs.accepting && gs.timeLeft > 0f)
        {
            gs.timeLeft -= Time.deltaTime;
            if (gs.timeLeft <= 0f)
            {
                gs.timeLeft = 0f;
                gs.accepting = false;
            }
        }
        if (timerText != null)
            timerText.text = $"Time: {Mathf.CeilToInt(gs.timeLeft)}";

        // 入力（A/D）
        if (gs.accepting)
        {
            inputRankIndex = FindNextUnansweredRankIndex(inputRankIndex);

            if (Input.GetKeyDown(leftKey)) Choose(0);
            if (Input.GetKeyDown(rightKey)) Choose(1);
        }

        // 全員入力完了 or 締切後に Space → BranchScene
        if (Input.GetKeyDown(proceedKey))
        {
            if (gs.AllAnsweredAliveTeams() || !gs.accepting)
            {
                gs.accepting = false;
                SceneManager.LoadScene("BranchScene");
            }
        }

        UpdateCurrentTeamUI();
    }

    void ShowQuestion()
{
    var gs = GameState.I;
    var q = gs.CurrentQ;
    if (q == null) return;

    if (questionText != null) questionText.text = q.questionText;

    // ★表示は runtime を使う（毎回ランダムに左右が入れ替わってる）
    if (choiceAText != null) choiceAText.text = string.IsNullOrEmpty(gs.runtimeLeftText) ? "" : ("A: " + gs.runtimeLeftText);
    if (choiceBText != null) choiceBText.text = string.IsNullOrEmpty(gs.runtimeRightText) ? "" : ("D: " + gs.runtimeRightText);

    if (leftChoiceImage != null && gs.runtimeLeftImage != null) leftChoiceImage.sprite = gs.runtimeLeftImage;
    if (rightChoiceImage != null && gs.runtimeRightImage != null) rightChoiceImage.sprite = gs.runtimeRightImage;
}


    public void ChooseLeft()  => Choose(0);
    public void ChooseRight() => Choose(1);

    void Choose(int choice)
    {
        var gs = GameState.I;
        if (!gs.accepting) return;

        inputRankIndex = FindNextUnansweredRankIndex(inputRankIndex);
        if (inputRankIndex < 0 || inputRankIndex >= rankingTeams.Count) return;

        int team = rankingTeams[inputRankIndex];
        SetTeamAnswer(team, choice);

        // 次の入力へ（ランキングの次へ）
        inputRankIndex++;
        inputRankIndex = FindNextUnansweredRankIndex(inputRankIndex);
    }

    void SetTeamAnswer(int teamIndex, int choice)
    {
        var gs = GameState.I;
        if (!gs.teamAlive[teamIndex]) return;

        gs.teamAnswers[teamIndex] = choice;

        // 傾き（左=+30°, 右=-30°）
        if (trolleyTilts != null && teamIndex < trolleyTilts.Length && trolleyTilts[teamIndex] != null)
            trolleyTilts[teamIndex].SetChoiceVisual(choice);
    }

    int FindNextUnansweredRankIndex(int startRank)
    {
        var gs = GameState.I;
        if (rankingTeams == null || rankingTeams.Count == 0) return 0;

        if (startRank < 0) startRank = 0;
        if (startRank >= rankingTeams.Count) startRank = rankingTeams.Count - 1;

        for (int i = startRank; i < rankingTeams.Count; i++)
        {
            int team = rankingTeams[i];
            if (!gs.teamAlive[team]) continue;          // 念のため
            if (gs.teamAnswers[team] == -1) return i;   // 未回答ならここ
        }

        // もう未回答が無い
        return rankingTeams.Count; // 範囲外扱い
    }

    void UpdateCurrentTeamUI()
    {
        if (currentTeamText == null) return;

        var gs = GameState.I;
        if (gs.AllAnsweredAliveTeams())
        {
            currentTeamText.text = "全員入力完了！Spaceで発表へ";
            return;
        }

        int idx = FindNextUnansweredRankIndex(inputRankIndex);
        if (idx >= 0 && idx < rankingTeams.Count)
        {
            int team = rankingTeams[idx];
            currentTeamText.text = $"入力中：Team {team + 1}（ランキング順）";
        }
        else
        {
            currentTeamText.text = "未回答チームなし（Spaceで発表へ）";
        }
    }

    // ===== ランキング順の作成＆並べ替え =====

    void BuildRankingTeams()
    {
        var gs = GameState.I;
        rankingTeams = new List<int>(GameState.TeamCount);

        for (int team = 0; team < GameState.TeamCount; team++)
        {
            // 「間違えても参加」なら alive は常にtrueの想定だけど、
            // 将来脱落ルールに戻しても動くようにaliveチェックは残す
            if (!gs.teamAlive[team]) continue;
            rankingTeams.Add(team);
        }

        // スコア降順、同点はチーム番号昇順
        rankingTeams.Sort((a, b) =>
        {
            int diff = gs.teamScore[b].CompareTo(gs.teamScore[a]);
            if (diff != 0) return diff;
            return a.CompareTo(b);
        });
    }

    void CacheLaneSlotsIfNeeded()
    {
        if (laneSlots != null && laneSlots.Length == GameState.TeamCount) return;

        // 今RunSceneに置いてある7台の位置を「スロット」として記録
        // 先頭→最後尾になるように Z の大きい順で並べる
        var list = new List<(float z, Vector3 pos)>(GameState.TeamCount);
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyTilts == null || i >= trolleyTilts.Length || trolleyTilts[i] == null) continue;
            var t = trolleyTilts[i].transform;
            list.Add((t.position.z, t.position));
        }
        list.Sort((a, b) => b.z.CompareTo(a.z)); // Z大きい＝先頭（逆ならここを反転）

        laneSlots = new Vector3[GameState.TeamCount];
        for (int i = 0; i < GameState.TeamCount; i++)
            laneSlots[i] = list[i].pos;
    }

    void ArrangeTrolleysByRanking()
    {
        // rankingTeams[0] のチームを laneSlots[0]（先頭）へ、…という風に置く
        for (int rank = 0; rank < rankingTeams.Count; rank++)
        {
            int team = rankingTeams[rank];
            if (trolleyTilts[team] == null) continue;
            trolleyTilts[team].transform.position = laneSlots[rank];
        }
    }
}
