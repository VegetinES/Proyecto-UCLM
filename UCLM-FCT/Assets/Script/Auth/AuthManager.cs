using System;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Supabase.Gotrue;
using TMPro;
using Client = Supabase.Client;

public class AuthManager : MonoBehaviour
{
    // Configuración de Supabase (valores por defecto que serán reemplazados)
    private string _supabaseUrl = "";
    private string _supabaseKey = "";
    
    // ID de usuario por defecto
    public const string DEFAULT_USER_ID = "1";
    
    // Claves para PlayerPrefs
    private const string ACCESS_TOKEN_KEY = "supabase_access_token";
    private const string REFRESH_TOKEN_KEY = "supabase_refresh_token";
    
    // Singleton
    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }
    
    // Cliente Supabase
    private Client _supabase;
    
    // Estado de sesión
    private bool _isLoggedIn = false;
    private string _userID = DEFAULT_USER_ID;
    private string _userEmail = "";
    
    // Propiedades públicas
    public bool IsLoggedIn => _isLoggedIn;
    public string UserID => _userID;
    public string UserEmail => _userEmail;
    
    [SerializeField] public TMP_Text welcomeText;
    
    private bool _initialized = false;
    
    private void Awake()
    {
        Debug.Log("AuthManager: Awake - Path: " + Application.persistentDataPath);
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEnvironmentVariables();
            InitializeSupabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadEnvironmentVariables()
    {
        EnvironmentLoader.Initialize();
        _supabaseUrl = EnvironmentLoader.GetVariable("SUPABASE_URL", "");
        _supabaseKey = EnvironmentLoader.GetVariable("SUPABASE_PUBLIC_KEY", "");
        
        // Verificar si se cargaron las variables
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_supabaseKey))
        {
            Debug.LogError("AuthManager: No se pudieron cargar las variables de entorno de Supabase. Asegúrate de que el archivo .env esté configurado correctamente.");
        }
        else
        {
            Debug.Log("AuthManager: Variables de entorno de Supabase cargadas correctamente");
        }
    }
    
    private async void InitializeSupabase()
    {
        try
        {
            Debug.Log("AuthManager: Inicializando Supabase...");
            _supabase = new Client(_supabaseUrl, _supabaseKey);
            await _supabase.InitializeAsync();
            await RestoreSession();
            _initialized = true;
            Debug.Log("AuthManager: Inicializado correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al inicializar Supabase: {e.Message}");
            LoadDefaultUser();
        }
    }
    
    private async Task RestoreSession()
    {
        try
        {
            Debug.Log("AuthManager: Intentando restaurar sesión...");
            
            // Intentar restaurar sesión con tokens guardados
            string accessToken = PlayerPrefs.GetString(ACCESS_TOKEN_KEY, "");
            string refreshToken = PlayerPrefs.GetString(REFRESH_TOKEN_KEY, "");
            
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                Debug.Log("AuthManager: Tokens encontrados, intentando restaurar sesión...");
                try {
                    // Primero establecer la sesión con los tokens existentes
                    await _supabase.Auth.SetSession(accessToken, refreshToken);
                    
                    // Intentar actualizar la sesión (sin parámetros)
                    try {
                        // RefreshSession usa el refreshToken ya establecido internamente
                        var session = await _supabase.Auth.RefreshSession();
                        
                        if (session != null && !string.IsNullOrEmpty(session.AccessToken)) 
                        {
                            // Si se actualizó correctamente, guardar los nuevos tokens
                            SaveSessionTokens(session);
                            _isLoggedIn = true;
                            _userID = session.User.Id;
                            _userEmail = session.User.Email ?? "";
                            SaveUserToSqlite();
                            UpdateUIForLoggedInUser();
                            Debug.Log($"AuthManager: Sesión actualizada correctamente para usuario {_userEmail}");
                            return;
                        }
                    } 
                    catch (Exception refreshEx) 
                    {
                        Debug.LogWarning($"Error al actualizar token: {refreshEx.Message}. Intentando con token original...");
                    }
                    
                    // Si el refresh falló, intentar obtener el usuario con el token actual
                    try {
                        var user = await _supabase.Auth.GetUser(accessToken);
                        
                        if (user != null)
                        {
                            _isLoggedIn = true;
                            _userID = user.Id;
                            _userEmail = user.Email ?? "";
                            SaveUserToSqlite();
                            UpdateUIForLoggedInUser();
                            Debug.Log($"AuthManager: Sesión restaurada para usuario {_userEmail}");
                            return;
                        }
                    }
                    catch (Exception getUserEx) {
                        Debug.LogWarning($"Error al obtener usuario: {getUserEx.Message}");
                        // Continuar con el flujo normal
                    }
                } 
                catch (Exception e) 
                {
                    Debug.LogError($"Error al restaurar sesión: {e.Message}");
                    
                    // Verificar diferentes tipos de errores
                    string errorMsg = e.Message.ToLower();
                    
                    // Si el usuario no existe o el token es inválido
                    if (errorMsg.Contains("user_not_found") || 
                        errorMsg.Contains("invalid refresh token") ||
                        errorMsg.Contains("refresh_token_not_found") ||
                        errorMsg.Contains("invalid token claim") ||
                        errorMsg.Contains("token is expired"))
                    {
                        Debug.LogWarning("Se detectó que el usuario ya no existe o el token es inválido/expirado.");
                        await HandleInvalidRefreshToken(_userID);
                        return;
                    }
                }
            }
            
            // Intentar restaurar desde caché de Supabase
            try {
                var session = await _supabase.Auth.RetrieveSessionAsync();
                if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                {
                    _isLoggedIn = true;
                    _userID = session.User.Id;
                    _userEmail = session.User.Email ?? "";
                    
                    // Guardar tokens
                    SaveSessionTokens(session);
                    SaveUserToSqlite();
                    UpdateUIForLoggedInUser();
                    Debug.Log($"AuthManager: Sesión recuperada de caché para usuario {_userEmail}");
                    return;
                }
            } 
            catch (Exception e) 
            {
                Debug.LogError($"Error al recuperar sesión de caché: {e.Message}");
                
                // Verificar tipos de errores
                string errorMsg = e.Message.ToLower();
                
                // Si el usuario no existe o el token es inválido
                if (errorMsg.Contains("user_not_found") || 
                    errorMsg.Contains("invalid refresh token") ||
                    errorMsg.Contains("refresh_token_not_found") ||
                    errorMsg.Contains("invalid token claim") ||
                    errorMsg.Contains("token is expired"))
                {
                    Debug.LogWarning("Se detectó que el usuario ya no existe o el token es inválido en la caché.");
                    await HandleInvalidRefreshToken(_userID);
                    return;
                }
            }
            
            Debug.Log("AuthManager: No se pudo restaurar la sesión, cargando usuario por defecto");
            LoadDefaultUser();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al restaurar sesión: {e.Message}");
            LoadDefaultUser();
        }
    }
    
    private async Task HandleInvalidRefreshToken(string userId)
    {
        Debug.Log($"Manejando token inválido o usuario inexistente para usuario {userId}...");
        
        try
        {
            // 1. Eliminar datos de MongoDB si está disponible
            if (MongoDbService.Instance != null && MongoDbService.Instance.IsConnected())
            {
                Debug.Log($"Eliminando datos de MongoDB para usuario {userId}...");
                
                try 
                {
                    // Eliminar todos los datos del usuario
                    await MongoDbService.Instance.DeleteUserDataAsync(userId);
                    Debug.Log($"Todos los datos de MongoDB para usuario {userId} eliminados");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error al eliminar datos de MongoDB: {e.Message}");
                }
            }
            
            // 2. Eliminar todos los datos en SQLite
            Debug.Log($"Eliminando datos de SQLite para usuario {userId}...");
            
            try 
            {
                // Eliminar perfiles
                var localProfiles = SqliteDatabase.Instance.GetProfiles(userId);
                foreach (var profile in localProfiles)
                {
                    SqliteDatabase.Instance.DeleteProfile(profile.ProfileID);
                    Debug.Log($"Perfil SQLite eliminado: {profile.ProfileID}");
                }
                
                // Sobrescribir configuración con valores por defecto
                var config = SqliteDatabase.Instance.GetConfiguration(userId);
                if (config != null)
                {
                    SqliteDatabase.Instance.SaveConfiguration(userId, 3, false, true, 50, 50, 50, 50, false);
                    Debug.Log("Configuración de usuario restablecida a valores por defecto");
                }
                
                // Sobrescribir control parental con valores por defecto
                var parental = SqliteDatabase.Instance.GetParentalControl(userId);
                if (parental != null)
                {
                    SqliteDatabase.Instance.SaveParentalControl(userId, false, "", true, true, true, true, true);
                    Debug.Log("Control parental de usuario restablecido a valores por defecto");
                }
                
                Debug.Log($"Datos de SQLite para usuario {userId} eliminados/restablecidos");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error al eliminar datos de SQLite: {e.Message}");
            }
            
            // 3. Limpiar tokens
            ClearSessionTokens();
            Debug.Log("Tokens de sesión eliminados");
            
            // 4. Volver al usuario por defecto
            _isLoggedIn = false;
            _userID = DEFAULT_USER_ID;
            _userEmail = "";
            
            // 5. Asegurar que existe el usuario por defecto
            EnsureDefaultUserExists();
            UpdateUIForDefaultUser();
            
            Debug.Log("Usuario restablecido a por defecto después de detectar un token inválido o usuario inexistente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al manejar token inválido: {e.Message}");
            // Asegurar que volvemos al usuario por defecto incluso si hay errores
            LoadDefaultUser();
        }
    }
    
    // Guarda o actualiza el usuario en SQLite
    private void SaveUserToSqlite()
    {
        try
        {
            SqliteDatabase.Instance.SaveUser(_userID, _userEmail, false);
            Debug.Log($"AuthManager: Usuario guardado en SQLite: {_userID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar usuario en SQLite: {e.Message}");
        }
    }

    public void LoadDefaultUser()
    {
        _isLoggedIn = false;
        _userID = DEFAULT_USER_ID;
        _userEmail = "";
        
        // Asegurar que existe el usuario por defecto
        EnsureDefaultUserExists();
        
        // Limpiar tokens
        ClearSessionTokens();
        UpdateUIForDefaultUser();
        
        Debug.Log("AuthManager: Usuario por defecto cargado");
    }
    
    private void EnsureDefaultUserExists()
    {
        try
        {
            SqliteDatabase.Instance.SaveUser(DEFAULT_USER_ID, "", false);
            Debug.Log("AuthManager: Usuario por defecto verificado/creado correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear usuario por defecto: {e.Message}");
        }
    }

    
    // Actualizar UI según estado de sesión
    private void UpdateUIForLoggedInUser()
    {
        if (welcomeText != null)
            welcomeText.text = "¡BIENVENIDO!";
    }
    
    private void UpdateUIForDefaultUser()
    {
        if (welcomeText != null)
            welcomeText.text = "INICIAR SESIÓN";
    }
    
    // Iniciar sesión
    public async Task<bool> LoginUser(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignInWithPassword(email, password);
    
            if (session?.User != null)
            {
                _isLoggedIn = true;
                _userID = session.User.Id;
                _userEmail = session.User.Email ?? "";
        
                SaveSessionTokens(session);
            
                // Guardar usuario en SQLite y actualizar último login
                SqliteDatabase.Instance.SaveUser(_userID, _userEmail, false);
            
                if (DataManager.Instance != null)
                {
                    // Sincronizar datos de usuario
                    await DataManager.Instance.SyncUserDataAsync(_userID);
                
                    // Registrar login en MongoDB
                    if (MongoDbService.Instance.IsConnected())
                    {
                        await MongoDbService.Instance.SaveLoginAsync(_userID, _userEmail);
                    }
                }
        
                UpdateUIForLoggedInUser();
                Debug.Log($"AuthManager: Inicio de sesión exitoso para {email}");
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al iniciar sesión: {e.Message}");
            return false;
        }
    }
    
    // Cerrar sesión
    public async Task<bool> LogoutUser()
    {
        try
        {
            if (_isLoggedIn)
                await _supabase.Auth.SignOut();
                
            LoadDefaultUser();
            Debug.Log("AuthManager: Sesión cerrada correctamente");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cerrar sesión: {e.Message}");
            return false;
        }
    }
    
    // Guardar tokens
    private void SaveSessionTokens(Session session)
    {
        if (session != null)
        {
            PlayerPrefs.SetString(ACCESS_TOKEN_KEY, session.AccessToken);
            PlayerPrefs.SetString(REFRESH_TOKEN_KEY, session.RefreshToken);
            PlayerPrefs.Save();
            Debug.Log("AuthManager: Tokens de sesión guardados");
        }
    }
    
    // Limpiar tokens
    public void ClearSessionTokens()
    {
        PlayerPrefs.DeleteKey(ACCESS_TOKEN_KEY);
        PlayerPrefs.DeleteKey(REFRESH_TOKEN_KEY);
        PlayerPrefs.Save();
        Debug.Log("AuthManager: Tokens de sesión eliminados");
    }
    
    public bool IsInitialized()
    {
        return _initialized;
    }
    
    // Método para verificar el estado actual y asegurar que la base de datos esté en buen estado
    public void ValidateCurrentState()
    {
        if (!_initialized)
        {
            Debug.LogWarning("AuthManager: No está completamente inicializado al validar estado");
            return;
        }
    
        try
        {
            var user = SqliteDatabase.Instance.GetUser(_userID);
            if (user == null)
            {
                SqliteDatabase.Instance.SaveUser(_userID, _userEmail, false);
            }
        
            Debug.Log($"AuthManager: Estado validado para usuario {_userID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al validar estado actual: {e.Message}");
        }
    }
    
    public static bool IsDefaultUser(string userId)
    {
        return userId == DEFAULT_USER_ID;
    }
}