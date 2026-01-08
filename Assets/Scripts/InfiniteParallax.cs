using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    private float length, startpos;
    public GameObject cam; // Kéo Main Camera vào đây
    public float parallaxEffect; // 0 = đứng im, 1 = đi theo cam hoàn toàn. Nền xa nên để khoảng 0.5 -> 0.9

    void Start()
    {
        startpos = transform.position.x;
        // Lấy chiều rộng của tấm ảnh sprite để biết khi nào cần lặp lại
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        // Tính quãng đường camera đã đi được so với vị trí bắt đầu của background
        float temp = (cam.transform.position.x * (1 - parallaxEffect));
        
        // Tính quãng đường background cần di chuyển
        float dist = (cam.transform.position.x * parallaxEffect);

        // Di chuyển background
        transform.position = new Vector3(startpos + dist, transform.position.y, transform.position.z);

        // --- LOGIC VÔ TẬN (TILING) ---
        // Nếu camera đã đi quá nửa chiều dài ảnh về bên phải -> Dời điểm bắt đầu sang phải
        if (temp > startpos + length) startpos += length;
        // Nếu camera đã đi quá nửa chiều dài ảnh về bên trái -> Dời điểm bắt đầu sang trái
        else if (temp < startpos - length) startpos -= length;
    }
}