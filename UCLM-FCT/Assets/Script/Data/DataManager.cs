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
    
    public async void SaveConfiguration(string userId, int colors, bool autoNarrator)
    {
        Debug.Log($"DataManager: Guardando configuración para usuario {userId}");
        
        int profileId = ProfileManager.Instance?.GetCurrentProfileId() ?? 0;
        var config = profileId > 0 
            ? SqliteDatabase.Instance.GetConfiguration(userId, profileId) 
            : SqliteDatabase.Instance.GetConfiguration(userId);
    
        if (config != null)
        {
            if (profileId > 0)
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration, profileId);
            }
            else
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration);
            }
        }
        else
        {
            if (profileId > 0)
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, true, 50, 50, 50, 50, false, profileId);
            }
            else
            {
                SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator);
            }
        }
    
        Debug.Log($"DataManager: Configuración guardada en SQLite. Estado online: {_isOnline}, Usuario por defecto: {AuthManager.IsDefaultUser(userId)}");
        
        // Sincronizar con MongoDB si estamos online y no es usuario por defecto
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            Debug.Log("DataManager: Intentando guardar configuración en MongoDB...");
            
            try
            {
                // Obtener la configuración completa actual
                var fullConfig = profileId > 0 
                    ? SqliteDatabase.Instance.GetConfiguration(userId, profileId) 
                    : SqliteDatabase.Instance.GetConfiguration(userId);
                
                var parental = profileId > 0 
                    ? SqliteDatabase.Instance.GetParentalControl(userId, profileId) 
                    : SqliteDatabase.Instance.GetParentalControl(userId);
                
                // Si no hay configuración de control parental, crear una por defecto
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
                
                // Guardar en MongoDB (en un hilo separado)
                if (profileId > 0)
                {
                    // Si hay un perfil seleccionado, guardar la configuración del perfil
                    var profileData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "profileId", profileId },
                        { "name", ProfileManager.Instance?.GetCurrentProfileName() ?? "" },
                        { "lastUpdate", DateTime.UtcNow.ToString("o") }
                    };
                    
                    var configData = fullConfig != null ? new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "colors", fullConfig.Colors },
                        { "autoNarrator", fullConfig.AutoNarrator },
                        { "sound", fullConfig.Sound },
                        { "generalSound", fullConfig.GeneralSound },
                        { "musicSound", fullConfig.MusicSound },
                        { "effectsSound", fullConfig.EffectsSound },
                        { "narratorSound", fullConfig.NarratorSound },
                        { "vibration", fullConfig.Vibration }
                    } : null;
                    
                    await MongoDbService.Instance.SaveProfileDataAsync(userId, profileId, profileData, configData);
                }
                else
                {
                    // Si no hay perfil seleccionado, guardar la configuración del usuario
                    await MongoDbService.Instance.SaveUserDataAsync(userId, AuthManager.Instance.UserEmail, fullConfig, pc);
                }
                
                Debug.Log("DataManager: Configuración guardada correctamente en MongoDB");
            }
            catch (Exception e)
            {
                Debug.LogError($"DataManager: Error al guardar configuración en MongoDB: {e.Message}");
            }
        }
    }
    
    public async void SaveParentalControl(string userId, bool activated, string pin)
    {
        Debug.Log($"DataManager: Guardando control parental para usuario {userId}");
        
        int profileId = ProfileManager.Instance?.GetCurrentProfileId() ?? 0;
        
        // Guardar localmente en SQLite
        if (profileId > 0)
        {
            SqliteDatabase.Instance.SaveParentalControl(userId, activated, pin, true, true, true, true, true, profileId);
        }
        else
        {
            SqliteDatabase.Instance.SaveParentalControl(userId, activated, pin);
        }
        
        Debug.Log($"DataManager: Control parental guardado en SQLite. Estado online: {_isOnline}, Usuario por defecto: {AuthManager.IsDefaultUser(userId)}");
        
        // Sincronizar con MongoDB si estamos online y no es usuario por defecto
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            Debug.Log("DataManager: Intentando guardar control parental en MongoDB...");
            
            try
            {
                // Obtener la configuración completa actual
                var config = profileId > 0 
                    ? SqliteDatabase.Instance.GetConfiguration(userId, profileId) 
                    : SqliteDatabase.Instance.GetConfiguration(userId);
                
                // Si no hay configuración, crear una por defecto
                var cfg = config != null ? config : new LocalConfiguration { 
                    UserID = userId,
                    ProfileID = profileId,
                    Colors = 3, 
                    AutoNarrator = false, 
                    Sound = true,
                    GeneralSound = 50,
                    MusicSound = 50,
                    EffectsSound = 50,
                    NarratorSound = 50,
                    Vibration = false
                };
                
                // Obtener los datos actualizados de control parental
                var parentalDb = profileId > 0 
                    ? SqliteDatabase.Instance.GetParentalControl(userId, profileId) 
                    : SqliteDatabase.Instance.GetParentalControl(userId);
                
                // Crear objeto de control parental para MongoDB
                var parental = new SharedModels.ParentalControl { 
                    Activated = activated, 
                    Pin = pin,
                    SoundConf = parentalDb?.SoundConf ?? true,
                    AccessibilityConf = parentalDb?.AccessibilityConf ?? true,
                    StatisticsConf = parentalDb?.StatisticsConf ?? true,
                    AboutConf = parentalDb?.AboutConf ?? true,
                    ProfileConf = parentalDb?.ProfileConf ?? true
                };
                
                // Guardar en MongoDB (en un hilo separado)
                if (profileId > 0)
                {
                    // Si hay un perfil seleccionado, guardar el control parental del perfil
                    var profileData = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "profileId", profileId },
                        { "name", ProfileManager.Instance?.GetCurrentProfileName() ?? "" },
                        { "parentalControl", new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "activated", activated },
                                { "pin", pin },
                                { "soundConf", parentalDb?.SoundConf ?? true },
                                { "accessibilityConf", parentalDb?.AccessibilityConf ?? true },
                                { "statisticsConf", parentalDb?.StatisticsConf ?? true },
                                { "aboutConf", parentalDb?.AboutConf ?? true },
                                { "profileConf", parentalDb?.ProfileConf ?? true }
                            }
                        },
                        { "lastUpdate", DateTime.UtcNow.ToString("o") }
                    };
                    
                    await MongoDbService.Instance.SaveProfileDataAsync(userId, profileId, profileData);
                }
                else
                {
                    // Si no hay perfil seleccionado, guardar el control parental del usuario
                    await MongoDbService.Instance.SaveUserDataAsync(userId, AuthManager.Instance.UserEmail, cfg, parental);
                }
                
                Debug.Log("DataManager: Control parental guardado correctamente en MongoDB");
            }
            catch (Exception e)
            {
                Debug.LogError($"DataManager: Error al guardar control parental en MongoDB: {e.Message}");
            }
        }
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
    
    public Task SaveStatisticsAsync(string userId, int level, bool completed, int timeSpent)
    {
        int profileId = ProfileManager.Instance?.GetCurrentProfileId() ?? 0;
        
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            // Si hay un perfil seleccionado, guardar las estadísticas en MongoDB incluyendo el profileId
            if (profileId > 0)
            {
                // Crear una tarea personalizada para incluir el perfil
                var task = new TaskCompletionSource<bool>();
                Task.Run(async () => {
                    try
                    {
                        await MongoDbService.Instance.SaveStatisticsAsync(userId, level, completed, timeSpent);
                        var profileData = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "profileId", profileId },
                            { "statistics", new System.Collections.Generic.Dictionary<string, object>
                                {
                                    { "level", level },
                                    { "completed", completed },
                                    { "timeSpent", timeSpent },
                                    { "timestamp", DateTime.UtcNow.ToString("o") }
                                }
                            }
                        };
                        await MongoDbService.Instance.SaveProfileDataAsync(userId, profileId, profileData);
                        task.SetResult(true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error al guardar estadísticas del perfil: {e.Message}");
                        task.SetResult(false);
                    }
                });
                return task.Task;
            }
            else
            {
                // Si no hay perfil, guardar las estadísticas normalmente
                return MongoDbService.Instance.SaveStatisticsAsync(userId, level, completed, timeSpent);
            }
        }
        return Task.CompletedTask;
    }
    
    public bool IsOnline()
    {
        return _isOnline;
    }
}