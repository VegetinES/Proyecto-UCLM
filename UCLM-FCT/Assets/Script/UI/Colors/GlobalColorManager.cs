using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalColorManager : MonoBehaviour
{
    public static GlobalColorManager Instance { get; private set; }
    public event Action<int> OnColorIntensityChanged;
    
    private int currentColorIntensity = 3;
    private bool configLoaded = false;
    private bool isWaitingForDataManager = false;
    
    [Header("Depuración")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        StartCoroutine(WaitForDataManagerAndLoad());
    }
    
    private IEnumerator WaitForDataManagerAndLoad()
    {
        isWaitingForDataManager = true;
        DebugLog("Esperando a que DataManager esté disponible...");
        
        // Esperar hasta que DataManager esté disponible con un límite de tiempo
        float timeWaited = 0f;
        float maxWaitTime = 10f;
        
        while (DataManager.Instance == null && timeWaited < maxWaitTime)
        {
            timeWaited += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (DataManager.Instance == null)
        {
            Debug.LogError("GlobalColorManager: DataManager no disponible después de esperar");
            isWaitingForDataManager = false;
            yield break;
        }
        
        DebugLog("DataManager encontrado, esperando un poco más para asegurar inicialización...");
        yield return new WaitForSeconds(0.5f);
        
        isWaitingForDataManager = false;
        LoadColorIntensity();
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugLog($"Nueva escena cargada: {scene.name}");
        StartCoroutine(NotifyColorIntensityNextFrame());
    }
    
    private IEnumerator NotifyColorIntensityNextFrame()
    {
        yield return null;
        
        // Asegurarnos de que no estamos aún esperando a DataManager
        while (isWaitingForDataManager)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (!configLoaded && DataManager.Instance != null)
        {
            LoadColorIntensity();
        }
        
        NotifyColorIntensityChanged();
    }
    
    private void LoadColorIntensity()
    {
        try
        {
            if (DataManager.Instance == null)
            {
                DebugLog("DataManager no disponible al cargar configuración de colores", true);
                return;
            }
            
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                int previousIntensity = currentColorIntensity;
                currentColorIntensity = config.Colors;
                currentColorIntensity = Mathf.Clamp(currentColorIntensity, 1, 5);
                configLoaded = true;
                
                DebugLog($"Intensidad de color cargada: {currentColorIntensity} (anterior: {previousIntensity})");
                
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
    
    private void NotifyColorIntensityChanged()
    {
        int listenersCount = OnColorIntensityChanged != null ? OnColorIntensityChanged.GetInvocationList().Length : 0;
        DebugLog($"<color=cyan>Notificando cambio de intensidad a {currentColorIntensity} a {listenersCount} listeners</color>");
        
        OnColorIntensityChanged?.Invoke(currentColorIntensity);
    }
    
    public void ForceRefresh()
    {
        DebugLog("Forzando actualización de colores...");
        if (DataManager.Instance != null)
        {
            LoadColorIntensity();
        }
        else
        {
            StartCoroutine(WaitForDataManagerAndLoad());
        }
    }
    
    public void UpdateColorIntensity(int intensity)
    {
        try
        {
            if (DataManager.Instance == null)
            {
                DebugLog("DataManager no disponible al actualizar intensidad de color", true);
                return;
            }
        
            string userId = DataManager.Instance.GetCurrentUserId();
            intensity = Mathf.Clamp(intensity, 1, 5);
        
            if (currentColorIntensity == intensity)
            {
                DebugLog($"La intensidad ya es {intensity}, no se realizan cambios");
                return;
            }
        
            DebugLog($"<color=yellow>Actualizando intensidad de color de {currentColorIntensity} a {intensity}</color>");
        
            currentColorIntensity = intensity;
        
            var config = SqliteDatabase.Instance.GetConfiguration(userId);
            if (config != null)
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, intensity, config.AutoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration);
            }
            else
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, intensity, false);
            }
        
            DebugLog($"Intensidad de color actualizada a {intensity} para usuario {userId}");
            NotifyColorIntensityChanged();
            configLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GlobalColorManager: Error al actualizar intensidad de color: {e.Message}");
        }
    }
    
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