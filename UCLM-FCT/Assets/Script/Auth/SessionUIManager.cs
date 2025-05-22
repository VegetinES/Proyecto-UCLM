using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionUIManager : MonoBehaviour
{
    // GameObjects para diferentes estados de inicio de sesión
    public GameObject TutorLoggedIn;      // Para tutores con sesión iniciada
    public GameObject ProfileLoggedIn;     // Para perfiles con sesión iniciada
    public GameObject UserLoggedIn;        // Para usuarios normales con sesión iniciada
    public GameObject NotLoggedIn;         // Para cuando no hay sesión iniciada
    
    [SerializeField] private float maxWaitTime = 10.0f; // Aumentar tiempo de espera
    
    private void Start()
    {
        // Estado por defecto - Sin sesión
        SetAllInactive();
        if (NotLoggedIn != null) NotLoggedIn.SetActive(true);
        
        // Esperar a que AuthManager esté inicializado
        StartCoroutine(CheckAuthState());
    }
    
    private IEnumerator CheckAuthState()
    {
        Debug.Log("SessionUIManager: Esperando a que AuthManager esté disponible...");
        
        // Esperar a que AuthManager esté disponible
        float elapsed = 0f;
        
        while (elapsed < maxWaitTime)
        {
            if (AuthManager.Instance != null && AuthManager.Instance.IsInitialized())
            {
                Debug.Log("SessionUIManager: AuthManager encontrado y inicializado");
                
                // Validar el estado actual
                AuthManager.Instance.ValidateCurrentState();
                
                // Actualizar UI según estado de sesión
                UpdateUI();
                
                Debug.Log("SessionUIManager: UI actualizada correctamente");
                yield break;
            }
            
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogError("AuthManager no disponible después del tiempo de espera");
            
        // Modo sin sesión por defecto (ya establecido en Start)
        Debug.Log("SessionUIManager: Usando modo sin sesión por defecto");
    }
    
    public void UpdateUI()
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;
    
        if (!isLoggedIn)
        {
            // Si no hay sesión iniciada
            SetAllInactive();
            if (NotLoggedIn != null) NotLoggedIn.SetActive(true);
            Debug.Log("Estado de sesión: Sin sesión");
            return;
        }
        
        // Hay sesión iniciada, determinar el tipo de usuario
        string userId = AuthManager.Instance.UserID;
        var user = SqliteDatabase.Instance.GetUser(userId);
        
        if (user != null && user.IsTutor)
        {
            // Es un tutor
            SetAllInactive();
            if (TutorLoggedIn != null) TutorLoggedIn.SetActive(true);
            Debug.Log("Estado de sesión: Tutor con sesión");
        }
        else if (ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile())
        {
            // Es un perfil
            SetAllInactive();
            if (ProfileLoggedIn != null) ProfileLoggedIn.SetActive(true);
            Debug.Log("Estado de sesión: Perfil con sesión");
        }
        else
        {
            // Es un usuario normal
            SetAllInactive();
            if (UserLoggedIn != null) UserLoggedIn.SetActive(true);
            Debug.Log("Estado de sesión: Usuario con sesión");
        }
    
        // Asegurar que el DataManager está disponible
        if (DataManager.Instance != null)
        {
            Debug.Log("SessionUIManager: DataManager está disponible");
        }
    }
    
    // Método auxiliar para desactivar todos los objetos
    private void SetAllInactive()
    {
        if (TutorLoggedIn != null) TutorLoggedIn.SetActive(false);
        if (ProfileLoggedIn != null) ProfileLoggedIn.SetActive(false);
        if (UserLoggedIn != null) UserLoggedIn.SetActive(false);
        if (NotLoggedIn != null) NotLoggedIn.SetActive(false);
    }
    
    public void GoToLogin()
    {
        SceneManager.LoadScene("Login");
    }
}