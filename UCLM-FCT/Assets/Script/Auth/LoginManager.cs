using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Realms;

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
            
            // No hacer nada si es el usuario por defecto
            if (userId == AuthManager.DEFAULT_USER_ID)
                return;
                
            using (var realm = Realm.GetInstance(new RealmConfiguration { SchemaVersion = 1 }))
            {
                // Verificar si el usuario actual ya tiene control parental configurado
                var user = realm.Find<User>(userId);
                var defaultUser = realm.Find<User>(AuthManager.DEFAULT_USER_ID);
                
                if (user != null && defaultUser != null && 
                    defaultUser.ParentalControl != null && 
                    defaultUser.ParentalControl.Activated)
                {
                    // Verificar si el usuario actual no tiene control parental activado
                    if (user.ParentalControl == null || !user.ParentalControl.Activated || string.IsNullOrEmpty(user.ParentalControl.Pin))
                    {
                        realm.Write(() => {
                            // Si el usuario actual no tiene control parental o no tiene PIN, transferir la configuración
                            // del usuario por defecto
                            if (user.ParentalControl == null)
                            {
                                var newParentalControl = realm.Add(new ParentalControl {
                                    ID = userId,
                                    User = user,
                                    Activated = defaultUser.ParentalControl.Activated,
                                    Pin = defaultUser.ParentalControl.Pin,
                                    SoundConf = defaultUser.ParentalControl.SoundConf,
                                    AccessibilityConf = defaultUser.ParentalControl.AccessibilityConf,
                                    StatisticsConf = defaultUser.ParentalControl.StatisticsConf,
                                    AboutConf = defaultUser.ParentalControl.AboutConf,
                                    ProfileConf = defaultUser.ParentalControl.ProfileConf
                                });
                                
                                user.ParentalControl = newParentalControl;
                                Debug.Log("Control parental transferido del usuario por defecto al usuario actual");
                            }
                            else
                            {
                                // Actualizar la configuración del control parental existente
                                user.ParentalControl.Activated = defaultUser.ParentalControl.Activated;
                                user.ParentalControl.Pin = defaultUser.ParentalControl.Pin;
                                user.ParentalControl.SoundConf = defaultUser.ParentalControl.SoundConf;
                                user.ParentalControl.AccessibilityConf = defaultUser.ParentalControl.AccessibilityConf;
                                user.ParentalControl.StatisticsConf = defaultUser.ParentalControl.StatisticsConf;
                                user.ParentalControl.AboutConf = defaultUser.ParentalControl.AboutConf;
                                user.ParentalControl.ProfileConf = defaultUser.ParentalControl.ProfileConf;
                                
                                Debug.Log("Configuración de control parental actualizada con la del usuario por defecto");
                            }
                        });
                    }
                }
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