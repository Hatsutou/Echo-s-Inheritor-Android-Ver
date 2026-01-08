using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Thanh chính (Màu Đỏ/Xanh)")]
    public Image bloodFill;
    public Image manaFill;
    public Image staminaFill;

    [Header("Thanh hiệu ứng (Màu Vàng)")]
    public Image bloodYellow; 
    public Image manaYellow;
    public Image staminaYellow;

    [Header("Cài đặt hiệu ứng")]
    public float effectSpeed = 2f; // Tốc độ tụt của thanh vàng
    public float effectDelay = 0.5f; // Thời gian chờ trước khi tụt

    // Timer riêng cho từng thanh
    private float bloodTimer;
    private float manaTimer;
    private float staminaTimer;

    void Update()
    {
        // 1. XỬ LÝ THANH MÁU (BLOOD)
        if (bloodYellow != null && bloodFill != null)
        {
            // Nếu thanh vàng ĐANG LỚN HƠN thanh đỏ (Mất máu)
            if (bloodYellow.fillAmount > bloodFill.fillAmount)
            {
                bloodTimer += Time.deltaTime;
                if (bloodTimer > effectDelay)
                {
                    // Tụt dần dần xuống
                    bloodYellow.fillAmount = Mathf.Lerp(bloodYellow.fillAmount, bloodFill.fillAmount, effectSpeed * Time.deltaTime);
                }
            }
            // Nếu thanh vàng NHỎ HƠN thanh đỏ (Hồi máu)
            else
            {
                // Tăng thanh vàng lên ngay lập tức
                bloodYellow.fillAmount = bloodFill.fillAmount;
                bloodTimer = 0; // Reset timer
            }
        }

        // 2. XỬ LÝ THANH MANA
        if (manaYellow != null && manaFill != null)
        {
            if (manaYellow.fillAmount > manaFill.fillAmount)
            {
                manaTimer += Time.deltaTime;
                if (manaTimer > effectDelay)
                {
                    manaYellow.fillAmount = Mathf.Lerp(manaYellow.fillAmount, manaFill.fillAmount, effectSpeed * Time.deltaTime);
                }
            }
            else
            {
                manaYellow.fillAmount = manaFill.fillAmount;
                manaTimer = 0;
            }
        }

        // 3. XỬ LÝ THANH STAMINA (Đặc biệt)
        if (staminaYellow != null && staminaFill != null)
        {
            // Logic tương tự: Vàng > Xanh thì chờ rồi tụt
            if (staminaYellow.fillAmount > staminaFill.fillAmount)
            {
                staminaTimer += Time.deltaTime;
                if (staminaTimer > effectDelay)
                {
                    staminaYellow.fillAmount = Mathf.Lerp(staminaYellow.fillAmount, staminaFill.fillAmount, effectSpeed * Time.deltaTime);
                }
            }
            else
            {
                // Hồi phục: Kéo thanh vàng lên ngay
                staminaYellow.fillAmount = staminaFill.fillAmount;
                // [QUAN TRỌNG] Không reset timer ở đây để tránh bị giật khi vừa hồi vừa mất
                // staminaTimer = 0; 
            }
        }
    }

    // ================= CÁC HÀM CẬP NHẬT (GỌI TỪ CONTROLLER) =================

    // [SỬA] Thêm biến 'immediate' (mặc định là false để không ảnh hưởng code cũ)
    public void UpdateBlood(float current, float max, bool immediate = false)
    {
        float ratio = current / max;
        if (bloodFill != null) bloodFill.fillAmount = ratio;

        // [MỚI] Nếu yêu cầu ngay lập tức -> Gán luôn thanh vàng bằng thanh đỏ
        if (immediate && bloodYellow != null)
        {
            bloodYellow.fillAmount = ratio;
        }

        bloodTimer = 0; 
    }

    // [SỬA] Tương tự với Mana
    public void UpdateMana(float current, float max, bool immediate = false)
    {
        float ratio = current / max;
        if (manaFill != null) manaFill.fillAmount = ratio;

        // [MỚI] Snap ngay lập tức
        if (immediate && manaYellow != null)
        {
            manaYellow.fillAmount = ratio;
        }

        manaTimer = 0;
    }

    // [SỬA] Stamina có sẵn 'isGradual', ta thêm 'immediate' vào cuối
    public void UpdateStamina(float current, float max, bool isGradual = false, bool immediate = false)
    {
        float ratio = current / max;
        if (staminaFill != null) staminaFill.fillAmount = ratio;

        // [MỚI] Ưu tiên cao nhất: Snap ngay lập tức
        if (immediate && staminaYellow != null)
        {
            staminaYellow.fillAmount = ratio;
            return; // Xong việc, thoát luôn
        }

        // XỬ LÝ LOGIC CŨ
        if (isGradual)
        {
            if (staminaYellow != null) staminaYellow.fillAmount = staminaFill.fillAmount;
        }
        else
        {
            staminaTimer = 0;
        }
    }
}