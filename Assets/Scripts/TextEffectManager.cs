using UnityEngine;
using TMPro;
using UnityEngine.UI; // Cần thêm cái này để dùng Image
using System.Collections;
using System.Collections.Generic;

public class TextEffectManager : MonoBehaviour
{
    public TextMeshProUGUI sourceText;

    [Header("VFX Resources")]
    public Sprite shardSprite; // Kéo cái ảnh vuông trắng 10x10 vào đây

    [Header("Shatter Settings (Vụn Vỡ)")]
    public int shardsPerChar = 30; // Số mảnh vỡ trên mỗi chữ cái (càng cao càng mịn nhưng nặng máy)
    public Vector2 shardSizeRange = new Vector2(3f, 8f); // Kích thước ngẫu nhiên của mảnh vỡ
    public float shatterDuration = 2.0f; // Thời gian bay của mảnh vỡ

    // --- [MỚI] THÊM PHẦN NÀY ĐỂ CHỈNH TỐC ĐỘ ---
    [Header("Physics Settings (Chỉnh tốc độ rơi)")]
    public float gravity = 400f; // Mặc định cũ là 1500 (quá nhanh). Hãy thử 300-500.
    public float initialExplosionForce = 20f; // Độ nảy ban đầu nhẹ nhàng
    // ------------------------------------------

    [Header("Assemble Settings (Hội Tụ)")]
    public float assembleDuration = 2.5f; // Thời gian bay lên lâu hơn chút cho đẹp

    // --- HÀM 1: ĐẬP VỠ & RƠI RỤNG (CRUMBLE EFFECT) ---
    public IEnumerator ShatterEffect()
    {
        string originalString = sourceText.text;
        
        // Lấy màu gốc
        TMP_TextInfo textInfo = sourceText.textInfo;
        Color baseColor = sourceText.color;
        if (sourceText.enableVertexGradient) baseColor = sourceText.colorGradient.topLeft;

        sourceText.text = ""; 

        List<GameObject> shards = new List<GameObject>();
        List<Vector3> velocities = new List<Vector3>(); 

        float charSpacing = sourceText.fontSize * 0.6f;

        for (int i = 0; i < originalString.Length; i++)
        {
            if (originalString[i] == ' ') continue;

            float charCenterX = (i * charSpacing) - (originalString.Length * charSpacing / 2);
            Vector3 charCenterPos = new Vector3(charCenterX, 0, 0);

            for (int k = 0; k < shardsPerChar; k++)
            {
                GameObject shardObj = new GameObject("Shard");
                shardObj.transform.SetParent(sourceText.transform.parent, false);

                Image img = shardObj.AddComponent<Image>();
                img.sprite = shardSprite;
                img.color = baseColor; 

                float size = Random.Range(shardSizeRange.x, shardSizeRange.y);
                shardObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

                Vector3 randomOffset = new Vector3(Random.Range(-10f, 10f), Random.Range(-20f, 20f), 0);
                shardObj.transform.localPosition = sourceText.transform.localPosition + charCenterPos + randomOffset;
                shardObj.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

                shards.Add(shardObj);

                // [SỬA] Dùng biến initialExplosionForce để chỉnh độ nảy
                // Random Y từ -Force đến +Force (Rơi xuống hoặc nảy nhẹ lên)
                Vector3 velocity = new Vector3(
                    Random.Range(-initialExplosionForce, initialExplosionForce), 
                    Random.Range(-initialExplosionForce * 2, initialExplosionForce), // Thiên về rơi xuống nhiều hơn
                    0
                );
                velocities.Add(velocity);
            }
        }

        // VÒNG LẶP RƠI
        float timer = 0;
        
        while (timer < shatterDuration)
        {
            float dt = Time.deltaTime;
            timer += dt;

            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i] == null) continue;

                // [SỬA] Dùng biến gravity công khai
                Vector3 currentVel = velocities[i];
                currentVel.y -= gravity * dt; // Trừ đi trọng lực tùy chỉnh
                velocities[i] = currentVel; 

                shards[i].transform.localPosition += currentVel * dt;
                shards[i].transform.Rotate(0, 0, Random.Range(-90f, 90f) * dt);

                if (timer > shatterDuration * 0.6f) // Bắt đầu mờ muộn hơn chút
                {
                    Image img = shards[i].GetComponent<Image>();
                    Color c = img.color;
                    c.a = Mathf.Lerp(1, 0, (timer - shatterDuration * 0.6f) / (shatterDuration * 0.4f));
                    img.color = c;
                }
            }
            yield return null;
        }

        foreach (var shard in shards) Destroy(shard);
    }
    // --- HÀM 2: HỘI TỤ & TÁI SINH (REVERSE CRUMBLE) ---
    public IEnumerator AssembleEffect(string newName)
    {
        sourceText.text = ""; // Đảm bảo ẩn chữ thật đi
        List<GameObject> shards = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        // 1. TÍNH TOÁN VỊ TRÍ ĐÍCH CHO CÁC MẢNH VỠ
        // (Chúng ta sẽ giả lập vị trí các chữ cái mới)
        float charSpacing = sourceText.fontSize * 0.6f;
        
        // Màu sắc: Tạm thời để trắng để HUD tự áp Gradient sau, 
        // hoặc bạn có thể để màu đỏ nhạt (Color.red) nếu muốn các mảnh vỡ có màu máu ngay.
        Color shardColor = Color.white; 

        for (int i = 0; i < newName.Length; i++)
        {
            if (newName[i] == ' ') continue;

            // Xác định tâm của chữ cái đích (Target Center)
            float charCenterX = (i * charSpacing) - (newName.Length * charSpacing / 2);
            Vector3 charCenterPos = new Vector3(charCenterX, 0, 0);

            // Tạo một chùm mảnh vỡ cho chữ cái này (giống hệt lúc phá vỡ)
            for (int k = 0; k < shardsPerChar; k++)
            {
                GameObject shardObj = new GameObject("Shard_Assemble");
                shardObj.transform.SetParent(sourceText.transform.parent, false);

                Image img = shardObj.AddComponent<Image>();
                img.sprite = shardSprite;
                img.color = shardColor; 
                
                // Ban đầu tàng hình (Alpha = 0)
                Color c = img.color;
                c.a = 0;
                img.color = c;

                // Kích thước ngẫu nhiên
                float size = Random.Range(shardSizeRange.x, shardSizeRange.y);
                shardObj.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);

                // A. VỊ TRÍ ĐÍCH (TARGET): Tụ lại thành hình chữ cái
                // Random nhẹ xung quanh tâm để tạo hình dáng chữ (Cluster)
                Vector3 randomOffsetTarget = new Vector3(Random.Range(-10f, 10f), Random.Range(-20f, 20f), 0);
                Vector3 targetPos = sourceText.transform.localPosition + charCenterPos + randomOffsetTarget;
                targetPositions.Add(targetPos);

                // B. VỊ TRÍ XUẤT PHÁT (START): Dưới đáy sâu
                // Rải rác ngẫu nhiên ở phía dưới (-200 đến -400 pixel so với đích)
                Vector3 startPos = targetPos + new Vector3(Random.Range(-60f, 60f), Random.Range(-200f, -450f), 0);
                shardObj.transform.localPosition = startPos;
                startPositions.Add(startPos);
                
                // Xoay lung tung
                shardObj.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

                shards.Add(shardObj);
            }
        }

        // 2. DIỄN HOẠT BAY LÊN (REVERSE GRAVITY)
        float timer = 0;
        while (timer < assembleDuration)
        {
            timer += Time.deltaTime;
            float t = timer / assembleDuration;
            
            // Hàm EaseOutCubic: Bay vút lên nhanh lúc đầu, rồi từ từ chậm lại khi vào khớp
            // Giúp hiệu ứng trông có lực hút hơn
            float ease = 1 - Mathf.Pow(1 - t, 3); 

            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i] == null) continue;

                // Di chuyển từ Dưới lên Đích
                shards[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], ease);
                
                // Xoay mảnh vỡ về vị trí ngay ngắn (Identity)
                shards[i].transform.localRotation = Quaternion.Lerp(shards[i].transform.localRotation, Quaternion.identity, t);
                
                // Hiện dần ra (Fade In)
                Image img = shards[i].GetComponent<Image>();
                Color c = img.color;
                // Hiện nhanh hơn chút để người chơi thấy nó bay lên
                c.a = Mathf.Lerp(0, 1, t * 3f); 
                img.color = c;
            }
            yield return null;
        }

        // 3. KẾT THÚC: Xóa mảnh vỡ -> Hiện chữ thật
        foreach (var frag in shards) Destroy(frag);
        
        // 1. Điền tên thật
        sourceText.text = newName; 
        
        // 2. [QUAN TRỌNG] Bật lại hiển thị (Alpha = 1)
        sourceText.alpha = 1f; 
        
        // Force update để ăn gradient ngay lập tức
        sourceText.ForceMeshUpdate();
    }
}