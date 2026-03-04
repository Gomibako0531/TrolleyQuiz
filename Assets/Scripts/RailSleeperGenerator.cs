using UnityEngine;

public class RailSleeperGenerator : MonoBehaviour
{
    [Header("Rails (in this segment)")]
    public Transform leftRail;
    public Transform rightRail;

    [Header("Sleeper Prefab (LineRenderer)")]
    public GameObject sleeperPrefab;

    [Header("Layout")]
    public int count = 20;
    public float spacing = 2.5f;

    [Tooltip("このセグメント内で枕木を置き始めるローカルZ")]
    public float startLocalZ = 0f;

    [Tooltip("枕木をレールより少し上に置きたい場合の高さ")]
    public float yOffset = 0f;

    [Tooltip("レールの内側に少し余白を取りたい場合（両側）")]
    public float margin = 0.1f;

    void Start()
    {
        if (!leftRail || !rightRail || !sleeperPrefab)
        {
            Debug.LogError("leftRail / rightRail / sleeperPrefab を設定してください");
            return;
        }

        // 左右レール間の距離（このセグメントのローカル空間で）
        Vector3 l = transform.InverseTransformPoint(leftRail.position);
        Vector3 r = transform.InverseTransformPoint(rightRail.position);

        float width = Mathf.Abs(r.x - l.x) - margin * 2f;
        float centerX = (l.x + r.x) * 0.5f;
        float y = (l.y + r.y) * 0.5f + yOffset;

        for (int i = 0; i < count; i++)
        {
            float z = startLocalZ + i * spacing;

            var go = Instantiate(sleeperPrefab, transform);
            go.name = $"Sleeper_{i+1}";
            go.transform.localPosition = new Vector3(centerX, y, z);
            go.transform.localRotation = Quaternion.identity;

            // LineRendererなら幅（長さ）を左右レール間に合わせる
            var lr = go.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.useWorldSpace = false;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(-width * 0.5f, 0f, 0f));
                lr.SetPosition(1, new Vector3( width * 0.5f, 0f, 0f));
            }
        }
    }
}