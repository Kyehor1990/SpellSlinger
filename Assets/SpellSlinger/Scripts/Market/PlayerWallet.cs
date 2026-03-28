using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int currentMoney = 0;

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log($"<color=yellow>Altın Alındı! Toplam Para: {currentMoney}</color>");
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log($"<color=red>Para Harcandı! Kalan: {currentMoney}</color>");
            return true;
        }
        Debug.Log("Yeterli paran yok!");
        return false;
    }
}