using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuButton : MonoBehaviour, IPointerEnterHandler
{
    // Khi chuột lướt vào nút này -> Báo ngay cho Controller
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PauseMenuController.Instance != null)
        {
            // Gọi hàm mới 'SetSelectionByMouse' thay vì hàm UpdateHighlight cũ
            PauseMenuController.Instance.SetSelectionByMouse(GetComponent<RectTransform>());
        }
    }
}