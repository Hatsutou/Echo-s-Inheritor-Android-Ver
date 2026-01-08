using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Cài Đặt")]
    public float moveSpeed = 3f;
    public float detectionRange = 5f; 
    public float attackRange = 1.2f;  
    public float attackCooldown = 2f; 
    
    [Header("Cài Đặt Ngủ Đông")] // --- [MỚI] ---
    public float sleepDelay = 9f; // Thời gian chờ trước khi nằm xuống (giây)
    
    private Transform player;
    private Animator anim;
    private float lastAttackTime;
    private bool isFacingRight = false; // (Sửa lại false cho đúng sprite của bạn)

    // --- BIẾN TRẠNG THÁI MỚI ---
    private bool isSleeping = true; // Mặc định ban đầu là đang ngủ (bãi rác)
    private float timeSinceLastSawPlayer = 9f; // Đếm thời gian người chơi khuất tầm mắt
    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // === LOGIC 1: ĐANG NGỦ (BÃI RÁC) ===
        if (isSleeping)
        {
            // Nếu người chơi đến gần -> Thức Dậy
            if (distanceToPlayer < detectionRange)
            {
                WakeUp();
            }
        }
        // === LOGIC 2: ĐANG THỨC (QUÁI VẬT) ===
        else
        {
            // Nếu người chơi vẫn trong tầm nhìn
            if (distanceToPlayer < detectionRange)
            {
                timeSinceLastSawPlayer = 0f; // Reset bộ đếm

                // Logic chiến đấu bình thường
                if (distanceToPlayer > attackRange)
                {
                    ChasePlayer();
                }
                else
                {
                    StopChasing();
                    if (Time.time > lastAttackTime + attackCooldown)
                    {
                        Attack();
                    }
                }
            }
            // Nếu người chơi đã đi xa
            else
            {
                StopChasing(); // Đứng lại
                timeSinceLastSawPlayer += Time.deltaTime; // Bắt đầu đếm giờ

                // Nếu đã chờ đủ lâu -> Đi Ngủ
                if (timeSinceLastSawPlayer > sleepDelay)
                {
                    GoToSleep();
                }
            }
        }
    }

    void WakeUp()
    {
        isSleeping = false;
        anim.SetTrigger("WakeUp");
        // Reset các trigger khác để tránh lỗi
        anim.ResetTrigger("Sleep");
    }

    void GoToSleep()
    {
        isSleeping = true;
        anim.SetTrigger("Sleep");
        // Reset các trigger khác
        anim.ResetTrigger("WakeUp");
        anim.SetBool("IsRunning", false);
    }

    void ChasePlayer()
    {
        // (Giữ nguyên code cũ)
        anim.SetBool("IsRunning", true);
        Vector2 target = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (transform.position.x < player.position.x && !isFacingRight) Flip();
        else if (transform.position.x > player.position.x && isFacingRight) Flip();
    }

    void StopChasing()
    {
        anim.SetBool("IsRunning", false);
    }

    void Attack()
    {
        anim.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}