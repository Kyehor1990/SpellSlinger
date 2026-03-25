using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI Referansları")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image iconImage;
    public Button cardButton;

    private StatUpgradeData myUpgradeData;
    private UpgradeManager manager;

    public void SetupCard(StatUpgradeData data, UpgradeManager mgr)
    {
        myUpgradeData = data;
        manager = mgr;

        nameText.text = data.upgradeName;
        descriptionText.text = data.description;
        
        if(data.icon != null) 
        {
            iconImage.sprite = data.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.color = new Color(0,0,0,0); // İkon yoksa şeffaf yap
        }

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        manager.OnUpgradeSelected(myUpgradeData);
    }
}