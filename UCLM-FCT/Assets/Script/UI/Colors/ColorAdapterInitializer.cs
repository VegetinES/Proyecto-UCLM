using System.Collections;
using UnityEngine;

// Este componente inicializa los adaptadores de color en la escena.
public class ColorAdapterInitializer : MonoBehaviour
{
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private float initializationDelay = 0.2f;
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        if (initializeOnStart)
        {
            StartCoroutine(InitializeWithDelay());
        }
    }
    
    private IEnumerator InitializeWithDelay()
    {
        yield return new WaitForSeconds(initializationDelay);
        InitializeColorAdapters();
    }
    
    public void InitializeColorAdapters()
    {
        // Asegurar que existe un GlobalColorManager
        EnsureGlobalColorManager();
        
        // Inicializar todos los adaptadores de color en la escena
        ColorAdapter[] adapters = FindObjectsOfType<ColorAdapter>();
        
        DebugLog($"Inicializando {adapters.Length} adaptadores de color");
        
        foreach (var adapter in adapters)
        {
            adapter.Initialize();
        }
    }
    
    private void EnsureGlobalColorManager()
    {
        if (GlobalColorManager.Instance == null)
        {
            DebugLog("Creando nueva instancia de GlobalColorManager");
            GameObject managerObj = new GameObject("GlobalColorManager");
            managerObj.AddComponent<GlobalColorManager>();
        }
        else
        {
            DebugLog("GlobalColorManager ya existe");
        }
    }
    
    // Método público para forzar la inicialización desde otro script
    public void ForceInitialization()
    {
        DebugLog("<color=green>Forzando inicialización de adaptadores de color</color>");
        
        // Forzar al GlobalColorManager a cargar la intensidad actual
        if (GlobalColorManager.Instance != null)
        {
            GlobalColorManager.Instance.ForceRefresh();
        }
        
        // Forzar una actualización en todos los adaptadores
        ColorAdapter[] adapters = FindObjectsOfType<ColorAdapter>();
        foreach (var adapter in adapters)
        {
            adapter.ForceRefresh();
        }
        
        DebugLog($"Actualización forzada completada en {adapters.Length} adaptadores");
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorAdapterInitializer] {message}");
        }
    }
}