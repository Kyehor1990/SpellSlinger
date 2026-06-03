using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;
    
[SerializeField] private GameObject damageTextPrefab;

[Header("Damage Popup Renkleri")]
[SerializeField] public Color normalColor = Color.white;
[SerializeField] public Color critColor = Color.red;
[SerializeField] public Color lightningColor = Color.yellow;
[SerializeField] public Color burnColor = Color.orange;
[SerializeField] public Color iceColor = new Color(0.537f, 0.831f, 0.961f); // #89D4F5
[SerializeField] public Color waterColor = new Color(0.12f, 0.23f, 0.53f); // Koyu mavi
   [SerializeField, Min(0)] private int prewarmCount = 20;
    private readonly Queue<DamageText> damageTextPool = new Queue<DamageText>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PrewarmDamageTextPool();
           
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
public void ShowDamage(Vector3 position, float damageAmount, Color textColor = default, bool isCritical = false)

    {
        if (damageTextPrefab == null) return;
        
      
        Vector3 spawnPos = new Vector3(position.x, position.y, -1f);
        
        DamageText dtScript = GetDamageText();
        if (dtScript != null)
        {
            dtScript.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            dtScript.gameObject.SetActive(true);
        
Color finalColor = isCritical ? Color.red : (textColor == default ? Color.white : textColor);
dtScript.Setup(damageAmount, finalColor, isCritical, ReleaseDamageText);

        }
        else
        {
            GameObject damageTextObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
         
            TextMeshPro tmp = damageTextObj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
Color finalColor = isCritical ? Color.red : (textColor == default ? Color.white : textColor);
tmp.text = damageAmount.ToString("F0");
tmp.color = finalColor;

            }
        }
    }

    private void PrewarmDamageTextPool()
    {
        if (damageTextPrefab == null || prewarmCount <= 0) return;

        for (int i = 0; i < prewarmCount; i++)
        {
            DamageText damageText = CreateDamageTextInstance();
            if (damageText == null) return;

            damageText.gameObject.SetActive(false);
            damageTextPool.Enqueue(damageText);
        }
    }

    private DamageText GetDamageText()
    {
        while (damageTextPool.Count > 0)
        {
            DamageText pooledText = damageTextPool.Dequeue();
            if (pooledText != null)
            {
                return pooledText;
            }
        }

        return CreateDamageTextInstance();
    }

    private DamageText CreateDamageTextInstance()
    {
        if (damageTextPrefab == null) return null;

        GameObject damageTextObj = Instantiate(damageTextPrefab);
        DamageText damageText = damageTextObj.GetComponent<DamageText>();
        if (damageText == null)
        {
            Destroy(damageTextObj);
        }

        return damageText;
    }

    private void ReleaseDamageText(DamageText damageText)
    {
        if (damageText == null) return;

        damageText.gameObject.SetActive(false);
        damageTextPool.Enqueue(damageText);
    }
}
