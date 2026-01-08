using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // --- [MỚI] BIẾN STATIC ĐỂ LƯU TRẠNG THÁI GIỮA CÁC SCENE ---
    // Static giúp biến này sống sót kể cả khi load lại scene
    public static bool shouldSkipIntro = false;

    // === CÁC BIẾN KÉO THẢ MỚI ===
    [Header("Các Nhóm UI")]
    [Tooltip("Kéo Image của Title_Border vào đây")]
    [SerializeField] private Image titleBorder;
    [Tooltip("Kéo TextMeshProUGUI của Title_Text vào đây")]
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Tooltip("Kéo CanvasGroup của PressButton_Group vào đây")]
    [SerializeField] private CanvasGroup pressButtonTextGroup;
    
    [Tooltip("Kéo CanvasGroup của MainMenu_Group vào đây")]
    [SerializeField] private CanvasGroup mainMenuGroup;

    [Header("Tốc Độ Fade")]
    [SerializeField] private float titleFadeInSpeed = 1.0f;
    [SerializeField] private float promptFadeInSpeed = 1.0f;
    [SerializeField] private float menuCrossFadeSpeed = 2.0f;

    // --- Các biến Menu (giữ nguyên như cũ) ---
    [Header("Cấu hình Menu")]
    [SerializeField] private RectTransform selectorImage;
    [SerializeField] private List<RectTransform> menuItems;
    [SerializeField] private List<Image> menuButtonImages;
    [SerializeField] private List<TextMeshProUGUI> menuTexts;

    [Header("Màu Sắc & Hiệu Ứng")]
    public Color selectedColor;
    public Color defaultColor = Color.white;
    public float blinkSpeed = 3f;
    [Range(0f, 1f)]
    public float minBlinkAlpha = 0.5f;

    // === QUẢN LÝ TRẠNG THÁI ===
    private enum MenuState { FadingInTitle, AwaitingButtonPress, FadingInMenu, Active }
    private MenuState currentState = MenuState.FadingInTitle;
    private int currentSelectedIndex = 0;
    private bool inputPressed = false;

    // --- HÀM KHỞI ĐỘNG (START) MỚI ---
    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);

        // 1. KIỂM TRA FILE SAVE ĐỂ ẨN/HIỆN NÚT CONTINUE
        // Giả sử nút Continue luôn được kéo vào vị trí số 0 (đầu tiên) trong Inspector
        if (PlayerPrefs.GetInt("HasCheckpoint") == 0)
        {
            // Nếu CHƯA có file save -> Xóa nút Continue khỏi danh sách và ẩn nó đi
            if (menuItems.Count > 0 && menuItems[0].name.Contains("Continue"))
            {
                // Tắt hiển thị GameObject
                menuItems[0].gameObject.SetActive(false);

                // Loại bỏ khỏi danh sách quản lý để con trỏ không nhảy vào đó
                menuItems.RemoveAt(0);
                menuButtonImages.RemoveAt(0);
                menuTexts.RemoveAt(0);
            }
        }
        else
        {
            // Nếu CÓ file save -> Giữ nguyên nút Continue
            // Đảm bảo nó đang bật (đề phòng trường hợp tắt tay trong editor)
            if (menuItems.Count > 0) menuItems[0].gameObject.SetActive(true);
        }

        // --- [MỚI] KIỂM TRA XEM CÓ CẦN BỎ QUA INTRO KHÔNG ---
        if (shouldSkipIntro)
        {
            // TRƯỜNG HỢP 1: QUAY VỀ TỪ SETTING -> HIỆN MENU LUÔN
            
            // Hiện Title và Border ngay lập tức
            SetImageAlpha(titleBorder, 1f);
            SetTextAlpha(titleText, 1f);
            
            // Ẩn dòng "Press Any Button"
            pressButtonTextGroup.alpha = 0f;
            
            // Hiện Main Menu ngay lập tức
            mainMenuGroup.alpha = 1f;
            mainMenuGroup.interactable = true;
            mainMenuGroup.blocksRaycasts = true;

            // Set trạng thái Active luôn
            currentState = MenuState.Active;
            
            // Reset lại selector để nó nằm đúng chỗ
            StartCoroutine(InitializeMenu());

            // Reset biến này về false để lần sau mở game lại chạy intro bình thường
            shouldSkipIntro = false; 
        }
        else
        {
            // TRƯỜNG HỢP 2: MỞ GAME LẦN ĐẦU -> CHẠY INTRO NHƯ CŨ
            SetImageAlpha(titleBorder, 0f);
            SetTextAlpha(titleText, 0f);
            pressButtonTextGroup.alpha = 0f;
            mainMenuGroup.alpha = 0f;
            mainMenuGroup.interactable = false;
            mainMenuGroup.blocksRaycasts = false;
            
            currentState = MenuState.FadingInTitle;
            StartCoroutine(RunTitleSequence());
        }
    }

    // --- HÀM UPDATE QUẢN LÝ CHUNG ---
    void Update()
    {
        // 1. Nếu đang chờ nhấn nút
        if (currentState == MenuState.AwaitingButtonPress)
        {
            // Bật hiệu ứng nhấp nháy cho chữ "Press Any Button" (cả nền và chữ)
            BlinkPressButtonText();
            
            // Nếu nhấn nút bất kỳ
            if (Input.anyKeyDown)
            {
                currentState = MenuState.FadingInMenu;
                // Bắt đầu "Cross-Fade" (vừa mờ ra, vừa mờ vào)
                StartCoroutine(FadeToMainMenu());
            }
        }
        
        // 2. Nếu menu đã hoạt động
        if (currentState == MenuState.Active)
        {
            HandleMenuInput(); 
            HandleMenuBlinking(); 
        }

        // Phím tắt xóa Save (Ví dụ: F5)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("ĐÃ XÓA TOÀN BỘ SAVE GAME!");
        }
    }

    // === CÁC COROUTINE ĐIỀU KHIỂN FADE ===
    
    // 1. Chuỗi "Fancy" Fade-in Tiêu Đề
    private IEnumerator RunTitleSequence()
    {
        yield return new WaitForSeconds(0.5f); // Chờ 0.5s
        
        // 1a. Fade in Nền Tiêu Đề (Title_Border)
        yield return StartCoroutine(FadeImage(titleBorder, titleFadeInSpeed, 1f));
        
        yield return new WaitForSeconds(0.25f); // Chờ 0.25s

        // 1b. Fade in Chữ Tiêu Đề (Title_Text)
        yield return StartCoroutine(FadeText(titleText, titleFadeInSpeed, 1f));
        
        yield return new WaitForSeconds(0.5f); // Chờ 0.5s
        
        // 1c. Fade in Nhóm "Press Any Button" (cả nền Fade_1 và chữ)
        yield return StartCoroutine(FadeCanvasGroup(pressButtonTextGroup, promptFadeInSpeed, 1f));

        // 1d. Đã xong, chuyển sang trạng thái chờ
        currentState = MenuState.AwaitingButtonPress;
    }

    // 2. Chuỗi "Cross-Fade" (Nhiều Coroutine chạy song song)
    private IEnumerator FadeToMainMenu()
    {
        // 2a. Fade OUT nhóm "Press Any Button"
        StartCoroutine(FadeCanvasGroup(pressButtonTextGroup, menuCrossFadeSpeed, 0f));
        
        // 2b. Fade IN nhóm "Main Menu"
        yield return StartCoroutine(FadeCanvasGroup(mainMenuGroup, menuCrossFadeSpeed, 1f));
        
        // 2c. Kích hoạt menu
        mainMenuGroup.interactable = true;
        mainMenuGroup.blocksRaycasts = true;
        currentState = MenuState.Active;
        
        // Cập nhật vị trí viền chọn (sửa lỗi lệch)
        StartCoroutine(InitializeMenu());
    }
    
    // === CÁC HÀM HỖ TRỢ FADE (Generic) ===
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float speed, float targetAlpha)
    {
        while (!Mathf.Approximately(cg.alpha, targetAlpha))
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    private IEnumerator FadeImage(Image img, float speed, float targetAlpha)
    {
        Color color = img.color;
        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(color.a, targetAlpha, speed * Time.deltaTime);
            img.color = color;
            yield return null;
        }
        color.a = targetAlpha;
        img.color = color;
    }

    private IEnumerator FadeText(TextMeshProUGUI txt, float speed, float targetAlpha)
    {
        Color color = txt.color;
        while (!Mathf.Approximately(color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(color.a, targetAlpha, speed * Time.deltaTime);
            txt.color = color;
            yield return null;
        }
        color.a = targetAlpha;
        txt.color = color;
    }

    // === CÁC HÀM TIỆN ÍCH SET ALPHA (dùng trong Start) ===
    private void SetImageAlpha(Image img, float alpha)
    {
        Color color = img.color;
        color.a = alpha;
        img.color = color;
    }
    
    private void SetTextAlpha(TextMeshProUGUI txt, float alpha)
    {
        Color color = txt.color;
        color.a = alpha;
        txt.color = color;
    }

    // === LOGIC MENU (LÊN/XUỐNG) - Y HỆT NHƯ CŨ, KHÔNG CẦN THAY ĐỔI ===
    
    // (Hàm này giờ nhấp nháy CẢ NHÓM: Nền Fade_1 và Chữ)
    void BlinkPressButtonText()
    {
        float alpha = Mathf.Lerp(minBlinkAlpha, 1.0f, (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
        pressButtonTextGroup.alpha = alpha;
    }

    IEnumerator InitializeMenu()
    {
        yield return new WaitForEndOfFrame(); 
        UpdateSelectionVisuals(); 
    }

    void HandleMenuInput()
    {
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool selectionChanged = false;

        if (verticalInput > 0.5f && !inputPressed) {
            currentSelectedIndex--; inputPressed = true; selectionChanged = true;
        } else if (verticalInput < -0.5f && !inputPressed) {
            currentSelectedIndex++; inputPressed = true; selectionChanged = true;
        } else if (verticalInput > -0.5f && verticalInput < 0.5f) {
            inputPressed = false;
        }

        if (currentSelectedIndex < 0) currentSelectedIndex = menuItems.Count - 1;
        if (currentSelectedIndex >= menuItems.Count) currentSelectedIndex = 0;

        if (selectionChanged) {
            UpdateSelectionVisuals();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) {
            SelectButton();
        }
    }

    void HandleMenuBlinking()
    {
        if (menuTexts.Count == 0) return;
        float alpha = Mathf.Lerp(minBlinkAlpha, 1.0f, (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
        Color newColor = selectedColor; 
        newColor.a = alpha; 
        menuTexts[currentSelectedIndex].color = newColor;
    }

    void UpdateSelectionVisuals()
    {
        if (selectorImage != null && menuItems.Count > 0) {
            selectorImage.position = new Vector3(
                menuItems[currentSelectedIndex].position.x,
                menuItems[currentSelectedIndex].position.y,
                selectorImage.position.z);
        }

        for (int i = 0; i < menuButtonImages.Count; i++) 
        {
            if (i == currentSelectedIndex) 
            {
                // [SAI] menuButtonImages[i].enabled = false; 
                // -> Lệnh này làm mất hitbox của nút!
                
                // [ĐÚNG] Chỉ làm nó trong suốt thôi
                menuButtonImages[i].color = Color.clear; // Màu trong suốt
                menuButtonImages[i].enabled = true;      // Vẫn BẬT để bắt click
            } 
            else 
            {
                menuButtonImages[i].enabled = true;
                menuButtonImages[i].color = Color.white; // Hoặc màu gốc của nút
                menuTexts[i].color = defaultColor;
            }
        }
    }
    // --- HÀM XỬ LÝ CHỌN NÚT (LOGIC MỚI) ---
    void SelectButton()
    {
        // Lấy tên của nút đang chọn để xử lý logic (An toàn hơn dùng index số)
        string selectedButtonName = menuItems[currentSelectedIndex].name;

        // 1. NÚT CONTINUE
        if (selectedButtonName.Contains("Continue"))
        {
            // Lấy tên scene đã lưu để load
            string savedScene = PlayerPrefs.GetString("SavedScene", "GameLevel");
            LevelLoader.LoadLevel(savedScene);
        }
        // 2. NÚT NEW GAME
        else if (selectedButtonName.Contains("NewGame")) // Đặt tên GameObject là Btn_NewGame nhé
        {
            // Xóa dữ liệu cũ để chơi lại từ đầu
            PlayerPrefs.DeleteAll(); 
            // Load màn 1
            LevelLoader.LoadLevel("GameLevel"); 
        }
        // 3. NÚT SETTINGS
        else if (selectedButtonName.Contains("Setting"))
        {
            SceneManager.LoadScene("SettingScreen");
        }
        // 4. NÚT CREDITS
        else if (selectedButtonName.Contains("Credit"))
        {
            Debug.Log("Mở Credits!");
        }
        // 5. NÚT QUIT
        else if (selectedButtonName.Contains("Quit") || selectedButtonName.Contains("Exit"))
        {
            Debug.Log("Thoát game!");
            Application.Quit();
        }
    }

    // === HÀM MỚI ĐỂ CHUỘT GỌI ===

    // === HÀM MỚI SỬA ĐỔI ===

    // Thay vì nhận số int (dễ bị sai), ta nhận thẳng cái GameObject của nút đó
    public void OnButtonHover(GameObject buttonObj)
    {
        // Duyệt qua danh sách hiện tại để tìm xem nút này đang đứng thứ mấy
        int foundIndex = -1;
        for (int i = 0; i < menuItems.Count; i++)
        {
            // So sánh xem GameObject trong list có khớp với cái nút vừa hover không
            // Lưu ý: menuItems là List<RectTransform>, nên ta so sánh .gameObject
            if (menuItems[i].gameObject == buttonObj)
            {
                foundIndex = i;
                break; // Tìm thấy rồi thì dừng lại
            }
        }

        // Nếu tìm thấy và nó khác nút đang chọn thì mới cập nhật
        if (foundIndex != -1 && currentSelectedIndex != foundIndex)
        {
            currentSelectedIndex = foundIndex;
            UpdateSelectionVisuals();
        }
    }

    // Hàm này được gọi khi CHUỘT CLICK
    // (Nó chỉ cần gọi hàm SelectButton mà chúng ta đã viết)
    public void OnButtonClick()
    {
        SelectButton();
    }
}