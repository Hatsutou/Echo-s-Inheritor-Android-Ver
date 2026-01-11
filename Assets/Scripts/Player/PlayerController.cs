using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // [BẮT BUỘC] Thêm dòng này để xử lý Scene

public class PlayerController : MonoBehaviour
{
    // =================================================
    // 1. CÀI ĐẶT CHUNG (SETTINGS)
    // =================================================
    // --- [FIX LỖI] THÊM DÒNG NÀY ĐỂ MÁY HIỂU 'isMenuOpen' LÀ GÌ ---
    private bool isMenuOpen => PauseMenuController.Instance != null && PauseMenuController.Instance.isMenuOpen;
    [Header("Cài Đặt Di Chuyển")]
    public float moveSpeed = 8f;
    public float sprintSpeed = 12f;
    public float crouchSpeed = 4f; // Tốc độ khi ngồi
    public float jumpForce = 12f;
    public float sprintJumpForce = 16f;

    [Header("Cài Đặt Tấn Công")]
    public float attackStepForce = 3f;
    public float attackStepDuration = 0.1f;

    [Header("Cài Đặt Kỹ Năng (Roll/Slide)")]
    public float rollForce = 15f;
    public float rollCooldown = 1f;
    public float slideForce = 12f;
    public float slideCooldown = 1.5f;

    [Header("Cài Đặt Leo Trèo (Ledge Climb)")]
    public float wallCheckDist = 0.6f; // Độ dài tia check tường
    public float wallCheckOffsetY = 1.0f; // --- [MỚI] Độ cao tia check tường (Blue) ---
    public float ledgeCheckHeight = 1.5f; // Vị trí tia check mép (trên đầu)
    public Vector2 climbOffset = new Vector2(0.5f, 1.8f); // Vị trí dịch chuyển sau khi leo
    public LayerMask wallLayer; // Layer của tường (nhớ gán trong Inspector)
    public float wallSlideSpeed = 2f; // --- [MỚI] Tốc độ trượt xuống ---
    public Vector2 wallJumpForce = new Vector2(8f, 15f);// --- LỰC NHẢY BẬT TƯỜNG (X: Đẩy ra, Y: Đẩy lên) ---
    private float wallStickForce = 5f;//Lực "ép" vào tường để không bị rơi ra (Sticky Force)

    [Header("Kiểm Tra Mặt Đất")]
    public Transform groundCheck; 
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // [MỚI] Thêm biến để chỉnh lực cho dễ
    [Header("Cài Đặt Knockdown")]
    public float knockbackForceX = 8f; // Lực đẩy lùi ngang
    public float knockbackForceY = 5f; // Lực nảy lên
    public float getUpForwardForce = 3f; // Lực tiến lên khi đứng dậy

    [Header("Cài Đặt Đòn Đánh Mạnh (Heavy Attack)")]
    public float maxChargeTime = 2.0f; // Thời gian cần để tụ đầy bình (Max Dam)
    public float minChargeTime = 0.5f; // Thời gian tối thiểu để tung đòn (nếu thả sớm quá thì hủy hoặc đánh yếu)
    public float heavyAttackDamageMultipler = 2.5f; // Hệ số nhân Dam khi tụ đầy

    [Header("Heavy Attack Combo (Đòn Mạnh 2)")]
    public float heavyAttack2StaminaCost = 25f; // Tốn sức hơn hoặc bằng đòn 1
    public float heavyAttack2DamageMult = 3.5f; // Sát thương khủng hơn đòn 1
    public float comboWindowStart = 0.5f;       // Chờ bao lâu sau đòn 1 thì bắt đầu cho bấm? (Thường là khi vừa chém xong)
    public float comboWindowDuration = 1.0f;    // Cửa sổ kéo dài bao lâu? (1s)
    private bool canPerformHeavyCombo = false;  // Biến kiểm tra: Có được phép đánh đòn 2 không?
    // [MỚI] Biến này cực quan trọng: 1 là đòn đầu, 2 là đòn sau
    private int heavyComboStep = 1;

    [Header("=== HITBOX SETTINGS ===")]
    // Kéo cái GameObject HeavyHitbox bạn vừa tạo vào đây
    public GameObject heavyHitboxObj; 
    // Kéo script gây dam (ví dụ PlayerAttack) gắn trên cái Hitbox đó vào đây
    // Để mình còn chỉnh sửa lượng dam trước khi chém
    public PlayerAttack heavyAttackScript;

    [Header("=== HỆ THỐNG STAMINA ===")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f; // Tốc độ hồi phục (điểm/giây)
    public float staminaRegenDelay = 1.0f; // Thời gian chờ trước khi bắt đầu hồi

    [Header("Chi Phí Tiêu Hao (Cost)")]
    // 1. NHÓM TIÊU HAO TỨC THÌ (Mất 1 cục)
    public float jumpStaminaCost = 10f;
    public float rollStaminaCost = 15f;
    public float attackStaminaCost = 10f;       // Đánh thường
    public float heavyAttackStaminaCost = 30f;  // Đánh mạnh
    public float ledgeClimbStaminaCost = 20f;   // Leo lên mép

    // 2. NHÓM TIÊU HAO TỪ TỪ (Mất theo thời gian)
    public float sprintStaminaCost = 10f;      // Chạy nhanh (điểm/giây)
    public float ledgeHangStaminaCost = 5f;    // Treo mình & Chuẩn bị nhảy (điểm/giây)

    [Header("=== HỆ THỐNG MANA (NĂNG LƯỢNG) ===")]
    public float maxMana = 100f;
    public float slideManaCost = 15f; // Trượt tốn 15 Mana

    [Header("Inventory")]
    public ConsumableManager consumableManager; // Kéo script Manager vào đây

    [Header("Fall Damage Settings")]
    public float abyssLimitY = -20f;   // Rơi xuống đáy thế giới -> Chết luôn

    // --- THAY BẰNG CÁC BIẾN MỚI (TÍNH THEO VẬN TỐC) ---
    [Header("Fall Thresholds (Velocity Based)")]
    public float minFallSpeedToHurt = 18f;    // Tốc độ >= 18 mới mất máu
    public float minFallSpeedToStamina = 12f; // Tốc độ >= 12 mới mất stamina
    public float damageMultiplier = 2f;       // Hệ số nhân (Vận tốc * 2 = Dam)

    // Biến nội bộ mới để lưu vận tốc khung hình trước
    private float lastFrameVelocityY;
    private bool wasTouchingGround; // Trạng thái chạm đất ở khung hình trước
    
    // Biến lưu vị trí an toàn (Public để PlayerHealth truy cập)
    [HideInInspector] public Vector3 lastSafePosition;

    [Header("Slope Settings")]
    public float slopeCheckDistance = 0.5f; // Độ dài tia quét dốc (dài hơn chân một chút)
    public float maxSlopeAngle = 45f;       // Góc dốc tối đa leo được
    public LayerMask whatIsGround;          // Layer mặt đất (bao gồm cả dốc)
    public float slopeForce = 10f;          // Lực ép xuống đất để không bị nảy

    [Header("Wall Slide Settings")]
    public float wallSlidingSpeed = 2f; // Tốc độ trượt xuống (số càng to trượt càng nhanh)

    [Header("Wall Jump Settings")]
    public float wallJumpForceX = 10f; // Lực bật ra xa tường (Phương ngang)
    public float wallJumpForceY = 12f; // Lực bật lên cao (Phương dọc - có thể dùng chung jumpForce)
    public float wallJumpCooldown = 0.2f; // Thời gian khóa phím (để không bị dính lại tường ngay lập tức)
    
    private bool isWallJumpLocked = false; // Biến cờ để khóa di chuyển tạm thời

    [Header("Air Attack Settings")]
    private bool isAirAttacking = false;    // Biến kiểm tra trạng thái
    public GameObject airAttackHitbox;// [MỚI] Biến chứa Hitbox riêng
    // [MỚI] Ngưỡng tốc độ an toàn khi dùng chiêu (Cao hơn mức 18f bình thường)
    // Ví dụ: 35f tương đương rơi khoảng 2-3 tầng lầu vẫn không sao
    public float airAttackSafeSpeed = 35f;

    // Thêm các biến hỗ trợ Tay cầm vào đầu class (trước hàm Start) nếu chưa có
    // [Header("Controller Settings")]
    // public float triggerThreshold = 0.5f; // Độ nhạy cò Trigger
    // private float dashButtonTimer = 0f;   // Đếm giờ giữ nút B
    // private bool isDashButtonHeld = false;
    // private bool isCrouchToggled = false; // Ngồi bằng nút bấm (RSB)
    // private float lastDPadInput = 0f;     // Chống spam D-Pad
    
    
    // =================================================
    // 2. BIẾN NỘI BỘ (INTERNAL VARIABLES)
    // =================================================
    private Rigidbody2D rb;
    private Animator anim;
    private BoxCollider2D col; // Dùng BoxCollider2D

    // Collider Info
    private Vector2 standSize;
    private Vector2 standOffset;

    // State Variables
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded; 
    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isHurt = false;
    
    // Action Flags
    private bool isAttacking = false;
    private bool isAttackStepping = false; 
    private bool isRolling = false;        
    private bool isSliding = false;
    
    // Ledge Climb Flags [MỚI]
    private bool isTouchingWall;
    private bool isTouchingLedge;
    private bool isHanging = false;
    private bool isClimbing = false;
    private int wallDir = 0; // [MỚI] 1 là tường bên phải, -1 là tường bên trái

    // Combat Variables
    private int comboStep = 0;          
    private float lastAttackTime = 0;   
    private float comboResetTime = 2f;  
    private float attackCooldown = 0.4f; 
    private float nextAttackAllowTime = 0; 
    
    // Cooldowns
    private float nextRollTime = 0;
    private float nextSlideTime = 0;

    private float defaultGravity; // Biến để lưu trọng lực gốc

    private float ledgeGrabCooldown = 0.5f; // Thời gian cấm bám lại sau khi thả
    private float nextLedgeGrabTime = 0f;
    private bool isWallSliding = false;
    private float lastAttackInputTime = -1f; //Thời điểm bấm nút đánh
    private bool isUsingItem = false;
    public bool isKnockedDown = false; // <-- THÊM CẢ DÒNG NÀY NỮA (Cho phần sau)
    // [MỚI] Biến kiểm tra đang trong giai đoạn "Đứng Dậy" (để cho phép Roll)
    private bool isGettingUp = false;
    // [MỚI] Biến lưu Coroutine để có thể dừng nó giữa chừng
    private Coroutine knockdownRoutine;
    private float lastGroundedTime; // Thời điểm cuối cùng chạm đất
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private float currentStamina;
    private float lastStaminaUseTime; // Để đếm giờ hồi phục
    private float currentMana;
    private PlayerHUD hud; // Tham chiếu UI
    private Coroutine manaCoroutine;
    public float currentBlood;

    // [MỚI] Biến để lưu Checkpoint đang đứng gần (nếu có)
    [HideInInspector] public Checkpoint currentCheckpoint; 
    [HideInInspector] public EndGameStatue currentEndGameStatue;
    [HideInInspector] public DeathMarkerController currentDeathMarker;
    // [MỚI] Biến trạng thái đang nghỉ
    private bool isRestingAtCheckpoint = false;
    // [THÊM DÒNG NÀY] Biến lưu Cửa đang đứng gần
    [HideInInspector] public DoorController currentDoor;
    private bool isWallJumping = false;
    private bool isJumpingAction = false; // [MỚI] Biến xác nhận đang thực hiện hành động nhảy
    [HideInInspector] public bool isOnElevator = false; // [MỚI] Biến kiểm tra đang đi thang máy
    // [MỚI] Biến này dùng để chặn nút E
    // True = Thang máy đang đứng chờ -> Cấm dùng Item (để dành nút E bấm thang)
    // False = Thang máy đang chạy hoặc ko ở gần thang -> Dùng Item thoải mái
    [HideInInspector] public bool isElevatorBlockingInput = false;

    private Vector2 slopeNormalPerp;        // Vector vuông góc với bề mặt dốc (hướng đi song song)
    private bool isOnSlope;                 // Đang ở trên dốc hay không?
    private float slopeDownAngle;           // Góc nghiêng của dốc hiện tại
    private float slopeDownAngleOld;        // Góc của frame trước
    private bool isJumpingFromSlope = false;// Biến lưu trạng thái nhảy để ngắt logic dốc
    private float gravityScaleGoc;//Biến lưu trọng lực mặc định để reset sau khi leo dốc

    // Layers
    int playerLayer;
    int enemyLayer;

    // =================================================
    // 3. CÁC HÀM KHỞI TẠO & UPDATE
    // =================================================
    void Start()
    {
        // 1. Lấy các Component cần thiết trước
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<BoxCollider2D>();

        // 2. Khởi tạo các thông số cơ bản
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        gravityScaleGoc = rb.gravityScale;
        defaultGravity = rb.gravityScale;
        
        standSize = col.size;
        standOffset = col.offset;

        // 3. Khởi tạo Stamina & Mana & UI
        currentStamina = maxStamina;
        currentMana = maxMana;
        
        hud = FindFirstObjectByType<PlayerHUD>();
        if (hud != null) 
        {
            hud.UpdateStamina(currentStamina, maxStamina);
            hud.UpdateMana(currentMana, maxMana);
        }

        // -----------------------------------------------------------
        // [LOGIC MỚI] KIỂM TRA CHUYỂN CẢNH HAY HỒI SINH?
        // -----------------------------------------------------------
        
        // Kiểm tra cờ "Transition" (1 = Đi qua cửa, 0 = Mới chơi/Chết)
        bool isSceneTransition = (PlayerPrefs.GetInt("IsSceneTransition", 0) == 1);

        if (isSceneTransition)
        {
            Debug.Log(">>> PHÁT HIỆN CHUYỂN CẢNH: Đang khôi phục chỉ số cũ...");
            LoadPlayerState(); // Gọi hàm load lại Máu/Mana/Item
            
            // [QUAN TRỌNG] Reset cờ ngay lập tức
            // Để nếu lát nữa chết trong phòng này, nó sẽ Reset full máu (Logic Hồi sinh)
            PlayerPrefs.SetInt("IsSceneTransition", 0); 
            PlayerPrefs.Save();
        }
        else
        {
            // LOGIC CŨ: Khởi tạo đầy bình (Mới chơi hoặc vừa Chết)
            currentStamina = maxStamina;
            currentMana = maxMana;
            // Máu đã được set full trong PlayerHealth.Start() mặc định rồi
            // ConsumableManager cũng mặc định số lượng trong Inspector

            // [MỚI] THÊM ĐOẠN NÀY ĐỂ HIỆN ITEM MẶC ĐỊNH
            // Vì ta đã cấm Manager tự chạy, nên giờ ta phải gọi thủ công ở đây
            if (consumableManager != null)
            {
                // Nếu bạn muốn hồi đầy bình khi hồi sinh thì gọi Refill luôn
                // consumableManager.RefillAllPotions(); 
                
                // Hoặc chỉ hiển thị những gì đang có trong Inspector
                consumableManager.UpdateUI(); 
            }
        }

        // Cập nhật UI lần đầu
        hud = FindFirstObjectByType<PlayerHUD>();
        if (hud != null) 
        {
            hud.UpdateStamina(currentStamina, maxStamina);
            hud.UpdateMana(currentMana, maxMana);
        }

        // =========================================================
        // 3. XỬ LÝ VỊ TRÍ XUẤT HIỆN (FINAL VERSION)
        // =========================================================
        
        // Lấy dữ liệu Checkpoint
        bool hasCheckpointData = (PlayerPrefs.GetInt("HasCheckpoint") == 1);
        string savedScene = PlayerPrefs.GetString("SavedScene", "");
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Chỉ dùng Checkpoint nếu nó nằm đúng trong Scene này
        bool shouldUseCheckpoint = hasCheckpointData && (savedScene == currentScene);

        GameObject startPoint = GameObject.Find("SpawnPoint");

        // --- ƯU TIÊN 1: VỀ CHECKPOINT (Nếu đúng màn) ---
        if (shouldUseCheckpoint)
        {
            float x = PlayerPrefs.GetFloat("RespawnX");
            float y = PlayerPrefs.GetFloat("RespawnY");
            
            transform.position = new Vector2(x, y);
            // Debug.Log("Đã hồi sinh tại Checkpoint.");
        }
        // --- ƯU TIÊN 2: VỀ CỬA/SPAWN POINT (Nếu sang map mới) ---
        else if (startPoint != null)
        {
            transform.position = startPoint.transform.position;
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
            // Debug.Log("Đã xuất hiện tại SpawnPoint.");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy điểm xuất hiện nào!");
        }
        // 5. [FIX LỖI RƠI TỰ DO] Reset vật lý ngay sau khi dịch chuyển
        // Để tránh việc nhân vật giữ vận tốc cũ khi chuyển cảnh
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
        }

        // 6. Lưu vị trí an toàn ban đầu
        lastSafePosition = transform.position; 
    }

    void Update()
    {
        // 1. ƯU TIÊN CAO NHẤT: Nếu đang nghỉ ngơi -> Khóa mọi Input
        if (isRestingAtCheckpoint)
        {
            rb.linearVelocity = Vector2.zero; // Đứng im
            horizontalInput = 0;
            anim.SetFloat("Speed", 0);
            return; // Ngắt toàn bộ logic bên dưới (không chạy, không nhảy, không đánh)
        }

        // --- [SỬA 1] KHAI BÁO BIẾN MOBILE CHUNG TẠI ĐÂY ---
        // Khai báo 1 lần dùng cho cả hàm, không cần khai báo lại bên dưới nữa
        bool mobileJump = (MobileInputManager.Instance != null && MobileInputManager.Instance.jumpPressed);
        float mobileX = (MobileInputManager.Instance != null) ? MobileInputManager.Instance.moveX : 0;

        // --- [MỚI] KIỂM TRA MENU ---
        // Biến này sẽ = true nếu Menu đang mở -> Ta dùng nó để chặn hành động
        bool isMenuBlocked = (PauseMenuController.Instance != null && PauseMenuController.Instance.isMenuOpen);

        HandleStamina();
        float currentMoveSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // =========================================================
        // BƯỚC 1: XÁC ĐỊNH TRẠNG THÁI CHẠM ĐẤT (GROUNDED) TRƯỚC
        // =========================================================
        SlopeCheck(); // 1. Check dốc
        bool isTouchingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer); // 2. Check đất

        // 3. Hợp nhất logic (Dốc + Đất bằng)
        if (isOnSlope || isTouchingGround)
        {
            // Nếu vận tốc Y gần bằng 0 -> Coi là đứng vững
            if (rb.linearVelocity.y < 0.1f)
            {
                isTouchingGround = true;
                isJumpingAction = false;
                isJumpingFromSlope = false;
            }
            // Đang đi dốc mà không nhảy -> Cũng là chạm đất
            else if (isOnSlope && !isJumpingAction) 
            {
                isTouchingGround = true; 
            }
        }
        
        // [QUAN TRỌNG] Cập nhật biến isGrounded NGAY TẠI ĐÂY
        // Để các hàm check tường bên dưới dùng được dữ liệu mới nhất
        isGrounded = isTouchingGround;

        // ================= [THÊM DÒNG NÀY VÀO NGAY ĐÂY] =================
        // Báo cáo cho Animator biết là "Tao chạm đất rồi nhé!"
        // Để cái điều kiện IsGrounded: false trong Any State nó chặn Wall Slide lại
        anim.SetBool("IsGrounded", isGrounded); 
        // ================================================================

        // ================= [THÊM LOGIC AIR ATTACK LANDING VÀO ĐÂY] =================
        if (isGrounded && isAirAttacking && rb.linearVelocity.y <= 0.1f)
        {
            isAirAttacking = false; 

            // [QUAN TRỌNG] XÓA DÒNG NÀY ĐI:
            // lastFrameVelocityY = 0f;  <-- XÓA NÓ! Để logic rơi bên trên đọc được vận tốc thật
            
            // Reset vận tốc hiện tại thì giữ nguyên
            rb.linearVelocity = Vector2.zero;

            // Tắt Hitbox...
            if (airAttackHitbox != null) airAttackHitbox.SetActive(false);
            
            // Tắt đèn đỏ Animator...
            anim.SetBool("IsAirAttacking", false);
            anim.SetTrigger("AirAttackLand");
            anim.ResetTrigger("AirAttack"); 
        }
        // =========================================================================

        // =========================================================
        // [FIX MỚI] CƯỠNG CHẾ TẮT WALL SLIDE KHI CHẠM ĐẤT
        // =========================================================
        if (isGrounded)
        {
            if (isWallSliding)
            {
                // Tắt biến logic
                isWallSliding = false; 
                
                // Tắt Animator Parameter ngay lập tức
                anim.SetBool("IsWallSliding", false); 
                
                // Reset cả hướng tường để không bị nhớ nhầm
                wallDir = 0; 
            }
        } 

        // =========================================================
        // BƯỚC 2: SAU ĐÓ MỚI CHECK TƯỜNG & MÉP
        // =========================================================
        CheckLedge();

        // Bây giờ hàm này sẽ thấy isGrounded = true -> Tự động tắt trượt tường ngay
        CheckWallSlide();

        // --- LOGIC GROUNDED HỢP NHẤT (Fix lỗi Jump/Fall ở bước trước) ---
        if (isOnSlope || isTouchingGround)
        {
            if (rb.linearVelocity.y < 0.1f)
            {
                isTouchingGround = true;
                isJumpingAction = false;
                isJumpingFromSlope = false;
            }
            else if (isOnSlope && !isJumpingAction) 
            {
                isTouchingGround = true; 
            }
        }
        
        // Cập nhật biến isGrounded
        isGrounded = isTouchingGround; // (Hoặc dùng logic coyote time của bạn)

        // =========================================================
        // 2. ƯU TIÊN SỐ 1: WALL JUMP (NHẢY TƯỜNG)
        // Phải đặt lên trên cùng để nó được ưu tiên hơn nhảy thường
        // =========================================================
        // [MỚI] Lấy input nhảy từ Mobile

        // Thêm || mobileJump vào điều kiện
        if (isWallSliding && (Input.GetKeyDown(KeyCode.Space) || mobileJump))
        {
            PerformWallJumpFromSlide(); 
            return; 
        }

        // =========================================================
        // 3. ƯU TIÊN SỐ 2: GROUND JUMP (NHẢY TỪ ĐẤT/DỐC)
        // =========================================================
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking && !isCrouching && currentStamina >= jumpStaminaCost)
        {
             UseStamina(jumpStaminaCost);
             
             rb.gravityScale = gravityScaleGoc; 
             isJumpingAction = true;
             isJumpingFromSlope = true;

             float currentJumpForce = isSprinting ? sprintJumpForce : jumpForce;
             rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
             return; 
        }

        // Reset biến nhảy từ dốc khi rơi
        if (rb.linearVelocity.y < 0) isJumpingFromSlope = false;

        // =========================================================
        // 4. XỬ LÝ DI CHUYỂN & DỐC
        // =========================================================
        float xInput = 0;
        // Chỉ nhận Input khi KHÔNG bị khóa do Wall Jump
        if (!isAttacking && !isUsingItem && !isHurt && !isWallJumpLocked) 
        {
            if (isKnockedDown) 
            {
                horizontalInput = 0;
            }
            else 
            {
                // Khai báo biến keyboardX ngay tại đây để dùng luôn
                float keyboardX = Input.GetAxisRaw("Horizontal");
                
                // mobileX đã có ở đầu hàm, lấy xuống dùng luôn
                horizontalInput = keyboardX + mobileX;
                horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f); 
            }
        }

        // [QUAN TRỌNG] Thêm điều kiện: Không trượt tường mới được đi dốc
        if (isOnSlope && !isJumpingAction && !isJumpingFromSlope && !isWallSliding && !isKnockedDown && !isHanging && !isClimbing)
        {
            Vector2 newVelocity = slopeNormalPerp * -xInput * currentMoveSpeed;

            if (xInput != 0)
            {
                rb.gravityScale = gravityScaleGoc;
                rb.linearVelocity = newVelocity;
                rb.AddForce(Vector2.down * slopeForce, ForceMode2D.Force); 
            }
            else
            {
                rb.gravityScale = 0f; 
                rb.linearVelocity = Vector2.zero; 
            }
        }
        // DI CHUYỂN BÌNH THƯỜNG (Đất bằng hoặc Trên không)
        else 
        {
            if (!isJumpingAction) rb.gravityScale = gravityScaleGoc;
            
            // [FIX - QUAN TRỌNG] Thêm !isKnockedDown vào đây
            // Để khi bị đánh, vận tốc do người chơi bấm nút KHÔNG ghi đè lên vận tốc bị đánh bay
            if (!isWallSliding && !isWallJumpLocked && !isKnockedDown && !isHanging && !isClimbing)
            {
                rb.linearVelocity = new Vector2(xInput * currentMoveSpeed, rb.linearVelocity.y);
            }
        }

        // ================= [THÊM ĐOẠN FIX NÀY VÀO] =================
        // "TRỢ LỰC THANG MÁY": Khắc phục lỗi kẹt animation Fall
        // Nếu vòng tròn ngắn không thấy đất, NHƯNG đang đi thang máy
        if (!isTouchingGround && isOnElevator)
        {
            // Chỉ kiểm tra khi nhân vật đang rơi xuống hoặc đứng yên (Vận tốc Y <= 0.1)
            // (Để tránh việc vừa nhảy lên đã bị hút xuống đất)
            if (rb.linearVelocity.y <= 0.1f)
            {
                // Bắn một tia Raycast dài hơn bình thường xuống dưới (ví dụ dài 0.2f hoặc 0.3f)
                // Nó giống như nhân vật cố vươn chân ra để tìm sàn thang máy đang trôi xuống
                RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.3f, groundLayer);
                
                if (hit.collider != null)
                {
                    isTouchingGround = true; // Ép buộc nhận là đang chạm đất
                }
            }
        }
        // =============================================================

        // =========================================================
        // 2. LOGIC TÍNH SÁT THƯƠNG RƠI THEO VẬN TỐC (MỚI)
        // =========================================================
        
        // Chỉ tính khi: Vừa mới tiếp đất VÀ Frame trước đó đang rơi xuống (vận tốc âm)
        if (isTouchingGround && !wasTouchingGround && lastFrameVelocityY < -0.1f)
        {
            if (isOnElevator) return; // Miễn tử kim bài thang máy

            float impactSpeed = Mathf.Abs(lastFrameVelocityY);
            
            // --- [LOGIC MỚI] XÁC ĐỊNH NGƯỠNG CHỊU ĐỰNG ---
            // Mặc định là ngưỡng của người thường (ví dụ: 18)
            float currentSafeThreshold = minFallSpeedToHurt;
            
            // Nếu đang dùng Air Attack -> Được buff ngưỡng chịu đựng lên cao (ví dụ: 35)
            if (isAirAttacking)
            {
                currentSafeThreshold = airAttackSafeSpeed;
                Debug.Log($"Đang Air Attack! Ngưỡng chịu đựng tăng lên: {currentSafeThreshold}");
            }
            // ---------------------------------------------

            // A. MỨC 1: RƠI QUÁ MẠNH -> MẤT MÁU + STAMINA
            // So sánh với ngưỡng hiện tại (currentSafeThreshold) thay vì số cứng
            if (impactSpeed > currentSafeThreshold)
            {
                // Tính dam dựa trên phần dôi dư
                float damage = (impactSpeed - currentSafeThreshold) * damageMultiplier;
                damage = Mathf.Round(damage);

                Debug.Log($"Rơi quá cao! Tốc độ: {impactSpeed}/{currentSafeThreshold}. Mất {damage} máu.");

                PlayerHealth health = GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    if (health.currentBlood > 0) TriggerHurt(0.5f);
                }
                UseStamina(maxStamina);
            }
            // B. MỨC 2: RƠI VỪA VỪA (Chỉ mất Stamina) - Logic cũ
            else if (impactSpeed > minFallSpeedToStamina && !isAirAttacking)
            {
                // Nếu đang Air Attack thì thôi tha cho cái Stamina rơi nhẹ này
                float stamLoss = (impactSpeed - minFallSpeedToStamina) * damageMultiplier;
                UseStamina(stamLoss);
            }
        }

        // [QUAN TRỌNG] Lưu lại vận tốc Y của frame hiện tại
        // Để frame sau (khi chạm đất và vận tốc về 0) ta vẫn biết frame trước rơi nhanh thế nào
        lastFrameVelocityY = rb.linearVelocity.y;

        // 4. KIỂM TRA RƠI VỰC (ABYSS)
        if (transform.position.y < abyssLimitY)
        {
            Debug.LogError($"CHẾT DO RƠI VỰC! Vị trí Y hiện tại: {transform.position.y}, Giới hạn: {abyssLimitY}");
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null) health.DieInstant(); 
            return;
        }

        // 5. CẬP NHẬT TRẠNG THÁI GROUNDED (Coyote Time)
        if (isTouchingGround)
        {
            lastGroundedTime = Time.time;

            // [MỚI] Nếu đã chạm đất thì tắt trạng thái đang nhảy đi
            // Để game hiểu là mọi vận tốc Y lúc này chỉ là do địa hình thôi
            if (rb.linearVelocity.y <= 0.1f) // Chỉ reset khi không bay lên quá mạnh
            {
                 isJumpingAction = false;
            }
            
            // [LOGIC AN TOÀN] Chỉ lưu vị trí mới nếu:
            // - Đang đứng vững (!KnockedDown, !Hurt)
            // - Vừa tiếp đất an toàn (hoặc đang đi bộ bình thường)
            if (!isKnockedDown && !isHurt)
            {
                lastSafePosition = transform.position;
            }
        }

        isGrounded = (Time.time - lastGroundedTime < 0.15f);
        
        // Lưu trạng thái cho frame sau
        wasTouchingGround = isTouchingGround;
        
        // NẾU BỊ KNOCKDOWN -> KHÓA CỨNG
        if (isKnockedDown) 
        {
            horizontalInput = 0; // [QUAN TRỌNG] Ép input về 0 để không bị trôi
            return; // Ngắt toàn bộ logic khác
        }

        // 1. KIỂM TRA TƯỜNG & MÉP (Ưu tiên cao nhất)
        CheckLedge();     
        CheckWallSlide(); 

        // A. Xử lý Nhảy Tường khi đang Trượt (Wall Jump from Slide)
        if (isWallSliding && Input.GetKeyDown(KeyCode.Space))
        {
            PerformWallJumpFromSlide();
            return; // Thoát luôn để tránh xung đột
        }

        // B. Nếu đang Bám hoặc Leo -> Chuyển quyền điều khiển sang hàm riêng
        if (isHanging || isClimbing) 
        {
            UpdateLedgeClimbState();
            return; 
        }

        // --- XỬ LÝ ĐÁNH MẠNH (HEAVY ATTACK) ---

        // 1. KÍCH HOẠT TỤ (Ấn giữ chuột phải)
        bool mobileHeavy = (MobileInputManager.Instance != null && MobileInputManager.Instance.heavyAttackPressed);
        // Kích hoạt bằng Chuột Phải HOẶC Nút Mobile
        if ((Input.GetMouseButtonDown(1) || mobileHeavy) && !isMenuOpen && isGrounded && !isAttacking && !isRolling)
        {
            // TRƯỜNG HỢP A: ĐANG TRONG THỜI GIAN COMBO -> CHUẨN BỊ GỒNG ĐÒN 2
            if (canPerformHeavyCombo)
            {
                if (currentStamina >= heavyAttack2StaminaCost)
                {
                    heavyComboStep = 2; // [QUAN TRỌNG] Đánh dấu là đang gồng đòn 2
                    StartCharging();    // Gọi hàm gồng chung
                }
                else
                {
                    Debug.Log("Muốn combo đòn 2 nhưng hết hơi!");
                }
                return; 
            }

            // TRƯỜNG HỢP B: ĐÁNH MỚI TỪ ĐẦU -> CHUẨN BỊ GỒNG ĐÒN 1
            if (!isAttacking && currentStamina >= heavyAttackStaminaCost)
            {
                heavyComboStep = 1; // [QUAN TRỌNG] Đánh dấu là gồng đòn 1
                StartCharging();    // Gọi hàm gồng chung
            }
        }

        // 2. TRONG KHI ĐANG TỤ
        if (isCharging)
        {
            // A. KHÓA CHẶT DI CHUYỂN (Fix lỗi của bạn ở đây)
            horizontalInput = 0;
            rb.linearVelocity = Vector2.zero; // Dừng mọi quán tính ngay lập tức
            
            // B. TÍNH TOÁN THỜI GIAN
            currentChargeTime += Time.deltaTime;

            // C. TỰ ĐỘNG CHÉM KHI ĐẦY (Auto-Release Logic)
            if (currentChargeTime >= maxChargeTime)
            {
                currentChargeTime = maxChargeTime; // Chốt số
                ReleaseHeavyAttack(); // TỰ ĐỘNG BỔ XUỐNG
                return; // Thoát luôn để không chạy code bên dưới
            }

            // D. XẢ SỚM (Nếu thả tay trước khi đầy -> Chém lực yếu hơn)
            // Logic: Nếu NHẢ chuột phải VÀ KHÔNG giữ nút mobile -> Thì tính là xả
            bool isHoldingMouse = Input.GetMouseButton(1);
            bool isHoldingMobile = (MobileInputManager.Instance != null && MobileInputManager.Instance.heavyAttackPressed);

            if (!isHoldingMouse && !isHoldingMobile)
            {
                ReleaseHeavyAttack();
                return;
            }

            // E. HỦY BỎ (Nếu bị đánh trúng hoặc bấm Roll)
            if (isKnockedDown) 
            { 
                CancelCharging(); 
                return; 
            }

            // Chặn code di chuyển bên dưới chạy
            return; 
        }

        // 3. CHẶN INPUT (Nếu đang lộn, trượt, hoặc lướt đánh -> Không làm gì khác)
        if (isRolling || isSliding || isAttackStepping) return;

        // --- XỬ LÝ CHẠY NHANH (SPRINT) ---
        // Chỉ chạy nhanh được khi: Đang chạm đất VÀ Không ngồi VÀ Không trượt tường
        bool mobileSprint = (MobileInputManager.Instance != null && MobileInputManager.Instance.isSprinting);
        
        // Chạy nếu: Giữ Shift HOẶC Mobile bật chế độ chạy
        if ((Input.GetKey(KeyCode.LeftShift) || mobileSprint) && isGrounded && !isCrouching && !isWallSliding)
        {
            isSprinting = true;
            anim.SetBool("IsSprinting", true); // (Tạo Bool này trong Animator nếu muốn)
        }
        else
        {
            isSprinting = false;
            anim.SetBool("IsSprinting", false);
        }
        // Gửi biến này vào Animator
        anim.SetBool("IsSprinting", isSprinting);

        // 4. XỬ LÝ NGỒI (CROUCH) & ĐI KHOM
        bool isMovingInput = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
        bool mobileCrouch = (MobileInputManager.Instance != null && MobileInputManager.Instance.isCrouching);

        if ((Input.GetKey(KeyCode.S) || mobileCrouch) && isGrounded)
        {
            isCrouching = true;
            isSprinting = false; //Nếu ngồi thì tắt chạy nhanh ngay
            anim.SetBool("IsCrouching", true);
            
            // [QUAN TRỌNG] Báo cho Animator biết có đang "Đi Khom" không
            anim.SetBool("IsCrouchMoving", isMovingInput); 

            SetColliderHeight(standSize.y * 0.6f); 
        }
        else
        {
            isCrouching = false;
            anim.SetBool("IsCrouching", false);
            anim.SetBool("IsCrouchMoving", false);
            
            SetColliderHeight(standSize.y); 
        }

        // 5. CÁC KỸ NĂNG (Roll, Slide, Attack)
        bool mobileRoll = (MobileInputManager.Instance != null && MobileInputManager.Instance.dashPressed);

        if ((Input.GetKeyDown(KeyCode.LeftAlt) || mobileRoll) && isGrounded && !isCrouching)
        {
            // Cho phép Roll nếu: (Bình thường) HOẶC (Đang đứng dậy)
            if ((!isKnockedDown || isGettingUp) && currentStamina >= rollStaminaCost)
            {
                // Nếu đang Đứng Dậy mà bấm Roll -> HỦY KNOCKDOWN NGAY
                if (isGettingUp)
                {
                    StopCoroutine(knockdownRoutine); // Dừng cái việc đứng dậy lại
                    isKnockedDown = false;
                    isGettingUp = false;
                    
                    // Reset Trigger để Animator không bị kẹt
                    anim.ResetTrigger("GetUp");
                    anim.ResetTrigger("Knockdown");
                }

                // Thực hiện Roll như bình thường
                if (Time.time > nextRollTime) 
                {
                    UseStamina(rollStaminaCost); // <--- TRỪ STAMINA 
                    StartCoroutine(PerformRoll()); 
                    return; // Thoát Update
                }
            }
        }

        // --- CHẶN INPUT NẾU BỊ KNOCKDOWN ---
        // (Lúc này nếu đang GettingUp mà không Roll thì vẫn bị chặn di chuyển)
        if (isKnockedDown) return;
        bool mobileSlide = (MobileInputManager.Instance != null && MobileInputManager.Instance.slidePressed);

        if ((Input.GetKeyDown(KeyCode.C) || mobileSlide) && isGrounded && !isCrouching)
        {
            // [MỚI] Kiểm tra đủ Stamina KHÔNG? VÀ đủ Mana KHÔNG?
            bool enoughStamina = currentStamina >= 10f; // (Giả sử trượt tốn 10 Stamina)
            bool enoughMana = currentMana >= slideManaCost;

            if (Time.time > nextSlideTime && enoughStamina && enoughMana) 
            { 
                // TRỪ TIỀN
                UseStamina(10f);       // Trừ Stamina (bạn tự chỉnh số này)
                UseMana(slideManaCost); // Trừ Mana

                StartCoroutine(PerformSlide()); 
                return; 
            }
        }

        // 5. XỬ LÝ TẤN CÔNG (Ground & Air)
        bool mobileAttack = (MobileInputManager.Instance != null && MobileInputManager.Instance.attackPressed);

        // --- [SỬA] TÁCH BIỆT LOGIC PC VÀ MOBILE ---
        bool wantToAttack = false;

#if UNITY_ANDROID || UNITY_IOS
        // TRÊN ĐIỆN THOẠI: Chỉ nhận lệnh từ nút ảo (MobileAttack)
        // Tuyệt đối KHÔNG nghe lệnh từ chuột hay bàn phím
        wantToAttack = mobileAttack;
#else
        // TRÊN PC: Nhận cả Chuột trái, Phím J, và nút ảo (để test)
        wantToAttack = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J) || mobileAttack;
#endif

        // Kiểm tra điều kiện chung
        if (wantToAttack && !isMenuOpen)
        {
            // [FIX QUAN TRỌNG] Logic xác định đánh dưới đất hay trên trời
            bool isReallyGrounded = isGrounded && !isJumpingAction && rb.linearVelocity.y <= 0.1f;

            if (isReallyGrounded)
            {
                if (Time.time > nextAttackAllowTime && !isAirAttacking) 
                { 
                    Attack(); 
                    return; 
                }
            }
            else
            {
                if (!isAirAttacking && !isWallSliding && !isHanging && !isClimbing)
                {
                    PerformAirAttack(); 
                    return;
                }
            }
        }

        // 6. XỬ LÝ TRẠNG THÁI ANIMATION & DI CHUYỂN
        // 1. Kiểm tra trạng thái (Giữ nguyên)
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isPlayingAttackAnim = stateInfo.IsName("Player_Attack1") || 
                                   stateInfo.IsName("Player_Attack2") || 
                                   stateInfo.IsName("Player_Attack3");

        // 2. Tính toán biến "Đang Đánh" (Bao gồm cả thời gian chờ) - (Giữ nguyên)
        // isAttacking = "Animation đang chạy" HOẶC "Mới bấm nút xong"
        isAttacking = isPlayingAttackAnim || (Time.time - lastAttackInputTime < 0.2f);

        // 3. [SỬA] Cập nhật Animator (Dùng isAttacking)
        // Để cái chốt chặn "IsAttacking" trong Animator bật lên NGAY LẬP TỨC
        anim.SetBool("IsAttacking", isAttacking);

        // 4. [SỬA] Xử lý Di Chuyển (Dùng isAttacking)
        // Chặn di chuyển ngay lập tức khi bấm nút, không chờ animation chạy
        // 4. [SỬA] Xử lý Di Chuyển & Animation
        if (isAttacking || isUsingItem || isHurt) 
        {
            horizontalInput = 0; 
        }
        else
        {
            // [FIX LỖI CS0103] Khai báo lại biến ngay tại đây để dùng
            float kX = Input.GetAxisRaw("Horizontal");
            float mX = (MobileInputManager.Instance != null) ? MobileInputManager.Instance.moveX : 0;
            
            // Cộng dồn và kẹp giá trị
            horizontalInput = kX + mX;
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f); 
        }

        // [FIX ANIMATION] Ngăn chặn Run chen vào Jump khi đang Wall Jump
        // Khi đang bị khóa di chuyển (Wall Jump), dù có bấm nút thì Animator vẫn nhận Speed = 0
        if (isWallJumpLocked)
        {
            anim.SetFloat("Speed", 0f);
        }
        else
        {
            anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        }

        // ================= [THAY THẾ TOÀN BỘ ĐOẠN NÀY] =================
        // 5. CẬP NHẬT yVelocity CHO ANIMATOR (LOGIC "THIẾT QUÂN LUẬT")
        // [FIX ANIMATION] Logic cập nhật yVelocity cho Animator
        float animationY = 0f;

        // 1. Nếu đang Air Attack -> "Nói dối" Animator là vận tốc = 0
        // Để nó không tự chuyển sang Fall/Jump
        if (isAirAttacking)
        {
            animationY = 0f; 
        }
        // 2. Nếu đang đi thang máy -> Coi như đứng yên
        else if (isOnElevator)
        {
            animationY = 0f;
        }
        // 3. Nếu đang trượt tường
        else if (isWallSliding)
        {
            animationY = 0f; 
        }
        // 4. Nếu đang nhảy (chủ động)
        else if (isJumpingAction) 
        {
            animationY = rb.linearVelocity.y;
        }
        // 5. Nếu đang rơi tự do
        else if (!isGrounded) 
        {
            if (rb.linearVelocity.y < -4.0f) 
            {
                animationY = rb.linearVelocity.y;
            }
            else 
            {
                animationY = 0f; 
            }
        }

        // Gửi thông số đã xử lý vào Animator
        anim.SetFloat("yVelocity", animationY);
        // ==============================================================

        // 7. NHẢY (JUMP)
        // Không nhảy được khi: Đang ngồi, Đang đánh, hoặc Đang trượt tường (đã xử lý ở trên)
        // Thêm điều kiện: currentStamina >= jumpStaminaCost

        if ((Input.GetKeyDown(KeyCode.Space) || mobileJump) && isGrounded && !isAttacking && !isCrouching && currentStamina >= jumpStaminaCost)
        {
            // Reset lại trọng lực ngay lập tức trước khi nhảy
            rb.gravityScale = gravityScaleGoc;
            UseStamina(jumpStaminaCost); // <--- TRỪ STAMINA

            // [MỚI] Bật cờ xác nhận: Đây là tao nhảy thật!
            isJumpingAction = true;

            float currentJumpForce = isSprinting ? sprintJumpForce : jumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
            {
                // Thêm điều kiện: Nếu đang Air Attack thì không đánh thường nữa
                if (!isAirAttacking && Time.time > nextAttackAllowTime) { Attack(); return; }
            }
        }

        // --- [MỚI] MOBILE USE ITEM (Nút Health riêng) ---
        bool mobileUseItem = (MobileInputManager.Instance != null && MobileInputManager.Instance.useItemPressed);
        if (mobileUseItem && isGrounded && !isRolling && !isSliding && !isAttackStepping && !isUsingItem && !isElevatorBlockingInput)
        {
            StartCoroutine(PerformUseItem());
            // Không return ở đây để các logic E bên dưới vẫn chạy nếu cần, 
            // nhưng PerformUseItem đã set cờ isUsingItem nên sẽ an toàn.
        }

        // 8. USE ITEM (Phím E)
        // Chỉ dùng được khi đứng yên dưới đất và không làm gì khác
        if (Input.GetKeyDown(KeyCode.E) && isGrounded && !isRolling && !isSliding && !isAttackStepping && !isUsingItem && !isElevatorBlockingInput)
        {
            // A. ƯU TIÊN ĐẶC BIỆT: KẾT THÚC GAME (Đặt lên đầu tiên)
            if (currentEndGameStatue != null)
            {
                // Dừng nhân vật lại
                rb.linearVelocity = Vector2.zero;
                anim.SetFloat("Speed", 0);
                
                // Gọi hàm kết thúc
                currentEndGameStatue.Interact();
                return;
            }
            
            // A. ƯU TIÊN 1: VÀO CỬA BOSS (Quan trọng nhất)
            if (currentDoor != null)
            {
                // Dừng nhân vật lại cho đẹp
                rb.linearVelocity = Vector2.zero;
                anim.SetFloat("Speed", 0);
                
                currentDoor.Interact();
                return; // Thoát ngay
            }

            // B. ƯU TIÊN 2: TƯƠNG TÁC TƯỢNG (CHECKPOINT)
            if (currentCheckpoint != null)
            {
                currentCheckpoint.Interact(); 
                return; 
            }

            // C. ƯU TIÊN 3: NHẶT BIA MỘ
            if (currentDeathMarker != null)
            {
                currentDeathMarker.Interact();
                return; 
            }

            // D. ƯU TIÊN 4: DÙNG ITEM
            StartCoroutine(PerformUseItem());
            return;
        }

        // ====================================================
        // --- XỬ LÝ STAMINA (HỒI PHỤC & TRỪ THEO THỜI GIAN) ---
        // ====================================================

        bool isDrainingStamina = false;

        // 1. TRỪ KHI CHẠY (Sprint)
        // Điều kiện: Đang chạy + Đang di chuyển thực sự
        if (isSprinting && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            isDrainingStamina = true;
            currentStamina -= sprintStaminaCost * Time.deltaTime;

            // Nếu hết thể lực -> Tắt chạy ngay
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isSprinting = false; 
                anim.SetBool("IsSprinting", false);
            }
        }

        // 2. TRỪ KHI TREO MÌNH (Hanging / Wall Jump Prep)
        // Bao gồm cả lúc treo thường và lúc chuẩn bị nhảy
        if (isHanging)
        {
            isDrainingStamina = true;
            currentStamina -= ledgeHangStaminaCost * Time.deltaTime;

            // Nếu hết thể lực khi đang treo -> RƠI TỰ DO
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                CancelHanging(); // Buông tay rơi xuống
            }
        }

        // 3. LOGIC HỒI PHỤC
        if (isDrainingStamina)
        {
            lastStaminaUseTime = Time.time; // Reset thời gian chờ
        }
        else
        {
            // Chỉ hồi khi đã qua thời gian chờ (Delay) và chưa đầy bình
            if (Time.time - lastStaminaUseTime > staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        // Cập nhật UI liên tục
        if (hud != null) hud.UpdateStamina(currentStamina, maxStamina);
        
        // ĐỔI ĐỒ (Ví dụ dùng phím R hoặc Lăn chuột)
        // Hoặc phím mũi tên xuống (Down Arrow) giống Dark Souls
        if (!isMenuOpen) 
        {
            bool mobileSwitch = (MobileInputManager.Instance != null && MobileInputManager.Instance.switchItemPressed);

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.R) || mobileSwitch)
            {
                if (consumableManager != null) consumableManager.SwitchItem();
            }
        }

        // 10. QUAY MẶT (FLIP)
        // Chỉ quay khi không đánh, không bám, không leo, không trượt tường
        if (!isPlayingAttackAnim && !isHanging && !isClimbing && !isWallSliding)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        // =========================================================
        // 1. XỬ LÝ TRƯỢT TƯỜNG (Ưu tiên xử lý trục Y và lực ép tường)
        // =========================================================
        if (isWallSliding && !isWallJumpLocked)
        {
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
            float pushDir = isFacingRight ? 1f : -1f;
            rb.AddForce(new Vector2(pushDir * wallStickForce, 0), ForceMode2D.Force);
            return; 
        }

        // =========================================================
        // 2. CHỐT CHẶN VẬT LÝ (QUAN TRỌNG NHẤT)
        // =========================================================
        // Nếu đang bị Knockdown, Lộn, Trượt, Leo trèo... 
        // -> TUYỆT ĐỐI KHÔNG CAN THIỆP VÀO VELOCITY
        // Để cho quán tính và lực đẩy (Knockback Force) tự do hoạt động
        if (isKnockedDown || isRolling || isSliding || isAttackStepping || isUsingItem || isHanging || isClimbing || isWallJumping) 
        {
            return; 
        }

        // [FIX] Nếu đang Wall Jump (Locked) -> Return luôn, để Physics tự lo việc bay lượn
        if (isWallJumpLocked) return;

        // =========================================================
        // 3. DI CHUYỂN BÌNH THƯỜNG (Chỉ chạy khi rảnh tay)
        // =========================================================
        float currentSpeed = moveSpeed; // Mặc định đi bộ

        if (isCrouching) 
        {
            currentSpeed = crouchSpeed; // Đi khom
        }
        else if (isSprinting) 
        {
            currentSpeed = sprintSpeed; // Chạy nhanh
            
            // Cộng hưởng quán tính khi nhảy (giữ nguyên logic cũ của bạn)
            if (!isGrounded) currentSpeed *= 1.2f;
        }

        // Áp dụng vận tốc di chuyển
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    // =================================================
    // 4. CÁC HÀM XỬ LÝ KỸ NĂNG (ROLL, SLIDE, ATTACK,...)
    // =================================================
    IEnumerator PerformRoll()
    {
        isRolling = true;
        anim.SetTrigger("Roll");
        SetColliderHeight(standSize.y * 0.6f);

        float direction = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * rollForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(0.5f); 

        SetColliderHeight(standSize.y);
        isRolling = false;
        nextRollTime = Time.time + rollCooldown;
    }

    IEnumerator PerformSlide()
    {
        isSliding = true;
        anim.SetTrigger("Slide");
        
        // Hạ thấp Collider
        SetColliderHeight(standSize.y * 0.5f);
        
        // Tắt va chạm với quái
        //Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        float direction = isFacingRight ? 1f : -1f;

        // [FIX QUAN TRỌNG] Thay vì gán 1 lần, ta dùng vòng lặp để ÉP vận tốc liên tục
        // Đảm bảo nhân vật lướt đi bất chấp ma sát
        float slideTimer = 0f;
        float duration = 0.4f; // Thời gian trượt (khớp với Animation)

        while (slideTimer < duration)
        {
            // Nếu lỡ đâm đầu vào tường thì dừng lại (để không bị rung lắc)
            if (isTouchingWall) 
            {
                break; 
            }

            // Gán vận tốc liên tục mỗi khung hình
            rb.linearVelocity = new Vector2(direction * slideForce, rb.linearVelocity.y);
            
            slideTimer += Time.deltaTime;
            yield return null; // Chờ sang frame tiếp theo
        }

        // Hết thời gian trượt -> Hãm phanh lại một chút
        rb.linearVelocity = Vector2.zero; 
        
        // Thời gian nghỉ (Recovery) để người chơi không spam nút liên tục
        yield return new WaitForSeconds(0.2f); 

        // Khôi phục Collider
        SetColliderHeight(standSize.y);
        
        // Bật lại va chạm
        //Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        isSliding = false;
        nextSlideTime = Time.time + slideCooldown;
    }

    void Attack()
    {
        // Kiểm tra đủ sức không
        if (currentStamina < attackStaminaCost) return; 

        UseStamina(attackStaminaCost); // <--- TRỪ STAMINA

        // 1. Ghi lại thời gian bấm (để duy trì trạng thái trong Update)
        lastAttackInputTime = Time.time;
        
        // 2. [QUAN TRỌNG] Cưỡng ép Animator biết là đang đánh NGAY LẬP TỨC
        anim.SetBool("IsAttacking", true); 
        
        // 3. [QUAN TRỌNG] Triệt tiêu vận tốc rơi NGAY LẬP TỨC
        // Để yVelocity về 0, không cho điều kiện Fall xảy ra
        rb.linearVelocity = Vector2.zero; 

        // 4. Logic Combo cũ
        if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;
        comboStep++;
        if (comboStep > 3) comboStep = 1;

        anim.SetInteger("ComboCounter", comboStep);
        anim.SetTrigger("Attack");
        
        lastAttackTime = Time.time;
        nextAttackAllowTime = Time.time + attackCooldown;

        StartCoroutine(PerformAttackStep());
    }

    IEnumerator PerformAttackStep()
    {
        isAttackStepping = true; 
        float direction = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * attackStepForce, rb.linearVelocity.y);
        yield return new WaitForSeconds(attackStepDuration);
        isAttackStepping = false; 
    }

    // Dùng void, KHÔNG dùng IEnumerator nữa
    void PerformAirAttack()
    {
        anim.SetBool("IsAirAttacking", true);
        isAirAttacking = true;
        
        // 1. Kích hoạt Animation "Start" (Vung kiếm)
        anim.SetTrigger("AirAttack");

        // 2. Bật Hitbox ngay lập tức
        if (airAttackHitbox != null)
        {
            airAttackHitbox.SetActive(true);
        }

        // [QUAN TRỌNG] Tuyệt đối KHÔNG can thiệp vào Rigidbody ở đây
        // Để nhân vật tự bay theo quán tính vòng cung của cú nhảy
    }

    IEnumerator PerformUseItem()
    {
        isUsingItem = true; // Khóa di chuyển
        
        // 1. Dừng nhân vật lại ngay lập tức
        rb.linearVelocity = Vector2.zero; 
        
        // 2. Chạy Animation uống thuốc
        anim.SetTrigger("UseItem");

        // 3. Thời gian chờ (Nên khớp với độ dài animation của bạn)
        // Ví dụ: animation dài 0.8s thì để 0.8f
        float itemDuration = 0.8f; 
        yield return new WaitForSeconds(itemDuration);

        // --- [SỬA LẠI ĐOẠN NÀY] ---
        // Thay vì Heal trực tiếp, ta bảo Manager dùng món đang chọn
        if (consumableManager != null)
        {
            consumableManager.UseCurrentItem();
        }
        // --------------------------

        Debug.Log("Đã dùng Item xong!");

        // 5. Mở khóa điều khiển
        isUsingItem = false; 
    }

    // Hàm này được gọi từ PlayerHealth khi bị đánh (nhẹ)
    public void TriggerHurt(float duration)
    {
        // Thêm dòng này để kiểm tra
        Debug.Log("GỌI TRIGGER HURT! Knoked: " + isKnockedDown + " | Hurt: " + isHurt);

        if (isKnockedDown || isHurt) return; // Nếu đang ngã hoặc đang đau rồi thì thôi

        StartCoroutine(PerformHurt(duration));
    }

    IEnumerator PerformHurt(float duration)
    {
        isHurt = true;
        
        // Dừng nhân vật lại ngay
        rb.linearVelocity = Vector2.zero; 
        anim.SetFloat("Speed", 0); // Báo Animator là tao đứng lại rồi, đừng chuyển sang Run!
        
        // Kích hoạt animation
        anim.SetTrigger("Hurt");

        // Chờ diễn xong cảnh đau
        yield return new WaitForSeconds(duration);

        isHurt = false;
    }

    IEnumerator PerformKnockdown(Vector2 dir)
    {
        isKnockedDown = true;
        isGettingUp = false;
        horizontalInput = 0;

        // Reset ngay lập tức các trạng thái "bay lượn" nếu có
        rb.gravityScale = defaultGravity; // (Trả lại trọng lực)
        
        // [MỚI] Reset trạng thái Air Attack nếu đang dở dang
        isAirAttacking = false;
        anim.SetBool("IsAirAttacking", false); 
        if (airAttackHitbox != null) airAttackHitbox.SetActive(false);

        // Hủy luôn trạng thái tụ lực/đánh đấm nếu đang dở dang
        isCharging = false;
        isAttacking = false;
        anim.SetBool("IsCharging", false);
        anim.SetBool("IsAttacking", false);

        // 1. Reset các Trigger cũ để tránh xung đột
        anim.ResetTrigger("Attack");

        // Ép Animator hiểu là tao KHÔNG CÓ vận tốc dọc, để nó không cố chuyển về Jump/Fall
        anim.SetFloat("yVelocity", 0); 
        anim.SetFloat("Speed", 0);
        
        // [QUAN TRỌNG] Bật Bool để KHOÁ Animator, cấm chuyển sang Fall/Idle
        anim.SetBool("IsKnockedDown", true); 
        // Kích hoạt Trigger để chuyển state lần đầu
        anim.SetTrigger("Knockdown");

        // 2. XỬ LÝ VẬT LÝ (Giữ nguyên)
        rb.linearVelocity = Vector2.zero; 
        rb.linearVelocity = new Vector2(dir.x * knockbackForceX, knockbackForceY);

        // Nằm đất 1.5 giây (Lúc này Animation Knockdown sẽ chạy hết và giữ nguyên frame cuối)
        yield return new WaitForSeconds(1.5f);

        // 3. GET UP (Đứng dậy)
        isGettingUp = true;
        anim.SetTrigger("GetUp");

        // Logic lướt tới khi đứng dậy
        float forwardDir = isFacingRight ? 1f : -1f;
        rb.linearVelocity = Vector2.zero; 
        rb.linearVelocity = new Vector2(forwardDir * getUpForwardForce, 0); 

        yield return new WaitForSeconds(0.8f);

        // [QUAN TRỌNG] Tắt Bool để Animator biết là sắp được tự do
        anim.SetBool("IsKnockedDown", false); 

        isKnockedDown = false;
        isGettingUp = false;
    }

    // =================================================
    // 5. CÁC HÀM XỬ LÝ LEO TRÈO (LEDGE CLIMB) - [MỚI]
    // =================================================
    void CheckLedge()
    {
        if (isGrounded || Time.time < nextLedgeGrabTime || isHanging || isClimbing) return;

        // --- [MỚI] KIỂM TRA INPUT NGƯỢC HƯỚNG ---
        // Nếu người chơi đang bấm nút đi ngược lại hướng mặt, thì ĐỪNG bám
        float xInput = Input.GetAxisRaw("Horizontal");
        bool pullingAway = (isFacingRight && xInput < -0.1f) || (!isFacingRight && xInput > 0.1f);
        
        if (pullingAway) return; // Thoát ngay, không bắn tia gì cả
        // ----------------------------------------

        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        
        // (Code Raycast cũ giữ nguyên)
        Vector2 wallCheckPos = (Vector2)transform.position + (Vector2.up * wallCheckOffsetY); 
        isTouchingWall = Physics2D.Raycast(wallCheckPos, direction, wallCheckDist, wallLayer);
        
        Vector2 ledgeCheckPos = (Vector2)transform.position + (Vector2.up * ledgeCheckHeight);
        isTouchingLedge = Physics2D.Raycast(ledgeCheckPos, direction, wallCheckDist, wallLayer);

        if (isTouchingWall && !isTouchingLedge && !isHanging && !isClimbing)
        {
            // [MỚI] Lưu lại hướng tường ngay khoảnh khắc bám
            wallDir = isFacingRight ? 1 : -1;

            StartHanging();
        }
    }

    void StartHanging()
    {
        isHanging = true;
        
        // [FIX] Khóa cứng mọi chuyển động. Thách cụ trọng lực cũng không kéo xuống được.
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeAll; // <--- CHÌA KHÓA LÀ ĐÂY
        
        // Đẩy nhân vật dính sát vào tường một chút để đảm bảo Animation đẹp
        // (Tùy chọn: Nếu thấy nhân vật bị lún vào tường thì bỏ đoạn này đi)
        float pushDir = isFacingRight ? 0.05f : -0.05f;
        transform.position += new Vector3(pushDir, 0, 0);

        anim.SetBool("IsHanging", true);
    }

    void UpdateLedgeClimbState()
    {
        if (isHanging)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.angularVelocity = 0f;

            // [MỚI] Lấy Input Mobile ngay trong hàm này
            bool mJump = (MobileInputManager.Instance != null && MobileInputManager.Instance.jumpPressed);
            // [MỚI] Lấy Input hướng (cho việc kéo cần ra xa tường)
            float mX = (MobileInputManager.Instance != null) ? MobileInputManager.Instance.moveX : 0;
            float totalX = Input.GetAxisRaw("Horizontal") + mX;

            // Logic check kéo ra xa (Pulling Away)
            bool pullingAway = (totalX * wallDir < -0.1f);
            
            if (pullingAway)
            {
                anim.SetBool("IsWallJumpPrep", true); 
                ForceFace(-wallDir);
                
                // 1. LOGIC NHẢY RA XA (Jump Off)
                // Thêm || mJump
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || mJump) 
                { 
                    PerformWallJump(); 
                    return; 
                }
            }
            else
            {
                anim.SetBool("IsWallJumpPrep", false); 
                ForceFace(wallDir); 
            }

            // 2. LOGIC LEO LÊN (Climb Up)
            // Thêm || mJump
            // Lưu ý: Chỉ leo lên khi KHÔNG kéo cần ra xa (!pullingAway)
            if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space) || mJump) && !pullingAway)
            {
                if (currentStamina >= ledgeClimbStaminaCost) 
                { 
                    UseStamina(ledgeClimbStaminaCost); 
                    ForceFace(wallDir); 
                    StartCoroutine(PerformClimb()); 
                }
            }

            // 3. LOGIC THẢ TAY (Drop)
            // Mobile: Kéo cần xuống (Btn_Down) -> Tương đương totalX hoặc logic riêng
            // Tạm thời nếu muốn mobile thả tay, bạn có thể gán nút "Ngồi" làm nút thả tay
            bool mCrouch = (MobileInputManager.Instance != null && MobileInputManager.Instance.isCrouching);
            
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || mCrouch) 
            {
                CancelHanging();
            }
        }
    }

    // --- HÀM PHỤ ĐỂ ÉP HƯỚNG MẶT (Đã Sửa) ---
    void ForceFace(int direction)
    {
        // direction: 1 là Phải, -1 là Trái
        if (direction == 0) return;

        // Nếu hướng yêu cầu KHÁC hướng hiện tại -> Lật ngay lập tức
        if ((direction == 1 && !isFacingRight) || (direction == -1 && isFacingRight))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    // Hàm phụ để thoát trạng thái treo (cho gọn code)
    void CancelHanging()
    {
        isHanging = false;
        anim.SetBool("IsHanging", false);
        anim.SetBool("IsWallSliding", false);
        anim.SetBool("IsWallJumpPrep", false); // Tắt luôn cái này cho chắc

        // Trả lại trọng lực
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero;
        
        // [FIX] Trả lại quyền di chuyển (Chỉ khóa xoay)
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 

        nextLedgeGrabTime = Time.time + ledgeGrabCooldown;
    }

    // --- HÀM THỰC HIỆN NHẢY BẬT TƯỜNG ---
    // --- HÀM THỰC HIỆN NHẢY BẬT TƯỜNG ---
    void PerformWallJump()
    {
        // 1. Reset trạng thái
        isHanging = false;
        anim.SetBool("IsHanging", false);
        anim.SetBool("IsWallJumpPrep", false);
        
        // 2. Trả lại vật lý
        rb.gravityScale = defaultGravity;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 3. Tính lực nhảy
        float jumpDirection = -wallDir;
        rb.linearVelocity = new Vector2(jumpDirection * wallJumpForce.x, wallJumpForce.y);
        
        // [FIX QUAN TRỌNG] Phải báo là ĐANG NHẢY thì Update mới tính animation bay lên
        isJumpingAction = true;      // <--- THIẾU CÁI NÀY
        isJumpingFromSlope = false; 

        // [FIX QUAN TRỌNG] Ép Animator nhận thông số nhảy ngay lập tức
        anim.SetFloat("Speed", 0f);              
        anim.SetFloat("yVelocity", wallJumpForce.y); // Gửi số dương để Animator chuyển sang Jump

        // 4. Xử lý hướng và Cooldown
        nextLedgeGrabTime = Time.time + 0.5f;
        ForceFace((int)jumpDirection);
        StartCoroutine(LockMovementRoutine()); 
    }

    IEnumerator PerformClimb()
    {
        isHanging = false;
        isClimbing = true;
        
        anim.SetBool("IsHanging", false);
        anim.SetBool("IsClimbing", true);
        anim.SetTrigger("LedgeClimb");

        // [FIX] Mở khóa để script có thể di chuyển nhân vật
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
        rb.linearVelocity = Vector2.zero; // Đảm bảo đứng yên để code tự lái

        Vector2 startPos = transform.position;
        float xOffset = isFacingRight ? climbOffset.x : -climbOffset.x;
        Vector2 targetPos = startPos + new Vector2(xOffset, climbOffset.y);

        float elapsedTime = 0f;
        float climbDuration = 0.5f; 

        while (elapsedTime < climbDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, elapsedTime / climbDuration);
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        transform.position = targetPos;

        // ================= [THÊM ĐOẠN NÀY VÀO ĐÂY] =================
        // Xóa ngay lập tức lệnh Nhảy/Tấn công nếu người chơi lỡ tay bấm trong lúc đang leo
        anim.ResetTrigger("Jump");      
        anim.ResetTrigger("AirAttack"); 
        anim.ResetTrigger("Attack");
        // ===========================================================
        
        rb.gravityScale = defaultGravity;
        isClimbing = false;
        anim.SetBool("IsClimbing", false);
        
        // Đảm bảo constraint vẫn đúng sau khi leo xong
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
    }

    void CheckWallSlide()
    {
        // [FIX] Nếu chạm đất -> Ép tắt Wall Slide ngay lập tức
        // Dù tia Raycast ở trên đầu có chạm tường đi nữa thì cũng kệ nó
        if (isGrounded)
        {
            isWallSliding = false;
            anim.SetBool("IsWallSliding", false);
            return; // Thoát hàm ngay, không bắn tia Raycast nữa
        }

        // [FIX QUAN TRỌNG] Thêm dòng Time.time < nextLedgeGrabTime
        // Ý nghĩa: Nếu vừa mới nhảy xong (chưa hết 0.5s) -> CẤM được tính là đang trượt tường
        if (isGrounded || isHanging || isClimbing || rb.linearVelocity.y > 0 || Time.time < nextLedgeGrabTime || isKnockedDown)
        {   
            isWallSliding = false;
            anim.SetBool("IsWallSliding", false);
            return;
        }

        float xInput = Input.GetAxisRaw("Horizontal");
        Vector2 checkPos = (Vector2)transform.position + (Vector2.up * wallCheckOffsetY);
        bool wallRight = Physics2D.Raycast(checkPos, Vector2.right, wallCheckDist, wallLayer);
        bool wallLeft = Physics2D.Raycast(checkPos, Vector2.left, wallCheckDist, wallLayer);

        if (wallRight || wallLeft)
        {
            // [FIX QUAN TRỌNG] Cập nhật biến toàn cục wallDir để dùng cho việc Nhảy
            wallDir = wallRight ? 1 : -1;

            int wallDirection = wallDir; // Dùng luôn biến vừa gán

            // Logic "Bóc ra" (Peel off)
            if (xInput != 0 && (int)Mathf.Sign(xInput) != wallDirection)
            {
                isWallSliding = false;
                anim.SetBool("IsWallSliding", false);
            }
            else
            {
                isWallSliding = true;
                anim.SetBool("IsWallSliding", true);
                
                // Ép mặt quay vào tường
                ForceFace(wallDirection);

                // Hãm tốc độ rơi
                if (rb.linearVelocity.y < -wallSlideSpeed)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
                }
            }
        }
        else
        {
            isWallSliding = false;
            anim.SetBool("IsWallSliding", false);
        }
    }

    // Hàm nhảy từ trạng thái trượt (Khác với nhảy từ trạng thái bám mép)
    void PerformWallJumpFromSlide()
    {
        // 1. Tính toán hướng
        float jumpDirection = -wallDir;

        // 2. Dịch chuyển nhẹ để tách khỏi tường (tránh ma sát vật lý)
        transform.position += new Vector3(jumpDirection * 0.1f, 0, 0);

        // 3. Gán lực nhảy
        // [FIX] Dùng wallJumpForceX và wallJumpForceY riêng biệt để dễ kiểm soát
        // Đảm bảo bạn đã set wallJumpForceX > 0 trong Inspector (ví dụ: 10)
        Vector2 jumpVector = new Vector2(jumpDirection * wallJumpForce.x, wallJumpForce.y);
        rb.linearVelocity = jumpVector;

        // 4. Reset trạng thái
        isWallSliding = false;       
        isJumpingAction = true;      
        isJumpingFromSlope = false;  
        
        // [FIX QUAN TRỌNG] Ép Animator cập nhật NGAY LẬP TỨC
        anim.SetBool("IsWallSliding", false);
        anim.SetFloat("yVelocity", wallJumpForce.y); // Fake thông số dương để Animator hiểu là đang bay lên
        
        // 5. Setup Cooldown và Khóa di chuyển
        Flip(); 
        
        // [FIX] Tăng thời gian cấm bám lại lên một chút nếu cần (0.5f là ổn)
        nextLedgeGrabTime = Time.time + 0.5f; 
        
        StartCoroutine(LockMovementRoutine()); 
    }

    // Coroutine dùng để khóa nút di chuyển trong tích tắc
    IEnumerator LockMovementRoutine()
    {
        isWallJumpLocked = true; // Bật cờ khóa
        yield return new WaitForSeconds(wallJumpCooldown); // Chờ 0.2s
        isWallJumpLocked = false; // Mở khóa
    }

    void StartCharging()
    {
        isCharging = true;
        isAttacking = true; 
        currentChargeTime = 0f;
        
        // Dừng nhân vật lại
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0; 
        anim.SetBool("IsCharging", true);

        // --- [LOGIC MỚI] PHÂN LOẠI ANIMATION GỒNG ---
        if (heavyComboStep == 2)
        {
            // Gồng thế thứ 2 (Ví dụ: Giơ kiếm lên cao quá đầu)
            anim.SetTrigger("ChargeStart2"); 
            Debug.Log("Bắt đầu tụ lực đòn 2...");
        }
        else
        {
            // Gồng thế thứ 1 (Mặc định)
            anim.SetTrigger("ChargeStart");
            Debug.Log("Bắt đầu tụ lực đòn 1...");
        }
    }

    void ReleaseHeavyAttack()
    {
        isCharging = false;
        anim.SetBool("IsCharging", false);

        // 1. TÍNH TOÁN CHI PHÍ & DAMAGE DỰA TRÊN STEP
        float cost = (heavyComboStep == 2) ? heavyAttack2StaminaCost : heavyAttackStaminaCost;
        float baseMult = (heavyComboStep == 2) ? heavyAttack2DamageMult : heavyAttackDamageMultipler;

        UseStamina(cost);

        // Tính % tụ lực
        float chargeRatio = Mathf.Clamp01(currentChargeTime / maxChargeTime);
        // Công thức dam: DamGoc + (Tỉ lệ tụ * (DamMax - DamGoc))
        // Ví dụ Step 2: DamMax là 3.5, DamGoc (tạm tính) là 1.5 -> Tụ càng lâu càng đau
        float finalDamageMultiplier = 1f + (chargeRatio * (baseMult - 1f));
        
        if (heavyAttackScript != null) heavyAttackScript.damageMultiplier = finalDamageMultiplier; 

        // 2. KÍCH HOẠT ANIMATION & COMBO
        if (heavyComboStep == 2)
        {
            // --- XỬ LÝ ĐÒN 2 ---
            anim.SetTrigger("HeavyAttack2"); // Chém kiểu 2
            Debug.Log($"Tung đòn 2! Dam x{finalDamageMultiplier}");
            
            // Đánh xong đòn 2 là hết combo -> Reset về 1
            heavyComboStep = 1;
            canPerformHeavyCombo = false; 
            
            // Cooldown kết thúc animation (không mở cửa sổ combo nữa)
            StartCoroutine(HeavyAttackCooldown(0.6f)); 
        }
        else
        {
            // --- XỬ LÝ ĐÒN 1 ---
            anim.SetTrigger("HeavyAttack"); // Chém kiểu 1
            Debug.Log($"Tung đòn 1! Dam x{finalDamageMultiplier}");
            
            // Đánh xong đòn 1 -> MỞ CỬA SỔ COMBO
            StartCoroutine(HeavyComboRoutine(0.5f)); 
        }
    }

    // Coroutine quản lý thời gian cho phép bấm nút tiếp theo
    IEnumerator HeavyComboRoutine(float attack1Duration)
    {
        // 1. Chờ đòn 1 chém xong đã
        yield return new WaitForSeconds(comboWindowStart); 

        // 2. MỞ CỬA SỔ COMBO (Cho phép bấm chuột phải lần 2)
        canPerformHeavyCombo = true;
        // (Tùy chọn) Bạn có thể làm nhân vật lóe sáng nhẹ hoặc hiện icon để báo hiệu lúc này
        
        // 3. Chờ hết thời gian cửa sổ (1s - 1.5s như bạn muốn)
        yield return new WaitForSeconds(comboWindowDuration);

        // 4. ĐÓNG CỬA SỔ (Hết cơ hội)
        canPerformHeavyCombo = false;
        
        // 5. Kết thúc trạng thái tấn công (nếu người chơi không bấm gì)
        isAttacking = false;
        rb.gravityScale = defaultGravity;
    }

    void CancelCharging()   
    {
        isCharging = false;
        isAttacking = false;
        anim.SetBool("IsCharging", false);
        rb.gravityScale = defaultGravity; // Trả lại trọng lực ngay
    }

    IEnumerator HeavyAttackCooldown(float duration)
    {
        // Đợi animation chém xong
        yield return new WaitForSeconds(duration);
        
        // Kết thúc trạng thái đánh
        isAttacking = false;
        rb.gravityScale = defaultGravity; // Trả lại trọng lực
    }

    // Hàm này sẽ được gọi bởi Animation Event lúc vung kiếm xuống
    public void EnableHeavyHitbox()
    {
        if (heavyHitboxObj != null) 
        {
            heavyHitboxObj.SetActive(true);
        }
    }

    // Hàm này sẽ được gọi bởi Animation Event lúc thu kiếm về
    public void DisableHeavyHitbox()
    {
        if (heavyHitboxObj != null) 
        {
            heavyHitboxObj.SetActive(false);
            
            // [Quan trọng] Reset dam về mặc định sau khi đánh xong
            if (heavyAttackScript != null) heavyAttackScript.damageMultiplier = 1f;
        }
    }

    // Hàm xử lý Stamina riêng biệt (Chạy mỗi khung hình)
    void HandleStamina()
    {
        bool hasChanged = false; // Biến kiểm tra xem có thay đổi gì không

        // 1. TRỪ KHI CHẠY (Sprint)
        if (isSprinting && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            currentStamina -= sprintStaminaCost * Time.deltaTime;
            hasChanged = true; // Đánh dấu là có thay đổi

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isSprinting = false;
                anim.SetBool("IsSprinting", false);
            }
        }

        // 2. TRỪ KHI TREO MÌNH
        if (isHanging)
        {
            currentStamina -= ledgeHangStaminaCost * Time.deltaTime;
            hasChanged = true; // Đánh dấu là có thay đổi

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                CancelHanging(); 
            }
        }

        // 3. LOGIC HỒI PHỤC
        // Nếu vừa mới bị trừ stamina (do chạy/treo hoặc do UseStamina ở nơi khác set lastTime)
        // Ta kiểm tra xem có đang bị trừ không để reset timer
        if (isSprinting || isHanging) // Đơn giản hóa điều kiện drain
        {
             // Đang bị trừ thì không hồi, nhưng lastStaminaUseTime đã được reset ở chỗ UseStamina hoặc ở đây
             // Thực ra logic UseStamina đã reset lastStaminaUseTime rồi.
             // Ở đây ta chỉ cần đảm bảo không hồi phục khi đang sprint/hang
        }
        else
        {
            // Chỉ hồi khi đã nghỉ đủ lâu (Delay)
            if (Time.time - lastStaminaUseTime > staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                hasChanged = true; // Đánh dấu là có thay đổi
                
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        // 4. CẬP NHẬT UI (ĐÃ SỬA)
        // Chỉ gọi cập nhật "Gradual" (True) khi Stamina thực sự thay đổi do chạy bộ hoặc hồi phục
        if (hasChanged && hud != null) 
        {
            hud.UpdateStamina(currentStamina, maxStamina, true);
        }
    }

    // Hàm hồi đầy thể lực ngay lập tức (Dùng cho Checkpoint)
    public void RefillStamina()
    {
        currentStamina = maxStamina;
        
        // Cập nhật UI ngay lập tức
        if (hud != null) 
        {
            hud.UpdateStamina(currentStamina, maxStamina);
        }
    }

    // Hàm này sẽ được gọi bởi Bình Mana (Mana Potion)
    public void RestoreMana(float amount)
    {
        float targetMana = currentMana + amount;
        if (targetMana > maxMana) targetMana = maxMana;

        if (manaCoroutine != null) StopCoroutine(manaCoroutine);
        manaCoroutine = StartCoroutine(RestoreManaRoutine(targetMana));
        
        Debug.Log("Đang nạp Mana...");
    }

    // Coroutine hồi Mana dần dần
    System.Collections.IEnumerator RestoreManaRoutine(float target)
    {
        float startValue = currentMana;
        float duration = 0.5f; // Hồi nhanh trong 0.5s
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentMana = Mathf.Lerp(startValue, target, elapsed / duration);

            // Cập nhật UI
            if (hud != null) hud.UpdateMana(currentMana, maxMana);

            yield return null;
        }

        currentMana = target;
        if (hud != null) hud.UpdateMana(currentMana, maxMana);
    }

    // =================================================
    // 6. CÁC HÀM TIỆN ÍCH (HELPER FUNCTIONS)
    // =================================================
    void SetColliderHeight(float targetHeight)
    {
        Vector2 currentSize = col.size;
        Vector2 currentOffset = col.offset;
        float heightDifference = currentSize.y - targetHeight;

        if (Mathf.Abs(heightDifference) < 0.01f) return;

        currentSize.y = targetHeight;
        col.size = currentSize;
        currentOffset.y -= heightDifference / 2f;
        col.offset = currentOffset;
    }

    void Flip()
    {
        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    public void TriggerKnockdown(Vector2 impactDirection)
    {
        // [MỚI] Nếu script này đã bị tắt (do nhân vật chết), thì không làm gì cả
        if (!this.enabled) return;
        
        if (isKnockedDown || isRolling || isSliding) return; 

        // Lưu lại coroutine đang chạy để có thể Stop nó sau này
        knockdownRoutine = StartCoroutine(PerformKnockdown(impactDirection));
    }

    public bool IsInvulnerable()
    {
        return isRolling || isSliding;
    }

    // Hàm trừ Stamina tức thì và reset thời gian hồi
    void UseStamina(float amount)
    {
        currentStamina -= amount;
        if (currentStamina < 0) currentStamina = 0;
        
        lastStaminaUseTime = Time.time; // Reset thời gian chờ hồi phục
        
        if (hud != null) hud.UpdateStamina(currentStamina, maxStamina);
    }
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    // Hàm tiện ích để trừ Mana
    void UseMana(float amount)
    {
        currentMana -= amount;
        if (currentMana < 0) currentMana = 0;
        
        if (hud != null) hud.UpdateMana(currentMana, maxMana);
    }

    // --- [MỚI] CÁC HÀM HỖ TRỢ NGHỈ NGƠI ---
    public void EnterRestState()
    {
        isRestingAtCheckpoint = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0);
        // Có thể thêm anim.SetBool("IsSitting", true) nếu bạn có animation ngồi
    }

    public void ExitRestState()
    {
        isRestingAtCheckpoint = false;
        // anim.SetBool("IsSitting", false);
    }

    // HÀM XỬ LÝ KHI CHẠM VÀO TRIGGER (Như chông gai, vùng sự kiện...)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem vật thể vừa chạm có phải là "Spikes" không
        if (other.CompareTag("Spikes"))
        {
            // Lấy component máu của nhân vật
            PlayerHealth health = GetComponent<PlayerHealth>();
            
            // Nếu còn sống thì cho chết ngay
            if (health != null && health.currentBlood > 0)
            {
                Debug.Log("Á á á! Chạm phải chông gai rồi!");
                // Gây sát thương bằng đúng lượng máu hiện tại -> Máu về 0
                health.TakeDamage(health.currentBlood);
            }
        }
    }

    void SlopeCheck()
    {
        // Hạ thấp điểm bắt đầu xuống dưới chân một chút
        Vector2 checkPos = (Vector2)transform.position - new Vector2(0.0f, standSize.y / 2);
        
        // Kích thước hộp: 
        // - Ngang: Bằng 100% người (để bắt mép tốt hơn)
        // - Cao: Rất mỏng
        Vector2 boxSize = new Vector2(standSize.x * 0.8f, 0.05f);

        // Khoảng cách quét: Dài hơn một chút (ví dụ 0.5f hoặc 0.8f) 
        // để khi chạy xuống dốc nhanh quá chân chưa kịp chạm thì tia đã bắt được rồi
        RaycastHit2D hit = Physics2D.BoxCast(checkPos, boxSize, 0f, Vector2.down, slopeCheckDistance, whatIsGround);    

        if (hit)
        {
            // Tính toán pháp tuyến và góc như cũ
            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;
            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            // Check điều kiện dốc (> 3 độ để lọc nhiễu sàn phẳng)
            if (slopeDownAngle > 3f && slopeDownAngle < maxSlopeAngle)
            {
                isOnSlope = true;
            }
            else
            {
                isOnSlope = false;
            }

            // [DEBUG] Vẽ cái hộp ra cho dễ nhìn (Màu xanh lá)
            // Lưu ý: DrawRay không vẽ được hộp, đây chỉ vẽ hướng pháp tuyến
            Debug.DrawRay(hit.point, slopeNormalPerp, Color.green);
            Debug.DrawRay(hit.point, hit.normal, Color.red);
        }
        else
        {
            isOnSlope = false;
            slopeDownAngle = 0.0f;
        }
    }

    // =========================================================
    // [MỚI] CÁC HÀM QUẢN LÝ TRẠNG THÁI (STATE MANAGEMENT)
    // =========================================================

    // 1. GỌI KHI BƯỚC QUA CỬA (Lưu lại tất cả)
    public void PrepareForSceneTransition()
    {
        // A. Lưu Mana & Stamina của bản thân
        PlayerPrefs.SetFloat("SavedMana", currentMana);
        PlayerPrefs.SetFloat("SavedStamina", currentStamina);
        
        // B. Lưu Máu (Gọi sang PlayerHealth)
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            PlayerPrefs.SetFloat("SavedBlood", health.currentBlood);
        }

        // C. Lưu Bình thuốc (Gọi sang Manager)
        if (consumableManager != null)
        {
            consumableManager.SaveInventoryState();
        }

        // D. [QUAN TRỌNG] Bật cờ Chuyển Cảnh
        PlayerPrefs.SetInt("IsSceneTransition", 1);
        PlayerPrefs.Save();
    }

    // 2. GỌI KHI VÀO MÀN MỚI (Khôi phục lại tất cả)
    void LoadPlayerState()
    {
        // A. Load Mana & Stamina
        if (PlayerPrefs.HasKey("SavedMana")) currentMana = PlayerPrefs.GetFloat("SavedMana");
        if (PlayerPrefs.HasKey("SavedStamina")) currentStamina = PlayerPrefs.GetFloat("SavedStamina");

        // B. Load Máu
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && PlayerPrefs.HasKey("SavedBlood"))
        {
            float savedBlood = PlayerPrefs.GetFloat("SavedBlood");
            health.SetHealth(savedBlood);
        }

        // C. Load Bình thuốc
        if (consumableManager != null)
        {
            consumableManager.LoadInventoryState();
        }

        // [QUAN TRỌNG] Thêm tham số 'true' vào cuối các hàm Update
        if (hud != null)
        {
            // Cập nhật Mana ngay lập tức
            hud.UpdateMana(currentMana, maxMana, true);
            
            // Cập nhật Stamina ngay lập tức (isGradual=false, immediate=true)
            hud.UpdateStamina(currentStamina, maxStamina, false, true);
        }
    }

    // Vẽ tia Check Ledge để dễ nhìn
    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Vẽ tia check tường (Xanh Dương) - Đã nâng cao
        Gizmos.color = Color.blue;
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        
        // Tính toán vị trí vẽ cho khớp với logic
        Vector2 wallCheckPos = (Vector2)transform.position + (Vector2.up * wallCheckOffsetY);
        Gizmos.DrawLine(wallCheckPos, wallCheckPos + direction * wallCheckDist);
        
        // Vẽ tia check mép (Xanh Lá)
        Gizmos.color = Color.green;
        Vector2 ledgePos = (Vector2)transform.position + (Vector2.up * ledgeCheckHeight);
        Gizmos.DrawLine(ledgePos, ledgePos + direction * wallCheckDist);
    }
}