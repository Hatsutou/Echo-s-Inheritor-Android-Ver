using UnityEngine;

public class EndGameStatue : MonoBehaviour
{
    [Header("UI Kết Thúc")]
    public EndGameSequence endGameSequence; // Kéo cái Canvas quản lý kết game vào đây

    private PlayerController playerRef;

    // Khi Player đi vào vùng
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerRef = other.GetComponent<PlayerController>();
            if (playerRef != null) 
            {
                playerRef.currentEndGameStatue = this;
                // Có thể hiện nút "E" nhắc nhở ở đây nếu muốn (giống Checkpoint thật)
            }
        }
    }

    // Khi Player đi ra
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerRef != null) 
            {
                playerRef.currentEndGameStatue = null;
                playerRef = null;
            }
        }
    }

    // Hàm này được PlayerController gọi khi bấm E
    public void Interact()
    {
        if (endGameSequence != null)
        {
            // Tắt script Player đi để không di chuyển được nữa
            if (playerRef != null) playerRef.enabled = false;

            // Bắt đầu chuỗi kết thúc
            endGameSequence.StartEnding();
        }
    }
}