using System;
using System.Linq;
using Realms;
using UnityEngine;

// Servicio principal para acceder a todos los repositorios
public class DataService
{
    private static DataService _instance;
    private bool _initialized = false;
    
    public static DataService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new DataService();
            }
            return _instance;
        }
    }
    
    // Repositorios
    public UserRepository UserRepo { get; private set; }
    public ConfigurationRepository ConfigRepo { get; private set; }
    public ParentalControlRepository ParentalRepo { get; private set; }
    public StatisticsRepository StatsRepo { get; private set; }
    
    private DataService()
    {
        InitializeRepositories();
    }
    
    private void InitializeRepositories()
    {
        try
        {
            UserRepo = new UserRepository();
            ConfigRepo = new ConfigurationRepository();
            ParentalRepo = new ParentalControlRepository();
            StatsRepo = new StatisticsRepository();
            _initialized = true;
            Debug.Log("DataService: Repositorios inicializados correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al inicializar repositorios: {e.Message}");
        }
    }
    
    // Método auxiliar para obtener el ID del usuario actual
    public string GetCurrentUserId()
    {
        if (AuthManager.Instance == null)
            return AuthManager.DEFAULT_USER_ID;
            
        return AuthManager.Instance.UserID;
    }
    
    // Métodos combinados para inicialización de usuarios nuevos
    public void EnsureUserExists(string userId)
    {
        try
        {
            // Crear usuario si no existe
            UserRepo.CreateDefaultUser(userId);
            
            // Obtener referencia al usuario
            var user = UserRepo.GetById(userId);
            
            if (user != null)
            {
                // Crear configuración por defecto si no existe
                ConfigRepo.CreateDefaultConfiguration(userId, user);
                
                // Crear control parental por defecto si no existe
                ParentalRepo.CreateDefaultParentalControl(userId, user);
                
                Debug.Log($"EnsureUserExists: Usuario {userId} verificado/creado correctamente");
            }
            else
            {
                Debug.LogWarning($"EnsureUserExists: No se pudo obtener el usuario {userId} después de crearlo");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en EnsureUserExists: {e.Message}");
        }
    }
    
    // Métodos de verificación de la base de datos
    public bool VerifyDatabase()
    {
        try
        {
            if (!_initialized)
            {
                Debug.LogWarning("VerifyDatabase: DataService no está inicializado completamente");
                return false;
            }
            
            var userId = GetCurrentUserId();
            var user = UserRepo.GetById(userId);
            
            if (user == null)
            {
                Debug.LogWarning($"VerifyDatabase: Usuario {userId} no encontrado");
                return false;
            }
            
            var config = ConfigRepo.GetById(userId);
            if (config == null)
            {
                Debug.LogWarning($"VerifyDatabase: Configuración para usuario {userId} no encontrada");
                return false;
            }
            
            Debug.Log($"VerifyDatabase: Base de datos verificada correctamente para el usuario {userId}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en la verificación de la base de datos: {e.Message}");
            return false;
        }
    }
    
    public string GetEffectiveUserId()
    {
        string userId = GetCurrentUserId();
    
        try
        {
            // Crear una nueva instancia de Realm dentro de un bloque using
            using (var realm = Realm.GetInstance(new RealmConfiguration { SchemaVersion = 1 }))
            {
                // Comprobar si el usuario es tutor y tiene perfiles asociados
                var user = realm.Find<User>(userId);
                if (user != null && user.IsTutor)
                {
                    // Aquí necesitarías lógica para determinar qué perfil está activo
                    // Por ejemplo, obteniendo el primer perfil disponible
                    var activeProfile = user.Profiles.FirstOrDefault();
                    if (activeProfile != null)
                    {
                        // Usamos directamente el ID del perfil sin convertirlo a string
                        return activeProfile.ProfileID.ToString();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en GetEffectiveUserId: {e.Message}");
        }
    
        // Si no es tutor, no tiene perfiles, o hay un error, usar el UID del usuario
        return userId;
    }
    
    public void ForceInitialize()
    {
        if (!_initialized)
        {
            InitializeRepositories();
        }
        
        // Asegurar que existe el usuario por defecto
        EnsureUserExists(AuthManager.DEFAULT_USER_ID);
        
        // Si hay un usuario activo diferente al por defecto, asegurar que también existe
        string currentUserId = GetCurrentUserId();
        if (currentUserId != AuthManager.DEFAULT_USER_ID)
        {
            EnsureUserExists(currentUserId);
        }
        
        Debug.Log("DataService: Inicialización forzada completada");
    }
}