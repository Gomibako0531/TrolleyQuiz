using UnityEngine;

public class TrackScroller : MonoBehaviour
{
    [Header("Scroll")]
    public float speed = 10f;

    [Tooltip("1ループ分の長さ（枕木の並び全体の長さ）")]
    public float length = 47.5f;

    Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Zが小さい方向（-Z）へ動かす
        float dz = speed * Time.deltaTime;
        transform.position += Vector3.back * dz;

        // 開始位置から length 以上進んだら、開始位置に戻す（常に一定周期でループ）
        if (startPos.z - transform.position.z >= length)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, startPos.z);
        }
    }
}