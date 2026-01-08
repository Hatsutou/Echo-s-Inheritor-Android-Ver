using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections; // [MỚI] Cần thêm cái này để dùng Coroutine

public class RestMenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuPanel; // Kéo cái RestMenu_Panel vào đây
    public GameObject mainHUD;//Kéo cái HUD_Canvas (hoặc Panel chứa máu/mana) vào đây

    // [MỚI] Kéo cái Text tiêu đề (VD: cái chữ đang ghi "RESTING") vào đây
    public TextMeshProUGUI menuTitleText;

    // [MỚI] Kéo cái Selection_Highlight bạn vừa tạo vào đây
    public RectTransform selectionHighlight;

    [Header("Buttons Configuration")]
    // Kéo 3 cái ảnh nút (Btn_LevelUp, Btn_Potions, Btn_Leave) vào list này theo thứ tự
    public List<Image> buttonImages; 
    // Kéo 3 cái Text con của nút vào list này
    public List<TextMeshProUGUI> buttonTexts;

    [Header("Sprites")]
    public Sprite defaultSprite;   // Kéo Buttons_4.png vào
    public Sprite selectedSprite;  // Kéo Button_Selected.png vào

    [Header("Visual Effects")]
    public Color selectedTextColor = new Color(1f, 0.8f, 0.4f); // Màu vàng nhạt
    public Color defaultTextColor = Color.white;
    public float blinkSpeed = 5f;

    // Logic Variables
    private int currentIndex = 0;
    private bool inputPressed = false;
    private bool isActive = false;
    // Tham chiếu
    private PlayerController playerControl;

    void Start()
    {
        // Tắt menu khi bắt đầu game
        CloseMenu();
        playerControl = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!isActive) return;

        HandleInput();
        HandleVisuals();
    }

    public void OpenMenu(string locationName)
    {
        if (mainHUD != null) mainHUD.SetActive(false);
        menuPanel.SetActive(true);

        if (menuTitleText != null)
        {
            menuTitleText.text = !string.IsNullOrEmpty(locationName) ? locationName : "REST AREA";
        }
        
        isActive = true;
        currentIndex = 0;

        // [THAY ĐỔI QUAN TRỌNG]
        // Không gọi UpdateSelectionVisuals() ngay lập tức nữa.
        // Mà thay vào đó là bắt đầu một Coroutine chờ đợi.
        StartCoroutine(SetupInitialSelection());
    }

    // [MỚI] Coroutine chờ UI sắp xếp xong
    private IEnumerator SetupInitialSelection()
    {
        // Chờ cho đến cuối khung hình hiện tại.
        // Lúc này Unity đã tính toán xong vị trí của tất cả các nút trong Layout Group.
        yield return new WaitForEndOfFrame();

        // Bây giờ mới cập nhật vị trí khung chọn, đảm bảo chính xác.
        UpdateSelectionVisuals();
    }

    public void CloseMenu()
    {
        // 1. Tắt Menu nghỉ
        menuPanel.SetActive(false);
        isActive = false;

        // 2. Bật lại HUD chính
        if (mainHUD != null) mainHUD.SetActive(true);
    }

    void HandleInput()
    {
        float vInput = Input.GetAxisRaw("Vertical");

        if (vInput < -0.5f && !inputPressed)
        {
            currentIndex++;
            if (currentIndex >= buttonImages.Count) currentIndex = 0;
            inputPressed = true;
            UpdateSelectionVisuals();
        }
        else if (vInput > 0.5f && !inputPressed)
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = buttonImages.Count - 1;
            inputPressed = true;
            UpdateSelectionVisuals();
        }
        else if (vInput == 0)
        {
            inputPressed = false;
        }

        // Chọn nút (Enter hoặc E hoặc Space)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            SelectButton();
        }
    }

    void HandleVisuals()
    {
        // Hiệu ứng nhấp nháy cho Text đang chọn
        float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        // Hoặc nhấp nháy độ sáng (giữ nguyên alpha = 1)
        float brightness = 0.5f + (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.25f; // Dao động từ 0.5 đến 1
        
        buttonTexts[currentIndex].color = new Color(selectedTextColor.r, selectedTextColor.g, selectedTextColor.b, brightness);
    }

    // [MỚI] HÀM CẬP NHẬT HIỂN THỊ (Thay thế UpdateButtonSprites)
    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < buttonImages.Count; i++)
        {
            if (i == currentIndex)
            {
                // 1. Di chuyển khung Highlight đến vị trí nút đang chọn
                // Dùng position toàn cầu để khớp vị trí bất kể cha con
                selectionHighlight.position = buttonImages[i].transform.position;

                // 2. Tắt ảnh nền của nút hiện tại (để lộ khung Highlight bên dưới)
                buttonImages[i].enabled = false;
            }
            else
            {
                // Nút không chọn: Bật ảnh nền mặc định lên
                buttonImages[i].enabled = true;
                
                // Reset màu chữ
                buttonTexts[i].color = defaultTextColor;
            }
        }
    }

    void SelectButton()
    {
        switch (currentIndex)
        {
            case 0: // LEVEL UP
                Debug.Log("Open Level Up Screen");
                // TODO: Mở UI nâng chỉ số chi tiết ở đây
                break;

            case 1: // POTIONS
                Debug.Log("Open Potion Crafting/Assigning");
                // TODO: Mở UI pha chế thuốc
                break;

            case 2: // LEAVE
                OnLeaveButtonPressed();
                break;
        }
    }

    public void OnLeaveButtonPressed()
    {
        // Gọi hàm thoát bên Checkpoint thông qua Player
        if (playerControl != null && playerControl.currentCheckpoint != null)
        {
            playerControl.currentCheckpoint.StopResting();
        }
    }
}