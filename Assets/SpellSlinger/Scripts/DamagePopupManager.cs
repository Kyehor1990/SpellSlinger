using UnityEngine;
using TMPro;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;
    
    [SerializeField] private GameObject damageTextPrefab;
   
    
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
    
    public void ShowDamage(Vector3 position, float damageAmount, Color textColor = default)
    {
        if (damageTextPrefab == null) return;
        
      
        Vector3 spawnPos = new Vector3(position.x, position.y, -1f);
        
        GameObject damageTextObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
        
      
        DamageText dtScript = damageTextObj.GetComponent<DamageText>();
        if (dtScript != null)
        {
        
            dtScript.Setup(damageAmount, textColor == default ? Color.white : textColor);
        }
        else
        {
         
            TextMeshPro tmp = damageTextObj.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = damageAmount.ToString("F0");
                tmp.color = textColor == default ? Color.white : textColor;
            }
        }
    }
}