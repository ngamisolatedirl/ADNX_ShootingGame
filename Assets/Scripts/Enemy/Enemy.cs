using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 20f;
    public float moveSpeed = 2f;
    private float currentHealth;
    public Action OnDeath;

    private Transform player;
    private UIManager uiManager;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindWithTag("Player").transform;
        uiManager = FindFirstObjectByType<UIManager>();
    }

    void Update()
    {
        MoveTowardsPlayer();
    }

    void MoveTowardsPlayer()
    {
        if (player == null) return;

        float dirX = player.position.x - transform.position.x;
        dirX = Mathf.Sign(dirX);

        transform.position += new Vector3(dirX * moveSpeed * Time.deltaTime, 0, 0);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        transform.localScale = scale;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log("TakeDamage gọi! HP còn: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Enemy chết! uiManager: " + uiManager);

            if (uiManager != null)
                uiManager.AddKill();
            else
                Debug.LogError("uiManager là NULL!");

            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}