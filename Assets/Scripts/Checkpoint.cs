using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    [Header("Cài đặt quan trọng")]
    public string checkpointID = "Checkpoint_01";
    // [MỚI] Tên hiển thị trên Menu (Viết hoa, có dấu thoải mái)
    public string checkpointDisplayName = "FORBIDEN FOREST ENTRANCE";

    [Header("Hình ảnh")]
    public Sprite inactiveSprite; 
    public Sprite activeSprite;   

    [Header("Trạng thái")]
    public bool isActivated = false; 
    public bool isResting = false;   

    private SpriteRenderer rend;
    private PlayerController playerRef; 
    
    // [QUAN TRỌNG] Tham chiếu đến Menu nghỉ ngơi
    private RestMenuController restMenu; 

    void Start()
    {
        rend = GetComponent<SpriteRenderer>();

        // 1. Load trạng thái đã kích hoạt chưa
        if (PlayerPrefs.GetInt(checkpointID) == 1)
        {
            isActivated = true;
        }
        rend.sprite = isActivated ? activeSprite : inactiveSprite;

        // 2. [MỚI] Tự tìm script quản lý Menu trong scene
        restMenu = FindFirstObjectByType<RestMenuController>();
        if (restMenu == null)
        {
            Debug.LogWarning("Chưa tìm thấy RestMenuController trong Scene! Hãy kiểm tra lại Canvas.");
        }
    }

    void Update()
    {
        // Khi đang nghỉ, bấm ESC để thoát
        if (isResting && Input.GetKeyDown(KeyCode.Escape))
        {
            StopResting();
        }
    }

    public void Interact()
    {
        if (!isActivated)
        {
            ActivateCheckpoint();
        }
        else
        {
            StartResting();
        }
    }

    void ActivateCheckpoint()
    {
        isActivated = true;
        rend.sprite = activeSprite;
        
        // --- LƯU DỮ LIỆU ---
        PlayerPrefs.SetInt(checkpointID, 1); 
        PlayerPrefs.SetFloat("RespawnX", transform.position.x);
        PlayerPrefs.SetFloat("RespawnY", transform.position.y);
        PlayerPrefs.SetInt("HasCheckpoint", 1);

        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("SavedScene", currentSceneName);
        
        PlayerPrefs.Save(); 

        Debug.Log($"CHECKPOINT LIT! (Scene: {currentSceneName} | ID: {checkpointID})");
    }

    void StartResting()
    {
        if (isResting) return;
        isResting = true;
        Debug.Log("Resting...");

        // 1. Hồi phục cho Player
        if (playerRef != null)
        {
            playerRef.EnterRestState(); 

            // Hồi Máu
            PlayerHealth health = playerRef.GetComponent<PlayerHealth>();
            if (health != null) health.Heal(9999);
            
            // Hồi Mana
            playerRef.RestoreMana(9999);

            // Hồi Stamina (Thêm dòng này)
            playerRef.RefillStamina(); 
            
            // Nạp lại bình thuốc
            if (playerRef.consumableManager != null)
            {
                playerRef.consumableManager.RefillAllPotions();
            }

            // HỒI SINH QUÁI VẬT
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnEnemies();
            }
        }

        // 2. Hiệu ứng Chớp mắt và Mở Menu
        if (SceneFader.Instance != null)
        {
            Debug.Log("Bắt đầu Fade tối..."); // Debug xem dòng này có hiện không?

            // 1. Nhắm mắt (Tối dần)
            SceneFader.Instance.FadeToBlack(() => 
            {
                Debug.Log("Đã tối hẳn. Bật Menu..."); // Debug dòng này

                // 2. Khi đang tối thui: Bật Menu lên (Lúc này HUD sẽ tự tắt do code ở Bước 1)
                // [SỬA] Truyền tên hiển thị vào hàm OpenMenu
                if (restMenu != null) 
                {
                    restMenu.OpenMenu(checkpointDisplayName);
                }
                
                // 3. Mở mắt ra (Sáng dần) -> Thấy Player ngồi + Menu bên trái
                SceneFader.Instance.FadeToClear(); 
            });
        }
        else
        {
            Debug.LogWarning("KHÔNG TÌM THẤY SCENE FADER!");
            if (restMenu != null) restMenu.OpenMenu(checkpointDisplayName);
        }
    }

    // [QUAN TRỌNG] Phải để public để RestMenuController gọi được khi bấm nút "Leave"
    public void StopResting()
    {
        // 1. Tắt Menu ngay lập tức
        if (restMenu != null)
        {
            restMenu.CloseMenu();
        }

        // 2. Trả lại quyền điều khiển
        isResting = false;
        Debug.Log("Stand up");

        if (playerRef != null)
        {
            playerRef.ExitRestState();
        }
    }

    // --- PHÁT HIỆN NGƯỜI CHƠI ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerRef = other.GetComponent<PlayerController>();
            if (playerRef != null) playerRef.currentCheckpoint = this;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (playerRef != null) 
            {
                if (isResting) StopResting();
                playerRef.currentCheckpoint = null;
                playerRef = null;
            }
        }
    }
}