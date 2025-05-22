using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public BoardManager boardManager;
    public LevelGenerator levelGenerator;
    public UIManager uiManager;
    public StatisticsManager statisticsManager;
    
    void Awake()
    {
        Debug.Log("GameInitializer: Awake llamado");
    }
    
    void Start()
    {
        Debug.Log("GameInitializer: Start llamado");
        
        // Verificar referencias
        if (boardManager == null)
        {
            Debug.LogError("GameInitializer: ¡BoardManager no asignado!");
            return;
        }
        
        if (levelGenerator == null)
        {
            Debug.LogError("GameInitializer: ¡LevelGenerator no asignado!");
            return;
        }
        
        // Obtener el nivel seleccionado
        int selectedLevel = LevelLoader.GetSelectedLevel();
        Debug.Log("GameInitializer: Nivel seleccionado: " + selectedLevel);
        
        // Configurar el BoardManager
        boardManager.levelNumber = selectedLevel;
        
        // Verificar si existe un StatisticsManager
        if (statisticsManager == null)
        {
            statisticsManager = FindFirstObjectByType<StatisticsManager>();
            
            // Si no existe, crear uno
            if (statisticsManager == null && StatisticsManager.Instance == null)
            {
                GameObject statsObj = new GameObject("StatisticsManager");
                statisticsManager = statsObj.AddComponent<StatisticsManager>();
                Debug.Log("GameInitializer: StatisticsManager creado");
            }
            else if (statisticsManager == null)
            {
                statisticsManager = StatisticsManager.Instance;
            }
        }
        
        try
        {
            // Configurar según el nivel
            LevelGenerator.LevelConfig config = levelGenerator.GetLevelConfig(selectedLevel);
            Debug.Log($"GameInitializer: Configuración obtenida - Ancho: {config.width}, Alto: {config.height}");
            
            boardManager.boardWidth = config.width;
            boardManager.boardHeight = config.height;
            boardManager.maxMoves = config.maxMoves;
            boardManager.maxTurns = config.maxTurns;
            boardManager.isSquare = config.isSquare;
            
            int obstacleCount = Random.Range(config.minObstacles, config.maxObstacles + 1);
            boardManager.obstacleCount = obstacleCount;
            
            Debug.Log($"GameInitializer: BoardManager configurado con {obstacleCount} obstáculos");
        }
        catch (System.Exception e)
        {
            Debug.LogError("GameInitializer: Error al configurar el nivel: " + e.Message);
            return;
        }
        
        // Inicializar la UI
        if (uiManager != null)
        {
            Debug.Log("GameInitializer: Inicializando UI");
            uiManager.Initialize(boardManager, selectedLevel);
        }
        else
        {
            Debug.LogWarning("GameInitializer: UIManager no asignado");
        }
        
        // Iniciar el nivel
        Debug.Log("GameInitializer: Inicializando tablero");
        boardManager.InitializeBoard();
    }
}