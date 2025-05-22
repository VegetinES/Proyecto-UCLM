using System;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Client = Supabase.Client;

public class RegisterManager : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Toggle isTutorToggle;
    public Button registerButton;
    
    [Header("Mensajes de error")]
    public GameObject errorPanel;
    public TMP_Text errorMessageText;
    
    [Header("Navegación")]
    public string menuScene = "Menu";
    public string profileScene = "Profile";
    
    [Header("Objetos UI")]
    public GameObject tutorObject;
    public GameObject noTutorObject;
    
    [Header("Depuración")]
    public bool enableDebugLogs = true;
    
    private void Start()
    {
        // Ocultar mensaje de error al inicio
        if (errorPanel != null) 
            errorPanel.SetActive(false);
            
        // Ocultar objetos de navegación
        if (tutorObject != null)
            tutorObject.SetActive(false);
            
        if (noTutorObject != null)
            noTutorObject.SetActive(false);
        
        // Asignar el evento de clic al botón
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterButtonClick);
    }
    
    public async void OnRegisterButtonClick()
    {
        DebugLog("Botón de registro presionado");
        
        // Deshabilitar botón durante el registro
        if (registerButton != null)
            registerButton.interactable = false;
            
        // Ocultar mensaje de error
        if (errorPanel != null)
            errorPanel.SetActive(false);
            
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;
        bool isTutor = isTutorToggle.isOn;
        
        DebugLog($"Validando: Email={email}, Contraseñas coinciden={password == confirmPassword}, Es tutor={isTutor}");
        
        // Validar email
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowError("El correo electrónico no es válido");
            return;
        }
        
        // Verificar contraseñas
        if (!PasswordsMatch(password, confirmPassword))
        {
            ShowError("Las contraseñas no coinciden");
            return;
        }
        
        if (!IsValidPassword(password))
        {
            ShowError("La contraseña debe tener al menos 6 caracteres, una mayúscula, una minúscula y un número");
            return;
        }
        
        try {
            // Intentar registrar al usuario
            bool success = await RegisterUser(email, confirmPassword, isTutor);
            
            if (success)
            {
                DebugLog("Usuario registrado correctamente");
                
                // Verificar que el usuario esté marcado como tutor si corresponde
                VerifyTutorStatus(AuthManager.Instance.UserID, isTutor);
                
                // Redirigir según tipo de usuario
                if (isTutor)
                {
                    DebugLog("Redirigiendo a escena de Perfil (usuario tutor)");
                    SceneManager.LoadScene(profileScene);
                }
                else
                {
                    DebugLog("Redirigiendo a escena de Menú (usuario no tutor)");
                    SceneManager.LoadScene(menuScene);
                }
            }
            else
            {
                ShowError("Error al registrar el usuario. Inténtalo de nuevo.");
            }
        }
        catch (Exception e) {
            ShowError($"Error: {e.Message}");
            DebugLog($"Excepción al registrar: {e}", true);
        }
    }
    
    private bool IsValidEmail(string email)
    {
        // Usar una expresión regular para validar el formato del email
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
    
    private bool PasswordsMatch(string password, string confirmPassword)
    {
        return password == confirmPassword;
    }
    
    private bool IsValidPassword(string password)
    {
        // Verificar longitud mínima
        if (password.Length < 6)
            return false;
            
        // Verificar si contiene al menos una mayúscula
        if (!Regex.IsMatch(password, @"[A-Z]"))
            return false;
            
        // Verificar si contiene al menos una minúscula
        if (!Regex.IsMatch(password, @"[a-z]"))
            return false;
            
        // Verificar si contiene al menos un número
        if (!Regex.IsMatch(password, @"[0-9]"))
            return false;
            
        // Verificar si contiene espacios
        if (password.Contains(" "))
            return false;
            
        return true;
    }
    
    private async System.Threading.Tasks.Task<bool> RegisterUser(string email, string password, bool isTutor)
    {
        try
        {
            DebugLog("Iniciando proceso de registro en Supabase...");
        
            // Obtener variables de entorno
            string supabaseUrl = EnvironmentLoader.GetVariable("SUPABASE_URL", "");
            string supabaseKey = EnvironmentLoader.GetVariable("SUPABASE_PUBLIC_KEY", "");
        
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                DebugLog("No se pudieron cargar las variables de entorno para Supabase", true);
                return false;
            }
        
            // Crear cliente con las claves cargadas desde .env
            var client = new Client(supabaseUrl, supabaseKey);
            await client.InitializeAsync();
        
            // Registrar usuario en Supabase
            var response = await client.Auth.SignUp(email, password);
        
            if (response?.User != null)
            {
                string userId = response.User.Id;
                DebugLog($"Usuario creado en Supabase con ID: {userId}");
            
                // Iniciar sesión con el usuario recién creado
                bool loginSuccess = await AuthManager.Instance.LoginUser(email, password);
                
                if (loginSuccess)
                {
                    // Actualizar propiedad de tutor
                    DebugLog($"Inicio de sesión exitoso, actualizando como tutor: {isTutor}");
                    
                    // Guardar explícitamente en SQLite el estado de tutor
                    SqliteDatabase.Instance.SaveUser(userId, email, isTutor);
                    
                    // Verificar que se haya guardado correctamente
                    var verifiedUser = SqliteDatabase.Instance.GetUser(userId);
                    DebugLog($"Estado de tutor verificado: {verifiedUser?.IsTutor}");
                    
                    // Asegurarse de que AuthManager tenga los datos correctos
                    if (AuthManager.Instance != null)
                    {
                        DebugLog("Validando estado actual en AuthManager");
                        AuthManager.Instance.ValidateCurrentState();
                    }
                    
                    // Registrar fecha de creación en MongoDB si está disponible
                    if (MongoDbService.Instance != null && MongoDbService.Instance.IsConnected())
                    {
                        await MongoDbService.Instance.SaveUserDataAsync(
                            userId, 
                            email, 
                            new LocalConfiguration { 
                                UserID = userId,
                                Colors = 3, 
                                AutoNarrator = false 
                            }, 
                            new SharedModels.ParentalControl { 
                                Activated = false, 
                                Pin = "" 
                            }
                        );
                    }
                    
                    return true;
                }
                else
                {
                    DebugLog("Error al iniciar sesión después del registro", true);
                }
            }
            else
            {
                DebugLog("La respuesta de Supabase no contiene un usuario válido", true);
            }
            
            return false;
        }
        catch (Exception e)
        {
            DebugLog($"Error al registrar usuario: {e.Message}", true);
            throw; // Propagamos la excepción para mostrar un mensaje adecuado
        }
        finally
        {
            // Volver a habilitar el botón
            if (registerButton != null)
                registerButton.interactable = true;
        }
    }
    
    private void VerifyTutorStatus(string userId, bool expectedIsTutor)
    {
        try
        {
            // Verificar explícitamente que el estado de tutor se haya guardado correctamente
            var user = SqliteDatabase.Instance.GetUser(userId);
            
            if (user != null)
            {
                bool actualIsTutor = user.IsTutor;
                
                DebugLog($"Verificación de estado de tutor - Esperado: {expectedIsTutor}, Actual: {actualIsTutor}");
                
                if (actualIsTutor != expectedIsTutor)
                {
                    DebugLog("¡ADVERTENCIA! El estado de tutor no se guardó correctamente. Intentando corregir...", true);
                    
                    // Corregir con más énfasis, forzando la actualización
                    SqliteDatabase.Instance.SaveUser(userId, user.Email, expectedIsTutor);
                    
                    // Verificar una vez más
                    var updatedUser = SqliteDatabase.Instance.GetUser(userId);
                    if (updatedUser != null)
                    {
                        DebugLog($"Después de corrección - Estado de tutor: {updatedUser.IsTutor}");
                        
                        if (updatedUser.IsTutor != expectedIsTutor)
                        {
                            // Si todavía no coincide, intentar una última vez con un enfoque diferente
                            DebugLog("Segundo intento de corrección...", true);
                            
                            // Ejecutar una consulta directa para forzar la actualización
                            SqliteDatabase.Instance.SaveUser(userId, user.Email, expectedIsTutor);
                        }
                    }
                }
            }
            else
            {
                DebugLog($"No se puede verificar el estado de tutor: usuario {userId} no encontrado", true);
                
                // Intentar crear el usuario si no existe
                DebugLog("Intentando crear el usuario en SQLite...");
                SqliteDatabase.Instance.SaveUser(userId, AuthManager.Instance.UserEmail, expectedIsTutor);
            }
        }
        catch (Exception e)
        {
            DebugLog($"Error al verificar estado de tutor: {e.Message}", true);
        }
    }
    
    private void ShowError(string message)
    {
        DebugLog($"Error de registro: {message}", true);
        
        if (errorPanel != null)
            errorPanel.SetActive(true);
            
        if (errorMessageText != null)
            errorMessageText.text = message;
        
        // Volver a habilitar el botón
        if (registerButton != null)
            registerButton.interactable = true;
    }
    
    private void DebugLog(string message, bool isError = false)
    {
        if (enableDebugLogs)
        {
            if (isError)
                Debug.LogError($"[RegisterManager] {message}");
            else
                Debug.Log($"[RegisterManager] {message}");
        }
    }
    
    // Al destruir el componente, asegurarnos de limpiar los listeners
    private void OnDestroy()
    {
        if (registerButton != null)
            registerButton.onClick.RemoveListener(OnRegisterButtonClick);
    }
}