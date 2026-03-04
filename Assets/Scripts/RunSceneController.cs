using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunSceneController : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI choiceAText; 
    public TextMeshProUGUI choiceBText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI currentTeamText;

    [Header("UI Image Choices (optional)")]
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public Image leftChoiceImage;
    public Image rightChoiceImage;

    [Header("Trolleys (Team1..Team7)")]
    public TrolleyTilt[] trolleyTilts = new TrolleyTilt[GameState.TeamCount];
    public TrolleyVisual[] trolleyVisuals = new TrolleyVisual[GameState.TeamCount];

    [Header("Keys")]
    public KeyCode leftKey = KeyCode.A;     // 左
    public KeyCode rightKey = KeyCode.D;    // 右
    public KeyCode proceedKey = KeyCode.Space;

    [Header("RunScene Order")]
    [Tooltip("見た目（位置）をスコア順に並べ替える")]
    public bool rankOrderByScore = true;

    // ★ 追加：RunScene用のトロッコ向き
    [Header("Trolley Visual Angle (RunScene)")]
    [Tooltip("RunSceneでのトロッコのYaw（通常は 0）")]
    public float trolleyYaw = 0f;

    // ★ 追加：モデル切替コンポーネント配列
    [Header("Trolley Model Switch (旗左↔旗右)")]
    [Tooltip("各チームのTrolleyModelSwitchを Team1..Team8 の順に入れる")]
    public TrolleyModelSwitch[] trolleyModelSwitches = new TrolleyModelSwitch[GameState.TeamCount];


    [Tooltip("入力順を『最初の並び』に固定する（おすすめON）")]
    public bool inputByInitialOrder = true;

    // ★ 元のテキスト位置を保存
    Vector2 originalLeftTextPos;
    Vector2 originalRightTextPos;
    bool textPositionCached = false;

    // RunSceneの“先頭→最後尾”スロット位置
    Vector3[] laneSlots;

    // ★見た目用：スコア順
    List<int> rankingTeams = new();

    // ★入力用：初期順
    List<int> inputTeams = new();

    // 入力中の index（inputTeams の何番目か）
    int inputIndex = 0;

    // 「初期順」を一度だけ決めて保持これはシーンに戻っても同じ順を使う
    static List<int> cachedInitialOrder;

    void Start()
    {

        // ① スロット記録（先頭→最後尾の座標）
        CacheLaneSlots();

        // ② チーム色を適用
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyVisuals != null && i < trolleyVisuals.Length && trolleyVisuals[i] != null)
                trolleyVisuals[i].SetColor(GameState.I.teamColors[i]);
        }

        // ③ 入力順（初期順）を作る：一度だけ決める
        BuildInputTeamsInitialOrder();

        // ④ 見た目用ランキング順を作る
        BuildRankingTeams();

        // ⑤ 見た目をランキング順に並べ替え
        if (rankOrderByScore)
            ArrangeTrolleysByRanking();

        // ★ ⑤-a Visual Yaw を全トロッコに適用（RunScene用の向き）
        ApplyVisualYaw();

        // ★ ⑤-b 前から旗左→旗右→旗左… と交互にモデルを切替
        ApplyAlternatingModels();

        // ⑥ 問題開始（60秒 + 未回答に戻す）
        GameState.I.StartQuestionTimer();

        // ⑦ 問題表示
        ShowQuestion();

        // ⑧ 傾きリセット
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyTilts != null && i < trolleyTilts.Length && trolleyTilts[i] != null)
                trolleyTilts[i].SetChoiceVisual(-1);
        }

        // ⑨ 画像ボタン入力登録
        if (leftChoiceButton != null) leftChoiceButton.onClick.AddListener(() => Choose(0));
        if (rightChoiceButton != null) rightChoiceButton.onClick.AddListener(() => Choose(1));

        // ⑩ 最初の入力対象
        inputIndex = FindNextUnansweredInputIndex(0);
        UpdateCurrentTeamUI();
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null) return;

        // タイマー更新
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
            inputIndex = FindNextUnansweredInputIndex(inputIndex);

            if (Input.GetKeyDown(leftKey)) Choose(0);
            if (Input.GetKeyDown(rightKey)) Choose(1);
        }

        // Space → BranchScene
        if (Input.GetKeyDown(proceedKey))
        {
            if (gs.AllAnsweredAliveTeams() || !gs.accepting)
            {
                gs.accepting = false;
                SceneManager.LoadScene("SeparateScene");
            }
        }

        UpdateCurrentTeamUI();
    }

    void Choose(int choice)
    {
        var gs = GameState.I;
        if (!gs.accepting) return;

        inputIndex = FindNextUnansweredInputIndex(inputIndex);
        if (inputIndex < 0 || inputIndex >= inputTeams.Count) return;

        int team = inputTeams[inputIndex];
        gs.teamAnswers[team] = choice;

        if (trolleyTilts != null && team < trolleyTilts.Length && trolleyTilts[team] != null)
            trolleyTilts[team].SetChoiceVisual(choice);

        // 次の入力へ
        inputIndex++;
        inputIndex = FindNextUnansweredInputIndex(inputIndex);
    }

    int FindNextUnansweredInputIndex(int start)
    {
        var gs = GameState.I;
        if (inputTeams == null || inputTeams.Count == 0) return 0;

        if (start < 0) start = 0;
        if (start >= inputTeams.Count) start = inputTeams.Count - 1;

        for (int i = start; i < inputTeams.Count; i++)
        {
            int team = inputTeams[i];
            if (!gs.teamAlive[team]) continue;         // 将来脱落に戻っても対応
            if (gs.teamAnswers[team] == -1) return i;  // 未回答
        }
        return inputTeams.Count; // 全員回答済み
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

        int idx = FindNextUnansweredInputIndex(inputIndex);
        if (idx >= 0 && idx < inputTeams.Count)
        {
            int team = inputTeams[idx];
            currentTeamText.text = $"入力中：Team {team + 1}（初期順）";
        }
        else
        {
            currentTeamText.text = "未回答チームなし（Spaceで発表へ）";
        }
    }
    void ShowQuestion()
{
    var gs = GameState.I;
    var q = gs.CurrentQ;
    if (q == null) return;

    if (questionText != null) questionText.text = q.questionText;

    bool hasLeftImg = gs.runtimeLeftImage != null;
    bool hasRightImg = gs.runtimeRightImage != null;

    bool hasLeftText = !string.IsNullOrWhiteSpace(gs.runtimeLeftText);
    bool hasRightText = !string.IsNullOrWhiteSpace(gs.runtimeRightText);

    // ★ 最初に一度だけ元の位置を保存
    if (!textPositionCached)
    {
        if (choiceAText != null)
            originalLeftTextPos = choiceAText.rectTransform.anchoredPosition;

        if (choiceBText != null)
            originalRightTextPos = choiceBText.rectTransform.anchoredPosition;

        textPositionCached = true;
    }

    // =========================
    // 左側
    // =========================
    if (leftChoiceImage != null)
    {
        leftChoiceImage.gameObject.SetActive(hasLeftImg);
        if (hasLeftImg) leftChoiceImage.sprite = gs.runtimeLeftImage;
    }

    if (choiceAText != null)
    {
        choiceAText.gameObject.SetActive(hasLeftText);
        choiceAText.text = gs.runtimeLeftText ?? "";

        RectTransform rt = choiceAText.rectTransform;

        if (hasLeftImg && hasLeftText)
        {
            // ★ 両方ある → Yを+100
            rt.anchoredPosition = originalLeftTextPos + new Vector2(0, 125f);
        }
        else
        {
            // ★ どちらか片方 → 元に戻す
            rt.anchoredPosition = originalLeftTextPos;
        }
    }

    // =========================
    // 右側
    // =========================
    if (rightChoiceImage != null)
    {
        rightChoiceImage.gameObject.SetActive(hasRightImg);
        if (hasRightImg) rightChoiceImage.sprite = gs.runtimeRightImage;
    }

    if (choiceBText != null)
    {
        choiceBText.gameObject.SetActive(hasRightText);
        choiceBText.text = gs.runtimeRightText ?? "";

        RectTransform rt = choiceBText.rectTransform;

        if (hasRightImg && hasRightText)
        {
            // ★ 両方ある → Yを+100
            rt.anchoredPosition = originalRightTextPos + new Vector2(0, 125f);
        }
        else
        {
            rt.anchoredPosition = originalRightTextPos;
        }
    }
}

    void BuildInputTeamsInitialOrder()
    {
        if (!inputByInitialOrder)
        {
            inputTeams = new List<int>(rankingTeams);
            return;
        }

        if (cachedInitialOrder != null && cachedInitialOrder.Count == GameState.TeamCount)
        {
            inputTeams = new List<int>(cachedInitialOrder);
            return;
        }

        // 初回：RunSceneに置いてあるトロッコの前から後を初期順として記録
        var list = new List<(float z, int team)>(GameState.TeamCount);
        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (trolleyTilts == null || team >= trolleyTilts.Length || trolleyTilts[team] == null) continue;
            list.Add((trolleyTilts[team].transform.position.z, team));
        }
        list.Sort((a, b) => b.z.CompareTo(a.z)); // Z大きい＝先頭（逆ならここを反転）

        cachedInitialOrder = new List<int>(GameState.TeamCount);
        foreach (var item in list) cachedInitialOrder.Add(item.team);

        inputTeams = new List<int>(cachedInitialOrder);
    }

    void BuildRankingTeams()
    {
        var gs = GameState.I;
        rankingTeams = new List<int>(GameState.TeamCount);

        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (!gs.teamAlive[team]) continue;
            rankingTeams.Add(team);
        }

        rankingTeams.Sort((a, b) =>
        {
            int diff = gs.teamScore[b].CompareTo(gs.teamScore[a]);
            if (diff != 0) return diff;
            return a.CompareTo(b);
        });
    }

    void CacheLaneSlots()
    {
        // 現在の並び前から後を「スロット座標」として記録
        var list = new List<(float z, Vector3 pos)>(GameState.TeamCount);
        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (trolleyTilts == null || team >= trolleyTilts.Length || trolleyTilts[team] == null) continue;
            list.Add((trolleyTilts[team].transform.position.z, trolleyTilts[team].transform.position));
        }
        list.Sort((a, b) => b.z.CompareTo(a.z));

        laneSlots = new Vector3[GameState.TeamCount];
        for (int i = 0; i < list.Count; i++)
            laneSlots[i] = list[i].pos;
    }

    void ArrangeTrolleysByRanking()
    {
        // rankingTeams[0] を laneSlots[0]（先頭）へ、…
        for (int rank = 0; rank < rankingTeams.Count; rank++)
        {
            int team = rankingTeams[rank];
            if (trolleyTilts[team] == null) continue;
            trolleyTilts[team].transform.position = laneSlots[rank];
        }
    }

    /// <summary>
    /// RunScene用のYawを全トロッコのVisualに適用する。
    /// TrolleyTiltのbaseRotは Awake() で記録されるので、
    /// TrolleyVisual.SetVisualYaw は visualRoot（子）だけを回すため競合しない。
    /// </summary>
    void ApplyVisualYaw()
    {
        for (int i = 0; i < GameState.TeamCount; i++)
        {
            if (trolleyVisuals == null || i >= trolleyVisuals.Length) continue;
            if (trolleyVisuals[i] == null) continue;
            trolleyVisuals[i].SetVisualYaw(trolleyYaw);
        }
    }

    /// <summary>
    /// rankingTeams の順番（= laneSlots の前後順）で
    /// 旗左 → 旗右 → 旗左 … と交互にモデルを切り替える。
    ///
    /// rank 0（先頭） → 旗左（FlagLeft）
    /// rank 1         → 旗右（FlagRight）
    /// rank 2         → 旗左
    /// ...
    /// </summary>
    void ApplyAlternatingModels()
    {
        if (trolleyModelSwitches == null) return;

        for (int rank = 0; rank < rankingTeams.Count; rank++)
        {
            int team = rankingTeams[rank];

            if (team >= trolleyModelSwitches.Length) continue;
            if (trolleyModelSwitches[team] == null) continue;

            bool useFlagLeft = (rank % 2 == 0); // 偶数rank = 旗左
            trolleyModelSwitches[team].SetFlagLeft(useFlagLeft);
        }
    }

}
