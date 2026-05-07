using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Attach vào RoomListItem prefab.
/// Mỗi dòng trong danh sách phòng hiển thị thông tin và nút Join.
/// </summary>
public class RoomListItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI hostNameText;
    public TextMeshProUGUI mapText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI roomTypeText;
    public TextMeshProUGUI ipText;
    public Button joinButton;
    public Image fullIndicator;     // đỏ nếu phòng đầy

    public void Setup(RoomInfo room, Action onJoin)
    {
        hostNameText.text  = room.hostName ?? "Unknown";
        mapText.text       = "Map: " + (room.selectedMap ?? "?");
        playerCountText.text = $"{room.currentPlayers}/{room.maxPlayers}";
        roomTypeText.text  = room.roomType == "lan" ? "LAN" : "Local";
        ipText.text        = room.hostIP ?? "";

        bool isFull = room.IsFull;
        joinButton.interactable = !isFull;
        joinButton.onClick.AddListener(() => onJoin?.Invoke());

        if (fullIndicator != null)
            fullIndicator.color = isFull ? Color.red : Color.green;
    }
}
