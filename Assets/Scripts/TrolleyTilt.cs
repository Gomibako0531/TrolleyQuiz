using UnityEngine;

public class TrolleyTilt : MonoBehaviour
{
    [SerializeField] float tiltAngle = 30f;

    Quaternion baseRot;

    void Awake()
    {
        baseRot = transform.rotation;
    }

    // choice: 0=A(左), 1=B(右), -1=未選択
    public void SetChoiceVisual(int choice)
    {
        if (choice == 0)
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, +tiltAngle); // 左=左に傾く
        else if (choice == 1)
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, -tiltAngle); // 右=右に傾く
        else
            transform.rotation = baseRot; // リセット
    }
}
