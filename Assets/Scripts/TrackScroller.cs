using UnityEngine;

public class TrackScroller : MonoBehaviour
{
    public Transform track1;
    public Transform track2;
    public Transform cam;          // Main Cameraを割り当て

    public float speed = 10f;
    public float length = 10f;     // 10 * scale.z = 50

    void Update()
    {
        float dz = speed * Time.deltaTime;
        track1.position += Vector3.back * dz;
        track2.position += Vector3.back * dz;

        // 「完全にカメラの後ろへ抜けた」判定：中心が camZ - length/2 より後ろ
        float behindZ = cam.position.z - (length * 0.5f);

        float frontZ = Mathf.Max(track1.position.z, track2.position.z);

        if (track1.position.z < behindZ)
            track1.position = new Vector3(track1.position.x, track1.position.y, frontZ + length);

        if (track2.position.z < behindZ)
            track2.position = new Vector3(track2.position.x, track2.position.y, frontZ + length);
    }
}
