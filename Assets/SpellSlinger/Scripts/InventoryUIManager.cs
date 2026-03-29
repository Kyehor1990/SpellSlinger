using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUIManager : MonoBehaviour
{
    private bool wasGameAlreadyPaused = false;

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
            bool isOpening = !inventoryUIContainer.activeSelf;

            if (isOpening)
            {
            wasGameAlreadyPaused = (Time.timeScale == 0f); 
            
            inventoryUIContainer.SetActive(true);
            Time.timeScale = 0f; 
            }
            else
            {
                inventoryUIContainer.SetActive(false);
            
                if (!wasGameAlreadyPaused)
                {
                    Time.timeScale = 1f; 
                }
            }
        }

    }
}