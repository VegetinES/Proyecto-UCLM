using System;
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
                    string atlasConnection = "mongodb+srv://enriquesequihernandez:LGA5jQ8x5WUy6YzQ@uclm.bgm4csu.mongodb.net/?retryWrites=true&w=majority&appName=UCLM";
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
                    
                    var document = new BsonDocument
                    {
                        { "_id", uid },
                        { "email", email },
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
            
            // Ejecutar la parte de MongoDB en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("users");
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", uid);
                    
                    // Verificar si existe el documento
                    var existingDoc = await collection.Find(filter).FirstOrDefaultAsync();
                    
                    if (existingDoc != null)
                    {
                        // Actualizar solo la fecha de último login
                        var update = Builders<BsonDocument>.Update.Set("lastLogin", DateTime.UtcNow.ToString("o"));
                        await collection.UpdateOneAsync(filter, update);
                    }
                    else
                    {
                        // Si no existe, crear un documento básico con fechas
                        var document = new BsonDocument
                        {
                            { "_id", uid },
                            { "email", email },
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
    
    public async Task SaveStatisticsAsync(string uid, int level, bool completed, int timeSpent)
    {
        try
        {
            if (!_isConnected)
            {
                Debug.LogWarning("MongoDbService: SaveStatisticsAsync: No hay conexión establecida");
                return;
            }
            
            Debug.Log($"MongoDbService: Guardando estadísticas para usuario {uid}, nivel {level}");
            
            // Ejecutar en un hilo separado
            await Task.Run(async () => {
                try {
                    var collection = _database.GetCollection<BsonDocument>("statistics");
                    var document = new BsonDocument
                    {
                        { "uid", uid },
                        { "level", level },
                        { "completed", completed },
                        { "timeSpent", timeSpent },
                        { "timestamp", DateTime.UtcNow.ToString("o") }
                    };
                    
                    await collection.InsertOneAsync(document);
                    
                    mainThread?.Post(_ => Debug.Log("MongoDbService: Estadísticas guardadas correctamente"), null);
                }
                catch (Exception e) {
                    mainThread?.Post(_ => Debug.LogError($"MongoDbService: Error al guardar estadísticas: {e.Message}"), null);
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
}