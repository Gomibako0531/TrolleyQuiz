using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonHoverSfx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public AudioSource sfxSource;
    public AudioClip hoverClip;
    public float volume = 1f;

    public float hoverScale = 1.1f;
    public float scaleSpeed = 8f;

    Vector3 originalScale;
    bool hovering = false;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        Vector3 target = hovering ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (sfxSource && hoverClip)
            sfxSource.PlayOneShot(hoverClip, volume);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }
}