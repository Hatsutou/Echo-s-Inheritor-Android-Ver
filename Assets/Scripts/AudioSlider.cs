using UnityEngine;
using UnityEngine.UI; // Cần cho Button
using System.Collections.Generic; // Cần cho List

public class AudioSlider : MonoBehaviour
{
    [Header("Các Nút Điều Khiển")]
    [SerializeField] private Button decreaseButton; // Nút mũi tên trái
    [SerializeField] private Button increaseButton; // Nút mũi tên phải

    [Header("Các Chấm Hiển Thị")]
    [Tooltip("Kéo 10 GameObject 'Fill' (con của các 'Dot') vào đây")]
    [SerializeField] private List<GameObject> dotFills;

    private int currentValue = 5; // Giá trị mặc định (ví dụ: 5/10)
    private int maxValue = 10;

    void Start()
    {
        // Gắn sự kiện OnClick cho 2 nút bằng code
        if (decreaseButton != null)
        {
            decreaseButton.onClick.AddListener(OnDecrease);
        }
        if (increaseButton != null)
        {
            increaseButton.onClick.AddListener(OnIncrease);
        }

        // Đảm bảo maxValue khớp với số chấm
        maxValue = dotFills.Count;

        // Cập nhật hình ảnh lúc khởi động
        UpdateVisuals();
    }

    // Hàm này được gọi khi nhấn nút Trái (<)
    public void OnDecrease()
    {
        // Giảm giá trị
        currentValue--;
        // Không cho giảm dưới 0
        if (currentValue < 0)
        {
            currentValue = 0;
        }
        
        // Cập nhật lại 10 cái chấm
        UpdateVisuals();
    }

    // Hàm này được gọi khi nhấn nút Phải (>)
    public void OnIncrease()
    {
        // Tăng giá trị
        currentValue++;
        // Không cho tăng quá max
        if (currentValue > maxValue)
        {
            currentValue = maxValue;
        }

        // Cập nhật lại 10 cái chấm
        UpdateVisuals();
    }

    // === HÀM MỚI ĐỂ CLICK TRỰC TIẾP VÀO CHẤM ===
    // Hàm này được gọi bởi các nút Dot_1, Dot_2...
    public void SetValue(int newValue)
    {
        // Chỉ nhận giá trị trong khoảng 0 đến maxValue
        currentValue = Mathf.Clamp(newValue, 0, maxValue);

        // Cập nhật lại 10 cái chấm ngay lập tức
        UpdateVisuals();
    }

    // Hàm chính cập nhật hình ảnh
    private void UpdateVisuals()
    {
        // Loop qua tất cả 10 cái chấm
        for (int i = 0; i < dotFills.Count; i++)
        {
            if (i < currentValue)
            {
                // Nếu 'i' nhỏ hơn giá trị hiện tại, BẬT chấm đặc (Fill)
                dotFills[i].SetActive(true);
            }
            else
            {
                // Nếu không, TẮT chấm đặc (Fill)
                dotFills[i].SetActive(false);
            }
        }

        // (Tùy chọn) Vô hiệu hóa nút nếu đã ở mức min/max
        decreaseButton.interactable = (currentValue > 0);
        increaseButton.interactable = (currentValue < maxValue);

        // (Sau này) Gửi giá trị này đến Audio Mixer
        // float volumeLevel = (float)currentValue / maxValue;
        // Debug.Log("Volume set to: " + volumeLevel);
    }
}