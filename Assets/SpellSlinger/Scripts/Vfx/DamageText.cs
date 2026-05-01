using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float lifetime = 0.8f;
    
    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;

    void Awake()
    {
     
        textMesh = GetComponent<TextMeshPro>();
    }

public void Setup(float damageAmount, Color color, bool isCritical = false)

    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

       
textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        textMesh.color = color;
        textColor = color;
        timer = lifetime;
        
        if (isCritical)
        {
            textMesh.transform.localScale = Vector3.one * 1.3f;
            
            textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            textMesh.fontStyle = FontStyles.Normal;
        }


        
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
    }

    void Update()
    {
       
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

       
        timer -= Time.deltaTime;
        
        if (textMesh != null)
        {
textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
            
            
            if (textMesh.transform.localScale.x > 1f)
            {
                textMesh.transform.localScale = Vector3.Lerp(textMesh.transform.localScale, Vector3.one, Time.deltaTime * 3f);
            }
        }


        if (timer <= 0)
        {
         
            Destroy(gameObject);
        }
    }
}