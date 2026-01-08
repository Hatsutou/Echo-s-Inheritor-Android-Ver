using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic; // Để dùng List

public class LevelLoader : MonoBehaviour
{
    [Header("UI References")]
    public Image loadingFill;
    public TextMeshProUGUI progressText;
    
    // [MỚI] Kéo cái Text nằm trong bảng Quick Tip vào đây
    public TextMeshProUGUI tipContentText; 

    // [MỚI] Biến tĩnh để lưu Tip cụ thể từ màn chơi trước gửi sang
    public static string specificTip = ""; 

    // [MỚI] Danh sách các câu Tip ngẫu nhiên (Bạn điền trong Inspector)
    [TextArea(3, 5)] // Giúp ô nhập text rộng ra cho dễ viết
    public List<string> randomTips;

    // ... (Các biến cũ: sceneToLoad, minLoadTime giữ nguyên) ...
    public static string sceneToLoad;
    public float minLoadTime = 2.0f;

    void Start()
    {
        // --- XỬ LÝ QUICK TIP ---
        if (tipContentText != null)
        {
            // Ưu tiên 1: Nếu có Tip cụ thể (do chết vì lý do đặc biệt) -> Hiện nó
            if (!string.IsNullOrEmpty(specificTip))
            {
                tipContentText.text = specificTip;
                specificTip = ""; // Xóa đi để lần sau không hiện lại
            }
            // Ưu tiên 2: Nếu không có gì đặc biệt -> Random ngẫu nhiên
            else if (randomTips.Count > 0)
            {
                int randomIndex = Random.Range(0, randomTips.Count);
                tipContentText.text = randomTips[randomIndex];
            }
        }
        // -----------------------

        // ========================================================
        // 2. [SỬA QUAN TRỌNG] LOGIC ĐỌC TÊN MÀN CHƠI
        // ========================================================
        
        // Ưu tiên 1: Lấy từ lệnh trực tiếp (Ví dụ từ Main Menu)
        string target = sceneToLoad;

        // Ưu tiên 2: Nếu không có lệnh trực tiếp -> Kiểm tra tin nhắn Hồi Sinh
        if (string.IsNullOrEmpty(target))
        {
            target = PlayerPrefs.GetString("TargetScene", "");
        }

        // 3. TIẾN HÀNH LOAD
        if (!string.IsNullOrEmpty(target))
        {
            // Xóa tin nhắn đi để tránh lỗi lần sau
            PlayerPrefs.DeleteKey("TargetScene"); 
            
            StartCoroutine(LoadAsynchronously(target));
        }
        else
        {
            Debug.LogError("LevelLoader: Không biết phải load màn nào! (Cả sceneToLoad và TargetScene đều rỗng)");
        }
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        // Bắt đầu load ngầm
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        // Ngăn không cho Unity tự động chuyển cảnh khi xong (để ta kiểm soát thanh loading)
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;

            // 1. Tính toán tiến độ thật (Unity trả về 0 -> 0.9)
            float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // 2. Tính toán tiến độ "giả" theo thời gian (để thanh chạy mượt, không nhảy cóc)
            float timeProgress = Mathf.Clamp01(timer / minLoadTime);

            // 3. Lấy số nhỏ nhất giữa 2 cái (để đảm bảo chạy đủ thời gian minLoadTime)
            float displayProgress = Mathf.Min(actualProgress, timeProgress);

            // Cập nhật UI
            if (loadingFill != null) loadingFill.fillAmount = displayProgress;
            if (progressText != null) progressText.text = Mathf.RoundToInt(displayProgress * 100) + "%";

            // Kiểm tra điều kiện hoàn thành:
            // Load xong (actual=1) VÀ Đã chạy đủ thời gian chờ (time=1)
            if (actualProgress >= 1f && timeProgress >= 1f)
            {
                // [MỚI] Fade Out màn hình Loading trước khi vào game
                // (Giả sử LoadingScene cũng có gắn SceneFader và FadePanel)
                if (SceneFader.Instance != null)
                {
                    // Tự viết hàm FadeOutManual trong SceneFader hoặc dùng code tay ở đây
                    // Để đơn giản, ta cho phép chuyển cảnh luôn, 
                    // vì bên Scene GameLevel đã có Fade In On Start rồi nên nó sẽ mượt.
                }

                // Cho phép vào game
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    // Hàm tĩnh để Main Menu gọi
    public static void LoadLevel(string sceneName)
    {
        sceneToLoad = sceneName;
        // Chuyển sang màn hình Loading trước
        SceneManager.LoadScene("LoadingScene"); 
    }
}