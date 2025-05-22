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
        ColorAdapter[] adapters = FindObjectsByType<ColorAdapter>(FindObjectsSortMode.None);
        
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
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorAdapterInitializer] {message}");
        }
    }
}