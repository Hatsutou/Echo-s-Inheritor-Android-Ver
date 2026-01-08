using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nhớ dùng TextMeshPro
using System.Collections;

public class BossHUD : MonoBehaviour
{
    [Header("UI Components")]
    public Image redBar;    // Kéo RedBar vào đây
    public Image yellowBar; // Kéo YellowBar vào đây
    public TextMeshProUGUI nameText; // Kéo BossNameText vào đây
    public GameObject panel; // Kéo cả cụm BossHealthBar_UI vào để ẩn hiện

    [Header("Settings")]
    public float chipSpeed = 2f; // Tốc độ tụt của thanh vàng

    [Header("Galaxy Gradient Effect")]
    public bool isGalaxyEffectActive = true; 
    public float colorSpeed = 0.5f; // Tốc độ trôi màu

    // Bảng màu Phase 1 (Galaxy êm dịu: Tím, Xanh, Hồng...)
    public Color[] phase1Colors;
    
    // Bảng màu Phase 2 (Galaxy hung tàn: Đỏ, Đen, Tím than...)
    public Color[] phase2Colors;

    // Biến nội bộ để nhớ đang dùng bảng màu nào
    private Color[] currentPalette;

    [Range(0.1f, 5f)]
    public float gradientSpread = 1.5f; // [MỚI] Độ chênh lệch màu giữa đầu và cuối chữ
                                        // Số càng lớn -> Càng nhiều màu xuất hiện cùng lúc (cầu vồng)
                                        // Số nhỏ -> Cả dòng chữ đồng màu

    [Header("Effects")]
    public TextEffectManager textEffect; // Kéo script TextEffectManager vào đây

    private float currentBlood;
    private float maxBlood;
    private float targetFillAmount;

    void Awake() 
    {
        // Mặc định ẩn đi cho đến khi Boss xuất hiện
        if (panel != null) panel.SetActive(false);
    }

    // --- SỬA LẠI HÀM START ĐỂ MẶC ĐỊNH DÙNG MÀU PHASE 1 ---
    void Start()
    {
        // Mặc định ban đầu dùng bảng màu Phase 1
        currentPalette = phase1Colors;
        if (nameText != null) nameText.enableVertexGradient = true; // Bật chế độ Gradient
    }

    // --- HÀM UPDATE TẠO HIỆU ỨNG TRÔI MÀU (SWIRL GRADIENT) ---
    void Update()
    {
        if (isGalaxyEffectActive && nameText != null && currentPalette != null && currentPalette.Length >= 2)
        {
            // Thời gian trôi
            float time = Time.time * colorSpeed;

            // ĐỂ TẠO HIỆU ỨNG NGANG (Horizontal):
            // - Phía bên TRÁI (TL, BL) sẽ cùng một màu (màu A).
            // - Phía bên PHẢI (TR, BR) sẽ cùng một màu (màu B).
            // -> Gradient sẽ tự động trộn từ Trái sang Phải.

            // 1. Màu bên Trái (Tại thời điểm hiện tại)
            Color leftColor = GetColorFromPalette(time);

            // 2. Màu bên Phải (Tại tương lai = Hiện tại + Độ rộng Spread)
            // Spread càng lớn thì màu bên phải càng khác biệt so với bên trái -> Tạo ra dải màu
            Color rightColor = GetColorFromPalette(time + gradientSpread);

            // 3. Áp dụng vào VertexGradient
            // Thứ tự hàm dựng: (TopLeft, TopRight, BottomLeft, BottomRight)
            // -> Ta gán: (Left, Right, Left, Right)
            nameText.colorGradient = new VertexGradient(leftColor, rightColor, leftColor, rightColor);
        }
    }

    // Hàm phụ trợ để lấy màu mượt mà từ danh sách
    Color GetColorFromPalette(float time)
    {
        float t = Mathf.Repeat(time, currentPalette.Length);
        int index1 = Mathf.FloorToInt(t);
        int index2 = (index1 + 1) % currentPalette.Length;
        return Color.Lerp(currentPalette[index1], currentPalette[index2], t - index1);
    }

    // --- HÀM CHUYỂN ĐỔI PALETTE MÀU (GỌI TỪ BOSS) ---
    public void SwitchToPhase2Colors()
    {
        currentPalette = phase2Colors; // Đổi sang bảng màu đỏ
    }

    // --- HÀM BẬT/TẮT HIỆU ỨNG GALAXY (FIX LỖI BÁO ĐỎ) ---
    public void EnableGalaxyText(bool enable)
    {
        isGalaxyEffectActive = enable;

        if (nameText != null)
        {
            if (enable)
            {
                // Bật Gradient lên
                nameText.enableVertexGradient = true; 
            }
            else
            {
                // Tắt đi thì về màu trắng bình thường
                nameText.enableVertexGradient = false; 
                nameText.color = Color.white; 
            }
        }
    }

    public void SetupHUD(string name, float maxHealth)
    {
        if (panel != null) panel.SetActive(true);
        if (nameText != null) nameText.text = name;

        this.maxBlood = maxHealth;
        this.currentBlood = maxHealth;
        
        // Reset 2 thanh về đầy
        targetFillAmount = 1f;
        redBar.fillAmount = 1f;
        yellowBar.fillAmount = 1f;
    }

    public void UpdateBlood(float currentHealth)
    {
        this.currentBlood = currentHealth;
        
        // 1. Thanh đỏ tụt ngay lập tức
        targetFillAmount = currentBlood / maxBlood;
        redBar.fillAmount = targetFillAmount;

        // 2. Thanh vàng tụt từ từ (Chạy Coroutine)
        StartCoroutine(UpdateYellowBar());
    }

    IEnumerator UpdateYellowBar()
    {
        // Chờ một chút trước khi tụt (tạo cảm giác delay "khựng" lại)
        yield return new WaitForSeconds(0.5f);

        // Vòng lặp giảm dần thanh vàng cho đến khi bằng thanh đỏ
        while (yellowBar.fillAmount > targetFillAmount)
        {
            yellowBar.fillAmount -= chipSpeed * Time.deltaTime;
            yield return null;
        }
        
        // Chốt lại cho bằng nhau chính xác
        yellowBar.fillAmount = targetFillAmount;
    }

    // --- HÀM MỚI: CẬP NHẬT MÁU TỨC THÌ (KHÔNG CÓ HIỆU ỨNG TỤT TỪ TỪ) ---
    public void SetHealthInstant(float health)
    {
        // 1. Cập nhật lại số máu hiện tại
        this.currentBlood = health;

        // 2. Tính toán tỷ lệ đầy (0.0 đến 1.0)
        // Lưu ý: Phải đảm bảo maxBlood > 0 để không lỗi chia cho 0
        float ratio = 0;
        if (maxBlood > 0) ratio = currentBlood / maxBlood;
        
        targetFillAmount = ratio;

        // 3. QUAN TRỌNG: Dừng ngay hiệu ứng tụt máu nếu nó đang chạy dở
        StopAllCoroutines(); 

        // 4. Ép cả 2 thanh nhảy về vị trí mới NGAY LẬP TỨC
        if (redBar != null) redBar.fillAmount = ratio;
        if (yellowBar != null) yellowBar.fillAmount = ratio;
    }

    public void HideHUD()
    {
        if (panel != null) panel.SetActive(false);
    }
}