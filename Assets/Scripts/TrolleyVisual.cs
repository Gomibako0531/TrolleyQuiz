using UnityEngine;

public class TrolleyVisual : MonoBehaviour
{
    [Tooltip("色を変えたいRenderer（子オブジェクトでもOK）")]
    public Renderer targetRenderer;

    public void SetColor(Color c)
    {
        if (targetRenderer == null) return;
        // 生成物ごとに色を変えるので material を使う
        targetRenderer.material.color = c;
    }
}
