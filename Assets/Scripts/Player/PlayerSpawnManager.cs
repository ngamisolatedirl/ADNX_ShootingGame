using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Config")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneFullyLoaded;

        if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            StartCoroutine(SpawnAfterDelay());
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneFullyLoaded;
    }

    private void OnSceneFullyLoaded(string sceneName, LoadSceneMode mode,
        List<ulong> completed, List<ulong> timedOut)
    {
        StopAllCoroutines();
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
{
    if (playerPrefab == null) { Debug.LogError("[SpawnManager] Thiếu playerPrefab!"); return; }
    if (spawnPoints == null || spawnPoints.Length == 0) { Debug.LogWarning("[SpawnManager] Thiếu spawnPoints!"); return; }

    int index = 0;
    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
    {
        if (index >= spawnPoints.Length) break;

        // Despawn player cũ nếu còn tồn tại
        var existing = NetworkManager.Singleton.SpawnManager
            .GetPlayerNetworkObject(client.ClientId);
        if (existing != null)
        {
            existing.Despawn(true); // true = destroy GameObject luôn
        }

        Vector3 spawnPos = spawnPoints[index].position;
        GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

        if (netObj != null)
            netObj.SpawnAsPlayerObject(client.ClientId);
        else
            Destroy(playerObj);

        index++;
    }
}
}