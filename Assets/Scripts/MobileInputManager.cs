using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileInputManager : MonoBehaviour
{
    public static MobileInputManager Instance;

    [Header("Settings")]
    public float doubleTapTime = 0.3f; // Thời gian để tính là bấm đúp
    public float heavyAttackHoldTime = 0.5f; // Thời gian giữ để tính là Heavy Attack

    // --- CÁC BIẾN PUBLIC ĐỂ PLAYER CONTROLLER ĐỌC ---
    [HideInInspector] public float moveX = 0f;       // Giả lập Horizontal Axis
    [HideInInspector] public bool isSprinting = false; 
    [HideInInspector] public bool isCrouching = false;
    [HideInInspector] public bool jumpPressed = false;
    [HideInInspector] public bool dashPressed = false; // Roll
    [HideInInspector] public bool slidePressed = false; // Slide
    [HideInInspector] public bool attackPressed = false;
    [HideInInspector] public bool heavyAttackPressed = false; // Giả lập giữ chuột phải
    [HideInInspector] public bool useItemPressed = false;
    [HideInInspector] public bool switchItemPressed = false;
    // [THÊM MỚI] Biến cho nút Tương Tác
    [HideInInspector] public bool interactPressed = false;

    // --- BIẾN NỘI BỘ ---
    private float lastLeftTapTime, lastRightTapTime, lastDashTapTime;
    private float attackHoldTimer = 0f;
    private bool isHoldingAttackBtn = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Reset các nút bấm dạng "Trigger" (chỉ true trong 1 frame)
        // Lưu ý: PlayerController phải đọc xong rồi ta mới reset ở cuối frame hoặc đầu frame sau
        // Ở đây ta dùng LateUpdate hoặc để Player tự reset cờ nếu cần. 
        // Tuy nhiên cách tốt nhất là dùng biến bool active liên tục cho Axis, và Trigger cho Action.
        
        // Logic Giữ Nút Đánh (Heavy Attack)
        if (isHoldingAttackBtn)
        {
            attackHoldTimer += Time.deltaTime;
            if (attackHoldTimer >= heavyAttackHoldTime)
            {
                heavyAttackPressed = true; // Kích hoạt gồng
            }
        }
        else
        {
            heavyAttackPressed = false;
        }
    }

    // Sau khi PlayerController đọc xong trong Update, ta reset các nút bấm tức thì (Frame sau)
    void LateUpdate()
    {
        jumpPressed = false;
        dashPressed = false;
        slidePressed = false;
        attackPressed = false;
        useItemPressed = false;
        switchItemPressed = false;
        interactPressed = false; // [THÊM MỚI] Reset sau mỗi khung hình
    }

    // ================= CÁC HÀM GẮN VÀO EVENT TRIGGER =================

    // --- 1. DI CHUYỂN (Trái/Phải) ---
    public void OnMoveLeftDown()
    {
        moveX = -1f;
        // Check Double Tap -> Sprint
        if (Time.time - lastLeftTapTime < doubleTapTime) isSprinting = true;
        lastLeftTapTime = Time.time;
    }
    
    public void OnMoveRightDown()
    {
        moveX = 1f;
        // Check Double Tap -> Sprint
        if (Time.time - lastRightTapTime < doubleTapTime) isSprinting = true;
        lastRightTapTime = Time.time;
    }

    public void OnMoveUp()
    {
        moveX = 0f;
        isSprinting = false; // Thả tay là hết chạy nhanh
    }

    // --- 2. NGỒI (Toggle - Bấm là ngồi, bấm lại là đứng) ---
    public void OnCrouchDown()
    {
        isCrouching = !isCrouching; // Đảo trạng thái
    }

    // --- 3. NHẢY ---
    public void OnJumpDown()
    {
        jumpPressed = true;
    }

    // --- 4. DASH (Tap = Roll, Double Tap = Slide) ---
    public void OnDashDown()
    {
        if (Time.time - lastDashTapTime < doubleTapTime)
        {
            // Double Tap -> Slide
            slidePressed = true;
        }
        else
        {
            // Single Tap -> Roll
            dashPressed = true;
        }
        lastDashTapTime = Time.time;
    }

    // --- 5. TẤN CÔNG (Tap = Light, Hold = Heavy) ---
    public void OnAttackDown()
    {
        isHoldingAttackBtn = true;
        attackHoldTimer = 0f;
    }

    public void OnAttackUp()
    {
        // Nếu thả ra sớm -> Đánh thường
        if (attackHoldTimer < heavyAttackHoldTime)
        {
            attackPressed = true;
        }
        // Nếu thả ra muộn -> Kết thúc gồng (PlayerController sẽ tự xử lý việc nhả nút Heavy)
        
        isHoldingAttackBtn = false;
        heavyAttackPressed = false;
    }

    // --- 6. ITEM (Máu & Đổi đồ) ---
    public void OnUseItemDown()
    {
        useItemPressed = true;
    }

    public void OnSwitchItemDown()
    {
        switchItemPressed = true;
    }
    
    // --- 7. PAUSE ---
    public void OnPauseDown()
    {
        if (PauseMenuController.Instance != null)
        {
            if (PauseMenuController.Instance.isMenuOpen) PauseMenuController.Instance.CloseMenu();
            else 
            {
                // Gọi hàm mở menu (cần public hàm TryOpenMenu hoặc OpenMenu trong PauseMenuController)
                // Tạm thời gọi hàm trung gian hoặc sửa script PauseMenu sau
                // PauseMenuController.Instance.TryOpenMenu(); 
            }
        }
    }

    // [THÊM MỚI] Hàm này sẽ gắn vào cái nút Interact nổi
    public void OnInteractDown()
    {
        interactPressed = true;
    }
}