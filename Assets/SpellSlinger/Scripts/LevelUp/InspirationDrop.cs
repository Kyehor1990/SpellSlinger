using System;
using UnityEngine;

public class InspirationDrop : MonoBehaviour
{
    private Transform target;
    private bool isFollowing;
    public float moveSpeed = 8f;
    [Header("Özellikler")]
    public int xpAmount = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform.root;
            isFollowing = true;
        }
    }

    private void Update()
    {
        if (isFollowing && target != null)
        {
            moveSpeed += 0.5f;
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            float sqrDistance = (transform.position - target.position).sqrMagnitude;
            if (sqrDistance < 0.01f)
            {
                Collect();
            }
        }
    }
    public void ForceFollow(Transform playerTransform)
    {
        target = playerTransform;
        isFollowing = true;
        
        moveSpeed = 15f; 
    }
    private void Collect()
    {
        if (target.TryGetComponent(out PlayerExperience playerXp)) 
        {
            playerXp.AddExperience(xpAmount);
        }
       
        Destroy(gameObject);
    }
    
}