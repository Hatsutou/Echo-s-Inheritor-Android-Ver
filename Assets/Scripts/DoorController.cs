using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorController : MonoBehaviour
{
    [Header("Cài Đặt Chuyển Cảnh")]
    public string sceneToLoad = "BossRoom"; 

    // Biến lưu tham chiếu đến Player (để xóa cửa khi player đi xa)
    private PlayerController player;

    [Header("Boss Intro Connection")]
    // Kéo cái BossCutsceneDirector vào đây
    public BossIntroManager bossDirector;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // [ĐÃ XÓA] dòng playerIsNear = true; vì không dùng đến
            
            player = other.GetComponent<PlayerController>();
            
            // Gán cửa này vào biến currentDoor của Player để chờ bấm E
            if (player != null)
            {
                player.currentDoor = this;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // [ĐÃ XÓA] dòng playerIsNear = false;
            
            // Xóa cửa khỏi Player khi đi ra xa
            if (player != null && player.currentDoor == this)
            {
                player.currentDoor = null;
            }
            player = null;
        }
    }

    public void Interact()
    {
        Debug.Log("Đang mở cửa vào phòng Boss...");

        // [MỚI] TRƯỚC KHI LOAD SCENE -> LƯU TRẠNG THÁI PLAYER
        // Tìm Player để ra lệnh lưu
        if (player == null) player = FindFirstObjectByType<PlayerController>(); // Tìm lại cho chắc
        
        if (player != null)
        {
            // Gọi hàm lưu mà ta vừa viết bên trên
            player.PrepareForSceneTransition();
            Debug.Log("Đã lưu trạng thái Player để chuyển cảnh.");
        }
        
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeOutAndLoad(sceneToLoad);
        }
        else
        {
            // Cách load scene cơ bản nếu chưa có Fader
            SceneManager.LoadScene(sceneToLoad);
        }

        // 2. [QUAN TRỌNG] GỌI INTRO CHẠY
        if (bossDirector != null)
        {
            bossDirector.StartBossIntro();
        }
        else
        {
            Debug.LogWarning("Chưa gắn BossIntroManager vào cửa! Boss sẽ không diễn Intro.");
        }
        
        // Tắt va chạm cửa hoặc disable script này để không bấm lại được nữa
        this.enabled = false;
    }
}