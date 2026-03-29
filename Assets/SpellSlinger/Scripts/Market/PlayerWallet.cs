using UnityEngine;
using TMPro;

public class PlayerWallet : MonoBehaviour
{
    public int currentMoney = 0;
    public TextMeshProUGUI moneyTextUI;

    private void Start()
    {
        UpdateUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        if (moneyTextUI != null)
        {
            moneyTextUI.text = "Altın: " + currentMoney;
        }
    }
}