using UnityEngine;
using UnityEngine.UI;

public class ClockTimerUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform hand; // ClockHand
    public Image ring;         // ClockRing (Image Type: Filled, Radial 360)

    [Header("Timer")]
    public float duration = 60f;   // 60 seconds
    public bool clockwise = true;  // 針の回転方向

    float timeLeft;
    bool running;

    void OnEnable()
    {
        ResetTimer();
        StartTimer();
    }

    public void ResetTimer()
    {
        timeLeft = duration;
        ApplyVisual(0f); // 開始状態
    }

    public void StartTimer() => running = true;
    public void StopTimer() => running = false;

    void Update()
    {
        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;

        float progress = 1f - (timeLeft / duration); // 0→1

        ApplyVisual(progress);

        if (timeLeft <= 0f)
        {
            running = false;
        }
    }

    void ApplyVisual(float progress)
    {
        // 針：1周 = 360度
        float angle = (clockwise ? -360f : 360f) * progress;
        if (hand) hand.localRotation = Quaternion.Euler(0, 0, angle);

        // リング：通った分だけ消える（残りを表示したいなら 1-progress）
        // 「針が通ると消える」 = 減っていく表現なので、fillAmountは残りにするのが自然
        if (ring) ring.fillAmount = 1f - progress;
    }
}