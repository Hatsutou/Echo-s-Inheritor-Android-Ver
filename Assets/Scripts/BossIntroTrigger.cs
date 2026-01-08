using UnityEngine;

public class BossIntroTrigger : MonoBehaviour
{
    public BossIntroManager director; // Kéo Director vào đây
    private bool hasTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return; // Chỉ chạy 1 lần duy nhất trong đời

        if (other.CompareTag("Player"))
        {
            if (director != null)
            {
                hasTriggered = true; // Khóa lại ngay
                director.StartBossIntro(); // Hô "Diễn!"
                
                // Tùy chọn: Đóng sập cửa lại sau lưng để nhốt Player
                // CloseDoorBehind(); 
            }
        }
    }
}