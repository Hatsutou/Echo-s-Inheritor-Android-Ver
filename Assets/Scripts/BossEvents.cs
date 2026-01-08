using UnityEngine;

public class BossEvents : MonoBehaviour
{
    private BossMalphas bossScript;

    void Start()
    {
        // Tự động tìm script chính nằm ở trên người Bố (Parent)
        bossScript = GetComponentInParent<BossMalphas>();
        
        if (bossScript == null) 
        {
            Debug.LogError("Không tìm thấy script BossMalphas ở cha!");
        }
    }

    // --- CÁC HÀM "ĐƯA THƯ" (Relay Methods) ---
    // Animator sẽ gọi các hàm này, và các hàm này sẽ gọi lên Bố

    public void TriggerEnableHitbox()
    {
        if (bossScript != null) bossScript.EnableHitbox();
    }

    public void TriggerDisableHitbox()
    {
        if (bossScript != null) bossScript.DisableHitbox();
    }

    public void TriggerEndAttack()
    {
        if (bossScript != null) bossScript.EndAttackState();
    }
}