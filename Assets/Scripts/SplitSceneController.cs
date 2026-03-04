using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplitSceneController : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("全トロッコが通る基準点（ここを通過する直線上に並びます）")]
    public Transform commonSpawnPoint;

    public GameObject trolleyPrefab;

    [Header("Line / Layout")]
    [Tooltip("進行直線の角度（左=-30°, 右=+30° のように使う）")]
    public float branchAngle = 30f;

    [Tooltip("同じ直線上での車間距離（進行方向に沿って後ろへ並べます）")]
    public float spacingAlongLine = 2.0f;

    [Tooltip("左右が同じ直線上で完全に重なるのを避けたい場合、少しだけ横にずらす量（0でOK）")]
    public float lateralOffset = 0.0f;

    [Header("Move")]
    public float moveDuration = 3f;
    public float moveSpeed = 6f;

    [Header("Next Scene")]
    public string nextSceneName = "BranchScene";

    [Header("Visual Angles (Inspectorで調整する)")]
    [Tooltip("左側レーンのトロッコ見た目の向き（Yaw）")]
    public float leftYaw = -30f;

    [Tooltip("右側レーンのトロッコ見た目の向き（Yaw）")]
    public float rightYaw = 30f;

    class Moving
    {
        public GameObject go;
        public Vector3 dir;
    }

    readonly List<GameObject> spawned = new();
    readonly List<Moving> movers = new();

    float timer = 0f;

    void Start()
    {
        if (GameState.I == null)
        {
            Debug.LogError("GameState が見つかりません。");
            return;
        }
        if (commonSpawnPoint == null || trolleyPrefab == null)
        {
            Debug.LogError("commonSpawnPoint または trolleyPrefab が未設定です。");
            return;
        }

        SpawnByAnswerOnSingleLines();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timer += dt;

        foreach (var m in movers)
        {
            if (m.go == null) continue;
            m.go.transform.position += m.dir * (moveSpeed * dt);
        }

        if (timer >= moveDuration)
        {
            Cleanup();
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void SpawnByAnswerOnSingleLines()
    {
        var gs = GameState.I;

        // 左/右チームを分ける
        List<int> leftTeams = new();
        List<int> rightTeams = new();

        for (int team = 0; team < GameState.TeamCount; team++)
        {
            if (!gs.teamAlive[team]) continue;

            int ans = gs.teamAnswers[team];
            if (ans == 0) leftTeams.Add(team);
            else if (ans == 1) rightTeams.Add(team);
            else rightTeams.Add(team); // 未回答は右へ（好みで変更OK）
        }

        // 左直線：進行 dir = -branchAngle、見た目 yaw = leftYaw、旗 = 左
        SpawnLine(leftTeams, -branchAngle, -lateralOffset, leftYaw, flagLeft: true);

        // 右直線：進行 dir = +branchAngle、見た目 yaw = rightYaw、旗 = 右
        SpawnLine(rightTeams, +branchAngle, +lateralOffset, rightYaw, flagLeft: false);
    }

    void SpawnLine(List<int> teams, float moveAngleY, float sideOffset, float visualYaw, bool flagLeft)
    {
        var gs = GameState.I;
        int count = teams.Count;
        if (count <= 0) return;

        // 進行方向（+Z を moveAngleY 回転）
        Vector3 dir = Quaternion.Euler(0f, moveAngleY, 0f) * Vector3.forward;

        // dirに直交する「横方向」（右方向）＝ lateralOffset 用
        Vector3 right = Quaternion.Euler(0f, moveAngleY, 0f) * Vector3.right;

        // ★全員が commonSpawnPoint を通る直線上に並べる
        for (int i = 0; i < count; i++)
        {
            int team = teams[i];

            // spawn点から進行方向の「後ろ」に並べる（-dir）
            float back = i * spacingAlongLine;
            Vector3 pos = commonSpawnPoint.position - dir * back + right * sideOffset;

            var go = Instantiate(trolleyPrefab);
            go.name = $"Team{team + 1}_Split";
            go.transform.position = pos;

            // 見た目角度（Visualだけ回転）
            var tv = go.GetComponent<TrolleyVisual>();
            if (tv != null) tv.SetVisualYaw(visualYaw);

            // ★旗の左右を回答側に合わせる（ここが今回の修正ポイント）
            var ms = go.GetComponent<TrolleyModelSwitch>();
            if (ms != null) ms.SetFlagLeft(flagLeft);

            PrepareStatic(go);
            ApplyTeamColor(go, gs.teamColors[team]);

            spawned.Add(go);
            movers.Add(new Moving { go = go, dir = dir });
        }
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

    void Cleanup()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);

        spawned.Clear();
        movers.Clear();
    }
}