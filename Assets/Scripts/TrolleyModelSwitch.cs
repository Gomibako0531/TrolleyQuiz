using UnityEngine;

/// <summary>
/// RunSceneに事前配置するトロッコに付ける。
/// 子オブジェクトとして「旗左モデル」「旗右モデル」の両方を持ち、
/// SetFlagLeft() で表示を切り替える。
/// </summary>
public class TrolleyModelSwitch : MonoBehaviour
{
    [Header("旗が左にあるモデル（子オブジェクト）")]
    public GameObject modelFlagLeft;

    [Header("旗が右にあるモデル（子オブジェクト）")]
    public GameObject modelFlagRight;

    void Awake()
    {
    }

    /// <summary>
    /// true  → 旗左モデルを表示
    /// false → 旗右モデルを表示
    /// </summary>
    public void SetFlagLeft(bool useFlagLeft)
    {
        if (modelFlagLeft  != null) modelFlagLeft .SetActive( useFlagLeft);
        if (modelFlagRight != null) modelFlagRight.SetActive(!useFlagLeft);
    }
}
