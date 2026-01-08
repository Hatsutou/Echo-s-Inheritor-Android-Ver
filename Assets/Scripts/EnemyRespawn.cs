using UnityEngine;

public class EnemyRespawn : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    // Lưu các component
    private Animator anim;
    private Rigidbody2D rb;
    private Enemy enemyScript; // Tham chiếu đến script máu/logic chính
    private Collider2D col;

    void Start()
    {
        // 1. Ghi nhớ vị trí gốc
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 2. Lấy component sẵn để dùng sau này
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyScript = GetComponent<Enemy>(); // Script Enemy mới của bạn
        col = GetComponent<Collider2D>();
    }

    public void ResetEnemy()
    {
        // 1. Dịch chuyển về nhà TRƯỚC khi bật (để tránh lỗi va chạm)
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        // 2. Bật lại GameObject
        gameObject.SetActive(true);

        // 3. Reset Vật lý
        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Dynamic; // Trả lại trọng lực (nếu lúc chết bị chuyển thành Kinematic)
        }

        // 4. Bật lại va chạm
        if (col != null) col.enabled = true;

        // 5. Reset Animation
        if (anim != null)
        {
            anim.Rebind(); // Reset animator về trạng thái Entry
            anim.Update(0f);
        }

        // 6. Reset Máu & Trạng thái (QUAN TRỌNG NHẤT)
        if (enemyScript != null)
        {
            enemyScript.currentBlood = enemyScript.maxBlood; // Đầy máu
            enemyScript.isDead = false; // Hồi sinh
            
            // Bật lại AI nếu có
            EnemyAI ai = GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = true;
        }
    }
}