using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro を文字（頂点）単位で砕け散らせる演出。
/// - TextMeshProUGUI / TextMeshPro 両対応（TMP_Text）
/// - 崩れた状態のまま保持（holdSeconds）可能
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TextShatterTMP : MonoBehaviour
{
    [Header("Timing")]
    public float preShakeTime = 0.15f;
    public float shatterTime = 0.6f;

    [Header("Hold (keep shattered)")]
    [Tooltip("砕けた形のまま残す秒数")]
    public float holdSeconds = 2.0f;

    [Tooltip("保持後に消すならON（フェードして消える）")]
    public bool hideAfterHold = false;

    [Header("Fade (only if hideAfterHold=true)")]
    public float fadeTime = 0.25f;

    [Header("Shake")]
    public float shakeAmplitude = 4f;

    [Header("Shatter Motion")]
    public float outward = 40f;        // 外向き初速
    public float upward = 25f;         // 上方向成分
    public float gravity = 120f;       // 擬似重力（UI用）
    public float spin = 720f;          // 回転（度/秒）
    public float randomJitter = 10f;   // ばらつき

    TMP_Text tmp;
    TMP_MeshInfo[] cachedMeshInfo;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    /// <summary>粉砕開始</summary>
    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(CoPlay());
    }

    IEnumerator CoPlay()
    {
        if (tmp == null) yield break;

        // テキストが非表示にされていたら再表示
        tmp.enabled = true;

        // メッシュ更新
        tmp.ForceMeshUpdate();
        var textInfo = tmp.textInfo;
        if (textInfo.characterCount == 0) yield break;

        // 元頂点を保持
        cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        // 1) 軽くシェイク
        float t = 0f;
        while (t < preShakeTime)
        {
            t += Time.deltaTime;
            float amp = shakeAmplitude * (1f - t / preShakeTime);

            ApplyVertexOffsetAll(new Vector3(
                Random.Range(-amp, amp),
                Random.Range(-amp, amp),
                0f
            ));

            yield return null;
        }

        // 2) 砕け散り（文字ごとに飛ばす）
        int charCount = textInfo.characterCount;
        Vector3[] vel = new Vector3[charCount];
        float[] ang = new float[charCount];

        Vector3 center = GetTextCenter(textInfo);

        for (int i = 0; i < charCount; i++)
        {
            var c = textInfo.characterInfo[i];
            if (!c.isVisible) continue;

            Vector3 charMid = (c.bottomLeft + c.topRight) * 0.5f;
            Vector3 dir = (charMid - center).normalized;
            if (dir.sqrMagnitude < 0.001f)
                dir = Random.insideUnitCircle.normalized;

            float jitter = Random.Range(-randomJitter, randomJitter);

            vel[i] = dir * (outward + jitter)
                   + Vector3.up * (upward + Random.Range(0, randomJitter));

            ang[i] = (Random.value < 0.5f ? -1f : 1f)
                   * (spin + Random.Range(-spin * 0.3f, spin * 0.3f));
        }

        float elapsed = 0f;

        while (elapsed < shatterTime)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            tmp.ForceMeshUpdate();
            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                var c = textInfo.characterInfo[i];
                if (!c.isVisible) continue;

                int matIndex = c.materialReferenceIndex;
                int vIndex = c.vertexIndex;

                Vector3[] src = cachedMeshInfo[matIndex].vertices;
                Vector3[] dst = textInfo.meshInfo[matIndex].vertices;

                // 元頂点（4頂点）
                Vector3 bl = src[vIndex + 0];
                Vector3 tl = src[vIndex + 1];
                Vector3 tr = src[vIndex + 2];
                Vector3 br = src[vIndex + 3];

                // 文字中心
                Vector3 mid = (bl + tr) * 0.5f;

                // 擬似重力
                vel[i] += Vector3.down * gravity * dt;

                // 平行移動：速度×時間
                Vector3 offset = vel[i] * elapsed;

                // 回転
                float rot = ang[i] * elapsed;
                Quaternion q = Quaternion.Euler(0f, 0f, rot);

                // 4頂点を回転させてからオフセット
                dst[vIndex + 0] = mid + q * (bl - mid) + offset;
                dst[vIndex + 1] = mid + q * (tl - mid) + offset;
                dst[vIndex + 2] = mid + q * (tr - mid) + offset;
                dst[vIndex + 3] = mid + q * (br - mid) + offset;
            }

            // メッシュ反映
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                var meshInfo = textInfo.meshInfo[m];
                meshInfo.mesh.vertices = meshInfo.vertices;
                tmp.UpdateGeometry(meshInfo.mesh, m);
            }

            yield return null;
        }

        // ★ここで「崩れた状態」の頂点が残ったままになる

        // 3) 崩れたまま保持
        if (holdSeconds > 0f)
            yield return new WaitForSeconds(holdSeconds);

        // 4) 消す場合だけフェードアウト
        if (hideAfterHold)
        {
            float f = 0f;
            Color baseColor = tmp.color;

            while (f < fadeTime)
            {
                f += Time.deltaTime;
                float a = Mathf.Lerp(baseColor.a, 0f, f / fadeTime);
                tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }

            tmp.enabled = false;
        }
    }

    void ApplyVertexOffsetAll(Vector3 offset)
    {
        tmp.ForceMeshUpdate();
        var textInfo = tmp.textInfo;

        // 初回に確実にキャッシュ
        if (cachedMeshInfo == null)
            cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var c = textInfo.characterInfo[i];
            if (!c.isVisible) continue;

            int matIndex = c.materialReferenceIndex;
            int vIndex = c.vertexIndex;

            Vector3[] src = cachedMeshInfo[matIndex].vertices;
            Vector3[] dst = textInfo.meshInfo[matIndex].vertices;

            dst[vIndex + 0] = src[vIndex + 0] + offset;
            dst[vIndex + 1] = src[vIndex + 1] + offset;
            dst[vIndex + 2] = src[vIndex + 2] + offset;
            dst[vIndex + 3] = src[vIndex + 3] + offset;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            var meshInfo = textInfo.meshInfo[m];
            meshInfo.mesh.vertices = meshInfo.vertices;
            tmp.UpdateGeometry(meshInfo.mesh, m);
        }
    }

    Vector3 GetTextCenter(TMP_TextInfo info)
    {
        Bounds b = new Bounds();
        bool init = false;

        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible) continue;

            Vector3 bl = c.bottomLeft;
            Vector3 tr = c.topRight;

            if (!init)
            {
                b = new Bounds((bl + tr) * 0.5f, Vector3.zero);
                init = true;
            }
            else
            {
                b.Encapsulate(bl);
                b.Encapsulate(tr);
            }
        }

        return b.center;
    }
}