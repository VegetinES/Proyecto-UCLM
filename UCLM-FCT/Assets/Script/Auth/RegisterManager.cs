using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Supabase;
using Supabase.Gotrue;
using Realms;
using Client = Supabase.Client;

public class RegisterManager : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Toggle isTutorToggle;
    public Button registerButton;
    
    [Header("Mensajes de error")]
    public GameObject errorPanel;
    public TMP_Text errorMessageText;
    
    [Header("Navegación")]
    public string menuScene = "Menu";
    
    [Header("Objetos UI")]
    public GameObject tutorObject;
    public GameObject noTutorObject;
    
    private void Start()
    {
        // Ocultar mensaje de error al inicio
        if (errorPanel != null) 
            errorPanel.SetActive(false);
            
        // Ocultar objetos de navegación
        if (tutorObject != null)
            tutorObject.SetActive(false);
            
        if (noTutorObject != null)
            noTutorObject.SetActive(false);
    }
    
    public async void OnRegisterButtonClick()
    {
        // Deshabilitar botón durante el registro
        if (registerButton != null)
            registerButton.interactable = false;
            
        // Ocultar mensaje de error
        if (errorPanel != null)
            errorPanel.SetActive(false);
            
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;
        bool isTutor = isTutorToggle.isOn;
        
        // Validar email
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowError("El correo electrónico no es válido");
            return;
        }
        
        // Verificar si el email ya existe
        bool emailExists = await CheckIfEmailExists(email);
        if (emailExists)
        {
            ShowError("El correo electrónico ya está registrado");
            return;
        }
        
        // Validar contraseña
        if (!PasswordsMatch(password, confirmPassword))
        {
            ShowError("Las contraseñas no coinciden");
            return;
        }
        
        if (!IsValidPassword(password))
        {
            ShowError("La contraseña debe tener al menos 6 caracteres, una mayúscula, una minúscula y un número");
            return;
        }
        
        // Intentar registrar al usuario
        bool success = await RegisterUser(email, confirmPassword, isTutor);
        
        if (success)
        {
            Debug.Log("Usuario registrado correctamente");
            
            // Activar objeto correspondiente según el tipo de usuario
            if (isTutor)
            {
                if (tutorObject != null)
                    tutorObject.SetActive(true);
            }
            else
            {
                if (noTutorObject != null)
                    noTutorObject.SetActive(true);
            }
            
            // Redirigir al menú principal
            StartCoroutine(RedirectAfterDelay(menuScene, 2.0f));
        }
        else
        {
            ShowError("Error al registrar el usuario. Inténtalo de nuevo.");
        }
    }
    
    private bool IsValidEmail(string email)
    {
        // Usar una expresión regular para validar el formato del email
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
    
    private bool PasswordsMatch(string password, string confirmPassword)
    {
        return password == confirmPassword;
    }
    
    private bool IsValidPassword(string password)
    {
        // Verificar longitud mínima
        if (password.Length < 6)
            return false;
            
        // Verificar si contiene al menos una mayúscula
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;
            
        // Verificar si contiene al menos una minúscula
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;
            
        // Verificar si contiene al menos un número
        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;
            
        // Verificar si contiene espacios
        if (password.Contains(" "))
            return false;
            
        return true;
    }
    
    private async System.Threading.Tasks.Task<bool> CheckIfEmailExists(string email)
    {
        try
        {
            var client = new Client(AuthManager.SUPABASE_URL, AuthManager.SUPABASE_PUBLIC_KEY);
            await client.InitializeAsync();
            
            try {
                // Intenta iniciar sesión con el email para verificar si existe
                // Usamos un método alternativo si SignInWithOtp no funciona
                await client.Auth.ResetPasswordForEmail(email);
                return true; // Si llega aquí, el email existe
            }
            catch (Exception e)
            {
                Debug.Log($"Respuesta al verificar email: {e.Message}");
                
                // Analizar el mensaje de error
                if (e.Message.Contains("User not found") || e.Message.Contains("Email not found") || e.Message.Contains("Invalid login credentials"))
                    return false; // Email no existe
                    
                return true; // Para otros errores, asumimos que el email existe
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error general al verificar email: {e.Message}");
            return false; // En caso de error general, permitimos continuar
        }
    }
    
    private async System.Threading.Tasks.Task<bool> RegisterUser(string email, string password, bool isTutor)
    {
        try
        {
            var client = new Client(AuthManager.SUPABASE_URL, AuthManager.SUPABASE_PUBLIC_KEY);
            await client.InitializeAsync();
            
            // Registrar usuario en Supabase
            var response = await client.Auth.SignUp(email, password);
            
            if (response?.User != null)
            {
                string userId = response.User.Id;
                Debug.Log($"Usuario creado en Supabase con ID: {userId}");
                
                // Iniciar sesión con el usuario recién creado
                bool loginSuccess = await AuthManager.Instance.LoginUser(email, password);
                
                if (loginSuccess)
                {
                    // Actualizar propiedad de tutor en una nueva transacción
                    Debug.Log($"Inicio de sesión exitoso, actualizando como tutor: {isTutor}");
                    UpdateUserAsTutor(userId, isTutor);
                    return true;
                }
                else
                {
                    Debug.LogError("Error al iniciar sesión después del registro");
                }
            }
            else
            {
                Debug.LogError("La respuesta de Supabase no contiene un usuario válido");
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al registrar usuario: {e.Message}");
            ShowError($"Error: {e.Message}");
            return false;
        }
        finally
        {
            // Volver a habilitar el botón
            if (registerButton != null)
                registerButton.interactable = true;
        }
    }
    
    private void UpdateUserAsTutor(string userId, bool isTutor)
    {
        try
        {
            // Usar un bloque using para asegurar que Realm se cierre correctamente
            using (var realm = Realm.GetInstance(new RealmConfiguration { SchemaVersion = 1 }))
            {
                var user = realm.Find<User>(userId);
                
                if (user != null)
                {
                    realm.Write(() => {
                        user.IsTutor = isTutor;
                        Debug.Log($"Usuario {userId} actualizado como tutor: {isTutor}");
                    });
                }
                else
                {
                    Debug.LogError($"No se encontró el usuario con ID: {userId}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al actualizar el estado de tutor: {e.Message}");
        }
    }
    
    private void ShowError(string message)
    {
        if (errorPanel != null)
            errorPanel.SetActive(true);
            
        if (errorMessageText != null)
            errorMessageText.text = message;
            
        Debug.LogWarning(message);
        
        // Volver a habilitar el botón
        if (registerButton != null)
            registerButton.interactable = true;
    }
    
    private IEnumerator RedirectAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}