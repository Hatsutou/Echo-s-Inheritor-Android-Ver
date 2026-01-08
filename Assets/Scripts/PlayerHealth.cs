using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Chỉ số Huyết (Blood)")]
    public float maxBlood = 100f; 
    public float currentBlood;

    [Header("Kết nối UI")]
    public PlayerHUD hud; // Kéo script PlayerHUD vào đây
    private DeadScreenController deadScreen;

    // Biến nội bộ
    private Animator anim;
    private PlayerController playerControl; // [QUAN TRỌNG] Kết nối với PlayerController cũ
    private Rigidbody2D rb;
    private bool isDead = false;
    private Coroutine healCoroutine;

    void Start()
    {
        // 1. Khởi tạo
        currentBlood = maxBlood;
        
        // 2. Lấy component
        anim = GetComponent<Animator>();
        playerControl = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();

        // 3. Tự tìm UI nếu quên kéo
        if (hud == null) hud = FindFirstObjectByType<PlayerHUD>();
        deadScreen = FindFirstObjectByType<DeadScreenController>();

        if (hud != null) hud.UpdateBlood(currentBlood, maxBlood);
        
        if (deadScreen == null) Debug.LogWarning("Chưa có DeadScreenController trong Scene!");
    }

    // Hàm nhận sát thương
    public void TakeDamage(float damage)
    {
        if (isDead) return; 

        // [MỚI] BỊ ĐÁNH LÀ TẮT MENU NGAY
        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.ForceClose();
        }

        // Kiểm tra nếu đang lướt (Roll/Slide) thì bất tử
        if (playerControl != null)
        {
            // (Bạn cần đảm bảo biến isRolling/isSliding bên PlayerController là public hoặc có hàm check)
            // Nhưng tạm thời cứ để nhận dam bình thường nếu chưa public
        }

        currentBlood -= damage;
        if (currentBlood < 0) currentBlood = 0;

        // Cập nhật UI
        if (hud != null) hud.UpdateBlood(currentBlood, maxBlood);

        if (currentBlood <= 0) 
        {
            Die();
        }
        else 
        {
            PlayHurtAnim();
        }
    }

    public void PlayHurtAnim()
    {
        // Gọi sang PlayerController để chạy coroutine Hurt (khựng lại)
        if (playerControl != null)
        {
            playerControl.TriggerHurt(0.5f); 
        }
    }

    // Hàm hồi máu (Gọi từ Item)
    public void Heal(float amount)
    {
        if (isDead) return;

        float targetBlood = currentBlood + amount;
        if (targetBlood > maxBlood) targetBlood = maxBlood;

        if (healCoroutine != null) StopCoroutine(healCoroutine);
        healCoroutine = StartCoroutine(HealRoutine(targetBlood));
    }

    System.Collections.IEnumerator HealRoutine(float target)
    {
        float startValue = currentBlood;
        float duration = 0.5f; 
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentBlood = Mathf.Lerp(startValue, target, elapsed / duration);
            if (hud != null) hud.UpdateBlood(currentBlood, maxBlood);
            yield return null; 
        }

        currentBlood = target;
        if (hud != null) hud.UpdateBlood(currentBlood, maxBlood);
    }

    public void DieInstant()
    {
        currentBlood = 0;
        if (hud != null) hud.UpdateBlood(0, maxBlood);
        Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("--- BẮT ĐẦU QUY TRÌNH CHẾT (DIE) ---");
        anim.SetBool("IsDead", true);

        // 1. TẠO THÔNG ĐIỆP CHẾT
        string deathMessage = "What did I told you?"; 
        try 
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (playerControl != null && playerControl.GetCurrentStamina() <= 1f)
            {
                deathMessage = "It's exhausting to the point of death...";
            }
            // Gán thông điệp (Bọc trong try-catch để tránh lỗi nếu LevelLoader chưa có)
            LevelLoader.specificTip = deathMessage; 
        }
        catch (System.Exception e) 
        {
            Debug.LogWarning("Lỗi khi tạo thông điệp chết (Không nghiêm trọng): " + e.Message);
        }

        // 2. TẮT ĐIỀU KHIỂN & VẬT LÝ
        if (playerControl != null)
        {
            playerControl.enabled = false; 
            
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero; 
                
                // [FIX QUAN TRỌNG] Chuyển sang chế độ Static (Tĩnh)
                // Để nhân vật "đứng hình" luôn giữa không trung, không rơi xuống đáy vũ trụ nữa
                rb.bodyType = RigidbodyType2D.Static; 
            }
            Debug.Log("Đã đóng băng vật lý Player.");
        }

        // 3. XỬ LÝ TIỀN NONG (Khu vực nguy hiểm dễ gây lỗi nhất)
        try
        {
            if (CurrencyManager.Instance != null)
            {
                Vector3 markerPos = transform.position;
                if (playerControl != null)
                {
                    markerPos = playerControl.lastSafePosition;
                    markerPos.y += 0.5f; 
                }
                CurrencyManager.Instance.ProcessDeathPenalty(markerPos);
                Debug.Log("Đã xử lý rơi tiền.");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy CurrencyManager! Bỏ qua việc trừ tiền.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("LỖI NGHIÊM TRỌNG KHI TRỪ TIỀN: " + e.Message);
            // Game vẫn sẽ chạy tiếp chứ không dừng lại
        }

        // 4. HIỂN THỊ MÀN HÌNH CHẾT
        if (deadScreen != null)
        {
            Debug.Log("Đang gọi DeadScreen hiển thị...");
            deadScreen.ShowDeadScreen();
        }
        else
        {
            Debug.LogError("LỖI: Biến 'deadScreen' đang bị NULL! Hãy kéo Canvas chứa DeadScreenController vào Inspector của PlayerHealth!");
        }

        StartCoroutine(AutoRespawnSequence());
    }

    // --- HÀM TỰ ĐỘNG HỒI SINH ---
    IEnumerator AutoRespawnSequence()
    {
        Debug.Log("1. Player chết. Bắt đầu đếm ngược hồi sinh...");
        
        // [SỬA QUAN TRỌNG] Dùng Realtime để chạy kể cả khi game đang Pause (TimeScale = 0)
        yield return new WaitForSecondsRealtime(3.0f);

        Debug.Log("2. Đang Fade màn hình...");
        
        // Logic Fade giữ nguyên (nếu Fader dùng Tweening không phụ thuộc TimeScale thì OK)
        if (SceneFader.Instance != null)
        {
            // Ép Fader chạy kể cả khi pause (nếu Fader hỗ trợ)
            // Hoặc đơn giản là bỏ qua Fade nếu đang kẹt, ưu tiên load scene
            // Ở đây mình tạm bỏ qua Fade để test logic load trước đã
        }
        
        // 3. Chuẩn bị dữ liệu
        string targetScene = PlayerPrefs.GetString("SavedScene", "GameLevel");
        
        // [QUAN TRỌNG] Đảm bảo biến tĩnh của LevelLoader không gây nhiễu
        LevelLoader.sceneToLoad = ""; 

        PlayerPrefs.SetString("TargetScene", targetScene); 
        PlayerPrefs.Save();

        Debug.Log($"3. Chuẩn bị load về: {targetScene}. Đang gọi LoadingScene...");

        // [QUAN TRỌNG] Phải mở lại thời gian trước khi load scene mới
        // Nếu không sang màn mới mọi thứ vẫn đứng im
        Time.timeScale = 1f; 

        // Thay vì gọi trực tiếp SceneManager.LoadScene("LoadingScene");
        // Hãy dùng Fader để nó Fade đen trước rồi mới Load
        if (SceneFader.Instance != null)
        {
            // Gọi hàm Fade và bảo nó load sang LoadingScene
            SceneFader.Instance.FadeOutAndLoad("LoadingScene"); 
        }
        else
        {
            // Fallback: Nếu lỡ mất Fader thì mới load thẳng
            SceneManager.LoadScene("LoadingScene");
        } 
    }

    // [MỚI] Hàm dùng để Gán máu khi chuyển cảnh
    public void SetHealth(float amount)
    {
        currentBlood = amount;
        // Đảm bảo không vượt quá max hoặc âm
        if (currentBlood > maxBlood) currentBlood = maxBlood;
        if (currentBlood < 0) currentBlood = 0;

        if (hud != null) hud.UpdateBlood(currentBlood, maxBlood, true);
    }
}