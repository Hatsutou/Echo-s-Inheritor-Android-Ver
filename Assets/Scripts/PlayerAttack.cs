using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Chỉ Số Sát Thương")]
    public int baseDamage = 20; // Đổi tên biến damage cũ thành baseDamage cho rõ nghĩa

    // [MỚI] Biến hệ số nhân (Mặc định là 1 = đánh thường)
    // Biến này sẽ được PlayerController âm thầm thay đổi khi bạn Tụ Lực
    [HideInInspector] // Ẩn đi trong Inspector để đỡ rối
    public float damageMultiplier = 1f; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra va chạm với bất kỳ thứ gì có Tag "Enemy"
        if (other.CompareTag("Enemy"))
        {
            // Tính toán sát thương chốt hạ
            int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

            // ====================================================
            // TRƯỜNG HỢP 1: ĐÁNH TRÚNG QUÁI THƯỜNG
            // ====================================================
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage);
                return; // Xong việc, thoát luôn
            }

            // ====================================================
            // TRƯỜNG HỢP 2: ĐÁNH TRÚNG BOSS MALPHAS (MỚI)
            // ====================================================
            // Tìm script BossMalphas trên vật thể bị chém
            BossMalphas boss = other.GetComponent<BossMalphas>();
            
            // [QUAN TRỌNG] Nếu chém vào tay chân (Hitbox con) mà không thấy script,
            // thì tìm ngược lên cha của nó (Boss_Malphas)
            if (boss == null)
            {
                boss = other.GetComponentInParent<BossMalphas>();
            }

            if (boss != null)
            {
                boss.TakeDamage(finalDamage);
                Debug.Log($"Đã chém trúng Malphas! Sát thương: {finalDamage}");
            }
        }
    }
}