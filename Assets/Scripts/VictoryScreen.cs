using UnityEngine;
using TMPro;
using System.Collections;

public class VictoryScreen : MonoBehaviour
{
    [Header("Components")]
    public CanvasGroup canvasGroup; // Kéo chính cái VictoryCanvas vào đây
    public TextMeshProUGUI victoryText; // Kéo cái chữ TextMeshPro vào đây

    [Header("Settings")]
    public float fadeDuration = 3.0f; // Thời gian hiện lên từ từ
    public float displayDuration = 5.0f; // Thời gian giữ chữ trên màn hình

    void Start()
    {
        // Đảm bảo lúc đầu game nó ẩn đi
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false; 
        }
    }

    // Hàm này để Boss gọi
    public void ShowVictory(string message)
    {
        if (victoryText != null) victoryText.text = message;
        StartCoroutine(PlayAnimation());
    }

    IEnumerator PlayAnimation()
    {
        float timer = 0;
        
        // 1. SETUP: Phóng to chữ một chút để lát nữa thu nhỏ (Zoom Out)
        if (canvasGroup != null) 
            canvasGroup.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        // 2. FADE IN & ZOOM OUT (Hiệu ứng Souls-like)
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            
            // Ease Out Cubic cho mượt
            float ease = 1 - Mathf.Pow(1 - t, 3); 

            if (canvasGroup != null)
            {
                // Hiện dần
                canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                
                // Thu nhỏ dần về kích thước gốc (tạo độ sâu)
                canvasGroup.transform.localScale = Vector3.Lerp(new Vector3(1.1f, 1.1f, 1f), Vector3.one, ease);
            }

            yield return null;
        }

        // Chốt hạ
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.transform.localScale = Vector3.one;
        }
    }
}