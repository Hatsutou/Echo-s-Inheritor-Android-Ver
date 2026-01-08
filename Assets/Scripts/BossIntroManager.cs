using UnityEngine;
using System.Collections;
using Unity.Cinemachine; // [BẮT BUỘC] Thêm dòng này

public class BossIntroManager : MonoBehaviour
{
    [Header("Diễn Viên")]
    public PlayerController player;
    public BossMalphas boss;
    public GameObject bossObject; 

    [Header("Cinemachine Cameras")]
    // Camera đang bám theo Player (Gameplay)
    public CinemachineCamera playerCamera; 
    
    // Camera quay cảnh Boss (Cutscene)
    public CinemachineCamera introCamera; 

    [Header("UI")]
    public GameObject playerHUD;
    // [THÊM BIẾN NÀY] Để nắm đầu thanh máu Boss
    public GameObject bossHUDObject;

    [Header("Cấu hình Thời Gian")]
    public float waitBeforeBossAppears = 1.5f; // Thời gian cam bay đến nơi rồi chờ
    public float bossIntroDuration = 4.0f; // Thời gian Boss diễn
    public float cameraReturnDelay = 0.5f; // Chờ xíu rồi mới trả cam về

    [Header("Lưu Trữ")]
    // Đặt một cái tên duy nhất cho Boss này. Ví dụ: "Malphas_Intro"
    public string introSaveKey = "Malphas_Intro_Seen";

    public void StartBossIntro()
    {
        // 1. KIỂM TRA ĐÃ XEM CHƯA?
        // 1 = Đã xem, 0 = Chưa xem
        if (PlayerPrefs.GetInt(introSaveKey, 0) == 1)
        {
            Debug.Log("Đã xem Intro rồi -> Bỏ qua!");
            SkipIntro(); // Gọi hàm bỏ qua
            return;
        }

        // 2. NẾU CHƯA XEM -> LƯU LẠI VÀ DIỄN
        PlayerPrefs.SetInt(introSaveKey, 1);
        PlayerPrefs.Save();

        StartCoroutine(IntroSequence());
    }

    // Hàm mới: Xử lý việc bỏ qua Intro
    void SkipIntro()
    {
        // 1. Bật Boss lên ngay lập tức
        if (bossObject != null)
        {
            bossObject.SetActive(true);
            
            if (boss != null)
            {
                // Đảm bảo Boss KHÔNG ở trạng thái chờ diễn
                boss.isWaitingForIntro = false; 
                
                // Quay mặt về phía Player cho lịch sự
                boss.FacePlayerImmediate();
            }
        }

        // 2. Đảm bảo HUD phải hiện ra (Vì IntroSequence thường tắt nó đi, giờ ta skip thì ta phải tự bật)
        if (playerHUD != null) playerHUD.SetActive(true);
        if (bossHUDObject != null) bossHUDObject.SetActive(true);

        // 3. Đảm bảo Camera Intro tắt (để dùng Camera Player)
        if (introCamera != null) 
        {
            introCamera.Priority = 0;
            introCamera.gameObject.SetActive(false);
        }

        // 4. Hủy trigger/script này đi để đỡ vướng
        this.enabled = false;
        // Hoặc tắt cả GameObject cha nếu cần
        // gameObject.SetActive(false); 
    }

    IEnumerator IntroSequence()
    {
        // 1. CHUẨN BỊ
        // Khóa Player
        if (player != null) player.EnterRestState();
        
        // Ẩn HUD
        if (playerHUD != null) playerHUD.SetActive(false);

        // Đảm bảo Boss chưa hiện (nếu muốn hiệu ứng bất ngờ)
        if (bossObject != null) bossObject.SetActive(false);
        // [THÊM DÒNG NÀY] Tắt ngay HUD Boss (để lúc lia cam không thấy nó)
        if (bossHUDObject != null) bossHUDObject.SetActive(false);

        // 2. BẮT ĐẦU LIA MÁY (ACTION!)
        // Trong Cinemachine, Cam nào Priority cao hơn thì sẽ được chọn.
        // Giả sử PlayerCam priority = 10. Ta set IntroCam = 20.
        if (introCamera != null)
        {
            introCamera.Priority = 20; 
            introCamera.gameObject.SetActive(true);
        }

        // Chờ camera bay đến nơi (Thời gian bay chỉnh trong CinemachineBrain trên Main Camera)
        // Mình chờ dư ra một chút (ví dụ 2s) để cam ổn định
        yield return new WaitForSeconds(2.0f); 

        // 3. BOSS XUẤT HIỆN
        yield return new WaitForSeconds(waitBeforeBossAppears);

        if (bossObject != null)
        {
            // [SỬA QUAN TRỌNG] Đảo lệnh SetActive xuống dưới cùng của cụm này
            if (boss != null)
            {
                // 1. Dặn Boss trước: "Mày sắp diễn Intro đấy nhé, cấm bật thanh máu!"
                boss.isWaitingForIntro = true; 
            }

            // 2. Bây giờ mới bật Boss lên
            // Lúc này hàm Start() của Boss chạy -> Nó thấy isWaitingForIntro = true -> Nó sẽ ẩn HUD đi
            bossObject.SetActive(true);
            
            if (boss != null)
            {
                boss.FacePlayerImmediate(); 
                boss.PlayIntroAnimation();
            }
        }

        // 4. NGẮM BOSS DIỄN
        yield return new WaitForSeconds(bossIntroDuration);

        // 5. TRẢ CAMERA VỀ
        // Hạ Priority của IntroCam xuống thấp hơn PlayerCam (hoặc tắt đi)
        if (introCamera != null)
        {
            introCamera.Priority = 0; 
            introCamera.gameObject.SetActive(false);
        }

        // Chờ camera bay ngược về Player
        yield return new WaitForSeconds(cameraReturnDelay);

        // 6. VÀO TRẬN (FIGHT START)
        if (playerHUD != null) playerHUD.SetActive(true);
        if (player != null) player.ExitRestState();
        
        if (boss != null)
        {
            boss.StartCombat();
        }
    }
}