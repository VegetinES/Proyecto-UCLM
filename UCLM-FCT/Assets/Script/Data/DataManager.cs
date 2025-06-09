using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    
    private bool _isOnline = false;
    private SynchronizationContext mainThread;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            mainThread = SynchronizationContext.Current;
            InitializeAsync();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private async void InitializeAsync()
    {
        Debug.Log("DataManager: Iniciando inicialización...");
        
        // Esperar a que la conexión a MongoDB se complete
        await ConnectToMongoDB();
    }
    
    private async Task ConnectToMongoDB()
    {
        Debug.Log("DataManager: Iniciando conexión a MongoDB...");
        
        _isOnline = await MongoDbService.Instance.ConnectAsync();
        Debug.Log($"DataManager: MongoDB connection: {(_isOnline ? "Connected" : "Failed")}");
    }
    
    public string GetCurrentUserId()
    {
        if (AuthManager.Instance == null)
            return AuthManager.DEFAULT_USER_ID;
            
        return AuthManager.Instance.UserID;
    }
    
    public async Task SyncUserDataAsync(string userId)
    {
        Debug.Log($"DataManager: Sincronizando datos para usuario {userId}");
        
        try
        {
            if (_isOnline && !AuthManager.IsDefaultUser(userId))
            {
                Debug.Log("DataManager: Intentando obtener datos del usuario desde MongoDB...");
                
                var cloudData = await MongoDbService.Instance.GetUserDataAsync(userId);
                
                if (cloudData != null)
                {
                    Debug.Log("DataManager: Datos encontrados en MongoDB. Sincronizando a SQLite...");
                    
                    // Actualizar datos locales con los de la nube
                    int colors = cloudData.GetValue("configuration").AsBsonDocument.GetValue("colors").AsInt32;
                    bool autoNarrator = cloudData.GetValue("configuration").AsBsonDocument.GetValue("autoNarrator").AsBoolean;
                    bool activated = cloudData.GetValue("parentalControl").AsBsonDocument.GetValue("activated").AsBoolean;
                    string pin = cloudData.GetValue("parentalControl").AsBsonDocument.GetValue("pin").AsString;
                    
                    SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator);
                    SqliteDatabase.Instance.SaveParentalControl(userId, activated, pin);
                    
                    Debug.Log("DataManager: Sincronización de MongoDB a SQLite completada correctamente");
                    
                    // Sincronizar los perfiles del usuario también
                    await SyncProfilesAsync(userId);
                }
                else
                {
                    Debug.Log("DataManager: No se encontraron datos en MongoDB para este usuario. Subiendo datos locales...");
                    
                    // No hay datos en MongoDB, subir los datos locales
                    var user = SqliteDatabase.Instance.GetUser(userId);
                    var config = SqliteDatabase.Instance.GetConfiguration(userId);
                    var parental = SqliteDatabase.Instance.GetParentalControl(userId);
                    
                    if (user != null && AuthManager.Instance != null)
                    {
                        Debug.Log("DataManager: Encontrados datos locales. Iniciando subida a MongoDB...");
                        
                        // Crear objeto de control parental para sincronización
                        var pc = new SharedModels.ParentalControl
                        {
                            Activated = parental?.Activated ?? false,
                            Pin = parental?.Pin ?? "",
                            SoundConf = parental?.SoundConf ?? true,
                            AccessibilityConf = parental?.AccessibilityConf ?? true,
                            StatisticsConf = parental?.StatisticsConf ?? true,
                            AboutConf = parental?.AboutConf ?? true,
                            ProfileConf = parental?.ProfileConf ?? true
                        };
                        
                        // Si no hay configuración, crear una por defecto
                        if (config == null)
                        {
                            Debug.Log("DataManager: No se encontró configuración local. Creando configuración por defecto...");
                            
                            SqliteDatabase.Instance.SaveConfiguration(userId, 3, false);
                            config = SqliteDatabase.Instance.GetConfiguration(userId);
                        }
                        
                        // Subir datos a MongoDB
                        await MongoDbService.Instance.SaveUserDataAsync(userId, AuthManager.Instance.UserEmail, config, pc);
                        Debug.Log("DataManager: Datos locales subidos correctamente a MongoDB");
                        
                        // Verificar que los datos se subieron correctamente
                        var verifyData = await MongoDbService.Instance.GetUserDataAsync(userId);
                        Debug.Log($"DataManager: Verificación de subida: {(verifyData != null ? "Exitosa" : "Fallida")}");
                        
                        // Sincronizar todos los perfiles
                        await SyncProfilesAsync(userId);
                    }
                    else
                    {
                        Debug.LogWarning("DataManager: No se encontraron datos locales completos para sincronizar a MongoDB");
                    }
                }
            }
            else
            {
                Debug.Log($"DataManager: No se puede sincronizar. Online: {_isOnline}, Usuario por defecto: {AuthManager.IsDefaultUser(userId)}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DataManager: Error durante la sincronización: {e.Message}");
        }
    }
    
    private async Task SyncProfilesAsync(string userId)
    {
        try
        {
            // Obtener todos los perfiles locales
            var localProfiles = SqliteDatabase.Instance.GetProfiles(userId);
            
            foreach (var profile in localProfiles)
            {
                // Para cada perfil, sincronizar sus datos
                var profileConfig = SqliteDatabase.Instance.GetConfiguration(userId, profile.ProfileID);
                var profileParental = SqliteDatabase.Instance.GetParentalControl(userId, profile.ProfileID);
                
                var profileData = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "profileId", profile.ProfileID },
                    { "name", profile.Name },
                    { "gender", profile.Gender },
                    { "lastUpdate", DateTime.UtcNow.ToString("o") }
                };
                
                var configData = profileConfig != null ? new System.Collections.Generic.Dictionary<string, object>
                {
                    { "colors", profileConfig.Colors },
                    { "autoNarrator", profileConfig.AutoNarrator },
                    { "sound", profileConfig.Sound },
                    { "generalSound", profileConfig.GeneralSound },
                    { "musicSound", profileConfig.MusicSound },
                    { "effectsSound", profileConfig.EffectsSound },
                    { "narratorSound", profileConfig.NarratorSound },
                    { "vibration", profileConfig.Vibration }
                } : null;
                
                if (profileParental != null)
                {
                    profileData["parentalControl"] = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "activated", profileParental.Activated },
                        { "pin", profileParental.Pin },
                        { "soundConf", profileParental.SoundConf },
                        { "accessibilityConf", profileParental.AccessibilityConf },
                        { "statisticsConf", profileParental.StatisticsConf },
                        { "aboutConf", profileParental.AboutConf },
                        { "profileConf", profileParental.ProfileConf }
                    };
                }
                
                await MongoDbService.Instance.SaveProfileDataAsync(userId, profile.ProfileID, profileData, configData);
                Debug.Log($"DataManager: Perfil {profile.ProfileID} sincronizado correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"DataManager: Error al sincronizar perfiles: {e.Message}");
        }
    }
    
    public bool IsOnline()
    {
        return _isOnline;
    }
}