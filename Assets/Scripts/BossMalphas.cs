using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BossMalphas : MonoBehaviour
{
    [Header("Thông Số Cơ Bản")]
    public string bossName = "Malphas, The Abyssal Reaper";
    public float maxHealth = 1000f;
    public float currentHealth;
    public bool isFlipped = false;

    [Header("Cài Đặt Di Chuyển & Tấn Công")]
    public float moveSpeed = 4f;
    public float attackRange = 2.5f; // Khoảng cách để bắt đầu đánh
    public float attackCooldown = 2f; // Thời gian nghỉ giữa các đòn
    private float nextAttackTime = 0;

    [Header("Tinh Chỉnh Độ Mượt (Vật Lý)")]
    public float acceleration = 50f; // Tăng tốc độ (Càng cao càng nhanh đạt tốc độ tối đa)
    public float deceleration = 50f; // Phanh lại (Càng cao phanh càng gấp)
    public float turnThreshold = 0.5f; // Tốc độ nhỏ hơn mức này mới được phép xoay người (để tránh xoay khi đang trôi nhanh)   

    [Header("Cài Đặt Phase")]
    public GameObject modelPhase1;
    public GameObject modelPhase2;
    public bool isPhase2 = false;
    public float phase2Threshold = 0.4f; 
    private bool isTransforming = false; // Đang biến hình thì bất tử/đứng im

    [Header("Mục Tiêu")]
    public Transform player;

    [Header("Combat Hitbox")]
    public GameObject attackHitboxObj; // Kéo cái BossHitbox vào đây
    public EnemyDamage damageScript;   // Kéo luôn cái script EnemyDamage trên cái Hitbox đó vào đây

    [Header("Kỹ Năng Dash (Lướt)")]
    public float dashSpeed = 15f;      // Tốc độ lướt (nhanh gấp 3-4 lần đi bộ)
    public float dashDuration = 0.5f;  // Thời gian lướt tối đa (ngắn thôi)
    public float dashCooldown = 5f;    // Bao lâu mới được lướt lại
    public float dashMinDistance = 6f; // Xa trên 6m thì mới cân nhắc lướt
    private float nextDashTime = 0;
    private float dashTimer = 0;

    [Header("Kỹ Năng Độn Thổ (Special Attack)")]
    public float specialAtkCooldown = 8f;     // Hồi chiêu lâu hơn Dash chút
    public float minUndergroundTime = 1.0f;   // Thời gian nấp dưới đất tối thiểu
    public float maxUndergroundTime = 2.5f;   // Thời gian nấp dưới đất tối đa
    public float specialMinDistance = 5f;     // Xa tầm này mới dùng
    private float nextSpecialAtkTime = 0;

    [Header("Cơ Chế Phase 2 (Quỷ)")]
    // Khoảng cách "Dí sát" (Gần hơn tầm đánh thường)
    public float personalSpaceRadius = 2.0f; 
    
    // Thời gian chịu đựng tối đa trước khi Nổ (giây)
    public float timeToTriggerExplosion = 3.0f;
    
    [Header("Phase 2 Hitbox")]
    public GameObject explosionHitboxObj; // Kéo cái Hitbox tròn (nổ) vào đây
    
    // Biến đếm giờ (Nội bộ)
    private float closeRangeTimer = 0f;

    [Header("Cấu Hình Phase 2 (Kích Thước & Tầm Xa)")]
    public float phase2AttackRange = 4.5f; // Tầm đánh xa hơn Phase 1 (ví dụ 4.5m)
    
    // Kích thước Collider cho Phase 2 (Bạn phải tự đo trong Editor)
    public Vector2 p2ColliderSize = new Vector2(2f, 3f); 
    public Vector2 p2ColliderOffset = new Vector2(0f, 1.5f);

    [Header("Cấu Hình Phase 2 (Sức Mạnh & Tốc Độ)")]
    public float phase2AttackCooldown = 0.8f; // Hồi chiêu cực nhanh (Phase 1 là 2s)
    public float phase2MoveSpeed = 6.0f;      // Chạy nhanh hơn (Phase 1 là 4s)

    [Header("Cutscene Settings")]
    public bool isWaitingForIntro = false; // Biến khóa logic AI khi đang Intro

    // Biến lưu thông số cũ của Phase 1 (để backup nếu cần)
    private Vector2 p1ColliderSize;
    private Vector2 p1ColliderOffset;
    private BoxCollider2D boxCol; // Giả sử bạn dùng BoxCollider2D
    private CapsuleCollider2D capCol; // Hoặc CapsuleCollider2D

    [Header("UI")]
    public BossHUD bossHUD;
    public VictoryScreen victoryScreen; // Kéo cái VictoryCanvas vào đây

    [Header("Lưu Trữ")]
    public string bossID = "Boss_Malphas"; // ID riêng để lưu trạng thái Boss này

    [Header("Phần Thưởng")]
    public int soulReward = 10000;

    // Trạng thái AI
    public enum BossState { Idle, Chase, Attack, Hurt, PhaseTransition, Dead, Dash }
    public BossState currentState = BossState.Idle;
    public float chaseBuffer = 0.5f; // Khoảng cách "dư ra" để Boss không bị giật
    private float moveDirection = 0f; // Biến lưu hướng muốn đi
    // Thêm biến để lưu vị trí cũ
    private float lastXPosition;
    private float stuckTimer;
    private SpriteRenderer spriteRenderer;
    private Collider2D mainCollider; // Để tắt va chạm khi độn thổ

    private Animator anim;
    private Rigidbody2D rb;
    private Material flashMaterial;
    // [MỚI] Biến này để lưu áo gốc, phòng khi chết bị kẹt màu trắng thì lôi ra dùng
    private Material defaultMaterial;

    void Start()
    {
        // 1. [QUAN TRỌNG] KIỂM TRA XEM BOSS ĐÃ BỊ DIỆT CHƯA?
        // Nếu biến này = 1 nghĩa là đã giết rồi -> Hủy Boss ngay lập tức
        if (PlayerPrefs.GetInt(bossID + "_Defeated") == 1)
        {
            // Tắt Boss
            gameObject.SetActive(false); 
            // Hoặc Destroy(gameObject);
            
            // Tắt cả thanh máu và sương mù chắn cửa (nếu có)
            if (bossHUD != null) bossHUD.gameObject.SetActive(false);
            
            return; // Dừng code tại đây
        }

        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        
        // Setup ban đầu
        modelPhase1.SetActive(true);
        modelPhase2.SetActive(false);
        anim = modelPhase1.GetComponent<Animator>();

        // [MỚI] Lấy SpriteRenderer từ Model con
        spriteRenderer = modelPhase1.GetComponent<SpriteRenderer>();
        mainCollider = GetComponent<Collider2D>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        // Tự tìm HUD nếu quên kéo
        if (bossHUD == null) bossHUD = FindFirstObjectByType<BossHUD>();

        // Khởi tạo thanh máu
        if (bossHUD != null)
        {
            bossHUD.SetupHUD(bossName, maxHealth);
        }

        lastXPosition = transform.position.x;

        // 1. Lấy SpriteRenderer
        if (modelPhase1 != null)
        {
             spriteRenderer = modelPhase1.GetComponentInChildren<SpriteRenderer>();
             
             // [MỚI] Lưu lại áo gốc của Phase 1
             if (spriteRenderer != null) defaultMaterial = spriteRenderer.material;
        }

        // 2. Lấy Material ra để tí nữa chỉnh sửa
        if (spriteRenderer != null) 
        {
            // Unity sẽ tự tạo ra một bản sao (Instance) của Material để không ảnh hưởng con khác
            flashMaterial = spriteRenderer.material;
        }

        // Tự động tìm Collider trên người cha
        boxCol = GetComponent<BoxCollider2D>();
        capCol = GetComponent<CapsuleCollider2D>();

        // Lưu lại kích thước Phase 1
        if (boxCol != null)
        {
            p1ColliderSize = boxCol.size;
            p1ColliderOffset = boxCol.offset;
        }
        else if (capCol != null)
        {
            p1ColliderSize = capCol.size;
            p1ColliderOffset = capCol.offset;
        }

        // Nếu đang chờ intro thì đừng làm gì cả
        if (isWaitingForIntro)
        {
            currentState = BossState.Idle; // Hoặc một state riêng như 'Intro'
            // Đảm bảo tắt thanh máu Boss lúc chưa hiện hình
            if (bossHUD != null) bossHUD.gameObject.SetActive(false);
        }

        // Nếu đang chờ Intro thì nhớ tắt HUD đi (phòng trường hợp nó tự bật lại)
        if (isWaitingForIntro)
        {
            if (bossHUD != null) bossHUD.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // [MỚI] Nếu đang chờ diễn intro thì đứng im
        if (isWaitingForIntro) return;

        // Thêm dòng này lên đầu hàm Update để soi trạng thái
        if (isPhase2) Debug.Log($"Phase 2 State: {currentState}");

        // 1. NGOẠI LỆ
        if (currentState == BossState.Dead || currentState == BossState.PhaseTransition || isTransforming) 
        {
            moveDirection = 0; 
            return;
        }

        if (currentState == BossState.Dash) 
        { 
            HandleDashState(); 
            return; 
        }

        float distance = Vector2.Distance(transform.position, player.position);

        // --- [MỚI] LOGIC TÍCH NỘ (ANTI-HUG) CHO PHASE 2 ---
        if (isPhase2 && currentState != BossState.Dead)
        {
            // Nếu Player đứng quá gần ("Dí sát")
            if (distance <= personalSpaceRadius)
            {
                closeRangeTimer += Time.deltaTime; // Tăng đồng hồ
                // (Mẹo: Có thể làm người Boss đỏ dần lên để báo hiệu sắp nổ)
            }
            else
            {
                // Nếu Player chạy ra xa -> Boss nguôi giận -> Giảm dần hoặc Reset
                closeRangeTimer -= Time.deltaTime;
                if (closeRangeTimer < 0) closeRangeTimer = 0;
            }
        }

        // 2. LOGIC ĐUỔI THEO (CHASE)
        if (currentState == BossState.Chase)
        {
            // ================================================================
            // PHẦN A: AI QUYẾT ĐỊNH KỸ NĂNG TẦM XA (Giữ nguyên cái bạn đang có)
            // ================================================================
            if (distance >= specialMinDistance)
            {
                bool canDash = Time.time >= nextDashTime;
                bool canSpecial = Time.time >= nextSpecialAtkTime;

                // Nếu là Phase 2: CẤM Dash và CẤM luôn Special (Độn thổ)
                if (isPhase2) 
                {
                    canDash = false;
                    canSpecial = false; 
                }

                if (canDash && canSpecial)
                {
                    if (Random.value > 0.5f) StartDash();
                    else StartCoroutine(PerformSpecialAttack());
                    return; 
                }
                else if (canDash)
                {
                    StartDash();
                    return;
                }
                else if (canSpecial)
                {
                    StartCoroutine(PerformSpecialAttack());
                    return;
                }
            }

            // ================================================================
            // PHẦN B: DI CHUYỂN & CHECK KẸT (Cơ bản giữ nguyên)
            // ================================================================
            float directionX = player.position.x - transform.position.x;
            moveDirection = Mathf.Sign(directionX);

            // Kiểm tra bị kẹt
            bool isStuck = Mathf.Abs(transform.position.x - lastXPosition) < (moveSpeed * Time.deltaTime * 0.1f);
            lastXPosition = transform.position.x;
            if (isStuck) stuckTimer += Time.deltaTime; else stuckTimer = 0;

            // ================================================================
            // PHẦN C: ĐIỀU KIỆN DỪNG (ĐÂY LÀ PHẦN CẦN SỬA!) 
            // ================================================================
            
            // [MỚI] 1. Chọn tầm đánh dựa trên Phase (Phase 2 tay dài hơn)
            float currentRange = isPhase2 ? phase2AttackRange : attackRange; 

            // [MỚI] 2. Tính điểm dừng dựa trên tầm đánh hiện tại
            float distThreshold = currentRange - chaseBuffer;
            
            // [MỚI] 3. Logic va chạm cũng dùng currentRange (Nới lỏng +0.5f để Boss to không bị kẹt)
            bool isColliding = (stuckTimer > 0.1f) && (distance <= currentRange + 0.5f); 
            
            bool isCloseEnough = distance <= distThreshold;

            if (isCloseEnough || isColliding)
            {
                if (IsFacingPlayer())
                {
                    StopChasing();
                }
            }
        }

        // 3. LOGIC IDLE (ĐANG ĐỨNG CHƠI)
        else if (currentState == BossState.Idle)
        {
            moveDirection = 0; // Phanh lại

            // [QUAN TRỌNG] Phải chọn đúng tầm đánh của Phase hiện tại
            float currentRange = isPhase2 ? phase2AttackRange : attackRange; 

            // A. Nếu Player chạy ra xa khỏi tầm đánh hiện tại -> Đuổi theo
            // (So sánh với currentRange chứ KHÔNG PHẢI attackRange)
            if (distance > currentRange)
            {
                currentState = BossState.Chase;
            }
            // B. Nếu Player vẫn ở trong tầm đánh -> KIỂM TRA TẤN CÔNG
            else 
            {
                // Nếu đã hồi chiêu -> Gọi hàm đánh ngay lập tức
                if (Time.time >= nextAttackTime)
                {
                    Debug.Log("Boss: Đã vào tầm, XỬ NÓ!"); // Log kiểm tra
                    HandleAttackLogic();
                }
            }
        }

        // 4. LOGIC ATTACK (ĐANG ĐÁNH)
        else if (currentState == BossState.Attack)
        {
            moveDirection = 0; // Đứng yên khi đánh
        }

        // 5. QUAY MẶT
        bool canRotate = true;

        if (currentState == BossState.Dead) canRotate = false;
        
        // Nếu là Phase 1 -> Bị cấm quay khi đang Dash hoặc Attack
        if (!isPhase2 && (currentState == BossState.Attack || currentState == BossState.Dash)) 
        {
            canRotate = false;
        }

        // Nếu là Phase 2 -> Cho phép quay thoải mái (trừ khi bạn muốn chặn ở chiêu nổ)

        if (canRotate)
        {
            LookAtPlayer();
        }
    } // <--- KẾT THÚC HÀM UPDATE Ở ĐÂY

    // Hàm này để Đạo diễn gọi: "Diễn đi cưng!"
    public void PlayIntroAnimation()
    {
        // [THÊM DÒNG NÀY] Ép tắt HUD một lần nữa cho chắc chắn
        if (bossHUD != null) bossHUD.gameObject.SetActive(false);

        if (anim != null) anim.SetTrigger("Intro");
    }

    // Hàm này dùng để ép Boss quay mặt về Player ngay lập tức (dùng cho Intro)
    public void FacePlayerImmediate()
    {
        if (player == null) return;

        // Tính hướng: Player đang ở bên trái hay bên phải Boss?
        float direction = player.position.x - transform.position.x;

        // Nếu Player ở bên Phải (direction > 0) mà Boss đang quay Trái (isFlipped = true) -> Quay lại
        if (direction > 0 && isFlipped)
        {
            Flip();
        }
        // Nếu Player ở bên Trái (direction < 0) mà Boss đang quay Phải (!isFlipped) -> Quay lại
        else if (direction < 0 && !isFlipped)
        {
            Flip();
        }
    }

    // Hàm này để Đạo diễn gọi khi diễn xong: "Cắt! Vào trận!"
    public void StartCombat()
    {
        isWaitingForIntro = false; // Mở khóa AI
        if (bossHUD != null) bossHUD.gameObject.SetActive(true); // Hiện thanh máu Boss
        
        // Gầm lên hoặc chuyển sang Chase luôn
        currentState = BossState.Chase; 
    }

    // --- [FIX 2] ĐƯA HÀM NÀY RA NGOÀI (Nằm ngang hàng với Update) ---
    bool IsFacingPlayer()
    {
        float directionToPlayer = player.position.x - transform.position.x;
        
        if (directionToPlayer > 0 && !isFlipped) return true; // Player bên phải, Boss quay phải -> Đúng
        if (directionToPlayer < 0 && isFlipped) return true;  // Player bên trái, Boss quay trái -> Đúng

        return false;
    }

    // 2. THỰC HIỆN DI CHUYỂN VẬT LÝ Ở ĐÂY (CỰC MƯỢT)
    void FixedUpdate()
    {
        if (currentState == BossState.Dash || currentState == BossState.Dead) return;

        // 1. TÍNH VẬN TỐC MỤC TIÊU
        float targetSpeedX = 0;

        if (currentState == BossState.Chase)
        {
            targetSpeedX = moveDirection * moveSpeed;
        }
        // Nếu Idle hoặc Attack thì targetSpeedX mặc định là 0 (để phanh lại)

        // 2. TÍNH TOÁN GIA TỐC/GIẢM TỐC (SMOOTHING)
        // Nếu đang muốn dừng (target=0) hoặc đang đổi hướng -> Dùng Deceleration (Phanh)
        // Nếu đang chạy cùng hướng -> Dùng Acceleration (Ga)
        float currentSpeedX = rb.linearVelocity.x;
        bool isAccelerating = Mathf.Abs(targetSpeedX) > 0 && Mathf.Sign(targetSpeedX) == Mathf.Sign(currentSpeedX);
        float rate = isAccelerating ? acceleration : deceleration;

        // Công thức làm mượt vận tốc: Di chuyển từ Vận tốc hiện tại -> Vận tốc mục tiêu
        float newSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, rate * Time.fixedDeltaTime);

        // 3. ÁP DỤNG VẬO RIGIDBODY
        rb.linearVelocity = new Vector2(newSpeedX, rb.linearVelocity.y);

        // 4. CẬP NHẬT ANIMATOR THEO VẬN TỐC THỰC TẾ
        // (Lúc này Animation sẽ chạy nhanh/chậm khớp với tốc độ trôi của Boss)
        anim.SetFloat("Speed", Mathf.Abs(newSpeedX));
    }

    void ChasePlayer()
    {
        currentState = BossState.Chase;
        LookAtPlayer();

        Vector2 target = new Vector2(player.position.x, rb.position.y);
        Vector2 newPos = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.deltaTime);
        rb.MovePosition(newPos);

        // [MỚI] Dùng Damp để animation chuyển đổi mềm mại từ từ (thay vì set cứng là 1)
        anim.SetFloat("Speed", 1f, 0.1f, Time.deltaTime);
    }

    void StopChasing()
    {
        currentState = BossState.Idle;
        moveDirection = 0;
        anim.SetFloat("Speed", 0f, 0.1f, Time.deltaTime);
        
        // Gọi logic tấn công
        HandleAttackLogic();
    }

    // ---------------------------------------------------------
    // BỘ NÃO ĐIỀU KHIỂN TẤN CÔNG (ĐÃ SỬA LẠI CHO KHỚP ANIMATOR)
    // ---------------------------------------------------------
    void HandleAttackLogic()
    {
        if (Time.time < nextAttackTime) return;

        currentState = BossState.Attack;
        rb.linearVelocity = Vector2.zero; 
        anim.SetFloat("Speed", 0); 

        // --- [MỚI] CHỌN THỜI GIAN HỒI CHIÊU THEO PHASE ---
        // Nếu là Phase 2 dùng cooldown ngắn (0.8s), Phase 1 dùng cooldown dài (2s)
        float currentCooldown = isPhase2 ? phase2AttackCooldown : attackCooldown;
        
        nextAttackTime = Time.time + currentCooldown;

        nextAttackTime = Time.time + attackCooldown;

        if (!isPhase2)
        {
            AttackPhase1(); // Dùng logic cũ (Attack Index)
        }
        else
        {
            AttackPhase2(); // Dùng logic mới (Trigger riêng)
        }

        CancelInvoke("EndAttackState");
        Invoke("EndAttackState", 3.0f); 
    }

    // --- LOGIC PHASE 1 (SỬA LẠI ĐÚNG NHƯ CŨ) ---
    void AttackPhase1()
    {
        // Setup sát thương
        if (damageScript != null) { damageScript.damage = 20f; damageScript.isHeavyAttack = false; }

        // 1. Random số từ 1 đến 3 (Ví dụ bạn có 3 chiêu)
        // Lưu ý: Random.Range int (min, max) không lấy số max, nên viết (1, 4) để lấy 1,2,3
        int atkID = Random.Range(1, 4); 
        
        Debug.Log($"Phase 1 Attack Index: {atkID}");

        // 2. Gửi Index trước
        anim.SetInteger("AttackIndex", atkID);
        
        // 3. Kéo cò Trigger tổng
        anim.SetTrigger("Attack"); 
    }

    // --- LOGIC PHASE 2 (ĐÃ ĐỒNG BỘ VỚI PHASE 1) ---
    void AttackPhase2()
    {
        // Reset về damage chuẩn của Phase 2 trước
        if (damageScript != null) 
        { 
            damageScript.damage = 35f; 
            damageScript.isHeavyAttack = true;
        }

        // --- 1. LOGIC CHIÊU NỔ (ANTI-HUG) ---
        // Nếu bị ôm quá lâu HOẶC Random trúng 10%
        bool triggerExplosion = (closeRangeTimer >= timeToTriggerExplosion) || (Random.Range(0f, 100f) < 10);

        if (triggerExplosion)
        {
            Debug.Log("Phase 2: BÙM! (Dùng Hitbox cũ nhưng Damage to)");
            
            // [QUAN TRỌNG] Buff chỉ số cho Hitbox cũ
            if (damageScript != null) 
            { 
                damageScript.damage = 60f; // Sát thương nổ cực to
            }

            // Gọi bằng Trigger riêng như cũ
            anim.SetTrigger("e_sp_atk");
            
            closeRangeTimer = 0f; 
            return;
        }

        // --- 2. LOGIC ĐÁNH THƯỜNG (Index 1-5) ---
        // Phần này giữ nguyên, dùng Index kết hợp Trigger Attack tổng
        int atkID = Random.Range(1, 6); 
        anim.SetInteger("AttackIndex", atkID);
        anim.SetTrigger("Attack");
    }

    // --- HAI HÀM NÀY ĐỂ GẮN VÀO ANIMATION EVENT ---
    public void EnableHitbox()
    {
        if (attackHitboxObj != null) attackHitboxObj.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (attackHitboxObj != null) attackHitboxObj.SetActive(false);
    }

    // Hàm này nên được gọi bởi Animation Event ở cuối mỗi đòn đánh
    public void EndAttackState()
    {
        if (currentState == BossState.Dead) return;

        // [THÊM ĐIỀU KIỆN NÀY]
        // Nếu đang chết, hoặc đang chuyển phase, hoặc đang biến hình -> CẤM RESET VỀ IDLE
        if (currentState == BossState.Dead || currentState == BossState.PhaseTransition || isTransforming) 
        {
            return;
        }
        
        // Reset vật lý
        anim.SetFloat("Speed", 0);
        rb.linearVelocity = Vector2.zero; 
        
        // --- [FIX 1] QUAY MẶT NGAY LẬP TỨC (BẤT CHẤP TẤT CẢ) ---
        // Không chờ vào Update mới quay, quay ngay tại đây
        float dir = player.position.x - transform.position.x;
        if ((dir > 0 && !isFlipped) || (dir < 0 && isFlipped))
        {
            Flip(); // Quay mặt luôn!
        }

        // --- [FIX 2] QUYẾT ĐỊNH HÀNH ĐỘNG TIẾP THEO ---
        
        // Kiểm tra khoảng cách
        float distance = Vector2.Distance(transform.position, player.position);
        float currentRange = isPhase2 ? phase2AttackRange : attackRange;

        // TRƯỜNG HỢP A: Vẫn trong tầm đánh + Đã hồi chiêu -> ĐÁNH TIẾP
        if (distance <= currentRange && Time.time >= nextAttackTime)
        {
            Debug.Log("Boss: Spam chiêu tiếp!");
            // currentState = BossState.Idle; // Không cần về Idle nữa
            HandleAttackLogic(); 
        }
        // TRƯỜNG HỢP B: Player đã chạy ra xa (hoặc luồn ra sau lưng)
        else
        {
            if (isPhase2)
            {
                // [QUAN TRỌNG] Phase 2: CHUYỂN NGAY SANG CHASE (Bỏ qua Idle)
                // Giúp Boss đuổi theo ngay lập tức không nghỉ một nhịp nào
                currentState = BossState.Chase;
                anim.SetFloat("Speed", 1); 
            }
            else
            {
                // Phase 1: Vẫn cho về Idle một chút cho giống người thường
                currentState = BossState.Idle;
                LookAtPlayer();
            }
        }
    }

    void StartDash()
    {
        currentState = BossState.Dash;
        dashTimer = dashDuration;
        nextDashTime = Time.time + dashCooldown;

        // Bật Animation
        anim.SetTrigger("Dash");
        anim.SetBool("IsDashing", true); // Bắt đầu Loop

        // Quay mặt về hướng người chơi trước khi lướt cho chuẩn
        LookAtPlayer();
        
        // Reset vận tốc tạm thời (để animation lấy đà `dash` diễn ra tại chỗ)
        rb.linearVelocity = Vector2.zero; 
    }

    void HandleDashState()
    {
        // Kiểm tra xem Animation đã chuyển sang state `dash_loop` chưa?
        // (Tránh trường hợp boss lướt đi khi đang diễn cảnh lấy đà)
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        
        // Nếu đang ở trong `dash_loop` thì mới di chuyển
        if (stateInfo.IsName("dash_loop") || stateInfo.IsName("Dash Loop")) // Tên animation của bạn
        {
            // Di chuyển cực nhanh về hướng mặt đang quay
            float direction = isFlipped ? -1f : 1f; // Tùy vào setup Flip của bạn
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);

            // Trừ thời gian
            dashTimer -= Time.deltaTime;

            // KIỂM TRA ĐIỀU KIỆN DỪNG:
            // 1. Hết giờ
            // 2. Hoặc đã áp sát người chơi (cách < 1.5m)
            float distToPlayer = Vector2.Distance(transform.position, player.position);
            
            if (dashTimer <= 0 || distToPlayer < 1.5f)
            {
                EndDash();
            }
        }
    }

    void EndDash()
    {
        // Dừng lại
        rb.linearVelocity = Vector2.zero;
        
        // Ngắt Loop -> Chuyển sang `dash_end`
        anim.SetBool("IsDashing", false);

        // Chờ diễn xong `dash_end` thì logic Update sẽ tự chuyển về Idle/Chase ở frame sau
        // Nhưng để chắc chắn, ta set tạm về Idle
        currentState = BossState.Idle; 
    }

    // Coroutine xử lý Độn Thổ (Đã vá lỗi rơi xuống địa ngục)
    System.Collections.IEnumerator PerformSpecialAttack()
    {
        currentState = BossState.Attack; 
        rb.linearVelocity = Vector2.zero; 

        // [MỚI] Lưu lại độ cao sàn nhà hiện tại để tí nữa trồi lên đúng chỗ này
        float floorY = transform.position.y;
        
        // [MỚI] Tắt trọng lực để không bị rơi tự do
        float originalGravity = rb.gravityScale; // Lưu lại gravity cũ
        rb.gravityScale = 0; 
        
        // 1. BIẾN MẤT
        anim.SetTrigger("SpecialDisappear");
        yield return new WaitForSeconds(1.120f); // Chờ animation lặn

        // --- TÀNG HÌNH & BẤT TỬ ---
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (mainCollider != null) mainCollider.enabled = false;
        //if (bossHUD != null) bossHUD.gameObject.SetActive(false);

        // 2. Ở DƯỚI ĐẤT
        float waitTime = Random.Range(minUndergroundTime, maxUndergroundTime);
        yield return new WaitForSeconds(waitTime);

        // 3. CHUẨN BỊ TRỒI LÊN
        // [FIX] Teleport đến X của Player, nhưng Y phải là floorY (sàn nhà)
        // Đừng lấy transform.position.y hiện tại vì lỡ nó bị trôi
        Vector2 targetPos = new Vector2(player.position.x, floorY);
        transform.position = targetPos;

        // Bật lại hình ảnh
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (mainCollider != null) mainCollider.enabled = true;
        if (bossHUD != null) bossHUD.gameObject.SetActive(true);
        
        // [MỚI] Trả lại trọng lực như cũ
        rb.gravityScale = originalGravity;

        LookAtPlayer();

        // 4. TRỒI LÊN TẤN CÔNG
        anim.SetTrigger("SpecialAppear");
        
        if (damageScript != null)
        {
            damageScript.damage = 50f; 
            damageScript.isHeavyAttack = true; 
        }

        yield return new WaitForSeconds(1.0f); // Chờ trồi lên xong

        // 5. KẾT THÚC
        nextSpecialAtkTime = Time.time + specialAtkCooldown;
        currentState = BossState.Idle;
    }

    public void LookAtPlayer()
    {
        // [FIX QUAN TRỌNG] Tách luật riêng cho từng trạng thái
        
        // 1. Nếu đang đuổi theo (Chase): Cần check vận tốc để không bị trượt băng
        if (currentState == BossState.Chase)
        {
            // [LOGIC CŨ]
            // if (Mathf.Abs(rb.linearVelocity.x) > turnThreshold) return;
            
            // [LOGIC MỚI - SỬA LẠI NHƯ SAU]
            // Nếu là Phase 1: Giữ nguyên luật "phanh lại mới được quay" cho nó thật
            // Nếu là Phase 2: Cho phép quay luôn (bỏ qua return) hoặc nâng ngưỡng lên cao
            if (!isPhase2 && Mathf.Abs(rb.linearVelocity.x) > turnThreshold) 
            {
                return;
            }
        }
        
        // 2. Nếu đang đứng chơi (Idle) hoặc vừa đánh xong: 
        // -> KHÔNG RETURN GÌ CẢ. Cho phép quay tự do luôn!
        // (Bỏ qua cái check turnThreshold ở đây)

        // --- Logic Quay Mặt (Giữ nguyên) ---
        if (player.position.x > transform.position.x && isFlipped)
        {
            Flip();
        }
        else if (player.position.x < transform.position.x && !isFlipped)
        {
            Flip();
        }
    }

    void Flip()
    {
        isFlipped = !isFlipped;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        // [MỚI - CHỈ DÀNH CHO PHASE 2]
        // Quay mặt cái là phanh gấp lại luôn để tránh trượt
        if (isPhase2)
        {
             rb.linearVelocity = Vector2.zero; 
             // Mẹo: Nếu muốn quay lại là tát luôn thì gọi HandleAttackLogic() ở đây
             // HandleAttackLogic(); 
        }
    }

    public void TakeDamage(float damage)
    {
        if (isTransforming || currentState == BossState.Dead) return;

        currentHealth -= damage;
        if (bossHUD != null) bossHUD.UpdateBlood(currentHealth);

        // 1. Hiệu ứng nháy trắng
        StartCoroutine(FlashWhiteEffect());

        // 2. [QUAN TRỌNG] KIỂM TRA BIẾN HÌNH (Đây là đoạn bị thiếu)
        // Nếu chưa sang Phase 2 VÀ Máu tụt xuống dưới 50% (hoặc ngưỡng bạn muốn)
        if (!isPhase2 && currentHealth <= maxHealth / 2)
        {
            StartCoroutine(SwitchToPhase2());
            return; // Dừng hàm ngay, không chạy logic chết ở dưới
        }

        // 3. Logic chết
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashWhiteEffect()
    {
        if (flashMaterial != null)
        {
            // Bật lên 1 (Trắng xóa)
            flashMaterial.SetFloat("_FlashAmount", 1f); 
            
            // Chờ cực nhanh (0.1 giây)
            yield return new WaitForSeconds(0.1f);
            
            // Tắt đi về 0 (Màu gốc)
            flashMaterial.SetFloat("_FlashAmount", 0f); 
        }
    }

    System.Collections.IEnumerator SwitchToPhase2()
    {
        // 1. GIAI ĐOẠN ĐẦU (Giữ nguyên)
        isTransforming = true;
        currentState = BossState.PhaseTransition;

        // --- [MỚI] DỌN DẸP CHIẾN TRƯỜNG (FIX LỖI MẤT ANIMATION) ---
        
        // A. Hủy bỏ hẹn giờ "EndAttackState" của Phase 1 đang chạy ngầm
        CancelInvoke(); 
        StopCoroutine("PerformSpecialAttack"); // Tắt luôn nếu đang định độn thổ

        // B. Reset các thông số vật lý
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Speed", 0);

        // C. Xóa các Trigger tấn công có thể đang bị kẹt
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("e_sp_atk");
        
        // D. Tắt Hitbox cho an toàn (lỡ đang bật thì tắt luôn)
        DisableHitbox();
        // ---------------------------------------------------------

        rb.linearVelocity = Vector2.zero;
        if (mainCollider != null) mainCollider.enabled = false;
        
        // --- [SỬA ĐỔI QUAN TRỌNG] ---
        // 1. KHÔNG GỌI HideHUD() NỮA!
        // 2. Thay vào đó, gọi hiệu ứng Vỡ Chữ
        if (bossHUD != null && bossHUD.textEffect != null)
        {
            StartCoroutine(bossHUD.textEffect.ShatterEffect());
        }

        // Phase 1 bắt đầu diễn
        anim.SetTrigger("Transform"); 
        
        // Chờ đoạn Start và đoạn Loop quay một lúc cho nguy hiểm
        yield return new WaitForSeconds(4.0f); // Tổng thời gian Phase 1 diễn (Start + Loop)

        // ---------------------------------------------
        // 2. GIAI ĐOẠN TRÁO HÀNG (SWAP) - XỬ LÝ TRANS END
        // ---------------------------------------------

        // [QUAN TRỌNG] Đồng bộ vị trí: Bắt Phase 2 đứng trùng khít với Phase 1
        modelPhase2.transform.position = modelPhase1.transform.position;
        // Nếu Boss có quay trái phải, nhớ đồng bộ cả Rotation/Scale
        modelPhase2.transform.localScale = modelPhase1.transform.localScale;

        // Tắt Phase 1
        modelPhase1.SetActive(false);
        
        // Bật Phase 2
        // -> VÌ BƯỚC 1 ĐÃ SET DEFAULT STATE LÀ "TRANS_END" NÊN NÓ SẼ TỰ CHẠY LUÔN!
        modelPhase2.SetActive(true);

        // --- [MỚI] THAY ĐỔI KÍCH THƯỚC COLLIDER ---
        if (boxCol != null)
        {
            boxCol.size = p2ColliderSize;
            boxCol.offset = p2ColliderOffset;
        }
        else if (capCol != null)
        {
            capCol.size = p2ColliderSize;
            capCol.offset = p2ColliderOffset;
        }
        // ------------------------------------------
        
        // Cập nhật Animator sang Phase 2 để tí nữa còn điều khiển
        anim = modelPhase2.GetComponent<Animator>();
        
        // Lấy lại các Component khác
        // 1. Cập nhật SpriteRenderer (Dùng GetComponentInChildren cho chắc ăn, lỡ Sprite nằm ở con)
        spriteRenderer = modelPhase2.GetComponentInChildren<SpriteRenderer>();

        // 2. [QUAN TRỌNG] Cập nhật luôn biến flashMaterial sang cái áo của Phase 2
        // Nếu không có dòng này, nó vẫn nháy cái áo cũ của Phase 1!
        if (spriteRenderer != null)
        {
             flashMaterial = spriteRenderer.material;
        }

        // 3. CHỜ HẾT TRANS END
        // Bạn cần biết animation Trans_End dài bao nhiêu giây để điền vào đây
        // Ví dụ nó dài 2 giây
        yield return new WaitForSeconds(2.0f);

        // 4. CHÍNH THỨC VÀO PHASE 2
        Debug.Log("PHASE 2 BẮT ĐẦU!");
        isTransforming = false;
        isPhase2 = true;
        currentState = BossState.Idle; // Lúc này Animator cũng đã tự transition về Idle rồi
        currentState = BossState.Chase; // Bắt buộc đuổi ngay lập tức
        anim.SetFloat("Speed", 1); // Ép animation chạy luôn cho chắc
        
        // Bật lại HUD, Collider
        if (mainCollider != null) mainCollider.enabled = true;

        if (bossHUD != null)
        {
            bossHUD.gameObject.SetActive(true);

            // 1. Setup HUD bình thường (Cứ điền tên thật cũng được, không quan trọng nữa)
            bossHUD.SetupHUD("Malphas, The Awakened", maxHealth); 
            
            // 2. [QUAN TRỌNG] Ép chữ tàng hình ngay lập tức!
            // Dù chữ có là gì thì Alpha = 0 cũng sẽ không ai thấy
            bossHUD.nameText.alpha = 0f; 

            // Các cài đặt khác
            bossHUD.SwitchToPhase2Colors();
            bossHUD.SetHealthInstant(currentHealth); 
            bossHUD.EnableGalaxyText(true);

            // 3. Gọi hiệu ứng
            if (bossHUD.textEffect != null)
            {
                StartCoroutine(bossHUD.textEffect.AssembleEffect("Malphas, The Awakened"));
            }
            else
            {
                // Nếu quên gắn script effect -> Thì bật lại Alpha = 1 để hiện tên luôn
                bossHUD.nameText.alpha = 1f;
            }
        }

        // --- [MỚI] CẬP NHẬT TỐC ĐỘ ---
        moveSpeed = phase2MoveSpeed; // Tăng tốc độ chạy lên 6
        
        currentState = BossState.Chase; 
        anim.SetFloat("Speed", 1);

        // Bật lại HUD, Collider
        if (mainCollider != null) mainCollider.enabled = true;

        if (bossHUD != null) 
        {
            bossHUD.gameObject.SetActive(true);
            
            // Setup lại tên và Max máu (Lúc này HUD sẽ tự reset về 100%)
            bossHUD.SetupHUD("Malphas, The Awakened", maxHealth); 

            // Gọi hàm mới để ép nó tụt xuống đúng mức máu hiện tại (50%) ngay lập tức
            bossHUD.SetHealthInstant(currentHealth); 
        }
        
        // Reset lại các thông số vật lý cho chắc
        rb.gravityScale = 1; 
    }

    // --- HÀM CHẾT (ĐÃ SỬA LẠI) ---
    void Die()
    {
        // Chặn chết 2 lần
        if (currentState == BossState.Dead) return;

        currentState = BossState.Dead;
        Debug.Log("Boss bắt đầu chuỗi Death Sequence!");

        // 1. Dừng mọi vật lý ngay lập tức
        rb.linearVelocity = Vector2.zero;
        // rb.isKinematic = true; // Bật cái này nếu muốn xác Boss không bị đẩy đi nữa
        
        // 2. Tắt các logic tấn công
        CancelInvoke();
        StopAllCoroutines(); // Dừng Dash, Attack, Flash...
        
        // 3. Tắt Hitbox và HUD
        DisableHitbox();
        if (mainCollider != null) mainCollider.enabled = false; // Tắt va chạm để Player không kẹt vào xác
        if (bossHUD != null) bossHUD.HideHUD();

        // 1. Dừng mọi thứ (bao gồm cả cái Flash đang chạy dở)
        StopAllCoroutines(); 
        CancelInvoke();

        // 2. [QUAN TRỌNG] Ép Boss về màu gốc ngay lập tức (Chữa lỗi bị trắng xóa)
        if (spriteRenderer != null && defaultMaterial != null)
        {
            spriteRenderer.material = defaultMaterial;
        }

        // 4. BẮT ĐẦU DIỄN CẢNH CHẾT (Gọi Coroutine)
        StartCoroutine(DeathSequence());
    }

    // --- KỊCH BẢN CHẾT LIÊN HOÀN (CINEMATIC DEATH) ---
    System.Collections.IEnumerator DeathSequence()
    {
        // ===================================================
        // CẢNH 1: QUỶ BỊ ĐAU (PHASE 2 HURT)
        // ===================================================
        // Nếu đang ở Phase 2 thì diễn Hurt trước
        if (isPhase2)
        {
            anim.SetTrigger("Hurt"); // Trigger Animation Hurt của Phase 2
            
            // [MẸO] Làm chậm tốc độ diễn xuống còn 1 nửa (Slow Motion)
            // Nhìn sẽ đau đớn và quằn quại hơn
            anim.speed = 0.5f; 

            yield return new WaitForSeconds(2.0f); // Chờ lâu hơn

            // [QUAN TRỌNG] Nhớ trả lại tốc độ bình thường cho cảnh sau
            anim.speed = 1.0f; 
            anim.SetTrigger("Back2Human");
            
            // Bạn phải xem Animation Back2Human dài bao lâu để điền vào đây
            // Ví dụ nó dài 1.5 giây
            yield return new WaitForSeconds(1.307f);
        }

        // ===================================================
        // CẢNH 3: TRÁO MODEL (HIỆN NGUYÊN HÌNH)
        // ===================================================
        
        // Đảm bảo vị trí trùng khớp
        modelPhase1.transform.position = modelPhase2.transform.position;
        modelPhase1.transform.localScale = modelPhase2.transform.localScale; // Giữ hướng quay mặt

        modelPhase2.SetActive(false); // Tắt Quỷ
        modelPhase1.SetActive(true);  // Bật Người

        // Cập nhật lại Animator sang con Phase 1
        anim = modelPhase1.GetComponent<Animator>();
        
        // Cập nhật lại SpriteRenderer (để lỡ có hiệu ứng gì sau đó)
        spriteRenderer = modelPhase1.GetComponentInChildren<SpriteRenderer>();

        // ===================================================
        // CẢNH 4: NGƯỜI CHẾT (PHASE 1 DEATH)
        // ===================================================
        anim.SetTrigger("Death"); // Trigger Animation Death của Phase 1

        // Chờ diễn xong Death (Ví dụ 2s)
        yield return new WaitForSeconds(2.0f);

        // ===================================================
        // CẢNH 5: HIỆN THÔNG BÁO (GỌI CANVAS MỚI)
        // ===================================================
        
        // [MỚI] Gọi qua script VictoryScreen riêng biệt
        if (victoryScreen != null)
        {
            victoryScreen.ShowVictory("VIGILIA FINITA");
        }

        // ===================================================
        // [SỬA] TRẢ TIỀN THƯỞNG (GỌI ĐÚNG HÀM ADDMONEY)
        // ===================================================
        if (CurrencyManager.Instance != null)
        {
            // Gọi hàm AddMoney (theo đúng tên trong script của bạn)
            CurrencyManager.Instance.AddMoney(soulReward);
            
            Debug.Log($"BOSS SLAIN! Đã cộng {soulReward} vào túi tiền.");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy CurrencyManager! Tiền thưởng bị mất.");
        }
        
        // ===================================================
        // LOGIC LƯU GAME KHI THẮNG
        // ===================================================
        
        // 1. Chỉ đánh dấu là Boss đã chết (để nó không hồi sinh nữa)
        PlayerPrefs.SetInt(bossID + "_Defeated", 1);

        // 3. Thiết lập màn chơi cần quay về (GameLevel)
        // Để LoadingScene biết đường mà load
        PlayerPrefs.SetString("TargetScene", "GameLevel");
        
        PlayerPrefs.Save(); // Lưu ngay
        Debug.Log("Victory! Boss Defeated saved. Returning to GameLevel...");

        // Chờ người chơi tận hưởng chiến thắng (6 giây)
        yield return new WaitForSeconds(6.0f);

        // --- CHUYỂN CẢNH ---
        // Đảm bảo Reset thời gian (nếu bạn có slow motion)
        Time.timeScale = 1f; 
        
        // Xóa biến tĩnh cũ cho chắc
        LevelLoader.sceneToLoad = ""; 

        SceneManager.LoadScene("LoadingScene"); 

        this.enabled = false;
    }
}