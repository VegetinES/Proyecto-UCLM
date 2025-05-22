using UnityEngine;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }
    
    private int currentProfileId = 0;
    private string currentProfileName = "";
    private bool isUsingProfile = false;
    private string ownerUserId = ""; // Nuevo campo para almacenar el ID del usuario propietario
    
    [Header("Depuración")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Cargar perfil de PlayerPrefs si existe
            LoadSavedProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadSavedProfile()
    {
        // Cargar datos guardados
        currentProfileId = PlayerPrefs.GetInt("CurrentProfileId", 0);
        currentProfileName = PlayerPrefs.GetString("CurrentProfileName", "");
        ownerUserId = PlayerPrefs.GetString("ProfileOwnerUserId", "");
        
        // Solo considerar que está usando un perfil si hay un ID válido
        isUsingProfile = currentProfileId > 0;
        
        if (isUsingProfile)
        {
            DebugLog($"Perfil cargado - ID: {currentProfileId}, Nombre: {currentProfileName}, Propietario: {ownerUserId}");
            
            // Verificar que el perfil pertenece al usuario actual
            ValidateProfile();
        }
    }
    
    private void ValidateProfile()
    {
        // Si no hay perfil activo, no hay nada que validar
        if (!isUsingProfile) return;
        
        // Obtener el ID de usuario actual
        string currentUserId = GetCurrentUserId();
        
        // Si el usuario propietario no coincide con el usuario actual, invalidar el perfil
        if (currentUserId != ownerUserId)
        {
            DebugLog($"Perfil {currentProfileId} no pertenece al usuario actual ({currentUserId} vs {ownerUserId}). Limpiando perfil.", true);
            ClearCurrentProfile();
            return;
        }
        
        // Verificar que el perfil existe en la base de datos
        bool profileExists = CheckProfileExists(currentProfileId, currentUserId);
        
        if (!profileExists)
        {
            DebugLog($"Perfil {currentProfileId} no encontrado para el usuario {currentUserId}. Limpiando perfil.", true);
            ClearCurrentProfile();
        }
    }
    
    private bool CheckProfileExists(int profileId, string userId)
    {
        try
        {
            if (SqliteDatabase.Instance == null)
            {
                DebugLog("No se puede verificar el perfil: SqliteDatabase no disponible", true);
                return false;
            }
            
            var profile = SqliteDatabase.Instance.GetProfileById(profileId);
            
            // Perfil existe y pertenece al usuario actual
            return profile != null && profile.UserID == userId;
        }
        catch (System.Exception e)
        {
            DebugLog($"Error al verificar existencia del perfil: {e.Message}", true);
            return false;
        }
    }
    
    public void SetCurrentProfile(int profileId, string profileName)
    {
        // Si el ID es 0 o negativo, limpiar el perfil
        if (profileId <= 0)
        {
            ClearCurrentProfile();
            return;
        }
        
        string userId = GetCurrentUserId();
        
        currentProfileId = profileId;
        currentProfileName = profileName;
        ownerUserId = userId;
        isUsingProfile = true;
        
        // Guardar en PlayerPrefs
        PlayerPrefs.SetInt("CurrentProfileId", profileId);
        PlayerPrefs.SetString("CurrentProfileName", profileName);
        PlayerPrefs.SetString("ProfileOwnerUserId", userId);
        PlayerPrefs.Save();
        
        DebugLog($"Perfil establecido - ID: {profileId}, Nombre: {profileName}, Usuario: {userId}");
    }
    
    public void ClearCurrentProfile()
    {
        currentProfileId = 0;
        currentProfileName = "";
        ownerUserId = "";
        isUsingProfile = false;
        
        // Limpiar PlayerPrefs
        PlayerPrefs.DeleteKey("CurrentProfileId");
        PlayerPrefs.DeleteKey("CurrentProfileName");
        PlayerPrefs.DeleteKey("ProfileOwnerUserId");
        PlayerPrefs.Save();
        
        DebugLog("Perfil limpiado");
    }
    
    public int GetCurrentProfileId()
    {
        return currentProfileId;
    }
    
    public string GetCurrentProfileName()
    {
        return currentProfileName;
    }
    
    public string GetOwnerUserId()
    {
        return ownerUserId;
    }
    
    public bool IsUsingProfile()
    {
        return isUsingProfile;
    }
    
    public void CreateProfile(string name, string gender)
    {
        string userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId) || userId == AuthManager.DEFAULT_USER_ID)
        {
            DebugLog("No se puede crear un perfil: Usuario no válido", true);
            return;
        }
        
        if (SqliteDatabase.Instance == null)
        {
            DebugLog("No se puede crear un perfil: SqliteDatabase no disponible", true);
            return;
        }
        
        int profileId = SqliteDatabase.Instance.SaveProfile(userId, name, gender);
        
        DebugLog($"Perfil creado - ID: {profileId}, Nombre: {name}, Género: {gender}, Usuario: {userId}");
    }
    
    public void DeleteLastProfile()
    {
        string userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId) || userId == AuthManager.DEFAULT_USER_ID)
        {
            DebugLog("No se pueden eliminar perfiles: Usuario no válido", true);
            return;
        }
        
        if (SqliteDatabase.Instance == null)
        {
            DebugLog("No se pueden eliminar perfiles: SqliteDatabase no disponible", true);
            return;
        }
        
        var profiles = SqliteDatabase.Instance.GetProfiles(userId);
        
        if (profiles.Count > 0)
        {
            var lastProfile = profiles[profiles.Count - 1];
            SqliteDatabase.Instance.DeleteProfile(lastProfile.ProfileID);
            
            DebugLog($"Perfil eliminado - ID: {lastProfile.ProfileID}, Nombre: {lastProfile.Name}");
            
            // Si el perfil eliminado era el actual, limpiar perfil actual
            if (lastProfile.ProfileID == currentProfileId)
                ClearCurrentProfile();
        }
        else
        {
            DebugLog("No hay perfiles para eliminar", true);
        }
    }
    
    private string GetCurrentUserId()
    {
        if (AuthManager.Instance == null)
        {
            DebugLog("AuthManager no disponible", true);
            return AuthManager.DEFAULT_USER_ID;
        }
        return AuthManager.Instance.UserID;
    }
    
    private void DebugLog(string message, bool isWarning = false)
    {
        if (!enableDebugLogs) return;
        
        if (isWarning)
            Debug.LogWarning($"[ProfileManager] {message}");
        else
            Debug.Log($"[ProfileManager] {message}");
    }
}