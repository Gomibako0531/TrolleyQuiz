using UnityEngine;

public class TrolleyVisual : MonoBehaviour
{
    [Header("References")]
    [Tooltip("見た目だけ回転させたい子(Visual)を入れる。未設定なら自分を使う")]
    public Transform visualRoot;

    Renderer[] rends;

    void Awake()
    {
        if (visualRoot == null) visualRoot = transform;
        rends = visualRoot.GetComponentsInChildren<Renderer>(true);
    }

    public void SetColor(Color c)
    {
        if (rends == null || rends.Length == 0)
            rends = visualRoot.GetComponentsInChildren<Renderer>(true);

        foreach (var r in rends)
        {
            // 共有マテリアルを汚したくないなら material を使う
            if (r != null) r.material.color = c;
        }
    }

    /// <summary>見た目だけ回転</summary>
    public void SetVisualYaw(float yDegrees)
    {
        if (visualRoot == null) visualRoot = transform;
        var e = visualRoot.localEulerAngles;
        e.y = yDegrees;
        visualRoot.localEulerAngles = e;
    }
}