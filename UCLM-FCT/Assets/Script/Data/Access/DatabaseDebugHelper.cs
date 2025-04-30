using UnityEngine;
using System.Collections;

public class DatabaseDebugHelper : MonoBehaviour
{
    [SerializeField] private float startupDelay = 1.0f;
    
    private void Start()
    {
        StartCoroutine(VerifyDatabaseWithDelay());
    }
    
    private IEnumerator VerifyDatabaseWithDelay()
    {
        // Esperar para dar tiempo a otros sistemas a inicializarse
        yield return new WaitForSeconds(startupDelay);
        
        VerifyDatabase();
    }

    public void VerifyDatabase()
    {
        Debug.Log("DatabaseDebugHelper: Verificando estado de la base de datos...");
        
        if (DataService.Instance == null)
        {
            Debug.LogError("DatabaseDebugHelper: DataService no disponible");
            return;
        }
        
        bool isValid = DataService.Instance.VerifyDatabase();
        
        if (!isValid)
        {
            Debug.LogWarning("DatabaseDebugHelper: La base de datos no pasó la verificación. Creando usuario por defecto...");
            
            var userId = AuthManager.Instance != null ? 
                AuthManager.Instance.UserID : AuthManager.DEFAULT_USER_ID;
                
            DataService.Instance.EnsureUserExists(userId);
            
            // Verificar nuevamente
            isValid = DataService.Instance.VerifyDatabase();
            
            if (!isValid)
            {
                Debug.LogError("DatabaseDebugHelper: La base de datos sigue sin ser válida después de intentar repararla");
            }
            else
            {
                Debug.Log("DatabaseDebugHelper: Base de datos reparada correctamente");
            }
        }
        else
        {
            Debug.Log("DatabaseDebugHelper: Base de datos verificada correctamente");
        }
    }
}