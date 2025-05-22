using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Supabase.Gotrue;

public class ChangePassword : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button changePasswordButton;
    
    [Header("Objetos de respuesta")]
    public GameObject successObject;
    public GameObject errorObject;
    
    private void Start()
    {
        // Configurar botón
        if (changePasswordButton != null)
            changePasswordButton.onClick.AddListener(OnChangePasswordClick);
        
        // Ocultar objetos al inicio
        if (successObject != null)
            successObject.SetActive(false);
            
        if (errorObject != null)
            errorObject.SetActive(false);
    }
    
    private async void OnChangePasswordClick()
    {
        // Ocultar mensajes anteriores
        if (successObject != null)
            successObject.SetActive(false);
            
        if (errorObject != null)
            errorObject.SetActive(false);
        
        // Verificar que los campos estén llenos
        if (newPasswordInput == null || confirmPasswordInput == null)
        {
            ShowError();
            return;
        }
        
        string newPassword = newPasswordInput.text;
        string confirmPassword = confirmPasswordInput.text;
        
        // Validar contraseñas
        if (!PasswordsMatch(newPassword, confirmPassword))
        {
            ShowError();
            return;
        }
        
        if (!IsValidPassword(newPassword))
        {
            ShowError();
            return;
        }
        
        // Deshabilitar botón durante el proceso
        if (changePasswordButton != null)
            changePasswordButton.interactable = false;
        
        try
        {
            // Cambiar contraseña en Supabase
            bool success = await ChangePasswordInSupabase(newPassword);
            
            if (success)
            {
                ShowSuccess();
                ClearInputs();
            }
            else
            {
                ShowError();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChangePassword: Error: {e.Message}");
            ShowError();
        }
        finally
        {
            // Volver a habilitar botón
            if (changePasswordButton != null)
                changePasswordButton.interactable = true;
        }
    }
    
    private bool PasswordsMatch(string password, string confirmPassword)
    {
        return password == confirmPassword && !string.IsNullOrEmpty(password);
    }
    
    private bool IsValidPassword(string password)
    {
        // Mínimo 6 caracteres
        if (password.Length < 6) return false;
        
        // Al menos una mayúscula
        if (!Regex.IsMatch(password, @"[A-Z]")) return false;
        
        // Al menos una minúscula
        if (!Regex.IsMatch(password, @"[a-z]")) return false;
        
        // Al menos un número
        if (!Regex.IsMatch(password, @"[0-9]")) return false;
        
        // Sin espacios
        if (password.Contains(" ")) return false;
        
        return true;
    }
    
    private async System.Threading.Tasks.Task<bool> ChangePasswordInSupabase(string newPassword)
    {
        try
        {
            // Verificar que el usuario esté logueado
            if (!AuthManager.Instance.IsLoggedIn)
            {
                Debug.LogError("ChangePassword: Usuario no está logueado");
                return false;
            }
        
            // Obtener variables de entorno
            string supabaseUrl = EnvironmentLoader.GetVariable("SUPABASE_URL", "");
            string supabaseKey = EnvironmentLoader.GetVariable("SUPABASE_PUBLIC_KEY", "");
        
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                Debug.LogError("ChangePassword: No se pudieron cargar variables de entorno");
                return false;
            }
        
            // Crear cliente Supabase
            var client = new Supabase.Client(supabaseUrl, supabaseKey);
            await client.InitializeAsync();
        
            // Cambiar contraseña usando UserAttributes
            var attrs = new UserAttributes { Password = newPassword };
            var response = await client.Auth.Update(attrs);
        
            return response != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"ChangePassword: Error en Supabase: {e.Message}");
            return false;
        }
    }
    
    private void ShowSuccess()
    {
        if (successObject != null)
            successObject.SetActive(true);
    }
    
    private void ShowError()
    {
        if (errorObject != null)
            errorObject.SetActive(true);
    }
    
    private void ClearInputs()
    {
        if (newPasswordInput != null)
            newPasswordInput.text = "";
            
        if (confirmPasswordInput != null)
            confirmPasswordInput.text = "";
    }
    
    private void OnDestroy()
    {
        if (changePasswordButton != null)
            changePasswordButton.onClick.RemoveListener(OnChangePasswordClick);
    }
}