using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField email;
    public TMP_InputField password;
    public Button loginButton;
    public GameObject errorMessage;
    public string menuScene = "Menu";
    
    [Header("Opciones adicionales")]
    [Tooltip("Si es true, transferirá la configuración de control parental del usuario por defecto al nuevo usuario")]
    public bool transferParentalControl = true;
    
    private void Start()
    {
        // Ocultar mensaje de error al inicio
        if (errorMessage != null) 
            errorMessage.SetActive(false);
    }
    
    public async void OnLoginButtonClick()
    {
        if (string.IsNullOrEmpty(email.text) || string.IsNullOrEmpty(password.text))
        {
            if (errorMessage != null)
                errorMessage.SetActive(true);
            return;
        }
    
        // Deshabilitar botón durante el login
        if (loginButton != null)
            loginButton.interactable = false;
        
        Debug.Log("Iniciando sesión...");
    
        bool success = await AuthManager.Instance.LoginUser(email.text, password.text);
    
        // Volver a habilitar el botón
        if (loginButton != null)
            loginButton.interactable = true;
        
        if (success)
        {
            Debug.Log("Sesión iniciada correctamente");
        
            // Registrar inicio de sesión en MongoDB
            if (DataManager.Instance != null)
            {
                string userId = AuthManager.Instance.UserID;
                string userEmail = AuthManager.Instance.UserEmail;
            
                // Actualizar fecha de último inicio de sesión en SQLite
                SqliteDatabase.Instance.UpdateLastLogin(userId);
            
                // Registrar inicio de sesión en MongoDB
                await MongoDbService.Instance.SaveLoginAsync(userId, userEmail);
            }
        
            // Transferir configuración de control parental si está habilitado
            if (transferParentalControl)
            {
                TransferParentalControlIfNeeded();
            }
        
            // Redirigir al menú principal
            StartCoroutine(RedirectAfterDelay(menuScene, 1.0f));
        }
        else
        {
            Debug.Log("Error al iniciar sesión");
            if (errorMessage != null)
                errorMessage.SetActive(true);
        }
    }
    
    private void TransferParentalControlIfNeeded()
    {
        try
        {
            string userId = AuthManager.Instance.UserID;
        
            if (userId == AuthManager.DEFAULT_USER_ID)
                return;
        
            var defaultParental = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
            var userParental = SqliteDatabase.Instance.GetParentalControl(userId);
        
            if (defaultParental != null && defaultParental.Activated && 
                (userParental == null || !userParental.Activated || string.IsNullOrEmpty(userParental.Pin)))
            {
                SqliteDatabase.Instance.SaveParentalControl(
                    userId,
                    defaultParental.Activated,
                    defaultParental.Pin,
                    defaultParental.SoundConf,
                    defaultParental.AccessibilityConf,
                    defaultParental.StatisticsConf,
                    defaultParental.AboutConf,
                    defaultParental.ProfileConf
                );
            
                Debug.Log("Control parental transferido del usuario por defecto al usuario actual");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al transferir configuración de control parental: " + e.Message);
        }
    }
    
    private IEnumerator RedirectAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}