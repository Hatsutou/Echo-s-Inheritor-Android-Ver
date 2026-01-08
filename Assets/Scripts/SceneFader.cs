using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    // Singleton để gọi từ bất cứ đâu
    public static SceneFader Instance;

    [Header("Cài đặt")]
    public CanvasGroup fadeGroup; // Kéo cái FadePanel vào đây
    public float fadeDuration = 1.0f; // Thời gian làm mờ (1 giây)
    public bool fadeInOnStart = true; // Có tự động làm sáng khi vào game không?

    void Awake()
    {
        // Singleton pattern cũ
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // [MỚI] Tự động bật FadePanel lên nếu lỡ tắt trong Editor
        if (fadeGroup != null)
        {
            // 1. Bật GameObject lên
            fadeGroup.gameObject.SetActive(true); 
            
            // 2. Ép Alpha về 1 (Đen xì) ngay lập tức để che màn hình lúc loading
            fadeGroup.alpha = 1f; 
            
            // 3. Chặn tia Raycast để không bấm xuyên qua được
            fadeGroup.blocksRaycasts = true;
        }
    }

    // [MỚI] THÊM HÀM NÀY ĐỂ DỌN DẸP
    void OnDestroy()
    {
        // Nếu cái Fader này bị hủy (do load lại màn), 
        // hãy xóa tham chiếu Instance đi để thằng mới lên thay.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        // 1. Tự động Fade In (Làm sáng) khi bắt đầu Scene
        if (fadeInOnStart && fadeGroup != null)
        {
            fadeGroup.alpha = 1f; // Đảm bảo đen xì trước
            StartCoroutine(Fade(1f, 0f)); // Chuyển từ 1 (Đen) -> 0 (Trong suốt)
        }
    }

    // Hàm này dùng để chuyển sang màn hình Loading
    // sceneName: Tên màn chơi muốn đến (VD: "GameLevel")
    public void FadeOutAndLoad(string sceneName)
    {
        StartCoroutine(FadeOutRoutine(sceneName));
    }

    IEnumerator FadeOutRoutine(string sceneName)
    {
        // 1. Chặn bấm chuột
        if (fadeGroup != null) fadeGroup.blocksRaycasts = true;

        // 2. Fade Out (Từ trong suốt -> Đen)
        // Dùng yield return để ĐỢI nó đen hẳn rồi mới làm tiếp
        yield return StartCoroutine(Fade(0f, 1f)); 

        // 3. LOGIC CHUYỂN CẢNH
        // Nếu đích đến LÀ "LoadingScene" -> Gọi thẳng SceneManager (để tránh đi đường vòng)
        if (sceneName == "LoadingScene")
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Nếu đích đến là map khác -> Gọi qua LevelLoader
            LevelLoader.LoadLevel(sceneName);
        }
    }

    // Hàm Fade chung (Alpha A -> Alpha B)
    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeGroup != null)
            {
                fadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            }
            yield return null;
        }
        
        // Chốt hạ
        if (fadeGroup != null)
        {
            fadeGroup.alpha = endAlpha;
            // Nếu alpha = 0 (trong suốt) thì tắt chặn chuột đi
            fadeGroup.blocksRaycasts = (endAlpha > 0.5f);
        }
    }

    // Hàm làm tối màn hình nhưng KHÔNG chuyển cảnh
    // Action onComplete: Là hành động sẽ làm sau khi tối xong (Ví dụ: Bật Menu)
    public void FadeToBlack(System.Action onComplete)
    {
        StartCoroutine(FadeRoutine(0f, 1f, onComplete));
    }

    // Hàm làm sáng màn hình trở lại
    public void FadeToClear()
    {
        StartCoroutine(FadeRoutine(1f, 0f, null));
    }

    IEnumerator FadeRoutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            if (fadeGroup != null)
                fadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            yield return null;
        }

        if (fadeGroup != null) fadeGroup.alpha = endAlpha;

        // Chạy hành động tiếp theo (nếu có)
        if (onComplete != null) onComplete.Invoke();
    }
}