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
   
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
           
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
        
        GameObject damageTextObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        
      
        DamageText dtScript = damageTextObj.GetComponent<DamageText>();
        if (dtScript != null)
        {
        
Color finalColor = isCritical ? Color.red : (textColor == default ? Color.white : textColor);
dtScript.Setup(damageAmount, finalColor, isCritical);

        }
        else
        {
         
            TextMeshPro tmp = damageTextObj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
Color finalColor = isCritical ? Color.red : (textColor == default ? Color.white : textColor);
tmp.text = damageAmount.ToString("F0");
tmp.color = finalColor;

            }
        }
    }
}