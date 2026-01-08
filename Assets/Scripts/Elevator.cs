using System.Collections;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [Header("Cài đặt Thang Máy")]
    public Transform topPoint;    
    public Transform bottomPoint; 
    public float speed = 3f;      
    public KeyCode interactKey = KeyCode.E; 
    public bool startAtTop = false;

    [Header("Debug Info")]
    public bool isMoving = false;
    public bool isAtTop = false;
    public bool playerInZone = false; // Player có đang đứng trong vùng bấm nút không?

    private Vector3 targetPos;
    private Rigidbody2D rb;
    private PlayerController currentPlayer; // Lưu tham chiếu Player đang đứng trên thang

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Setup vị trí ban đầu
        if (startAtTop) 
        { 
            transform.position = topPoint.position; 
            isAtTop = true; 
        }
        else 
        { 
            transform.position = bottomPoint.position; 
            isAtTop = false; 
        }
    }

    void Update()
    {
        // Chỉ nhận nút bấm khi Player ở trong vùng VÀ thang máy đang đứng yên
        if (playerInZone && !isMoving)
        {
            if (Input.GetKeyDown(interactKey))
            {
                StartElevator();
            }
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            // Di chuyển
            Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            // [SỬA LẠI ĐOẠN NÀY]
            // Ép kiểu về (Vector2) để chỉ so sánh X và Y, bỏ qua Z
            if (Vector2.Distance(transform.position, targetPos) < 0.05f) // Nới lỏng 0.01 thành 0.05 cho dễ trúng
            {
                Arrived();
            }
        }
    }

    void StartElevator()
    {
        isMoving = true;
        
        // Debug
        Debug.Log($"Bắt đầu chạy! Đang ở đỉnh? {isAtTop}");

        // Biến tạm để lấy vị trí X,Y của điểm đến
        Vector3 dest; 

        if (isAtTop) dest = bottomPoint.position;
        else dest = topPoint.position;

        // [QUAN TRỌNG] Giữ nguyên trục Z của thang máy hiện tại
        // Để tránh việc nó bị lệch trục Z so với tính toán
        targetPos = new Vector3(dest.x, dest.y, transform.position.z);

        // Gọi Coroutine mở khóa input (Code cũ của bạn)
        if (currentPlayer != null)
        {
            StartCoroutine(UnlockInputNextFrame());
        }
    }

    void Arrived()
    {
        isMoving = false;
        
        // Đảo ngược trạng thái vị trí (Đang đỉnh -> Đáy, Đang đáy -> Đỉnh)
        isAtTop = !isAtTop;
        
        // Căn chỉnh vị trí cho chuẩn xác tuyệt đối
        transform.position = targetPos;

        // [MỚI] Khi thang dừng lại -> KHÓA nút E
        // Để Player bấm E thì nó hiểu là gọi thang máy chứ không uống máu
        if (currentPlayer != null && playerInZone)
        {
            currentPlayer.isElevatorBlockingInput = true;
        }
    }

    // --- XỬ LÝ TRIGGER (Vùng bấm nút & An toàn) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                playerInZone = true;
                currentPlayer = player;

                // 1. Bật chế độ bất tử Fall Damage (Luôn luôn bật khi trong vùng)
                player.isOnElevator = true; 

                // 2. Kiểm tra xem có cần chặn nút E không?
                // Nếu thang đang đứng yên -> Chặn E (để bấm thang)
                // Nếu thang đang chạy -> Không chặn (để uống máu)
                if (!isMoving)
                {
                    player.isElevatorBlockingInput = true;
                }
                else
                {
                    player.isElevatorBlockingInput = false;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                playerInZone = false;
                currentPlayer = null;

                // Ra khỏi vùng -> Tắt hết mọi chế độ
                player.isOnElevator = false;           // Hết bất tử
                player.isElevatorBlockingInput = false;// Dùng item thoải mái
            }
        }
    }

    // --- XỬ LÝ COLLISION (Dán Player vào sàn) ---
    // Giữ nguyên logic Coroutine cũ để tránh lỗi Unity
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null) StartCoroutine(AttachPlayer(player));
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && gameObject.activeInHierarchy) StartCoroutine(DetachPlayer(player));
        }
    }

    IEnumerator AttachPlayer(PlayerController player)
    {
        yield return new WaitForEndOfFrame();
        if (player != null) player.transform.SetParent(transform);
    }

    IEnumerator DetachPlayer(PlayerController player)
    {
        yield return new WaitForEndOfFrame();
        if (player != null) player.transform.SetParent(null);
    }

    // Coroutine này đợi sang frame tiếp theo mới mở khóa nút E
    IEnumerator UnlockInputNextFrame()
    {
        // Chờ 1 frame (để PlayerController chạy xong logic của frame hiện tại đã)
        yield return null; 

        if (currentPlayer != null)
        {
            currentPlayer.isElevatorBlockingInput = false; // Giờ mới cho phép uống máu
        }
    }
}