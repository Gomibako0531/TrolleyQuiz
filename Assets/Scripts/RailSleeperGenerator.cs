using UnityEngine;

public class RailSleeperGenerator : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject sleeperPrefab;

    [Header("Material")]
    public Material sleeperMaterial; // ★ここにSleeperMatを入れる

    [Header("Layout")]
    public int count = 20;
    public float spacing = 2.5f;
    public float startZ = -25f;

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject s = Instantiate(sleeperPrefab, transform);

            // 位置と回転（進行方向Zに対して垂直）
            s.transform.localPosition = new Vector3(0f, 0f, startZ + i * spacing);
            s.transform.localRotation = Quaternion.identity;

            // ★Materialを設定
            LineRenderer lr = s.GetComponent<LineRenderer>();
            if (lr != null && sleeperMaterial != null)
            {
                // 個別インスタンスに反映（全体同時変更を避ける）
                lr.material = sleeperMaterial;
            }
        }
    }
}
