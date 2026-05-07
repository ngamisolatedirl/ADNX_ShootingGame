/// <summary>
/// Static context: truyền thông tin phòng giữa các scene.
/// Không cần MonoBehaviour, không cần DontDestroyOnLoad.
/// </summary>
public static class RoomContext
{
    /// <summary>Thông tin phòng hiện tại (host lẫn client đều dùng).</summary>
    public static RoomInfo CurrentRoom { get; set; }

    /// <summary>True nếu người chơi này là host.</summary>
    public static bool IsHost { get; set; }

    /// <summary>Map đang được chọn bởi host (cập nhật real-time trong RoomScene).</summary>
    public static string SelectedMap => CurrentRoom?.selectedMap ?? "Level1";

    public static void Clear()
    {
        CurrentRoom = null;
        IsHost = false;
    }
}
