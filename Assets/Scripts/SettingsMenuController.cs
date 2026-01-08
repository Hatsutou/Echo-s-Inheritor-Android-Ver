using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SettingsMenuController : MonoBehaviour
{
    // Biến này quyết định hành vi của nút Back
    // False: Load Scene Main Menu (Mặc định)
    // True: Chỉ tắt GameObject này đi (Dùng cho Pause Menu)
    public bool isPauseMenuMode = false;

    // === CÁC BIẾN MỚI ===
    [Header("Các Nền Được Chọn")]
    [Tooltip("Kéo các GameObject 'Selected_Background' của 3 tab vào đây")]
    [SerializeField] private List<GameObject> tabSelectedBackgrounds; // <-- List mới

    [Header("Màu Sắc Chữ")]
    [SerializeField] private Color defaultTextColor = Color.gray;
    [SerializeField] private Color selectedTextColor = Color.white;

    [Header("Hiệu Ứng Nhấp Nháy")]
    [SerializeField] private float blinkSpeed = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float minBlinkAlpha = 0.5f;

    [Header("Các Đối Tượng Tab")]
    [SerializeField] private List<TextMeshProUGUI> tabTexts;
    [SerializeField] private List<GameObject> tabPanels;

    [Header("Các Bảng Pop-up")]
    [Tooltip("Kéo GameObject 'ScreenMode_Popup' vào đây")]
    [SerializeField] private GameObject screenModePopup;
    [Tooltip("Kéo GameObject 'Resolution_Popup' vào đây")]
    [SerializeField] private GameObject resolutionPopup;
    [Tooltip("Kéo GameObject 'Qualities_Popup' vào đây")]
    [SerializeField] private GameObject qualitiesPopup;
    [Tooltip("Kéo GameObject 'Language_Popup' vào đây")]
    [SerializeField] private GameObject languagePopup;
    [Tooltip("Kéo GameObject 'Popup_Blocker' vào đây")]
    [SerializeField] private GameObject popupBlocker;

    [Header("Các Nút Cài Đặt")]
    [Tooltip("Kéo nút 'ScreenMode_Button' vào đây")]
    [SerializeField] private Button screenModeButton;
    [Tooltip("Kéo Text (TMP) con của 'ScreenMode_Button' vào đây")]
    [SerializeField] private TextMeshProUGUI screenModeButtonText;
    [Tooltip("Kéo nút 'Resolution_Button' vào đây")]
    [SerializeField] private Button resolutionButton;
    [Tooltip("Kéo Text (TMP) con của 'Resolution_Button' vào đây")]
    [SerializeField] private TextMeshProUGUI resolutionButtonText;
    [Tooltip("Kéo nút 'Qualities_Button' vào đây")]
    [SerializeField] private Button qualitiesButton;
    [Tooltip("Kéo Text (TMP) con của 'Qualities_Button' vào đây")]
    [SerializeField] private TextMeshProUGUI qualitiesButtonText;
    [SerializeField] private Button languageButton;
    [SerializeField] private TextMeshProUGUI languageButtonText; 

    [Header("Các Nút Lựa Chọn (Bên trong Pop-up)")]
    [SerializeField] private Button fullscreenOptionButton;
    [SerializeField] private Button windowedOptionButton;
    

    [Header("Các Nút Bật/Tắt (Toggles)")]
    [Tooltip("Kéo GameObject 'Fill' (con của Subtitles_Toggle_Button) vào đây")]
    [SerializeField] private GameObject subtitlesFill;
    [Tooltip("Kéo Text 'Toggle_Status_Text' vào đây")]
    [SerializeField] private TextMeshProUGUI subtitlesStatusText;
    [Tooltip("Kéo 'Fill' (con của Vibration_Toggle_Button) vào đây")]
    [SerializeField] private GameObject vibrationFill;
    [Tooltip("Kéo 'Toggle_Status_Text' (con của Vibration_Toggle_Group) vào đây")]
    [SerializeField] private TextMeshProUGUI vibrationStatusText;
    [Tooltip("Kéo 'Fill' (con của Tutorials_Toggle_Button) vào đây")]
    [SerializeField] private GameObject tutorialsFill;
    [Tooltip("Kéo 'Toggle_Status_Text' (con của Tutorials_Toggle_Group) vào đây")]
    [SerializeField] private TextMeshProUGUI tutorialsStatusText;

    [Header("Cài Đặt Scene")]
    [Tooltip("Tên chính xác của Scene Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Nút Đặc Biệt")]
    [Tooltip("Kéo cái nút 'Btn_ReturnToMainMenu' ở tab Gameplay vào đây")]
    [SerializeField] private GameObject returnToMainButton; // [MỚI]

    private int currentTabIndex = 0;

    // Biến này để theo dõi trạng thái
    private bool areSubtitlesOn = false;
    private bool isVibrationOn = false;
    private bool areTutorialsOn = true;

    void Start()
    {
        SelectTab(0, true);
        screenModePopup.SetActive(false);
        resolutionPopup.SetActive(false);
        qualitiesPopup.SetActive(false);
        languagePopup.SetActive(false);

        // Cập nhật trạng thái ban đầu
        areSubtitlesOn = false; // Đảm bảo trạng thái mặc định là "Tắt"
        UpdateSubtitlesVisuals(); // Gọi hàm mới để cập nhật Fill VÀ Text

        isVibrationOn = false; // Đảm bảo trạng thái mặc định là "Tắt"
        UpdateVibrationVisuals(); // Gọi hàm mới để cập nhật Fill VÀ Text

        areTutorialsOn = true; // <-- THÊM DÒNG NÀY (Mặc định Bật)
        UpdateTutorialsVisuals();
    }

    void Update()
    {
        HandleTabBlinking();

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ưu tiên: Nếu đang mở Popup thì đóng Popup trước
            if (popupBlocker.activeSelf)
            {
                CloseAllPopups();
            }
            // Nếu không có Popup nào -> Quay về Main Menu
            else
            {
                BackToMainMenu();
            }
        }
    }

    // --- [MỚI] HÀM QUAY VỀ (Gán cho nút Mũi Tên) ---
    public void BackToMainMenu()
    {
        // Kiểm tra xem đang ở chế độ nào
        if (isPauseMenuMode)
        {
            // [CHẾ ĐỘ PAUSE MENU]
            Debug.Log("Đóng Setting (Ẩn đi)");
            gameObject.SetActive(false); // Chỉ ẩn chính nó đi
            
            // (Tùy chọn) Gọi về PauseMenu để nó biết mà highlight lại nút System
            if (PauseMenuController.Instance != null)
            {
                PauseMenuController.Instance.OnCloseSettings();
            }
        }
        else
        {
            // [CHẾ ĐỘ MAIN MENU - Cũ]
            Debug.Log("Quay về Main Menu Scene...");
            // Bật cờ skip intro bên MainMenu (như bạn đã làm)
            MainMenuController.shouldSkipIntro = true;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // === HÀM MỚI DÀNH CHO NÚT BẤM (BUTTON) ===
    public void OnTabClicked(int tabIndex)
    {
        // Hàm này chỉ có 1 tham số (int), vì vậy Button có thể thấy nó.
        // Nó sẽ gọi hàm logic chính của chúng ta.
        SelectTab(tabIndex, false);
    }

    // === HÀM CHÍNH ĐÃ NÂNG CẤP ===
    public void SelectTab(int tabIndex, bool forceUpdate = false)
    {
        if (tabIndex == currentTabIndex && !forceUpdate) return;
        currentTabIndex = tabIndex;

        for (int i = 0; i < tabSelectedBackgrounds.Count; i++)
        {
            if (i == currentTabIndex)
            {
                // Đây là tab ĐƯỢC CHỌN:
                tabSelectedBackgrounds[i].SetActive(true); // BẬT nền 'Selected_.png'
                if(tabPanels.Count > i) tabPanels[i].SetActive(true);
            }
            else
            {
                // Đây là tab KHÔNG ĐƯỢC CHỌN:
                tabSelectedBackgrounds[i].SetActive(false); // TẮT nền 'Selected_.png'
                tabTexts[i].color = defaultTextColor;
                if(tabPanels.Count > i) tabPanels[i].SetActive(false);
            }
        }
    }

    // === CÁC HÀM MỚI ĐỂ QUẢN LÝ POP-UP ===

    // 1. Hàm này được gọi bởi nút "Screen Mode" (cái nút to)
    public void OnScreenModeClicked()
    {
        CloseAllPopups(); // Đóng mọi thứ khác trước
        popupBlocker.SetActive(true); // Bật Blocker
        screenModePopup.SetActive(true); // Bật Pop-up
    }

    // 2. Hàm này được gọi bởi nút "Fullscreen" (bên trong pop-up)
    public void OnFullscreenSelected()
    {
        Screen.fullScreen = true; // Áp dụng cài đặt
        screenModeButtonText.text = "Fullscreen"; // Cập nhật chữ trên nút
        CloseAllPopups(); // Đóng pop-up
    }

    // 3. Hàm này được gọi bởi nút "Windowed" (bên trong pop-up)
    public void OnWindowedSelected()
    {
        Screen.fullScreen = false; // Áp dụng cài đặt
        screenModeButtonText.text = "Windowed"; // Cập nhật chữ trên nút
        CloseAllPopups(); // Đóng pop-up
    }

    // === CÁC HÀM MỚI CHO RESOLUTION ===
    // 1. Hàm này được gọi bởi nút "Resolution" (cái nút to)
    public void OnResolutionClicked()
    {
        CloseAllPopups(); // Đóng mọi thứ khác trước
        popupBlocker.SetActive(true); // Bật Blocker
        resolutionPopup.SetActive(true); // Bật Pop-up
    }

    // 2. Hàm này được gọi bởi TẤT CẢ các nút bên trong pop-up
    // Chúng ta sẽ dùng một hàm duy nhất để xử lý
    public void OnResolutionSelected(TextMeshProUGUI selectedText)
    {
        // Lấy chuỗi text từ nút được nhấn (ví dụ: "1920x1080")
        string resString = selectedText.text;
        
        // Cập nhật chữ trên nút chính
        resolutionButtonText.text = resString;

        // Tách chuỗi (ví dụ: "1920x1080" -> width = 1920, height = 1080)
        string[] res = resString.Split('x');
        if (res.Length == 2)
        {
            int width = int.Parse(res[0]);
            int height = int.Parse(res[1]);
            
            // Áp dụng cài đặt độ phân giải
            Screen.SetResolution(width, height, Screen.fullScreen);
            Debug.Log($"Resolution set to {width}x{height}");
        }

        CloseAllPopups(); // Đóng pop-up
    }

    // === CÁC HÀM MỚI CHO QUALITIES ===
    // 1. Hàm này được gọi bởi nút "Qualities" (cái nút to)
    public void OnQualitiesClicked()
    {
        CloseAllPopups(); // Đóng mọi thứ khác trước
        popupBlocker.SetActive(true); // Bật Blocker
        qualitiesPopup.SetActive(true); // Bật Pop-up
    }

    // 2. Hàm này được gọi bởi CÁC nút bên trong pop-up
    public void OnQualitySelected(TextMeshProUGUI selectedText)
    {
        // Lấy chuỗi text từ nút được nhấn (ví dụ: "High")
        string qualityString = selectedText.text;
        
        // Cập nhật chữ trên nút chính
        qualitiesButtonText.text = qualityString;

        // === Áp dụng cài đặt ===
        // (Chúng ta sẽ dùng hàm có sẵn của Unity)
        
        // QualitySettings.names trả về một mảng: ["Low", "Medium", "High"...]
        // Chúng ta sẽ tìm index (số thứ tự) của chữ mà bạn nhấn
        int qualityIndex = System.Array.IndexOf(QualitySettings.names, qualityString);

        if (qualityIndex != -1) // Nếu tìm thấy
        {
            // Áp dụng cài đặt chất lượng
            QualitySettings.SetQualityLevel(qualityIndex, true);
            Debug.Log($"Quality set to: {qualityString} (Index: {qualityIndex})");
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy Quality Level tên là: {qualityString}");
        }

        CloseAllPopups(); // Đóng pop-up
    }

    // === HÀM MỚI ĐỂ ĐÓNG TẤT CẢ POPUP ===
    public void CloseAllPopups()
    {
        // Tắt Blocker và tất cả các pop-up
        popupBlocker.SetActive(false);
        screenModePopup.SetActive(false);
        resolutionPopup.SetActive(false);
        qualitiesPopup.SetActive(false);
        languagePopup.SetActive(false);
    }

    // === HÀM MỚI ĐỂ CHUỘT HOVER ===
    // Hàm này được gọi khi chuột BẮT ĐẦU di vào một Tab
    public void OnTabHover(int tabIndex)
    {
        // Hàm này chỉ có 1 tham số (int), vì vậy Event Trigger có thể thấy nó.
        // Nó sẽ gọi hàm logic chính của chúng ta.
        SelectTab(tabIndex, false);
    }

    // === HÀM MỚI CHO NÚT BẬT/TẮT SUBTITLES ===
    public void OnSubtitlesToggleClicked()
    {
        // Đảo ngược trạng thái (từ Tắt -> Bật, hoặc Bật -> Tắt)
        areSubtitlesOn = !areSubtitlesOn;

        // Cập nhật hình ảnh (Fill) và chữ (Text)
        UpdateSubtitlesVisuals();

        // (Sau này bạn sẽ dùng 'areSubtitlesOn' để áp dụng cài đặt)
        Debug.Log("Subtitles set to: " + areSubtitlesOn);
    }

    // === HÀM MỚI CHO NÚT BẬT/TẮT VIBRATION ===
    public void OnVibrationToggleClicked()
    {
        // Đảo ngược trạng thái
        isVibrationOn = !isVibrationOn;

        // Cập nhật hình ảnh
        UpdateVibrationVisuals();
    }

    // === HÀM MỚI CHO NÚT BẬT/TẮT TUTORIALS ===
    public void OnTutorialsToggleClicked()
    {
        // Đảo ngược trạng thái
        areTutorialsOn = !areTutorialsOn;

        // Cập nhật hình ảnh
        UpdateTutorialsVisuals();
    }

    // === HÀM MỚI ĐỂ CẬP NHẬT HÌNH ẢNH TOGGLE ===
    private void UpdateSubtitlesVisuals()
    {
        // Cập nhật chấm đặc (Fill)
        if (subtitlesFill != null)
        {
            subtitlesFill.SetActive(areSubtitlesOn);
        }
        
        // Cập nhật chữ (Text)
        if (subtitlesStatusText != null)
        {
            if (areSubtitlesOn)
            {
                subtitlesStatusText.text = "On";
            }
            else
            {
                subtitlesStatusText.text = "Off";
            }
        }
        
        Debug.Log("Subtitles set to: " + areSubtitlesOn);
    }

    // === HÀM MỚI ĐỂ CẬP NHẬT HÌNH ẢNH VIBRATION ===
    private void UpdateVibrationVisuals()
    {
        // Cập nhật chấm đặc (Fill)
        if (vibrationFill != null)
        {
            vibrationFill.SetActive(isVibrationOn);
        }
        
        // Cập nhật chữ (Text)
        if (vibrationStatusText != null)
        {
            if (isVibrationOn)
            {
                vibrationStatusText.text = "On";
            }
            else
            {
                vibrationStatusText.text = "Off";
            }
        }
        
        Debug.Log("Vibration set to: " + isVibrationOn);
    }

    // === HÀM MỚI ĐỂ CẬP NHẬT HÌNH ẢNH TUTORIALS ===
    private void UpdateTutorialsVisuals()
    {
        // Cập nhật chấm đặc (Fill)
        if (tutorialsFill != null)
        {
            tutorialsFill.SetActive(areTutorialsOn);
        }
        
        // Cập nhật chữ (Text)
        if (tutorialsStatusText != null)
        {
            if (areTutorialsOn)
            {
                tutorialsStatusText.text = "On";
            }
            else
            {
                tutorialsStatusText.text = "Off";
            }
        }
        
        Debug.Log("Tutorials set to: " + areTutorialsOn);
    }

    // === CÁC HÀM MỚI CHO LANGUAGE ===
    // 1. Hàm này được gọi bởi nút "Language" (cái nút to)
    public void OnLanguageClicked()
    {
        CloseAllPopups(); // Đóng mọi thứ khác trước
        popupBlocker.SetActive(true); // Bật Blocker
        languagePopup.SetActive(true); // Bật Pop-up
    }

    // 2. Hàm này được gọi bởi CÁC nút bên trong pop-up
    public void OnLanguageSelected(TextMeshProUGUI selectedText)
    {
        // Lấy chuỗi text từ nút được nhấn (ví dụ: "English")
        string langString = selectedText.text;
        
        // Cập nhật chữ trên nút chính
        languageButtonText.text = langString;

        // (Sau này bạn sẽ dùng 'langString' để gọi hệ thống Localization)
        Debug.Log($"Language set to: {langString}");

        // Đóng pop-up
        CloseAllPopups();
    }

    // === HÀM NHẤP NHÁY ===
    private void HandleTabBlinking()
    {
        if (tabTexts.Count == 0) return;
        float alpha = Mathf.Lerp(minBlinkAlpha, 1.0f, (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
        Color newColor = selectedTextColor;
        newColor.a = alpha;
        if (currentTabIndex < tabTexts.Count)
        {
            tabTexts[currentTabIndex].color = newColor;
        }
    }

    // [MỚI] Hàm này chạy mỗi khi bảng Setting được bật lên
    void OnEnable()
    {
        if (returnToMainButton != null)
        {
            // Nếu đang là Pause Menu -> True -> Hiện nút
            // Nếu đang là Main Menu -> False -> Ẩn nút
            returnToMainButton.SetActive(isPauseMenuMode);
        }
    }

    // [MỚI] Hàm gán cho nút Return To Main Menu
    public void OnReturnToMainMenuClicked()
    {
        Debug.Log("Thoát trận, về Main Menu...");
        
        // 1. Trả lại tốc độ thời gian (Quan trọng vì game đang pause)
        Time.timeScale = 1f; 

        // 2. Báo cho Main Menu biết là: "Tao về từ game đấy, đừng chạy intro dông dài"
        // (Nếu bạn muốn chạy intro thì để false)
        MainMenuController.shouldSkipIntro = false; 

        // 3. GỌI SCRIPT LEVEL LOADER CÓ SẴN CỦA BẠN
        // Hàm này sẽ tự lưu tên scene và chuyển sang "LoadingScene"
        LevelLoader.LoadLevel(mainMenuSceneName);
    }
}