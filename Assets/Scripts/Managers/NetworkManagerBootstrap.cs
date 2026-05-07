using UnityEngine;
using Unity.Netcode;

public class NetworkManagerBootstrap : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}