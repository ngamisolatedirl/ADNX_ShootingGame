using System;

[Serializable]
public class RoomInfo
{
    public string roomId;        // GUID unique
    public string hostName;      // tên host
    public string roomType;      // "localhost" | "lan"
    public string selectedMap;   // "Level1" | "Level2" | "Level3" | "Level4"
    public int mapIndex;         // 1-4
    public int currentPlayers;   // số người hiện tại
    public int maxPlayers;       // 4
    public string hostIP;        // IP để connect
    public ushort port;          // port (default 7777)

    public bool IsFull => currentPlayers >= maxPlayers;
}

[Serializable]
public class RoomInfoPacket
{
    public RoomInfo roomInfo;
    // Dùng để parse JSON qua UDP
}
