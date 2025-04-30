using UnityEngine;
using System.Collections;

public class DataServiceInitializer : MonoBehaviour
{
    private static bool initialized = false;
    
    [SerializeField] private float initDelay = 0.5f;
    
    private void Awake()
    {
        if (!initialized)
        {
            Debug.Log("DataServiceInitializer: Iniciando servicio de datos...");
            StartCoroutine(InitializeWithDelay());
            initialized = true;
        }
    }
    
    private IEnumerator InitializeWithDelay()
    {
        // Pequeño retraso para asegurar que otros sistemas estén inicializados
        yield return new WaitForSeconds(initDelay);
        
        Debug.Log("DataServiceInitializer: Inicializando DataService");
        
        // Esto inicializará el singleton de DataService
        var service = DataService.Instance;
        service.ForceInitialize();
        
        // Asegurar que existe el usuario por defecto
        string defaultUserId = AuthManager.DEFAULT_USER_ID;
        service.EnsureUserExists(defaultUserId);
        
        // Verificar que la base de datos está en buen estado
        bool isValid = service.VerifyDatabase();
        if (!isValid)
        {
            Debug.LogWarning("DataServiceInitializer: La base de datos no pasó la verificación. Recreando datos esenciales...");
            service.ForceInitialize();
        }
        
        Debug.Log("DataServiceInitializer: Servicio de datos inicializado correctamente");
    }
}