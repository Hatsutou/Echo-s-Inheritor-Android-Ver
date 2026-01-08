using UnityEngine;
using System.Collections; // Cần cái này để dùng Coroutine

public class Enemy : MonoBehaviour
{
    [Header("Thông Số")]
    // [SỬA] Đổi tên thành Blood và dùng float cho đồng bộ với Player
    public float maxBlood = 100f; 
    public float currentBlood;

    [Header("UI Thanh Máu")]
    // [MỚI] Kéo cái Prefab thanh máu vào đây
    public GameObject healthBarPrefab;

    // [MỚI] Biến để chỉnh độ cao thanh máu riêng cho từng loại quái
    // Mặc định là 2m, bạn có thể chỉnh trong Inspector
    public float healthBarOffsetY = 2.0f;

    // [MỚI] Biến để lưu trữ thanh máu đã được tạo ra
    private EnemyHealthBar myHealthBar;

    // Các Component cần thiết
    private Animator anim;
    private Collider2D col; 
    private EnemyAI aiScript; 

    // [SỬA] Để public để script Respawn có thể truy cập và reset
    public bool isDead = false; 

    void Start()
    {
        currentBlood = maxBlood;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        aiScript = GetComponent<EnemyAI>();

        // [CẬP NHẬT] TẠO RA THANH MÁU VÀ CHỈNH VỊ TRÍ
        if (healthBarPrefab != null)
        {
            // 1. Tạo ra làm con của quái
            GameObject hpBarObj = Instantiate(healthBarPrefab, transform);
            
            // 2. [MỚI] Đặt lại vị trí theo độ cao mong muốn
            // X = 0 (giữa đầu), Y = biến bạn chỉnh, Z = 0
            hpBarObj.transform.localPosition = new Vector3(0, healthBarOffsetY, 0);

            myHealthBar = hpBarObj.GetComponent<EnemyHealthBar>();
        }
    }

    // [SỬA] Nhận float damage
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentBlood -= damage;
        
        // [MỚI] GỌI CẬP NHẬT UI
        if (myHealthBar != null)
        {
            myHealthBar.UpdateHealthUI(currentBlood, maxBlood);
        }

        if (currentBlood > 0)
        {
            anim.SetTrigger("Hurt");
        }
        else
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy đã chết!");
        isDead = true;

        // 1. Chạy Animation
        anim.SetTrigger("Dead");

        // 2. Tắt AI
        if (aiScript != null) aiScript.enabled = false; 

        // 3. Xử lý Vật lý (Dừng lại và không rơi xuyên đất)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; 
            rb.bodyType = RigidbodyType2D.Kinematic; 
        }

        // 4. Tắt va chạm (để Player đi xuyên qua được xác)
        if (col != null) col.enabled = false;

        // 5. Cộng tiền
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(100);
        }

        // Ẩn thanh máu ngay lập tức khi chết
        if (myHealthBar != null)
        {
            myHealthBar.HideBar();
        }

        // 6. [QUAN TRỌNG] Đợi animation diễn xong mới tắt quái
        StartCoroutine(DeactivateRoutine());
    }

    // Coroutine chờ đợi rồi mới tắt
    IEnumerator DeactivateRoutine()
    {
        // Chờ 2 giây (hoặc thời gian animation chết của bạn)
        yield return new WaitForSeconds(2f);

        // Giờ mới tắt đi để GameManager hồi sinh sau này
        gameObject.SetActive(false); 
    }
}