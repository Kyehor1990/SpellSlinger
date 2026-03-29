using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI Referansları")]
    public GameObject inventoryUIContainer;
    private void Start()
    {
        if (inventoryUIContainer != null)
        {
            inventoryUIContainer.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUIContainer != null)
        {
            bool isActive = inventoryUIContainer.activeSelf;
            inventoryUIContainer.SetActive(!isActive);

            if (!isActive) // Açılıyorsa
                Time.timeScale = 0f;
            else           // Kapanıyorsa
                Time.timeScale = 1f;
            
            Debug.Log(isActive ? "Envanter Kapandı" : "Envanter Açıldı");
        }
    }
}