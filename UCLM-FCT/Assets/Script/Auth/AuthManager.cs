using System;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Supabase.Gotrue;
using TMPro;
using Realms;
using Client = Supabase.Client;

public class AuthManager : MonoBehaviour
{
    // Configuración de Supabase
    public const string SUPABASE_URL = "https://ujbqtvsbrwcgnxveufto.supabase.co";
    public const string SUPABASE_PUBLIC_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVqYnF0dnNicndjZ254dmV1ZnRvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDM0MTUxMjUsImV4cCI6MjA1ODk5MTEyNX0.Cb5rLYkElmNtxNeNqMDOzXccDIEcyaNUYYqQdpuSsG8";
    
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
                    SaveUserToRealm();
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
                SaveUserToRealm();
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
    
    // Guarda o actualiza el usuario en Realm
    private void SaveUserToRealm()
    {
        try
        {
            if (DataService.Instance == null)
            {
                Debug.LogError("AuthManager: DataService no disponible al intentar guardar usuario");
                return;
            }
            
            // Comprobar si el usuario existe, si no, crearlo
            var existingUser = DataService.Instance.UserRepo.GetById(_userID);
        
            if (existingUser == null)
            {
                // Crear un nuevo usuario
                var newUser = new User
                {
                    UID = _userID,
                    Email = _userEmail,
                    CreationDate = DateTimeOffset.Now,
                    LastLogin = DateTimeOffset.Now
                };
            
                DataService.Instance.UserRepo.Add(newUser);
            
                // Crear configuración y control parental por defecto
                DataService.Instance.EnsureUserExists(_userID);
                Debug.Log($"AuthManager: Usuario nuevo creado en Realm: {_userID}");
            }
            else
            {
                // Actualizar el email si es necesario
                DataService.Instance.UserRepo.Update(existingUser, user => {
                    user.Email = _userEmail;
                    user.LastLogin = DateTimeOffset.Now;
                });
            
                // Registrar el login
                DataService.Instance.UserRepo.RegisterLogin(_userID);
                Debug.Log($"AuthManager: Usuario existente actualizado en Realm: {_userID}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar usuario en Realm: {e.Message}");
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
            if (DataService.Instance == null)
            {
                Debug.LogError("AuthManager: DataService no disponible al intentar crear usuario por defecto");
                return;
            }
            
            DataService.Instance.EnsureUserExists(DEFAULT_USER_ID);
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
                
                // Guardar tokens
                SaveSessionTokens(session);
                SaveUserToRealm();
                
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
            if (DataService.Instance != null)
            {
                // Verificar que el usuario actual existe en la base de datos
                DataService.Instance.EnsureUserExists(_userID);
                Debug.Log($"AuthManager: Estado validado para usuario {_userID}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al validar estado actual: {e.Message}");
        }
    }
}