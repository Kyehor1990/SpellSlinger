using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;

public class InventoryUIManager : MonoBehaviour
{
    private bool wasGameAlreadyPaused = false;
    private bool tutorialHintHidden = false;
    private Vector3 tutorialHintStartScale = Vector3.one;
    private Tween tutorialHintTween;

    [Header("UI Referansları")]
    public GameObject inventoryUIContainer;

    [Header("Tutorial Hint")]
    [SerializeField] private GameObject tutorialHintObject;
    [SerializeField] private CanvasGroup tutorialHintCanvasGroup;
    [SerializeField] private TMP_Text tutorialHintText;
    [SerializeField] private bool hideTutorialAfterFirstTabPress = true;
    [SerializeField] private string tutorialHintMessage = "Parşömeni açmak için TAB tuşuna bas.";
    [SerializeField, Range(0.05f, 1f)] private float tutorialHideDuration = 0.25f;
    [SerializeField, Range(0.5f, 1f)] private float tutorialHideScale = 0.92f;
    private void Start()
    {
        if (inventoryUIContainer != null)
        {
            inventoryUIContainer.SetActive(false);
        }

        SetupTutorialHint();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            HideTutorialHintAfterFirstTabPress();
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

    private void SetupTutorialHint()
    {
        if (tutorialHintObject == null)
        {
            return;
        }

        tutorialHintObject.SetActive(true);
        tutorialHintStartScale = tutorialHintObject.transform.localScale;

        if (tutorialHintCanvasGroup == null)
        {
            tutorialHintCanvasGroup = tutorialHintObject.GetComponent<CanvasGroup>();
        }

        if (tutorialHintCanvasGroup != null)
        {
            tutorialHintCanvasGroup.alpha = 1f;
        }

        if (tutorialHintText != null)
        {
            tutorialHintText.text = tutorialHintMessage;
        }
    }

    private void HideTutorialHintAfterFirstTabPress()
    {
        if (!hideTutorialAfterFirstTabPress || tutorialHintHidden || tutorialHintObject == null)
        {
            return;
        }

        tutorialHintHidden = true;
        tutorialHintTween?.Kill();

        Transform hintTransform = tutorialHintObject.transform;
        Vector3 targetScale = tutorialHintStartScale * tutorialHideScale;

        Sequence hideSequence = DOTween.Sequence().SetUpdate(true);
        hideSequence.SetTarget(tutorialHintObject);

        if (tutorialHintCanvasGroup != null)
        {
            hideSequence.Join(tutorialHintCanvasGroup.DOFade(0f, tutorialHideDuration));
        }

        hideSequence.Join(hintTransform.DOScale(targetScale, tutorialHideDuration).SetEase(Ease.InQuad));
        hideSequence.OnComplete(() =>
        {
            if (tutorialHintObject != null)
            {
                tutorialHintObject.SetActive(false);
            }
        });

        tutorialHintTween = hideSequence;
    }

    private void OnDestroy()
    {
        tutorialHintTween?.Kill();
    }
}
