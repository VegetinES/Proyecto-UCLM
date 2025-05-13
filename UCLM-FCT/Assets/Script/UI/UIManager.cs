using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Elementos UI")]
    public GameObject winPanel;
    public Button nextLevelButton;
    public Button retryButton; 
    public Button mainMenuButton;
    
    private BoardManager boardManager;
    private int currentLevel;
    
    private void Awake()
    {
        Debug.Log("UIManager: Awake llamado");
        
        // Ocultar panel al inicio
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("UIManager: El panel de victoria no está asignado");
        }
    }
    
    public void Initialize(BoardManager board, int level)
    {
        Debug.Log($"UIManager: Inicializando para nivel {level}");
        
        boardManager = board;
        currentLevel = level;
        
        // Configurar botones
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            Debug.Log("UIManager: Botón Next Level configurado");
        }
        else
        {
            Debug.LogWarning("UIManager: El botón Next Level no está asignado");
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
            Debug.Log("UIManager: Botón Retry configurado");
        }
        else
        {
            Debug.LogWarning("UIManager: El botón Retry no está asignado");
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            Debug.Log("UIManager: Botón Main Menu configurado");
        }
        else
        {
            Debug.LogWarning("UIManager: El botón Main Menu no está asignado");
        }
    }
    
    public void ShowWinPanel()
    {
        Debug.Log("UIManager: Mostrando panel de victoria");
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("UIManager: El panel de victoria es null");
        }
    }
    
    private void OnNextLevelClicked()
    {
        Debug.Log("UIManager: Botón Next Level presionado - Generando nuevo tablero del mismo nivel");
        
        // Ocultar panel
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        
        // Cargar nuevo tablero del mismo nivel (simplemente recargamos el nivel actual)
        LevelLoader.LoadLevel(currentLevel);
    }
    
    private void OnRetryClicked()
    {
        Debug.Log("UIManager: Botón Retry presionado - Reiniciando nivel");
        
        // Ocultar panel
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        
        // Reiniciar mismo tablero (recargamos el nivel actual)
        LevelLoader.LoadLevel(currentLevel);
    }
    
    private void OnMainMenuClicked()
    {
        Debug.Log("UIManager: Botón Main Menu presionado - Volviendo al menú principal");
        
        // Volver al menú principal
        SceneManager.LoadScene("Menu");
    }
}