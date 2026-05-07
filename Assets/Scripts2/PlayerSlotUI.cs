using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach vào mỗi PlayerSlot GameObject trong RoomScene.
/// RoomManager gọi SetOccupied / SetEmpty để cập nhật UI.
/// </summary>
public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI statusText;
    public Image slotBackground;
    public GameObject hostBadge;        // badge "HOST" chỉ hiện với host

    [Header("Colors")]
    public Color occupiedColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    public Color emptyColor    = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    public void SetOccupied(string playerName, bool isHost)
    {
        playerNameText.text = playerName;
        statusText.text     = "Sẵn sàng";
        slotBackground.color = occupiedColor;
        hostBadge?.SetActive(isHost);
    }

    public void SetEmpty()
    {
        playerNameText.text  = "Chờ người chơi...";
        statusText.text      = "";
        slotBackground.color = emptyColor;
        hostBadge?.SetActive(false);
    }
}
