using UnityEngine;
using UnityEngine.UI; // Slider için gerekli

public class PlayerHealth : MonoBehaviour
{
    public int currentHealth;

    [Header("UI Elemanları")]
    public Slider healthSlider;
    public HealthBarShake shakeEffect; 

    [Header("Stat Referansı")]
    public PlayerStatsManager playerStats;
    private float regenTimer = 0f;

    void Start()
    {
        int maxHealth = (int)playerStats.GetStat(StatType.MaxHealth);
        currentHealth = maxHealth;
        
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    void Update()
    {
        float regenAmount = playerStats.GetStat(StatType.HealthRegen);
        if (regenAmount > 0 && currentHealth < playerStats.GetStat(StatType.MaxHealth))
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1f)
            {
                Heal(regenAmount);
                regenTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        float evasionChance = playerStats.GetStat(StatType.Evasion);
        if (Random.Range(0f, 100f) < evasionChance)
        {
            Debug.Log("<color=green>Saldırıdan Kaçındın!</color>");
            return;
        }

        float armor = playerStats.GetStat(StatType.Armor); // Örn: 15 ise %15 azaltır
        float damageReduction = damage * (armor / 100f);
        int finalDamage = Mathf.RoundToInt(damage - damageReduction);
        
        if (finalDamage < 1) finalDamage = 1;
        
       currentHealth -= finalDamage;
        healthSlider.value = currentHealth;

        
        if (shakeEffect != null) shakeEffect.TriggerShake();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        int maxHealth = (int)playerStats.GetStat(StatType.MaxHealth);
        
        healthSlider.maxValue = maxHealth; 
        
        currentHealth += Mathf.RoundToInt(amount);
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        healthSlider.value = currentHealth;
    }

    public void ApplyLifeSteal(float damageDealtToEnemy)
    {
        float lifeStealChance = playerStats.GetStat(StatType.LifeSteal);
        
        if (Random.Range(0f, 100f) < lifeStealChance)
        {
            float healAmount = damageDealtToEnemy * 0.5f; 
            
            if (healAmount < 1f) healAmount = 1f;
            
            Heal(healAmount);
            Debug.Log($"<color=red>Can Çalma Tetiklendi! +{healAmount} Can</color>");
        }
    }

    void Die() {  }
}