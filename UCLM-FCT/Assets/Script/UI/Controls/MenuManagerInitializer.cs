using UnityEngine;
using System.Collections;

public class MenuManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(InitializeManagers());
    }
    
    private IEnumerator InitializeManagers()
    {
        Debug.Log("MenuManagerInitializer: Iniciando secuencia de inicialización...");
        
        // Primero verificar y crear AuthManager si es necesario
        if (AuthManager.Instance == null)
        {
            Debug.Log("MenuManagerInitializer: Creando AuthManager...");
            GameObject authManager = new GameObject("AuthManager");
            authManager.AddComponent<AuthManager>();
            DontDestroyOnLoad(authManager);
            
            // Esperar a que se inicialice
            yield return new WaitForSeconds(0.5f);
        }
        
        // Luego verificar y crear DataManager si es necesario
        if (DataManager.Instance == null)
        {
            Debug.Log("MenuManagerInitializer: Creando DataManager...");
            GameObject dataManager = new GameObject("DataManager");
            dataManager.AddComponent<DataManager>();
            DontDestroyOnLoad(dataManager);
            
            // Esperar a que se inicialice
            yield return new WaitForSeconds(0.5f);
        }
        
        // Asegurarse de que exista ProfileManager
        if (ProfileManager.Instance == null)
        {
            Debug.Log("MenuManagerInitializer: Creando ProfileManager...");
            GameObject profileManager = new GameObject("ProfileManager");
            profileManager.AddComponent<ProfileManager>();
            DontDestroyOnLoad(profileManager);
        }
        
        // Esperar a que todos los managers estén listos
        float timeElapsed = 0f;
        float maxWaitTime = 8f;
        
        while ((AuthManager.Instance == null || !AuthManager.Instance.IsInitialized() || 
                DataManager.Instance == null) && timeElapsed < maxWaitTime)
        {
            timeElapsed += 0.2f;
            Debug.Log($"MenuManagerInitializer: Esperando a managers... {timeElapsed}s / {maxWaitTime}s");
            yield return new WaitForSeconds(0.2f);
        }
        
        if (timeElapsed >= maxWaitTime)
        {
            Debug.LogError("MenuManagerInitializer: Tiempo de espera agotado. No se pudieron inicializar los managers.");
            yield break;
        }
        
        Debug.Log("MenuManagerInitializer: Managers inicializados correctamente.");
    }
}