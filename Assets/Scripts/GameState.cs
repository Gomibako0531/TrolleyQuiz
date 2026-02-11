using UnityEngine;

/// <summary>
/// 全シーン共通のゲーム状態（Singleton）
/// ・チーム数：7
/// ・制限時間：60秒
/// ・回答：0=Left(A), 1=Right(D), -1=未回答
/// ・問題の左右を毎回ランダムに入れ替え（正解側も一緒に変わる）
/// </summary>
public class GameState : MonoBehaviour
{
    public static GameState I { get; private set; }

    public const int TeamCount = 7;

    [System.Serializable]
    public class QuestionData
    {
        [TextArea(2, 6)]
        public string questionText;

        public Sprite leftImage;
        public Sprite rightImage;

        public string leftText;
        public string rightText;

        // 0 = Left(A), 1 = Right(D)
        // ※Inspector上では「元の左右」に対する正解を設定する
        public int correct;
    }

    [Header("Questions")]
    public QuestionData[] questions;

    [Header("Timer")]
    public float defaultTimeLimit = 60f;
    [HideInInspector] public float timeLeft;
    [HideInInspector] public bool accepting;

    [Header("Teams")]
    public bool[] teamAlive = new bool[TeamCount];
    public int[] teamAnswers = new int[TeamCount]; // -1 / 0 / 1
    public int[] teamScore = new int[TeamCount];   // 正解数
    public Color[] teamColors = new Color[TeamCount];

    [Header("Progress")]
    public int questionIndex = 0;

    // ===== 追加：この「出題回」だけ使う表示用（左右ランダム結果） =====
    [Header("Runtime (auto)")]
    public string runtimeLeftText;
    public string runtimeRightText;
    public Sprite runtimeLeftImage;
    public Sprite runtimeRightImage;

    [Tooltip("この回の正解側（0=Left, 1=Right）。左右ランダム後の値。")]
    public int runtimeCorrect;

    public QuestionData CurrentQ
    {
        get
        {
            if (questions == null || questions.Length == 0) return null;
            questionIndex = Mathf.Clamp(questionIndex, 0, questions.Length - 1);
            return questions[questionIndex];
        }
    }

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        InitArrays();
        ResetAllTeamsAndScores();
    }

    void InitArrays()
    {
        if (teamAlive == null || teamAlive.Length != TeamCount) teamAlive = new bool[TeamCount];
        if (teamAnswers == null || teamAnswers.Length != TeamCount) teamAnswers = new int[TeamCount];
        if (teamScore == null || teamScore.Length != TeamCount) teamScore = new int[TeamCount];
        if (teamColors == null || teamColors.Length != TeamCount) teamColors = new Color[TeamCount];

        // デフォルト色（Inspectorで上書き推奨）
        if (teamColors[0] == default)
        {
            teamColors[0] = Color.red;
            teamColors[1] = Color.blue;
            teamColors[2] = Color.green;
            teamColors[3] = Color.yellow;
            teamColors[4] = Color.cyan;
            teamColors[5] = Color.magenta;
            teamColors[6] = new Color(1f, 0.5f, 0f); // orange
        }
    }

    public void StartQuestionTimer()
    {
        timeLeft = defaultTimeLimit;
        accepting = true;
        ResetAnswers();

        // ★ここで毎回ランダムに左右を決定する
        PrepareRuntimeQuestion();
    }

    void ResetAnswers()
    {
        for (int i = 0; i < TeamCount; i++)
            teamAnswers[i] = -1;
    }

    /// <summary>
    /// この出題回の「表示用左右」と「正解側」を作る（毎回ランダム）
    /// </summary>
    void PrepareRuntimeQuestion()
    {
        var q = CurrentQ;
        if (q == null) return;

        bool swap = Random.value < 0.5f; // 50%で左右入れ替え

        if (!swap)
        {
            runtimeLeftText = q.leftText;
            runtimeRightText = q.rightText;
            runtimeLeftImage = q.leftImage;
            runtimeRightImage = q.rightImage;
            runtimeCorrect = q.correct;
        }
        else
        {
            runtimeLeftText = q.rightText;
            runtimeRightText = q.leftText;
            runtimeLeftImage = q.rightImage;
            runtimeRightImage = q.leftImage;

            // 正解側も反転
            runtimeCorrect = 1 - q.correct;
        }
    }

    public bool AllAnsweredAliveTeams()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            if (!teamAlive[i]) continue;
            if (teamAnswers[i] == -1) return false;
        }
        return true;
    }

    public bool AnyWrong()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            if (!teamAlive[i]) continue;
            if (teamAnswers[i] != runtimeCorrect) return true;
        }
        return false;
    }

    public void ApplyScoringForCurrentQuestion()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            if (!teamAlive[i]) continue;
            if (teamAnswers[i] == runtimeCorrect) teamScore[i]++;
        }
    }

    public void EliminateWrongTeams()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            if (!teamAlive[i]) continue;
            if (teamAnswers[i] != runtimeCorrect) teamAlive[i] = false;
        }
    }

    public void NextQuestion()
    {
        if (questions == null || questions.Length == 0) return;

        questionIndex++;
        if (questionIndex >= questions.Length) questionIndex = 0;
    }

    public void ResetAllTeamsAndScores()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            teamAlive[i] = true;
            teamScore[i] = 0;
            teamAnswers[i] = -1;
        }
        questionIndex = 0;
        timeLeft = defaultTimeLimit;
        accepting = false;

        // 初期表示用（念のため）
        PrepareRuntimeQuestion();
    }
}
