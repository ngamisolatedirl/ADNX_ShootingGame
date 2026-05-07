using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnManager : MonoBehaviour
{
    public Transform[] spawnPoints; // 2 điểm spawn

    void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int i = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (i >= spawnPoints.Length) break;
            client.PlayerObject.transform.position = spawnPoints[i].position;
            i++;
        }
    }
}