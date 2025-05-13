using System;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Supabase.Gotrue;
using TMPro;
using Client = Supabase.Client;

public class AuthManager : MonoBehaviour
{
    // Configuración de Supabase
    public const string SUPABASE_URL = "https://ujbqtvsbrwcgnxveufto.supabase.co";
    public const string SUPABASE_PUBLIC_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVqYnF0dnNicndjZ254dmV1ZnRvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc0NjIwNDczNywiZXhwIjoyMDYxNzgwNzM3fQ.b_ZZpJzDM9W9itDqxOlm4cRd78m9Dlai1rnM2HbjyBA";
    
    // Claves para PlayerPrefs
    private const string ACCESS_TOKEN_KEY = "supabase_access_token";
    private const string REFRESH_TOKEN_KEY = "supabase_refresh_token";
    
    // ID de usuario por defecto
    public const string DEFAULT_USER_ID = "1";
    
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
            InitializeSupabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private async void InitializeSupabase()
    {
        try
        {
            Debug.Log("AuthManager: Inicializando Supabase...");
            _supabase = new Client(SUPABASE_URL, SUPABASE_PUBLIC_KEY);
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
                await _supabase.Auth.SetSession(accessToken, refreshToken);
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
            
            // Intentar restaurar desde caché de Supabase
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
            }
            else
            {
                Debug.Log("AuthManager: No se pudo restaurar la sesión, cargando usuario por defecto");
                LoadDefaultUser();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al restaurar sesión: {e.Message}");
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
    
    private void LoadDefaultUser()
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
    private void ClearSessionTokens()
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