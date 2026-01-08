using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public float damage = 10f;
    public bool isHeavyAttack = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Lấy script PlayerController trước
            PlayerController pc = collision.GetComponent<PlayerController>();

            // [QUAN TRỌNG - THÊM ĐOẠN NÀY]
            // Kiểm tra xem Player có đang lộn/trượt (Bất tử) không?
            // Nếu có -> Né đòn thành công -> Thoát luôn, không trừ máu
            if (pc != null && pc.IsInvulnerable())
            {
                Debug.Log("Dodged! (Né đòn thành công)");
                return; 
            }

            // -----------------------------------------------------------
            // Nếu chạy xuống được đây nghĩa là KHÔNG né được -> Ăn đòn
            // -----------------------------------------------------------

            PlayerHealth health = collision.GetComponent<PlayerHealth>();

            // 1. TRỪ MÁU (Giờ đã an toàn, chỉ trừ khi không né)
            if (health != null)
            {
                health.TakeDamage(damage);
                
                // (Không cần check Knockdown ở đây nữa vì TakeDamage tự lo Hurt/Die)
            }

            // 2. GÂY KNOCKDOWN (Vẫn giữ logic cũ)
            // Lúc này chắc chắn pc không null và không bất tử rồi
            if (isHeavyAttack && pc != null)
            {
                float directionX = (collision.transform.position.x - transform.position.x);
                directionX = (directionX > 0) ? 1f : -1f;

                Vector2 knockDirection = new Vector2(directionX, 0);
                pc.TriggerKnockdown(knockDirection);
            }
        }
    }
}