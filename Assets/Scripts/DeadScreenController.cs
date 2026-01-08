using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class DeadScreenController : MonoBehaviour
{
    [Header("Cài đặt Fade In")]
    public float fadeInDuration = 1.0f; // Hiện nhanh trong 1 giây
    public float delayBeforeFade = 0.5f; // Chờ 0.5 giây rồi hiện

    private CanvasGroup canvasGroup;
    private Canvas canvas; 

    void Start()
    {
        // 1. [FIX] TỰ ĐỘNG CÀI ĐẶT CANVAS (Để đảm bảo nó luôn nổi lên trên cùng)
        SetupCanvas();

        // 2. Thêm CanvasGroup nếu chưa có
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 3. Đảm bảo Alpha bắt đầu bằng 0
        canvasGroup.alpha = 0f; 
        canvasGroup.interactable = false; 
        canvasGroup.blocksRaycasts = false; 
    }

    // Hàm này ép Canvas phải hiển thị đè lên tất cả mọi thứ
    void SetupCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();

        // Ép chế độ hiển thị là đè lên màn hình
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; 
        
        // [QUAN TRỌNG] Đặt thứ tự hiển thị cực cao để đè lên HUD, Map...
        canvas.sortingOrder = 999; 
    }

    public void ShowDeadScreen()
    {
        Debug.Log("DeadScreen: Bắt đầu Fade In..."); 
        StartCoroutine(FadeInSequence());
    }

    IEnumerator FadeInSequence()
    {
        // Dùng Realtime để chờ, bất chấp game có bị pause hay không
        yield return new WaitForSecondsRealtime(delayBeforeFade);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            // [QUAN TRỌNG] Dùng unscaledDeltaTime để animation chạy mượt kể cả khi Time.timeScale = 0
            elapsed += Time.unscaledDeltaTime; 
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // Bật chặn tương tác
        canvasGroup.blocksRaycasts = true; 

        Debug.Log("DeadScreen: Đã hiện xong (Alpha = 1). Đang chờ reload...");

        // Chờ thêm 2 giây
        yield return new WaitForSecondsRealtime(2.0f);

        // Reload Game
        Debug.Log("DeadScreen: Tiến hành Reload Game.");
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Reset thời gian trước khi load
        Time.timeScale = 1f; 

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeOutAndLoad(currentScene);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }
    }
}