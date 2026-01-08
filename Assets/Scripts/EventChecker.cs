using UnityEngine;
using UnityEditor; // Chỉ chạy được trong Editor

public class EventChecker : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Check Animation Events")]
    static void CheckEvents()
    {
        // Lấy Animator của đối tượng đang chọn
        GameObject go = Selection.activeGameObject;
        if (go == null) { Debug.LogError("Hãy chọn GameObject Model_Phase2 trước!"); return; }

        Animator anim = go.GetComponent<Animator>();
        if (anim == null) { Debug.LogError("Không tìm thấy Animator!"); return; }

        RuntimeAnimatorController controller = anim.runtimeAnimatorController;
        if (controller == null) return;

        Debug.Log($"--- BẮT ĐẦU KIỂM TRA: {go.name} ---");

        // Duyệt qua tất cả Animation Clip trong Controller
        foreach (AnimationClip clip in controller.animationClips)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            for (int i = 0; i < events.Length; i++)
            {
                // Kiểm tra tên hàm có bị rỗng không
                if (string.IsNullOrEmpty(events[i].functionName) || string.IsNullOrWhiteSpace(events[i].functionName))
                {
                    Debug.LogError($"❌ PHÁT HIỆN LỖI: Animation '[{clip.name}]' tại giây thứ {events[i].time} có Event bị rỗng tên!");
                }
                else
                {
                    Debug.Log($"✅ Clip '{clip.name}' - Event: {events[i].functionName} (Time: {events[i].time})");
                }
            }
        }
        Debug.Log("--- KẾT THÚC KIỂM TRA ---");
    }
#endif
}