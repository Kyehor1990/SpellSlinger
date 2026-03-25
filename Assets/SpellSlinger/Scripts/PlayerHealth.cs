using UnityEngine;
using UnityEngine.UI; // Slider için gerekli

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI Elemanları")]
    public Slider healthSlider;
    public HealthBarShake shakeEffect; 

    void Start()
    {
        currentHealth = maxHealth;
        
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
       
        healthSlider.value = currentHealth;

        
        if (shakeEffect != null) shakeEffect.TriggerShake();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die() {  }
}