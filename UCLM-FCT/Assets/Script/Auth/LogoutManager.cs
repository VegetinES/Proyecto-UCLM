using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogoutManager : MonoBehaviour
{
    public Button logoutButton;
    public string menuScene = "Menu";
    
    private void Start()
    {
        // Asignar el evento de click al botón
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutButtonClick);
        }
        else
        {
            Debug.LogWarning("LogoutManager: No se ha asignado el botón de cierre de sesión.");
        }
    }
    
    public async void OnLogoutButtonClick()
    {
        // Deshabilitar botón durante el proceso de cierre de sesión
        if (logoutButton != null)
            logoutButton.interactable = false;
            
        Debug.Log("Cerrando sesión...");
        
        // Verificar que el AuthManager esté disponible
        if (AuthManager.Instance == null)
        {
            Debug.LogError("LogoutManager: AuthManager no está disponible.");
            
            if (logoutButton != null)
                logoutButton.interactable = true;
                
            return;
        }
        
        // Llamar al método de cierre de sesión
        bool success = await AuthManager.Instance.LogoutUser();
        
        // Volver a habilitar el botón
        if (logoutButton != null)
            logoutButton.interactable = true;
            
        if (success)
        {
            Debug.Log("Sesión cerrada correctamente");
            
            // Actualizar la UI si hay un SessionUIManager disponible
            SessionUIManager sessionUI = FindObjectOfType<SessionUIManager>();
            if (sessionUI != null)
            {
                sessionUI.UpdateUI();
            }
            
            // Redirigir a la escena de login
            StartCoroutine(RedirectAfterDelay(menuScene, 1.0f));
        }
        else
        {
            Debug.LogError("Error al cerrar sesión");
        }
    }
    
    private IEnumerator RedirectAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
    
    private void OnDestroy()
    {
        // Desuscribir del evento para evitar memory leaks
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnLogoutButtonClick);
        }
    }
}