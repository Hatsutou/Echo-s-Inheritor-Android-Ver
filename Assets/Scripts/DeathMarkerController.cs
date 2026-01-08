using UnityEngine;

public class DeathMarkerController : MonoBehaviour
{
    // Các Key lưu trữ
    public const string KEY_HAS_MARKER = "HasDeathMarker";
    public const string KEY_LOST_AMOUNT = "LostCurrencyAmount";
    public const string KEY_MARKER_X = "DeathPosX";
    public const string KEY_MARKER_Y = "DeathPosY";

    private bool isCollected = false;
    private PlayerController playerRef; // Lưu tham chiếu người chơi

    // Thay vì tự nhặt, ta chỉ "báo cáo" sự có mặt cho Player
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            playerRef = collision.GetComponent<PlayerController>();
            if (playerRef != null)
            {
                // Gán chính mình vào biến currentDeathMarker của Player
                playerRef.currentDeathMarker = this;
                Debug.Log("Đang đứng trên Bia Mộ. Bấm E để nhặt.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Nếu người chơi đi ra xa -> Xóa tham chiếu
            if (playerRef != null)
            {
                playerRef.currentDeathMarker = null;
                playerRef = null;
            }
        }
    }

    // Hàm này sẽ được PlayerController gọi khi bấm E
    public void Interact()
    {
        if (isCollected) return;
        CollectMarker();
    }

    void CollectMarker()
    {
        isCollected = true;

        // 1. Lấy lại tiền
        float lostMoney = PlayerPrefs.GetFloat(KEY_LOST_AMOUNT, 0);
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddMoney(lostMoney);
            Debug.Log($"Đã nhặt lại bia mộ! Nhận lại: {lostMoney} $");
        }

        // 2. Xóa dữ liệu trong bộ nhớ
        PlayerPrefs.SetInt(KEY_HAS_MARKER, 0);
        PlayerPrefs.DeleteKey(KEY_LOST_AMOUNT);
        PlayerPrefs.DeleteKey(KEY_MARKER_X);
        PlayerPrefs.DeleteKey(KEY_MARKER_Y);
        PlayerPrefs.Save();

        // 3. Xóa tham chiếu bên Player (Quan trọng để tránh lỗi bấm E lần nữa)
        if (playerRef != null)
        {
            playerRef.currentDeathMarker = null;
        }

        // 4. Hủy object
        Destroy(gameObject);
    }
}