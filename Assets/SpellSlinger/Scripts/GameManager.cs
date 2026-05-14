using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

    private bool isGameOver;

    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheReferences();
        HideGameOverPanel();
        WireRestartButton();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        HideGameOverPanel();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartCurrentScene);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        CacheReferences();

        if (playerController != null)
        {
            playerController.RemoveAccelerationBuff();
            playerController.enabled = false;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Game Over Panel is not assigned on GameManager.");
        }

        Time.timeScale = 0f;
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void CacheReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void WireRestartButton()
    {
        if (restartButton == null)
        {
            return;
        }

        restartButton.onClick.RemoveListener(RestartCurrentScene);
        restartButton.onClick.AddListener(RestartCurrentScene);
    }
}
