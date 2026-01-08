using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance;

    // --- BIẾN TRẠNG THÁI ---
    public bool isMenuOpen = false;

    [Header("Cài Đặt Radar (Tự đóng khi có địch)")]
    public float dangerRadius = 10f; 
    public LayerMask enemyLayer;     

    [Header("UI Components")]
    public GameObject menuPanel;       
    public GameObject systemPanel;     
    public GameObject placeholderPanel;
    public GameObject playerHUD; 

    [Header("Menu Navigation")]
    // QUAN TRỌNG: Kéo nút vào List này theo đúng thứ tự hiển thị trên màn hình
    // Ví dụ: 0:Equipment, 1:Inventory, 2:Status, 3:System
    public List<RectTransform> menuButtons; 
    
    [Header("Highlight FX Settings")]
    public RectTransform highlightRect;   
    public Image highlightImage;          
    [Range(1f, 20f)]
    public float blinkSpeed = 10f;        
    public Color textBrightColor = Color.white; 

    [Header("Settings Integration")]
    // Kéo cái Prefab Setting (đang nằm trong Hierarchy) vào đây
    public SettingsMenuController settingsMenu;
    
    // --- LOGIC VARIABLES ---
    private int currentIndex = 0;
    private Transform playerTransform;
    private Coroutine blinkCoroutine;
    
    private TextMeshProUGUI currentText; 
    private Color originalTextColor; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerTransform = player.transform;
        
        if (menuPanel != null) menuPanel.SetActive(false);

        // --- [THÊM DÒNG NÀY] ---
        // Đảm bảo tắt màn hình Setting khi bắt đầu game để tránh bị kẹt
        if (settingsMenu != null) settingsMenu.gameObject.SetActive(false);
    }

    void Update()
    {
        // --- [MỚI] CHỐT CHẶN ---
        // Nếu Settings đang mở thì PauseMenu KHÔNG ĐƯỢC CAN THIỆP
        // Để cho Settings tự xử lý nút ESC của nó
        if (settingsMenu != null && settingsMenu.gameObject.activeSelf)
        {
            return; 
        }

        // 1. INPUT BẬT/TẮT MENU (ESC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen) CloseMenu();
            else TryOpenMenu(); // Sửa lại gọi TryOpenMenu để check Radar
        }

        // Nếu menu chưa mở thì không chạy logic bên dưới
        if (!isMenuOpen) return;

        // 2. CHECK RADAR LIÊN TỤC
        CheckDanger();

        // 3. XỬ LÝ ĐIỀU HƯỚNG
        HandleMenuInput();
    }

    // --- LOGIC ĐIỀU HƯỚNG (CHỈ MŨI TÊN) ---
    void HandleMenuInput()
    {
        // Thay vì dùng GetAxis (dính cả W/S), ta bắt riêng phím mũi tên
        if (Input.GetKeyDown(KeyCode.DownArrow)) // Chỉ Mũi Tên Xuống
        {
            currentIndex++;
            if (currentIndex >= menuButtons.Count) currentIndex = 0; 
            UpdateSelectionVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow)) // Chỉ Mũi Tên Lên
        {
            currentIndex--;
            if (currentIndex < 0) currentIndex = menuButtons.Count - 1; 
            UpdateSelectionVisuals();
        }

        // Logic Chọn (Enter, E hoặc Chuột Trái)
        // Bỏ Space nếu bạn sợ nhảy nhót lung tung
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            ExecuteButtonAction();
        }
    }

    // --- HÀM CẬP NHẬT HÌNH ẢNH ---
    void UpdateSelectionVisuals()
    {
        if (menuButtons.Count == 0) return;

        RectTransform targetButton = menuButtons[currentIndex];

        if (currentText != null) currentText.color = originalTextColor;
        
        currentText = targetButton.GetComponentInChildren<TextMeshProUGUI>();
        if (currentText != null) originalTextColor = currentText.color;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect(targetButton));
    }

    IEnumerator BlinkEffect(RectTransform target)
    {
        while (true)
        {
            // Lerp vị trí
            highlightRect.position = Vector3.Lerp(highlightRect.position, target.position, Time.unscaledDeltaTime * 20f);

            // Nhấp nháy
            float t = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
            
            if (highlightImage != null)
            {
                Color c = highlightImage.color;
                c.a = Mathf.Lerp(0.2f, 1f, t);
                highlightImage.color = c;
            }

            if (currentText != null)
            {
                currentText.color = Color.Lerp(originalTextColor, textBrightColor, t);
            }

            yield return null;
        }
    }

    // --- XỬ LÝ CHỨC NĂNG TỪNG NÚT (SỬA LẠI THEO MENU CỦA BẠN) ---
    void ExecuteButtonAction()
    {
        // Vì menu bạn KHÔNG CÓ Resume/Quit, thứ tự index sẽ thay đổi.
        // Giả sử list 'Menu Buttons' trong Inspector bạn kéo theo thứ tự:
        // 0: Equipment
        // 1: Inventory
        // 2: Status
        // 3: System
        
        switch (currentIndex)
        {
            case 0: // EQUIPMENT (Index 0)
                OnClick_Equipment();
                break;

            case 1: // INVENTORY (Index 1)
                OnClick_Inventory();
                break;
            
            case 2: // STATUS (Index 2)
                OnClick_Status();
                break;

            case 3: // SYSTEM (Index 3)
                OnClick_System();
                break;
                
            // Nếu sau này thêm nút Quit vào cuối thì thêm case 4
        }
    }

    // --- LOGIC RADAR (CHECK ĐỊCH) ---
    void TryOpenMenu()
    {
        if (IsSafe()) OpenMenu();
        else Debug.Log("Có địch! Không mở menu được.");
    }

    bool IsSafe()
    {
        if (playerTransform == null) return true;
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(playerTransform.position, dangerRadius, enemyLayer);
        foreach (var hit in hitObjects)
        {
            if (hit.gameObject == playerTransform.gameObject) continue;
            // Nếu cần loại trừ thêm Tag gì thì thêm vào đây
            return false; 
        }
        return true; 
    }
    
    void CheckDanger()
    {
        // Nếu đang mở menu mà kẻ địch đi vào vùng cấm -> Đóng ngay lập tức
        if (!IsSafe()) CloseMenu();
    }

    // --- CÁC HÀM ĐÓNG MỞ ---
    void OpenMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (playerHUD != null) playerHUD.SetActive(false); 

        isMenuOpen = true;
        currentIndex = 0; // Reset về nút đầu (Equipment)
        
        StartCoroutine(SnapToFirstButton());
    }

    IEnumerator SnapToFirstButton()
    {
        yield return null; 
        if (menuButtons.Count > 0)
        {
            highlightRect.position = menuButtons[0].position;
            UpdateSelectionVisuals();
        }
    }

    public void CloseMenu()
    {
        if (currentText != null) currentText.color = originalTextColor;

        if (menuPanel != null) menuPanel.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(true);

        isMenuOpen = false;
        
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
    }

    public void ForceClose()
    {
        if (isMenuOpen) CloseMenu();
    }
    
    // --- HÀM HỖ TRỢ CHUỘT ---
    public void SetSelectionByMouse(RectTransform hoveredButton)
    {
        int index = menuButtons.IndexOf(hoveredButton);
        if (index != -1 && index != currentIndex)
        {
            currentIndex = index;     
            UpdateSelectionVisuals(); 
        }
    }
    
    // --- UI BUTTON LOGIC ---
    public void OnClick_Equipment() { ShowPlaceholder(); }
    public void OnClick_Inventory() { ShowPlaceholder(); }
    public void OnClick_Status()    { ShowPlaceholder(); }
    public void OnClick_System()
    {
        // --- [MỚI] Ẩn giao diện Menu chính đi ---
        if (menuPanel != null) menuPanel.SetActive(false); 
        // ----------------------------------------

        // Ẩn các panel cũ nếu cần
        if (placeholderPanel != null) placeholderPanel.SetActive(false);
        if (systemPanel != null) systemPanel.SetActive(false);

        // Bật màn hình Setting lên
        if (settingsMenu != null)
        {
            // [QUAN TRỌNG] Phải báo tin trước rồi mới được bật lên
            settingsMenu.isPauseMenuMode = true; 
            
            // Sau khi biến đã là true, lúc này bật lên hàm OnEnable mới bắt đúng giá trị
            settingsMenu.gameObject.SetActive(true); 
            
            settingsMenu.SelectTab(0, true);
        }
    }

    // --- [MỚI] HÀM GỌI KHI ĐÓNG SETTING ---
    public void OnCloseSettings()
    {
        // 1. Hiện lại Menu chính (Nút bấm, Background...)
        if (menuPanel != null) menuPanel.SetActive(true);

        // 2. (Tùy chọn) Highlight lại đúng nút System để người chơi bấm tiếp được luôn
        // Giả sử nút System của bạn là nút số 3 (theo thứ tự list 0,1,2,3)
        currentIndex = 3; 
        UpdateSelectionVisuals();
    }

    void ShowPlaceholder()
    {
        // Logic hiển thị panel tạm thời
        if (systemPanel != null) systemPanel.SetActive(false);
        if (placeholderPanel != null) placeholderPanel.SetActive(true);
        
        // Lưu ý: Hàm này KHÔNG đóng Menu Panel chính, nên menu vẫn mở bình thường.
    }
    
    void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, dangerRadius);
        }
    }
}