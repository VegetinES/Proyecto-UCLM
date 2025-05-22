using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }
    
    // Datos de la sesión actual
    private int currentLevel;
    private bool helpUsed = false;
    private int movesCount = 0;
    private float startTime;
    private float elapsedTime;
    private bool isTimerRunning = false;
    private bool isLevelCompleted = false;
    
    // Configuración
    [SerializeField] private float saveDelayOnExit = 1.5f; // Tiempo de espera para guardar al salir
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartLevel(int level)
    {
        currentLevel = level;
        startTime = Time.time;
        elapsedTime = 0f;
        helpUsed = false;
        movesCount = 0;
        isLevelCompleted = false;
        isTimerRunning = true;
        
        Debug.Log($"StatisticsManager: Iniciando nivel {level}");
    }
    
    public void RegisterHelpUsed()
    {
        helpUsed = true;
        Debug.Log("StatisticsManager: Ayuda utilizada");
    }
    
    public void RegisterMove()
    {
        movesCount++;
    }
    
    public void CompletedLevel()
    {
        if (isTimerRunning)
        {
            StopTimer();
            isLevelCompleted = true;
            SaveStatistics();
            
            Debug.Log($"StatisticsManager: Nivel {currentLevel} completado en {elapsedTime} segundos con {movesCount} movimientos. Ayuda: {helpUsed}");
        }
    }
    
    public void StopTimer()
    {
        if (isTimerRunning)
        {
            elapsedTime = Time.time - startTime;
            isTimerRunning = false;
        }
    }
    
    public async Task PrepareForExit()
    {
        // Si el tiempo sigue corriendo, detenerlo
        if (isTimerRunning)
        {
            StopTimer();
            
            // Guardar estadísticas si no se completó el nivel
            if (!isLevelCompleted)
            {
                SaveStatistics();
            }
            
            // Esperar a que se guarden las estadísticas
            await Task.Delay((int)(saveDelayOnExit * 1000));
        }
    }
    
    public void SaveStatistics()
    {
        // Determinar el ID de usuario o perfil para guardar estadísticas
        string userId = GetActiveUserId();
        int profileId = GetActiveProfileId();
        
        // Solo guardar si hay conexión a MongoDB y es un usuario registrado
        if (MongoDbService.Instance != null && MongoDbService.Instance.IsConnected() 
            && userId != AuthManager.DEFAULT_USER_ID)
        {
            SaveStatisticsToMongoDB(userId, profileId);
        }
    }
    
    private async void SaveStatisticsToMongoDB(string userId, int profileId)
    {
        try
        {
            await MongoDbService.Instance.SaveGameStatisticsAsync(
                userId,
                profileId,
                currentLevel,
                isLevelCompleted,
                movesCount,
                (int)elapsedTime,
                helpUsed,
                DateTime.UtcNow.ToString("o")
            );
            
            Debug.Log("StatisticsManager: Estadísticas guardadas en MongoDB");
        }
        catch (Exception e)
        {
            Debug.LogError($"StatisticsManager: Error al guardar estadísticas: {e.Message}");
        }
    }
    
    private string GetActiveUserId()
    {
        if (AuthManager.Instance != null)
        {
            return AuthManager.Instance.UserID;
        }
        return AuthManager.DEFAULT_USER_ID;
    }
    
    private int GetActiveProfileId()
    {
        if (ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile())
        {
            return ProfileManager.Instance.GetCurrentProfileId();
        }
        return 0;
    }
}