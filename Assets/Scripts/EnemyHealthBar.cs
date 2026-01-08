using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider redSlider;    
    public Slider yellowSlider;  
    public Canvas canvasGroup;   

    [Header("Settings")]
    public float yellowBarSpeed = 2f; 
    public float visibleRange = 8f; // [MỚI] Khoảng cách để thanh máu tự hiện ra

    private float yellowTimer;  
    private Transform playerTransform; // Để theo dõi vị trí người chơi
    private Vector3 originalScale; // Lưu kích thước gốc (0.01) để tính toán lật

    void Start()
    {
        // 1. Lưu lại kích thước chuẩn ban đầu (ví dụ 0.01, 0.01, 0.01)
        originalScale = transform.localScale;

        // 2. Tìm người chơi trong map
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // 3. Ẩn ban đầu
        if (canvasGroup != null) canvasGroup.enabled = false;
        
        // Đảm bảo đầy máu
        if (redSlider) redSlider.value = 1f;
        if (yellowSlider) yellowSlider.value = 1f;
    }

    void Update()
    {
        // =========================================================
        // 1. LOGIC CHỐNG LẬT (ANTI-FLIP)
        // =========================================================
        // Nếu thanh máu có cha (Enemy)
        if (transform.parent != null)
        {
            // Kiểm tra xem cha có đang bị lật mặt không (Scale X < 0)
            float parentX = transform.parent.localScale.x;
            
            // Nếu cha bị lật (-), con phải lật theo (-) để kết quả là Dương (+) -> Chữ xuôi
            // Nếu cha không lật (+), con giữ nguyên (+) -> Chữ xuôi
            // Tóm lại: Scale con luôn cùng dấu với Scale cha
            float newX = Mathf.Sign(parentX) * originalScale.x;
            
            transform.localScale = new Vector3(newX, originalScale.y, originalScale.z);
        }

        // =========================================================
        // 2. LOGIC TỰ ĐỘNG HIỆN/ẨN THEO KHOẢNG CÁCH
        // =========================================================
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            // Nếu Player ở gần -> Hiện
            if (distance < visibleRange)
            {
                if (canvasGroup != null && !canvasGroup.enabled) canvasGroup.enabled = true;
            }
            // Nếu Player đi xa -> Ẩn
            else
            {
                if (canvasGroup != null && canvasGroup.enabled) canvasGroup.enabled = false;
            }
        }

        // =========================================================
        // 3. HIỆU ỨNG THANH VÀNG (Giữ nguyên)
        // =========================================================
        if (redSlider == null || yellowSlider == null) return;

        if (yellowSlider.value > redSlider.value)
        {
            yellowTimer += Time.deltaTime;
            if (yellowTimer > 0.5f) 
            {
                yellowSlider.value = Mathf.Lerp(yellowSlider.value, redSlider.value, yellowBarSpeed * Time.deltaTime);
            }
        }
        else
        {
            yellowSlider.value = redSlider.value;
        }
    }

    // Hàm cập nhật máu (Vẫn dùng để cập nhật giá trị Slider)
    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        float targetValue = currentHealth / maxHealth;
        if (redSlider != null) redSlider.value = targetValue;
        yellowTimer = 0;
        
        // Không cần gọi ShowBar() ở đây nữa vì Update đã lo việc check khoảng cách rồi
        // Trừ khi bạn muốn đánh xa tít mù tắp vẫn hiện máu thì thêm logic vào
    }

    public void HideBar()
    {
        if (canvasGroup != null) canvasGroup.enabled = false;
    }
}