using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ConsumableManager : MonoBehaviour
{
    // [ĐỔI TÊN] HealthPotion -> BloodPotion
    public enum ItemType { BloodPotion, ManaPotion }

    [System.Serializable]
    public class ConsumableItem
    {
        public string itemName;
        public ItemType type;

        // [THAY ĐỔI Ở ĐÂY] Thay vì 1 icon, ta cần 3 icon cho các trạng thái
        [Header("Icons States")]
        public Sprite iconFull;  // Khi số lượng > 1
        public Sprite iconHalf;  // Khi số lượng == 1
        public Sprite iconEmpty; // Khi số lượng == 0

        [Header("Item Data")]
        public int amount;       // Số lượng đang có
        public float value;      // Giá trị hồi
    }

    [Header("UI References")]
    public Image itemIconImage;
    public TextMeshProUGUI countText;

    [Header("Inventory Data")]
    public List<ConsumableItem> inventory = new List<ConsumableItem>();

    private int currentIndex = 0;
    private PlayerHealth playerHealth;
    private PlayerController playerController;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerController = player.GetComponent<PlayerController>();
        }

        // --- [MỚI] KHÔNG UPDATE UI NGAY LẬP TỨC ---
        // Chúng ta sẽ đợi PlayerController ra lệnh Load xong mới Update
        UpdateUI(); //<--- Xóa hoặc comment dòng này đi cũng được, nhưng để cũng không sao vì sẽ bị ghi đè.
    }

    // ====================================================
    // [MỚI] HỆ THỐNG LƯU TRỮ (PERSISTENCE)
    // ====================================================

    // 1. Lưu số lượng thuốc hiện tại vào PlayerPrefs
    public void SaveInventoryState()
    {
        foreach (var item in inventory)
        {
            // Lưu theo tên: "Inv_BloodPotion", "Inv_ManaPotion"
            string key = "Inv_" + item.itemName; 
            PlayerPrefs.SetInt(key, item.amount);
        }
        PlayerPrefs.Save();
        Debug.Log("Đã lưu kho đồ!");
    }

    // 2. Load lại số lượng thuốc từ PlayerPrefs
    public void LoadInventoryState()
    {
        foreach (var item in inventory)
        {
            string key = "Inv_" + item.itemName;
            // Chỉ load nếu có dữ liệu đã lưu
            if (PlayerPrefs.HasKey(key))
            {
                item.amount = PlayerPrefs.GetInt(key);
            }
        }
        // Load xong thì cập nhật UI
        UpdateUI();
        Debug.Log("Đã khôi phục kho đồ từ màn trước!");
    }

    public void SwitchItem()
    {
        if (inventory.Count == 0) return;
        currentIndex++;
        if (currentIndex >= inventory.Count) currentIndex = 0;
        UpdateUI();
    }

    // Hàm dùng đồ (Sẽ gọi khi bấm E)
    public void UseCurrentItem()
    {
        if (inventory.Count == 0) return;

        ConsumableItem currentItem = inventory[currentIndex];

        // 1. Kiểm tra số lượng (Hết thì nghỉ)
        if (currentItem.amount <= 0) 
        {
            Debug.Log("Hết " + currentItem.itemName + " rồi!");
            // Vẫn gọi UpdateUI để đảm bảo nó hiện icon rỗng nếu chưa hiện
            UpdateUI();
            return; 
        }

        // 2. DÙNG LUÔN (Bất kể đầy hay vơi)
        if (currentItem.type == ItemType.BloodPotion)
        {
            if (playerHealth != null)
            {
                playerHealth.Heal(currentItem.value); // Hồi Blood
            }
        }
        else if (currentItem.type == ItemType.ManaPotion)
        {
            if (playerController != null) 
            {
                playerController.RestoreMana(currentItem.value); // Hồi Mana
            }
        }

        // 3. LUÔN LUÔN TRỪ SỐ LƯỢNG
        currentItem.amount--;
        
        // Cập nhật lại UI (số lượng và icon mới)
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (inventory.Count > 0)
        {
            ConsumableItem item = inventory[currentIndex];
            
            // [LOGIC MỚI Ở ĐÂY] Chọn icon dựa trên số lượng
            Sprite spriteToShow = null;

            if (item.amount > 1)
            {
                spriteToShow = item.iconFull;
            }
            else if (item.amount == 1)
            {
                spriteToShow = item.iconHalf;
            }
            else // item.amount <= 0
            {
                spriteToShow = item.iconEmpty;
            }

            // Cập nhật ảnh
            if (itemIconImage != null) 
            {
                itemIconImage.sprite = spriteToShow;
                // Nếu có ảnh thì hiện rõ, không có thì ẩn đi (đề phòng lỗi chưa kéo ảnh)
                itemIconImage.color = (spriteToShow != null) ? Color.white : Color.clear;
            }

            // Cập nhật số lượng text
            if (countText != null)
            {
                 // Nếu còn 0 thì có thể ẩn số 0 đi cho đẹp, hoặc để nguyên tùy bạn.
                 // Ở đây tôi để hiển thị số 0 luôn.
                countText.text = item.amount.ToString();
            }
        }
        else
        {
            // Nếu túi rỗng
            if (itemIconImage != null) itemIconImage.color = Color.clear; // Ẩn ảnh
            if (countText != null) countText.text = "";
        }
    }

    // Hàm làm đầy lại tất cả các bình (Gọi khi nghỉ ở Checkpoint)
    public void RefillAllPotions()
    {
        foreach (var item in inventory)
        {
            // Ví dụ: Mặc định hồi lại 5 bình cho mỗi loại
            // (Sau này bạn có thể nâng cấp biến 'maxAmount' riêng cho từng món)
            if (item.type == ItemType.BloodPotion) item.amount = 5; 
            else if (item.type == ItemType.ManaPotion) item.amount = 3;
        }
        UpdateUI();
        Debug.Log("Đã nạp đầy bình thuốc!");
    }
}