using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Range(0, 1)] public float parallaxEffectX; // 1 = Đứng im, 0 = Dính vào cam
    [Range(0, 1)] public float parallaxEffectY; // Chỉnh cái này để nền di chuyển khi leo cao

    private Transform cam;
    private Vector3 startPos;

    void Start()
    {
        cam = Camera.main.transform;
        startPos = transform.position;
    }

    void LateUpdate()
    {
        // Tính khoảng cách camera đã đi
        float distX = (cam.position.x * (1 - parallaxEffectX));
        float distY = (cam.position.y * (1 - parallaxEffectY));

        // Cập nhật vị trí để tạo ảo giác
        transform.position = new Vector3(startPos.x + cam.position.x * parallaxEffectX, startPos.y + cam.position.y * parallaxEffectY, transform.position.z);
        
        // (Tùy chọn) Logic lặp lại background nếu map quá rộng (Infinite Tiling)
        // ... (Code này phức tạp hơn, tạm thời bạn cứ copy paste 3 tấm background nối đuôi nhau là đủ)
    }
}