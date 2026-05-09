using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawn player SAU KHI scene load xong hoàn toàn (CS 1.6 style).
/// 
/// Setup bắt buộc:
/// 1. NetworkManager Inspector → field "Player Prefab" → XÓA TRỐNG (để null)
/// 2. Gắn script này vào bất kỳ GameObject nào trong Level scene
/// 3. Gán playerPrefab và spawnPoints trong Inspector
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Config")]
    [Tooltip("Kéo Player Prefab vào đây (KHÔNG gán vào NetworkManager nữa)")]
    public GameObject playerPrefab;

    [Tooltip("Các điểm spawn theo thứ tự P1, P2, P3, P4")]
    public Transform[] spawnPoints;

    private bool hasSpawned = false;

    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneFullyLoaded;

        // Offline (1 người): OnLoadEventCompleted không fire vì scene đã load sẵn
        // → spawn thủ công, nhưng chỉ khi không có client nào đang chờ
        if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
            StartCoroutine(SpawnAfterDelay());
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton?.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneFullyLoaded;
    }

    // Fired khi TẤT CẢ client đã load scene xong
    private void OnSceneFullyLoaded(string sceneName, LoadSceneMode mode,
        List<ulong> completed, List<ulong> timedOut)
    {
        Debug.Log($"[SpawnManager] {sceneName} load xong trên {completed.Count} máy.");
        StopAllCoroutines();
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        // Chờ 2 FixedUpdate để tilemap composite collider build xong
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        // Chỉ spawn 1 lần dù event fire nhiều lần
        if (hasSpawned) return;
        hasSpawned = true;

        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] Chưa gán playerPrefab!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SpawnManager] Chưa gán Spawn Points!");
            return;
        }

        int index = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (index >= spawnPoints.Length) break;

            Vector3 spawnPos = spawnPoints[index].position;

            // Instantiate ngay tại spawn point → không cần teleport → không conflict NetworkTransform
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            NetworkObject netObj = playerObj.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.SpawnAsPlayerObject(client.ClientId);
                Debug.Log($"[SpawnManager] Player {client.ClientId} spawned tại {spawnPos}");
            }
            else
            {
                Debug.LogError("[SpawnManager] playerPrefab thiếu NetworkObject!");
                Destroy(playerObj);
            }

            index++;
        }
    }
}