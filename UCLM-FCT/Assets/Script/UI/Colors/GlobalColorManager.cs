using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalColorManager : MonoBehaviour
{
    // Singleton para acceso global
    public static GlobalColorManager Instance { get; private set; }
    
    // Evento para notificar cambios en la intensidad de color
    public event Action<int> OnColorIntensityChanged;
    
    // Intensidad de color actual (1-5)
    private int currentColorIntensity = 3;
    
    // Control de inicialización
    private bool configLoaded = false;
    
    [Header("Depuración")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Suscribirse al evento de carga de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            DebugLog("Inicializado con DontDestroyOnLoad");
        }
        else
        {
            DebugLog("Ya existe una instancia. Destruyendo duplicado.");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Cargar la intensidad de color desde la base de datos
        LoadColorIntensity();
    }
    
    private void OnDestroy()
    {
        // Desuscribirse del evento de carga de escena
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // Evento cuando se carga una nueva escena
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugLog($"Nueva escena cargada: {scene.name}");
        
        // Esperar un frame para asegurarse de que todos los objetos estén inicializados
        StartCoroutine(NotifyColorIntensityNextFrame());
    }
    
    private IEnumerator NotifyColorIntensityNextFrame()
    {
        yield return null; // Esperar un frame
        
        // Recargar la intensidad de color si no se ha cargado todavía
        if (!configLoaded)
        {
            LoadColorIntensity();
        }
        
        // Notificar a todos los adaptadores del color actual
        NotifyColorIntensityChanged();
    }
    
    // Cargar la intensidad de color desde la base de datos
    private void LoadColorIntensity()
    {
        try
        {
            if (DataService.Instance == null)
            {
                DebugLog("DataService no disponible al cargar configuración de colores", true);
                return;
            }
            
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                int previousIntensity = currentColorIntensity;
                currentColorIntensity = config.Colors;
                
                // Asegurar que está en el rango válido
                currentColorIntensity = Mathf.Clamp(currentColorIntensity, 1, 5);
                configLoaded = true;
                
                DebugLog($"Intensidad de color cargada: {currentColorIntensity} (anterior: {previousIntensity})");
                
                // Notificar el cambio después de cargar solo si cambió
                if (previousIntensity != currentColorIntensity)
                {
                    NotifyColorIntensityChanged();
                }
            }
            else
            {
                DebugLog("No se pudo cargar la configuración de colores. Usando valor por defecto: " + currentColorIntensity, true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GlobalColorManager: Error al cargar intensidad de color: {e.Message}");
        }
    }
    
    // Método para notificar a todos los listeners del cambio de intensidad
    private void NotifyColorIntensityChanged()
    {
        int listenersCount = OnColorIntensityChanged != null ? OnColorIntensityChanged.GetInvocationList().Length : 0;
        DebugLog($"<color=cyan>Notificando cambio de intensidad a {currentColorIntensity} a {listenersCount} listeners</color>");
        
        OnColorIntensityChanged?.Invoke(currentColorIntensity);
    }
    
    // Método para forzar la recarga de la configuración y notificar
    public void ForceRefresh()
    {
        DebugLog("Forzando actualización de colores...");
        LoadColorIntensity();
    }
    
    // Método para actualizar la intensidad de color en la base de datos
    public void UpdateColorIntensity(int intensity)
    {
        try
        {
            if (DataService.Instance == null)
            {
                DebugLog("DataService no disponible al actualizar intensidad de color", true);
                return;
            }
            
            string userId = DataService.Instance.GetCurrentUserId();
            
            // Primero asegurarse de que la intensidad es válida
            intensity = Mathf.Clamp(intensity, 1, 5);
            
            // Si la intensidad no cambió, no hacer nada
            if (currentColorIntensity == intensity)
            {
                DebugLog($"La intensidad ya es {intensity}, no se realizan cambios");
                return;
            }
            
            DebugLog($"<color=yellow>Actualizando intensidad de color de {currentColorIntensity} a {intensity}</color>");
            
            // Actualizar el valor local
            currentColorIntensity = intensity;
            
            // Actualizar en la base de datos
            DataService.Instance.ConfigRepo.UpdateColors(userId, intensity);
            
            DebugLog($"Intensidad de color actualizada a {intensity} para usuario {userId}");
            
            // Notificar el cambio
            NotifyColorIntensityChanged();
            
            // Marcar la configuración como cargada
            configLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GlobalColorManager: Error al actualizar intensidad de color: {e.Message}");
        }
    }
    
    // Método público para obtener la intensidad actual
    public int GetCurrentIntensity()
    {
        return currentColorIntensity;
    }
    
    private void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            if (isWarning)
                Debug.LogWarning($"[GlobalColorManager] {message}");
            else
                Debug.Log($"[GlobalColorManager] {message}");
        }
    }
}