using System.Collections;
using UnityEngine;

public class AuthInitializer : MonoBehaviour
{
    [SerializeField] private float initDelay = 1.0f;
    
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(InitializeManagers());
    }
    
    private IEnumerator InitializeManagers()
    {
        // Asegurarte de que el AuthManager existe
        if (FindFirstObjectByType<AuthManager>() == null)
        {
            GameObject authObj = new GameObject("AuthManager");
            authObj.AddComponent<AuthManager>();
            DontDestroyOnLoad(authObj);
            Debug.Log("AuthInitializer: AuthManager creado");
        }
        
        // Dar tiempo a que se inicialice
        yield return new WaitForSeconds(initDelay);
        
        // Notificar a todos los SessionUIManager que actualicen su estado
        var sessionManagers = FindObjectsByType<SessionUIManager>(FindObjectsSortMode.None);
        foreach (var manager in sessionManagers)
        {
            manager.UpdateUI();
        }
        
        Debug.Log("AuthInitializer: Inicialización completa");
    }
}