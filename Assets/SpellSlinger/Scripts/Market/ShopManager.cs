using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Market Verileri")]
    public List<WordData> allAvailableWords;
    public int rerollCost = 5;

    [Header("Arka Plan Referansları")]
    public PlayerWallet playerWallet;
    public PlayerInventory playerInventory;
    public PlayerStatsManager playerStats;
    [SerializeField] private EnemyWaveManager enemyWaveManager;

    [Header("UI Referansları")]
    public GameObject shopPanel;
    public TextMeshProUGUI moneyText;
    public Button[] slotButtons;
    public TextMeshProUGUI[] slotNames;
    public TextMeshProUGUI[] slotPrices;

    [Header("Shop Pixel Art")]
    [SerializeField] private Image shopPanelImage;
    [SerializeField] private Sprite shopPanelSprite;
    [SerializeField] private Sprite shopButtonNormalSprite;
    [SerializeField] private Sprite shopButtonHoverSprite;
    [SerializeField] private RectTransform shopItemsContainer;

    private WordData[] currentShopWords = new WordData[5];

    [HideInInspector] public bool isShopActive = false;

    [Header("Olasılık Ağırlıkları")]
    public float baseCommon = 50f;
    public float baseUncommon = 25f;
    public float baseRare = 15f;
    public float baseEpic = 9f;
    public float baseLegendary = 1f;

    private void Start()
    {
        if (enemyWaveManager == null)
        {
            enemyWaveManager = Object.FindFirstObjectByType<EnemyWaveManager>();
        }

        ApplyShopVisuals();
        RegisterShopTooltipHandlers();
        shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        isShopActive = true;
        shopPanel.SetActive(true);
        Time.timeScale = 0f;

        RollShopItems();
        RebuildShopItemsLayout();
    }

    public void RollShopItems()
    {
        WordTooltipUI.HideAll();

        float luck = playerStats != null ? playerStats.GetStat(StatType.Luck) : 0f;

        float currentCommon = Mathf.Max(0, baseCommon - (luck * 1.5f)); 
        float currentUncommon = Mathf.Max(0, baseUncommon - (luck * 1f));
        float currentRare = baseRare + (luck * 0.5f);
        float currentEpic = baseEpic + (luck * 0.7f);
        float currentLegendary = baseLegendary + (luck * 0.3f);
        
        float totalWeight = currentCommon + currentUncommon + currentRare + currentEpic + currentLegendary;

        for (int i = 0; i < currentShopWords.Length; i++)
        {
            WordRarity selectedRarity = RollRarity(currentCommon, currentUncommon, currentRare, currentEpic, currentLegendary, totalWeight);
            
            List<WordData> filteredWords = allAvailableWords.FindAll(w => w.rarity == selectedRarity);

            WordData selectedWord;
            
            if (filteredWords.Count > 0)
            {
                selectedWord = filteredWords[Random.Range(0, filteredWords.Count)];
            }
            else
            {
                selectedWord = allAvailableWords[Random.Range(0, allAvailableWords.Count)];
            }

            currentShopWords[i] = selectedWord;

            slotNames[i].text = selectedWord.runeText;
            slotPrices[i].text = GetWordPrice(selectedWord).ToString();
            slotButtons[i].interactable = true; 
        }
    }

    private WordRarity RollRarity(float c, float u,float r, float e, float l, float total)
    {
        float randomVal = Random.Range(0f, total);
        
        if (randomVal <= c) return WordRarity.Common;
        randomVal -= c;
        if (randomVal <= u) return WordRarity.Uncommon;
        randomVal -= u;
        if (randomVal <= r) return WordRarity.Rare;
        randomVal -= r;
        if (randomVal <= e) return WordRarity.Epic;
        
        return WordRarity.Legendary;
    }

    public void TryReroll()
    {
        if (playerWallet.SpendMoney(rerollCost))
        {
            RollShopItems();
        }
    }

    public void TryBuyWord(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShopWords.Length) return;
        if (currentShopWords[slotIndex] == null) return;

        WordTooltipUI.HideForSource(GetSlotRect(slotIndex));

        WordData selectedWord = currentShopWords[slotIndex];
        int price = GetWordPrice(selectedWord);

        if (playerWallet.SpendMoney(price))
        {
            playerInventory.AddWord(selectedWord);
            
            currentShopWords[slotIndex] = null;
            slotButtons[slotIndex].interactable = false;
            slotNames[slotIndex].text = "SATILDI";
            slotPrices[slotIndex].text = "-";
        }
    }

    public void CloseShop()
    {
        if (!isShopActive)
        {
            return;
        }

        isShopActive = false;
        WordTooltipUI.HideAll();
        shopPanel.SetActive(false);
        Time.timeScale = 1f;

        enemyWaveManager?.StartNextWaveNow();
    }

    private int GetWordPrice(WordData word)
    {
        return word != null ? Mathf.Max(0, word.shopPrice) : 0;
    }

    private void OnValidate()
    {
        ApplyPanelSprite();
        ApplySlotButtonSprites();
    }

    private void ApplyShopVisuals()
    {
        ApplyPanelSprite();
        ApplySlotButtonSprites();
        AssignSlotParents();
        RebuildShopItemsLayout();
    }

    private void ApplyPanelSprite()
    {
        if (shopPanelImage == null && shopPanel != null)
        {
            shopPanelImage = shopPanel.GetComponent<Image>();
        }

        if (shopPanelImage == null || shopPanelSprite == null)
        {
            return;
        }

        shopPanelImage.sprite = shopPanelSprite;
        shopPanelImage.color = Color.white;
        shopPanelImage.preserveAspect = true;
    }

    private void ApplySlotButtonSprites()
    {
        if (slotButtons == null)
        {
            return;
        }

        foreach (Button slotButton in slotButtons)
        {
            if (slotButton == null)
            {
                continue;
            }

            Image buttonImage = slotButton.targetGraphic as Image;
            if (buttonImage == null)
            {
                buttonImage = slotButton.GetComponent<Image>();
            }

            if (buttonImage != null && shopButtonNormalSprite != null)
            {
                buttonImage.sprite = shopButtonNormalSprite;
                buttonImage.type = Image.Type.Simple;
                buttonImage.preserveAspect = false;
                slotButton.targetGraphic = buttonImage;
            }

            if (shopButtonHoverSprite == null)
            {
                continue;
            }

            SpriteState spriteState = slotButton.spriteState;
            spriteState.highlightedSprite = shopButtonHoverSprite;
            spriteState.pressedSprite = shopButtonHoverSprite;
            spriteState.selectedSprite = shopButtonHoverSprite;
            slotButton.spriteState = spriteState;
            slotButton.transition = Selectable.Transition.SpriteSwap;
        }
    }

    private void AssignSlotParents()
    {
        if (slotButtons == null || shopItemsContainer == null)
        {
            return;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button slotButton = slotButtons[i];
            if (slotButton == null)
            {
                continue;
            }

            if (slotButton.transform.parent != shopItemsContainer)
            {
                slotButton.transform.SetParent(shopItemsContainer, false);
            }
        }
    }

    private void RebuildShopItemsLayout()
    {
        if (shopItemsContainer == null)
        {
            return;
        }

        if (shopItemsContainer.GetComponent<LayoutGroup>() == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(shopItemsContainer);
    }

    private void RegisterShopTooltipHandlers()
    {
        if (slotButtons == null)
        {
            return;
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button slotButton = slotButtons[i];
            if (slotButton == null)
            {
                continue;
            }

            int slotIndex = i;
            EventTrigger eventTrigger = slotButton.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = slotButton.gameObject.AddComponent<EventTrigger>();
            }

            AddShopTooltipEntry(eventTrigger, EventTriggerType.PointerEnter, eventData => ShowShopTooltip(slotIndex, eventData));
            AddShopTooltipEntry(eventTrigger, EventTriggerType.PointerExit, _ => HideShopTooltip(slotIndex));
        }
    }

    private void AddShopTooltipEntry(EventTrigger eventTrigger, EventTriggerType eventId, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        if (eventTrigger == null || callback == null)
        {
            return;
        }

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventId };
        entry.callback.AddListener(callback);
        eventTrigger.triggers.Add(entry);
    }

    private void ShowShopTooltip(int slotIndex, BaseEventData eventData)
    {
        WordData wordData = GetCurrentShopWord(slotIndex);
        if (wordData == null)
        {
            return;
        }

        PointerEventData pointerEventData = eventData as PointerEventData;
        WordTooltipUI.ShowWordDelayed(wordData, 1, GetSlotRect(slotIndex), pointerEventData != null ? pointerEventData.position : null);
    }

    private void HideShopTooltip(int slotIndex)
    {
        WordTooltipUI.HideForSource(GetSlotRect(slotIndex));
    }

    private WordData GetCurrentShopWord(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentShopWords.Length)
        {
            return null;
        }

        if (slotButtons != null &&
            slotIndex < slotButtons.Length &&
            slotButtons[slotIndex] != null &&
            !slotButtons[slotIndex].interactable)
        {
            return null;
        }

        return currentShopWords[slotIndex];
    }

    private RectTransform GetSlotRect(int slotIndex)
    {
        if (slotButtons == null || slotIndex < 0 || slotIndex >= slotButtons.Length || slotButtons[slotIndex] == null)
        {
            return null;
        }

        return slotButtons[slotIndex].transform as RectTransform;
    }
}
