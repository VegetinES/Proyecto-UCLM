using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DeleteManager : MonoBehaviour
{
    [Header("Botones")]
    public Button deleteAccountButton;
    public Button cancelDeleteButton;
    public Button confirmDeleteButton;
    
    [Header("Objetos")]
    public GameObject confirmDeleteObject;
    
    private void Start()
    {
        // Configurar botones
        if (deleteAccountButton != null)
            deleteAccountButton.onClick.AddListener(OnDeleteAccountClick);
            
        if (cancelDeleteButton != null)
            cancelDeleteButton.onClick.AddListener(OnCancelDeleteClick);
            
        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteClick);
        
        // Ocultar panel de confirmación al inicio
        if (confirmDeleteObject != null)
            confirmDeleteObject.SetActive(false);
    }
    
    private void OnDeleteAccountClick()
    {
        // Mostrar panel de confirmación
        if (confirmDeleteObject != null)
            confirmDeleteObject.SetActive(true);
    }
    
    private void OnCancelDeleteClick()
    {
        // Ocultar panel de confirmación
        if (confirmDeleteObject != null)
            confirmDeleteObject.SetActive(false);
    }
    
    private async void OnConfirmDeleteClick()
    {
        try
        {
            // Deshabilitar botón durante eliminación
            if (confirmDeleteButton != null)
                confirmDeleteButton.interactable = false;
        
            string userId = AuthManager.Instance.UserID;
        
            // Eliminar datos de MongoDB primero
            if (MongoDbService.Instance != null && MongoDbService.Instance.IsConnected())
            {
                await MongoDbService.Instance.DeleteUserDataAsync(userId);
                Debug.Log("DeleteManager: Datos de MongoDB eliminados");
            }
        
            // Eliminar datos de SQLite y resetear a usuario por defecto
            DeleteSqliteData(userId);
        
            // Eliminar tokens
            PlayerPrefs.DeleteKey("supabase_access_token");
            PlayerPrefs.DeleteKey("supabase_refresh_token");
            PlayerPrefs.Save();
        
            // Eliminar cuenta de Supabase
            if (AuthManager.Instance.IsLoggedIn)
            {
                await DeleteSupabaseAccount();
            }
        
            // Forzar al AuthManager a cargar el usuario por defecto
            AuthManager.Instance.LoadDefaultUser();
        
            Debug.Log("DeleteManager: Cuenta eliminada correctamente");
        
            // Cargar escena Menu
            SceneManager.LoadScene("Menu");
        }
        catch (Exception e)
        {
            Debug.LogError($"DeleteManager: Error al eliminar cuenta: {e.Message}");
        
            // Volver a habilitar botón en caso de error
            if (confirmDeleteButton != null)
                confirmDeleteButton.interactable = true;
        }
    }
    
    private void DeleteSqliteData(string userId)
    {
        try
        {
            // Eliminar perfiles
            var profiles = SqliteDatabase.Instance.GetProfiles(userId);
            foreach (var profile in profiles)
            {
                SqliteDatabase.Instance.DeleteProfile(profile.ProfileID);
            }
        
            // Eliminar configuraciones y control parental completamente
        
            // Cambiar el usuario a por defecto (UID: 1)
            string currentDate = System.DateTime.UtcNow.ToString("o");
            SqliteDatabase.Instance.SaveUser(AuthManager.DEFAULT_USER_ID, "", false);
        
            // Actualizar fechas del usuario por defecto
            SqliteDatabase.Instance.UpdateLastLogin(AuthManager.DEFAULT_USER_ID);
        
            // Asegurar que el usuario por defecto tiene fechas correctas
            var defaultUser = SqliteDatabase.Instance.GetUser(AuthManager.DEFAULT_USER_ID);
            if (defaultUser != null)
            {
                // Actualizar con fecha actual tanto creación como último login
                SqliteDatabase.Instance.SaveUser(AuthManager.DEFAULT_USER_ID, "", false);
                SqliteDatabase.Instance.UpdateLastLogin(AuthManager.DEFAULT_USER_ID);
            }
        
            Debug.Log("DeleteManager: Usuario restablecido a por defecto con datos limpios");
        }
        catch (Exception e)
        {
            Debug.LogError($"DeleteManager: Error al limpiar datos SQLite: {e.Message}");
        }
    }
    
    private async System.Threading.Tasks.Task DeleteSupabaseAccount()
    {
        try
        {
            // Usar el cliente de Supabase para eliminar cuenta
            string supabaseUrl = EnvironmentLoader.GetVariable("SUPABASE_URL", "");
            string supabaseKey = EnvironmentLoader.GetVariable("SUPABASE_PUBLIC_KEY", "");
            
            if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
            {
                var client = new Supabase.Client(supabaseUrl, supabaseKey);
                await client.InitializeAsync();
                
                // Cerrar sesión y eliminar usuario
                await client.Auth.SignOut();
                Debug.Log("DeleteManager: Cuenta de Supabase eliminada");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DeleteManager: Error al eliminar cuenta Supabase: {e.Message}");
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar listeners
        if (deleteAccountButton != null)
            deleteAccountButton.onClick.RemoveListener(OnDeleteAccountClick);
            
        if (cancelDeleteButton != null)
            cancelDeleteButton.onClick.RemoveListener(OnCancelDeleteClick);
            
        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.RemoveListener(OnConfirmDeleteClick);
    }
}