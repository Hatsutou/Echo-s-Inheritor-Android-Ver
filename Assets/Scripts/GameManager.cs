using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Danh sách chứa tất cả các script hồi sinh của quái
    public List<EnemyRespawn> allEnemies = new List<EnemyRespawn>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Tìm tất cả quái có script EnemyRespawn (kể cả đang ẩn nếu dùng Unity mới)
        EnemyRespawn[] foundEnemies = FindObjectsByType<EnemyRespawn>(FindObjectsSortMode.None);
        allEnemies.AddRange(foundEnemies);

        // --- THÊM DÒNG NÀY ĐỂ KIỂM TRA ---
        Debug.Log("GameManager: Đã tìm thấy " + allEnemies.Count + " con quái để quản lý.");
        
        if (allEnemies.Count == 0)
        {
            Debug.LogError("LỖI: Không tìm thấy quái nào! Kiểm tra xem quái đã gắn script EnemyRespawn chưa?");
        }
    }

    // Hàm gọi từ Checkpoint
    public void RespawnEnemies()
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.ResetEnemy();
            }
        }
        Debug.Log("Toàn bộ quái vật đã hồi sinh!");
    }
}