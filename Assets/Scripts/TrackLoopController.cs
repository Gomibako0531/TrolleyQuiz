using UnityEngine;

public class TrackLoopController : MonoBehaviour
{
    public Transform segmentA;
    public Transform segmentB;

    [Header("Scroll")]
    public float speed = 8f;      // 速度
    public float length = 47.5f;  // (count-1)*spacing = 19*2.5
    public float duration = 60f;

    [Header("Placement")]
    public float startZ = 30f;    // ★カメラから見えるZに調整（まず30でOK）
    public float baseY = 0f;      // ★線路の高さ
    public float baseX = 0f;      // ★中央

    float t;

    void Start()
    {
        if (!segmentA || !segmentB) return;

        // ★開始位置を強制（これで「見えない」を潰す）
        segmentA.position = new Vector3(baseX, baseY, startZ);
        segmentB.position = new Vector3(baseX, baseY, startZ + length);
    }

    void Update()
    {
        if (!segmentA || !segmentB) return;
        if (t >= duration) return;
        t += Time.deltaTime;

        float dz = speed * Time.deltaTime;

        // ★手前に流す（Zが小さくなる方向へ）
        segmentA.position += new Vector3(0f, 0f, -dz);
        segmentB.position += new Vector3(0f, 0f, -dz);

        // ★後ろに抜けた方を前へ
        float backZ = startZ - length;                  // ここより小さくなったら抜けた判定
        float frontZ = Mathf.Max(segmentA.position.z, segmentB.position.z);

        if (segmentA.position.z < backZ)
            segmentA.position = new Vector3(baseX, baseY, frontZ + length);

        if (segmentB.position.z < backZ)
            segmentB.position = new Vector3(baseX, baseY, frontZ + length);
    }
}