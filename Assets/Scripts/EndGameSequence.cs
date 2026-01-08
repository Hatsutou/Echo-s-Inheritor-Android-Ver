using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndGameSequence : MonoBehaviour
{
    [Header("UI Components")]
    public CanvasGroup mainCanvasGroup; // Kéo Panel đen vào đây
    public TextMeshProUGUI thankYouText; // Chữ "Thank you..."
    public GameObject creditsPanel; // Panel chứa tên tác giả
    public TextMeshProUGUI toBeContinuedText; // Chữ "To be continued..."

    [Header("Settings")]
    public float fadeDuration = 1.5f;
    public float textDisplayTime = 3.0f;
    public float creditsDuration = 5.0f; // Thời gian chạy Credit

    void Start()
    {
        // 1. Ẩn tất cả lúc đầu game để không bị che màn hình
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0;
            mainCanvasGroup.blocksRaycasts = false;
        }
        
        if (thankYouText != null) thankYouText.alpha = 0;
        if (toBeContinuedText != null) toBeContinuedText.alpha = 0;
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // --- HÀM MÀ BẠN ĐANG BỊ THIẾU ---
    // [QUAN TRỌNG] Phải có chữ "public" thì script khác mới gọi được
    public void StartEnding()
    {
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // 1. FADE OUT TO BLACK (Màn hình tối dần)
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.blocksRaycasts = true; // Chặn chuột
            yield return StartCoroutine(FadeCanvasGroup(mainCanvasGroup, 0, 1, fadeDuration));
        }

        yield return new WaitForSeconds(1.0f);

        // 2. HIỆN "THANK YOU"
        if (thankYouText != null)
        {
            yield return StartCoroutine(FadeText(thankYouText, 0, 1, 1.0f)); // Hiện lên
            yield return new WaitForSeconds(textDisplayTime); // Đọc chữ
            yield return StartCoroutine(FadeText(thankYouText, 1, 0, 1.0f)); // Mờ đi
        }

        yield return new WaitForSeconds(0.5f);

        // 3. HIỆN CREDITS
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
            CanvasGroup creditsGroup = creditsPanel.GetComponent<CanvasGroup>();
            if (creditsGroup == null) creditsGroup = creditsPanel.AddComponent<CanvasGroup>(); // Tự thêm nếu quên
            
            creditsGroup.alpha = 0;
            yield return StartCoroutine(FadeCanvasGroup(creditsGroup, 0, 1, 1.0f)); // Hiện Credit
            
            // Chờ người chơi đọc Credit
            yield return new WaitForSeconds(creditsDuration);

            yield return StartCoroutine(FadeCanvasGroup(creditsGroup, 1, 0, 1.0f)); // Ẩn Credit
            creditsPanel.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f);

        // 4. HIỆN "TO BE CONTINUED..."
        if (toBeContinuedText != null)
        {
            yield return StartCoroutine(FadeText(toBeContinuedText, 0, 1, 1.0f));
            yield return new WaitForSeconds(textDisplayTime);
            yield return StartCoroutine(FadeText(toBeContinuedText, 1, 0, 1.0f));
        }

        yield return new WaitForSeconds(1.0f);

        // 5. VỀ MAIN MENU (Nhớ unlock thời gian cho chắc)
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Hàm phụ để Fade Canvas Group (Panel)
    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    // Hàm phụ để Fade TextMeshPro
    IEnumerator FadeText(TextMeshProUGUI text, float start, float end, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            text.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
        text.alpha = end;
    }
}