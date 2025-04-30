using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionUIManager : MonoBehaviour
{
    // GameObject que se muestra cuando el usuario ha iniciado sesión
    public GameObject LoggedIn;
    
    // GameObject que se muestra cuando NO hay sesión iniciada
    public GameObject NotLoggedIn;
    
    // Escena de login
    public string loginScene = "Login";
    
    [SerializeField] private float maxWaitTime = 5.0f;
    
    private void Start()
    {
        // Esperar a que AuthManager esté inicializado
        StartCoroutine(CheckAuthState());
    }
    
    private IEnumerator CheckAuthState()
    {
        Debug.Log("SessionUIManager: Esperando a que AuthManager esté disponible...");
        
        // Esperar a que AuthManager esté disponible
        float elapsed = 0f;
        
        while ((AuthManager.Instance == null || !AuthManager.Instance.IsInitialized()) && elapsed < maxWaitTime)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (AuthManager.Instance == null)
        {
            Debug.LogError("AuthManager no disponible después del tiempo de espera");
            
            // Modo sin sesión por defecto
            if (LoggedIn != null) LoggedIn.SetActive(false);
            if (NotLoggedIn != null) NotLoggedIn.SetActive(true);
            
            yield break;
        }
        
        // Validar el estado actual
        AuthManager.Instance.ValidateCurrentState();
        
        // Actualizar UI según estado de sesión
        UpdateUI();
        
        Debug.Log("SessionUIManager: UI actualizada correctamente");
    }
    
    public void UpdateUI()
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;
        
        // Mostrar/ocultar elementos según estado de sesión
        if (LoggedIn != null) LoggedIn.SetActive(isLoggedIn);
        if (NotLoggedIn != null) NotLoggedIn.SetActive(!isLoggedIn);
        
        Debug.Log("Estado de sesión: " + (isLoggedIn ? "Con sesión" : "Sin sesión"));
        
        // Asegurar que la base de datos está en buen estado
        if (DataService.Instance != null)
        {
            if (!DataService.Instance.VerifyDatabase())
            {
                Debug.LogWarning("Base de datos no válida después de actualizar UI. Forzando inicialización...");
                DataService.Instance.ForceInitialize();
            }
        }
    }
    
    public void GoToLogin()
    {
        SceneManager.LoadScene(loginScene);
    }
}