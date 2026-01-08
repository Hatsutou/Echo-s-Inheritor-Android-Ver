using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Cài đặt")]
    public float currentMoney = 0; 
    public TextMeshProUGUI moneyText; 

    [Header("Hiệu ứng")]
    public string currencySuffix = " $";

    [Header("Death Marker")]
    // KÉO PREFAB DeathMarker VÀO ĐÂY TRONG INSPECTOR
    public GameObject deathMarkerPrefab; 

    private const string MONEY_KEY = "PlayerMoney";
    // Sử dụng lại các key bên DeathMarkerController cho đồng bộ
    private const string KEY_HAS_MARKER = "HasDeathMarker";
    private const string KEY_LOST_AMOUNT = "LostCurrencyAmount";
    private const string KEY_MARKER_X = "DeathPosX";
    private const string KEY_MARKER_Y = "DeathPosY";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. Tải số tiền hiện tại
        currentMoney = PlayerPrefs.GetFloat(MONEY_KEY, 0);
        UpdateUI();

        // 2. [MỚI] KIỂM TRA VÀ SINH RA BIA MỘ (Nếu có)
        SpawnDeathMarker();
    }

    // [MỚI] Hàm sinh ra bia mộ khi load màn chơi
    void SpawnDeathMarker()
    {
        // Kiểm tra xem trong bộ nhớ có báo là đang có bia mộ không (1 là có)
        if (PlayerPrefs.GetInt(KEY_HAS_MARKER, 0) == 1)
        {
            if (deathMarkerPrefab != null)
            {
                // Lấy tọa độ đã lưu
                float x = PlayerPrefs.GetFloat(KEY_MARKER_X);
                float y = PlayerPrefs.GetFloat(KEY_MARKER_Y);
                Vector3 markerPos = new Vector3(x, y, 0);

                // Tạo bia mộ tại vị trí đó
                Instantiate(deathMarkerPrefab, markerPos, Quaternion.identity);
                Debug.Log("Đã sinh ra bia mộ tại: " + markerPos);
            }
            else
            {
                Debug.LogWarning("Chưa gán Prefab DeathMarker vào CurrencyManager!");
            }
        }
    }


    // Hàm XỬ LÝ KHI CHẾT (Gọi từ PlayerHealth)
    // [ĐÃ SỬA] Hàm XỬ LÝ KHI CHẾT
    public void ProcessDeathPenalty(Vector3 deathPosition)
    {
        // 1. LUÔN LUÔN TẠO BIA MỘ MỚI (Ghi đè lên bia mộ cũ)
        // Không cần kiểm tra if (currentMoney > 0) nữa.
        // Dù là 0 đồng thì vẫn lưu lại vị trí và số tiền (0) vào bia mộ.
        
        PlayerPrefs.SetInt(KEY_HAS_MARKER, 1); // Đánh dấu là CÓ bia mộ
        PlayerPrefs.SetFloat(KEY_LOST_AMOUNT, currentMoney); // Lưu số tiền (kể cả 0)
        PlayerPrefs.SetFloat(KEY_MARKER_X, deathPosition.x);
        PlayerPrefs.SetFloat(KEY_MARKER_Y, deathPosition.y);
        
        Debug.Log($"Đã tạo bia mộ tại {deathPosition}. Chứa: {currentMoney}$. Bia mộ cũ (nếu có) đã bị thay thế.");

        // 2. Reset tiền hiện tại về 0
        currentMoney = 0;
        SaveMoney(); // Lưu số 0 vào ví hiện tại
        
        // 3. Lưu tất cả xuống ổ cứng
        PlayerPrefs.Save(); 
    }

    // Hàm nhận tiền
    public void AddMoney(float amount)
    {
        currentMoney += amount;
        
        // [MỚI] Lưu ngay lập tức
        SaveMoney();
        
        UpdateUI();
    }

    // Hàm tiêu tiền
    public bool SpendMoney(float amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            
            // [MỚI] Lưu ngay lập tức sau khi tiêu
            SaveMoney();
            
            UpdateUI();
            return true;
        }
        else
        {
            Debug.Log("Không đủ tiền!");
            return false;
        }
    }

    // Hàm phụ để lưu dữ liệu xuống ổ cứng
    void SaveMoney()
    {
        PlayerPrefs.SetFloat(MONEY_KEY, currentMoney);
        PlayerPrefs.Save();
        Debug.Log("Đã lưu tiền: " + currentMoney);
    }

    void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = Mathf.FloorToInt(currentMoney).ToString() + currencySuffix;
        }
    }
    
    // (Tùy chọn) Hàm Reset tiền để test (gọi khi bấm New Game nếu muốn)
    public void ResetMoney()
    {
        currentMoney = 0;
        SaveMoney();
        UpdateUI();
    }
}