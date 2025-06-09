using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using UnityEngine;

public class MongoDbService
{
    private static MongoDbService _instance;
    public static MongoDbService Instance => _instance ??= new MongoDbService();
    
    private MongoClient _client;
    private IMongoDatabase _database;
    private bool _isConnected = false;
    
    // Para operaciones en hilos separados
    private SynchronizationContext mainThread;
    
    private MongoDbService()
    {
        try
        {
            // Solo capturar el contexto si estamos en el hilo principal
            if (Thread.CurrentThread.IsBackground == false)
            {
                mainThread = SynchronizationContext.Current;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error en constructor: {e.Message}");
        }
    }
    
    public async Task<bool> ConnectAsync()
    {
        try
        {
            Debug.Log("MongoDbService: Iniciando conexión a MongoDB Atlas...");
        
            // Ejecutar la conexión en un hilo separado
            return await Task.Run(async () => {
                try { 
                    // Obtener cadena de conexión desde variables de entorno
                    string atlasConnection = EnvironmentLoader.GetVariable("MONGODB_CONNECTION", "");
                
                    if (string.IsNullOrEmpty(atlasConnection))
                    {
                        mainThread?.Post(_ => Debug.LogError("MongoDbService: No se pudo cargar la cadena de conexión a MongoDB desde las variables de entorno"), null);
                        _isConnected = false;
                        return false;
                    }
                
                    _client = new MongoClient(atlasConnection);
                    _database = _client.GetDatabase("game_data");
            
                    // Intentar una operación de ping para verificar la conexión
                    await _database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
            
                    _isConnected = true;
                
                    // Loguear en el hilo principal
                    mainThread?.Post(_ => Debug.Log("MongoDbService: Conexión establecida correctamente"), null);
                
                    return true;
                }
                catch (Exception e) {
                    // Loguear error en el hilo principal
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error conectando a MongoDB Atlas: {e.Message}"), null);
                    _isConnected = false;
                    return false;
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
            _isConnected = false;
            return false;
        }
    }
    
    public async Task SaveUserDataAsync(string uid, string email, LocalConfiguration config, SharedModels.ParentalControl parental)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: SaveUserDataAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Guardando datos para usuario {uid} con email {email}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("users");
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", uid);
                    
                    // Obtener documento existente para preservar fechas si existe
                    var existingDoc = await collection.Find(filter).FirstOrDefaultAsync();
                    
                    // Determinar las fechas
                    string createdAt = DateTime.UtcNow.ToString("o"); // ISO 8601
                    string lastLogin = DateTime.UtcNow.ToString("o");
                    
                    // Si existe el documento, mantener la fecha de creación
                    if (existingDoc != null)
                    {
                        if (existingDoc.Contains("createdAt"))
                            createdAt = existingDoc["createdAt"].AsString;
                        
                        // Actualizar fecha de último login
                        lastLogin = DateTime.UtcNow.ToString("o");
                    }
                    
                    // Obtener el valor de isTutor desde SQLite
                    bool isTutor = false;
                    var user = SqliteDatabase.Instance.GetUser(uid);
                    if (user != null)
                    {
                        isTutor = user.IsTutor;
                    }
                    
                    var document = new BsonDocument
                    {
                        { "_id", uid },
                        { "email", email },
                        { "isTutor", isTutor }, // Añadir campo isTutor
                        { "configuration", new BsonDocument
                            {
                                { "colors", config.Colors },
                                { "autoNarrator", config.AutoNarrator },
                                { "sound", config.Sound },
                                { "generalSound", config.GeneralSound },
                                { "musicSound", config.MusicSound },
                                { "effectsSound", config.EffectsSound },
                                { "narratorSound", config.NarratorSound },
                                { "vibration", config.Vibration }
                            }
                        },
                        { "parentalControl", new BsonDocument
                            {
                                { "activated", parental.Activated },
                                { "pin", parental.Pin },
                                { "soundConf", parental.SoundConf },
                                { "accessibilityConf", parental.AccessibilityConf },
                                { "statisticsConf", parental.StatisticsConf },
                                { "aboutConf", parental.AboutConf },
                                { "profileConf", parental.ProfileConf }
                            }
                        },
                        { "createdAt", createdAt },
                        { "lastLogin", lastLogin },
                        { "lastUpdate", DateTime.UtcNow.ToString("o") }
                    };
                    
                    await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true });
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Datos guardados correctamente para {uid}"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al guardar datos de usuario: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }
    
    public async Task<BsonDocument> GetUserDataAsync(string uid)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: GetUserDataAsync: No hay conexión establecida");
                return null;
            }
            
            Debug.Log($"MongoDbService: Obteniendo datos para usuario {uid}");
            
            // Ejecutar en un hilo separado
            return await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("users");
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", uid);
                    var result = await collection.Find(filter).FirstOrDefaultAsync();
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Búsqueda completada. Resultado: {(result != null ? "Datos encontrados" : "No hay datos")}"), null);
                    
                    return result;
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al obtener datos de usuario: {e.Message}"), null);
                    return null;
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
            return null;
        }
    }
    
    public async Task SaveLoginAsync(string uid, string email)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: SaveLoginAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Registrando inicio de sesión para usuario {uid}");
            
            // Capturar valores de Unity en el hilo principal
            string platform = Application.platform.ToString();
            string version = Application.version;
            
            // Obtener el valor de isTutor desde SQLite
            bool isTutor = false;
            var user = SqliteDatabase.Instance.GetUser(uid);
            if (user != null)
            {
                isTutor = user.IsTutor;
            }
            
            // Ejecutar la parte de MongoDB en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("users");
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", uid);
                    
                    // Verificar si existe el documento
                    var existingDoc = await collection.Find(filter).FirstOrDefaultAsync();
                    
                    if (existingDoc != null)
                    {
                        // Actualizar la fecha de último login y asegurar que isTutor está actualizado
                        var update = Builders<BsonDocument>.Update
                            .Set("lastLogin", DateTime.UtcNow.ToString("o"))
                            .Set("isTutor", isTutor);
                        await collection.UpdateOneAsync(filter, update);
                    }
                    else
                    {
                        // Si no existe, crear un documento básico con fechas
                        var document = new BsonDocument
                        {
                            { "_id", uid },
                            { "email", email },
                            { "isTutor", isTutor }, // Incluir campo isTutor
                            { "createdAt", DateTime.UtcNow.ToString("o") },
                            { "lastLogin", DateTime.UtcNow.ToString("o") }
                        };
                        
                        await collection.InsertOneAsync(document);
                    }
                    
                    // Registrar login en colección separada para historial
                    var logsCollection = _database.GetCollection<BsonDocument>("login_logs");
                    var logDocument = new BsonDocument
                    {
                        { "uid", uid },
                        { "email", email },
                        { "isTutor", isTutor }, // Incluir campo isTutor en logs también
                        { "loginTime", DateTime.UtcNow.ToString("o") },
                        { "platform", platform }, // Usar el valor capturado en el hilo principal
                        { "appVersion", version } // Usar el valor capturado en el hilo principal
                    };
                    
                    await logsCollection.InsertOneAsync(logDocument);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Login registrado correctamente para {uid}"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al registrar login: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }
    
    public bool IsConnected()
    {
        return _isConnected;
    }
    
    public async Task SaveProfileDataAsync(string userId, int profileId, Dictionary<string, object> profileData, Dictionary<string, object> configData = null)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: SaveProfileDataAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Guardando datos de perfil {profileId} para usuario {userId}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("user_profiles");
                    var filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("userId", userId),
                        Builders<BsonDocument>.Filter.Eq("profileId", profileId)
                    );
                    
                    // Crear documento BSON con los datos del perfil
                    var profileBson = new BsonDocument();
                    foreach (var kvp in profileData)
                    {
                        profileBson.Add(kvp.Key, BsonValue.Create(kvp.Value));
                    }
                    
                    // Añadir datos de configuración si existen
                    if (configData != null)
                    {
                        var configBson = new BsonDocument();
                        foreach (var kvp in configData)
                        {
                            configBson.Add(kvp.Key, BsonValue.Create(kvp.Value));
                        }
                        profileBson.Add("config", configBson);
                    }
                    
                    // Añadir información básica
                    profileBson.Add("userId", userId);
                    profileBson.Add("lastUpdate", DateTime.UtcNow.ToString("o"));
                    
                    // Usar upsert para insertar si no existe o actualizar si existe
                    await collection.ReplaceOneAsync(filter, profileBson, new ReplaceOptions { IsUpsert = true });
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Datos de perfil guardados correctamente para {userId}/{profileId}"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al guardar datos de perfil: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }

    public async Task DeleteProfileDataAsync(string userId, int profileId)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: DeleteProfileDataAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Eliminando datos de perfil {profileId} para usuario {userId}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("user_profiles");
                    var filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("userId", userId),
                        Builders<BsonDocument>.Filter.Eq("profileId", profileId)
                    );
                    
                    var result = await collection.DeleteOneAsync(filter);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Datos de perfil eliminados: {result.DeletedCount} documentos"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al eliminar datos de perfil: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }
    
    public async Task DeleteUserDataAsync(string userId)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: DeleteUserDataAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Eliminando datos de usuario {userId}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    // Eliminar al usuario de la colección de usuarios
                    var usersCollection = _database.GetCollection<BsonDocument>("users");
                    var userFilter = Builders<BsonDocument>.Filter.Eq("_id", userId);
                    var userResult = await usersCollection.DeleteOneAsync(userFilter);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Usuario eliminado de colección users: {userResult.DeletedCount}"), null);
                    
                    // Eliminar historial de login
                    var logsCollection = _database.GetCollection<BsonDocument>("login_logs");
                    var logsFilter = Builders<BsonDocument>.Filter.Eq("uid", userId);
                    var logsResult = await logsCollection.DeleteManyAsync(logsFilter);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Registros de login eliminados: {logsResult.DeletedCount}"), null);
                    
                    // Eliminar estadísticas del usuario
                    var statsCollection = _database.GetCollection<BsonDocument>("statistics");
                    var statsFilter = Builders<BsonDocument>.Filter.Eq("uid", userId);
                    var statsResult = await statsCollection.DeleteManyAsync(statsFilter);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Estadísticas eliminadas: {statsResult.DeletedCount}"), null);
                    
                    // Eliminar perfiles de usuario
                    var profilesCollection = _database.GetCollection<BsonDocument>("user_profiles");
                    var profilesFilter = Builders<BsonDocument>.Filter.Eq("userId", userId);
                    var profilesResult = await profilesCollection.DeleteManyAsync(profilesFilter);
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Perfiles eliminados: {profilesResult.DeletedCount}"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al eliminar datos de usuario: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }
    
    public async Task SaveGameStatisticsAsync(
        string userId, 
        int profileId, 
        int level, 
        bool completed, 
        int moves, 
        int timeSpent, 
        bool helpUsed, 
        string timestamp)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: SaveGameStatisticsAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Guardando estadísticas para usuario {userId}, perfil {profileId}, nivel {level}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("statistics");
                    var document = new BsonDocument
                    {
                        { "userId", userId },
                        { "profileId", profileId },
                        { "level", level },
                        { "completed", completed },
                        { "moves", moves },
                        { "timeSpent", timeSpent },
                        { "helpUsed", helpUsed },
                        { "timestamp", timestamp }
                    };
                    
                    await collection.InsertOneAsync(document);
                    
                    mainThread?.Post(_ => Debug.Log("MongoDbService: Estadísticas de juego guardadas correctamente"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al guardar estadísticas de juego: {e.Message}"), null);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
        }
    }

    public async Task<List<BsonDocument>> GetGameStatisticsAsync(string userId, int profileId = 0)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: GetGameStatisticsAsync: No hay conexión establecida");
                return new List<BsonDocument>();
            }
            
            Debug.Log($"MongoDbService: Obteniendo estadísticas para usuario {userId}, perfil {profileId}");
            
            // Ejecutar en un hilo separado
            return await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("statistics");
                    
                    // Filtro base por usuario
                    var filter = Builders<BsonDocument>.Filter.Eq("userId", userId);
                    
                    // Si se especifica un perfil, añadir al filtro
                    if (profileId > 0)
                    {
                        filter = Builders<BsonDocument>.Filter.And(
                            filter,
                            Builders<BsonDocument>.Filter.Eq("profileId", profileId)
                        );
                    }
                    
                    // Ordenar por timestamp descendente (más recientes primero)
                    var sort = Builders<BsonDocument>.Sort.Descending("timestamp");
                    
                    var result = await collection.Find(filter).Sort(sort).ToListAsync();
                    
                    mainThread?.Post(_ => Debug.Log($"MongoDbService: Se encontraron {result.Count} estadísticas"), null);
                    
                    return result;
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al obtener estadísticas: {e.Message}"), null);
                    return new List<BsonDocument>();
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"MongoDbService: Error general: {e.Message}");
            return new List<BsonDocument>();
        }
    }
}