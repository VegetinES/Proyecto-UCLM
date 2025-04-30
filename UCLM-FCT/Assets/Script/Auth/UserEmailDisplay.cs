using UnityEngine;
using TMPro;

public class UserEmailDisplay : MonoBehaviour
{
    [SerializeField] private TMP_InputField emailInputField;

    private void OnEnable()
    {
        // Verificar que el input field está asignado
        if (emailInputField == null) // Se verifica que el input field está asignado en el componente
        {
            Debug.LogError("El TMP_InputField no está asignado en el componente UserEmailDisplay");
            return;
        }

        // Se verifica que AuthManager está disponible
        if (AuthManager.Instance == null)
        {
            Debug.LogWarning("AuthManager no está disponible");
            return;
        }

        // Se muestra el email del usuario
        DisplayUserEmail();
    }

    private void DisplayUserEmail()
    {
        // Obtener el email del usuario del AuthManager
        string userEmail = AuthManager.Instance.UserEmail;
        
        // Establecer el texto del input field
        emailInputField.text = userEmail;
        
        Debug.Log($"Email del usuario mostrado: {userEmail}");
    }
}