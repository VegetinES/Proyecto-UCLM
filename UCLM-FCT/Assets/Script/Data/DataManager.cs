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
        
        var config = SqliteDatabase.Instance.GetConfiguration(userId);
    
        if (config != null)
        {
            SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration);
        }
        else
        {
            SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator);
        }
    
        Debug.Log($"DataManager: Configuración guardada en SQLite. Estado online: {_isOnline}, Usuario por defecto: {AuthManager.IsDefaultUser(userId)}");
        
        // Sincronizar con MongoDB si estamos online y no es usuario por defecto
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            Debug.Log("DataManager: Intentando guardar configuración en MongoDB...");
            
            try
            {
                // Obtener la configuración completa actual
                var fullConfig = SqliteDatabase.Instance.GetConfiguration(userId);
                var parental = SqliteDatabase.Instance.GetParentalControl(userId);
                
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
                await MongoDbService.Instance.SaveUserDataAsync(userId, AuthManager.Instance.UserEmail, fullConfig, pc);
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
        
        // Guardar localmente en SQLite
        SqliteDatabase.Instance.SaveParentalControl(userId, activated, pin);
        
        Debug.Log($"DataManager: Control parental guardado en SQLite. Estado online: {_isOnline}, Usuario por defecto: {AuthManager.IsDefaultUser(userId)}");
        
        // Sincronizar con MongoDB si estamos online y no es usuario por defecto
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            Debug.Log("DataManager: Intentando guardar control parental en MongoDB...");
            
            try
            {
                // Obtener la configuración completa actual
                var config = SqliteDatabase.Instance.GetConfiguration(userId);
                
                // Si no hay configuración, crear una por defecto
                var cfg = config != null ? config : new LocalConfiguration { 
                    UserID = userId,
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
                var parentalDb = SqliteDatabase.Instance.GetParentalControl(userId);
                
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
                await MongoDbService.Instance.SaveUserDataAsync(userId, AuthManager.Instance.UserEmail, cfg, parental);
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
    
    public Task SaveStatisticsAsync(string userId, int level, bool completed, int timeSpent)
    {
        if (_isOnline && !AuthManager.IsDefaultUser(userId))
        {
            return MongoDbService.Instance.SaveStatisticsAsync(userId, level, completed, timeSpent);
        }
        return Task.CompletedTask;
    }
    
    public bool IsOnline()
    {
        return _isOnline;
    }
}