using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Để lấy độ rộng RectTransform

public class InteractIndicator : MonoBehaviour
{
    public PlayerController player; 
    public GameObject visualRoot;   

    [Header("Settings")]
    public float animSpeed = 10f; 
    
    [Tooltip("Khoảng cách cộng thêm thủ công nếu thấy vẫn chưa vừa ý")]
    public float extraPadding = 0.5f; 

    // Các biến nội bộ
    private Vector3 baseScale; 
    private Vector3 basePos;   
    private bool isShown = false; 
    private Coroutine animCoroutine; 
    private RectTransform rectTrans; // [MỚI] Để đo chiều rộng nút

    void Start()
    {
        if (visualRoot != null) 
        {
            baseScale = visualRoot.transform.localScale;
            basePos = visualRoot.transform.localPosition;
            rectTrans = visualRoot.GetComponent<RectTransform>(); // Lấy RectTransform
            
            visualRoot.transform.localScale = new Vector3(0, baseScale.y, baseScale.z);
            visualRoot.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null || visualRoot == null) return;

        bool canInteract = (player.currentDoor != null) || 
                           (player.currentCheckpoint != null) || 
                           (player.currentEndGameStatue != null) ||
                           (player.currentDeathMarker != null);

        if (canInteract != isShown)
        {
            isShown = canInteract;
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            
            if (isShown)
            {
                visualRoot.SetActive(true);
                animCoroutine = StartCoroutine(AnimateScale(0f, 1f));
            }
            else
            {
                animCoroutine = StartCoroutine(AnimateScale(1f, 0f));
            }
        }

        HandleOrientation();
    }

    IEnumerator AnimateScale(float startRatio, float endRatio)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;
            float currentRatio = Mathf.Lerp(startRatio, endRatio, t);
            ApplyScale(currentRatio);
            yield return null;
        }
        ApplyScale(endRatio);
        if (endRatio == 0f) visualRoot.SetActive(false);
    }

    void ApplyScale(float ratio)
    {
        float targetX = baseScale.x * ratio;
        // Lật scale theo hướng player
        if (player.transform.localScale.x < 0) targetX = -Mathf.Abs(targetX); 
        else targetX = Mathf.Abs(targetX);

        visualRoot.transform.localScale = new Vector3(targetX, baseScale.y, baseScale.z);
    }

    void HandleOrientation()
    {
        if (!visualRoot.activeSelf) return;

        // 1. Xử lý Lật mặt chữ
        float currentAbsX = Mathf.Abs(visualRoot.transform.localScale.x);
        float finalScaleX = currentAbsX;
        
        // 2. Tính toán độ lệch vị trí (Offset)
        Vector3 targetPos = basePos;

        if (player.transform.localScale.x < 0) // Đang quay trái
        {
            finalScaleX = -currentAbsX; 
            
            // [LOGIC SỬA LỖI] 
            // Khi quay trái, ta phải đẩy nút sang trái thêm một đoạn bằng:
            // (Chiều rộng nút * Tỷ lệ Scale) + Padding
            float buttonWidth = 0f;
            if (rectTrans != null)
            {
                // Tính chiều rộng thực tế trong local space
                // (Lấy chiều rộng gốc * scale gốc của trục X)
                buttonWidth = rectTrans.rect.width * Mathf.Abs(baseScale.x);
            }

            // Dịch chuyển vị trí gốc thêm một đoạn bằng chiều rộng nút
            targetPos.x = basePos.x + buttonWidth + extraPadding;
        }

        // Áp dụng thay đổi
        visualRoot.transform.localScale = new Vector3(finalScaleX, baseScale.y, baseScale.z);
        visualRoot.transform.localPosition = targetPos;
    }
}