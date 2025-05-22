using UnityEngine;
using System.Collections;

public class InitializationManager : MonoBehaviour
{
    public static InitializationManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Inicializar el cargador de variables de entorno primero
            EnvironmentLoader.Initialize();
            Debug.Log("InitializationManager: EnvironmentLoader inicializado");
            
            StartCoroutine(InitializeInSequence());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator InitializeInSequence()
    {
        Debug.Log("InitializationManager: Iniciando secuencia de inicialización...");
        
        // 1. Primero DataManager
        if (DataManager.Instance == null)
        {
            GameObject dataManagerObj = new GameObject("DataManager");
            dataManagerObj.AddComponent<DataManager>();
            Debug.Log("InitializationManager: DataManager creado");
        }
        
        // Esperar a que DataManager esté listo
        while (DataManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Esperar un poco más para asegurar la inicialización completa
        yield return new WaitForSeconds(0.5f);
        
        // 2. Luego GlobalColorManager
        if (GlobalColorManager.Instance == null)
        {
            GameObject colorManagerObj = new GameObject("GlobalColorManager");
            colorManagerObj.AddComponent<GlobalColorManager>();
            Debug.Log("InitializationManager: GlobalColorManager creado");
        }
        
        // 3. Finalmente GlobalSoundManager
        if (GlobalSoundManager.Instance == null)
        {
            GameObject soundManagerObj = new GameObject("GlobalSoundManager");
            soundManagerObj.AddComponent<GlobalSoundManager>();
            Debug.Log("InitializationManager: GlobalSoundManager creado");
        }
        
        Debug.Log("InitializationManager: Todos los managers han sido inicializados en secuencia");
    }
}