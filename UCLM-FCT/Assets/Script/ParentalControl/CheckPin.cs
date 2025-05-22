using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class CheckPin : MonoBehaviour, IPointerClickHandler
{
    [Header("Input del PIN")]
    public TMP_InputField pinInput;
    
    [Header("Mensaje de error")]
    public GameObject errorMessage;
    
    [Header("Navegación en la misma escena")]
    public GameObject nextPage;
    public GameObject actualPage;
    
    [Header("Panel de control parental")]
    [Tooltip("GameObject a desactivar cuando el PIN sea correcto (normalmente el panel de control parental)")]
    public GameObject panelToDeactivate;
    
    [Header("Navegación entre escenas")]
    [Tooltip("Nombre de la escena a cargar (dejar vacío si navegamos en la misma escena)")]
    public string targetSceneName;
    
    [Tooltip("Si es true, intentará primero cargar otra escena. Si es false o hay error, navegará entre páginas de la misma escena")]
    public bool prioritizeSceneNavigation = false;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Verificar que el input del PIN esté asignado
        if (pinInput == null)
        {
            Debug.LogError("El input del PIN no está asignado");
            return;
        }
        
        string pin = pinInput.text.Trim();
        
        // Comprobar PIN
        if (VerifyPin(pin))
        {
            // PIN correcto, navegar
            if (errorMessage != null)
                errorMessage.SetActive(false);
                
            Navigate();
        }
        else
        {
            // PIN incorrecto, mostrar error
            if (errorMessage != null)
                errorMessage.SetActive(true);
                
            Debug.Log("PIN incorrecto");
        }
    }
    
    private bool VerifyPin(string inputPin)
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            int profileId = GetCurrentProfileId();
            string storedHash = "";
            bool pinFound = false;
            
            // Primero, intentar obtener el PIN específico del perfil
            if (profileId > 0)
            {
                var profileParental = SqliteDatabase.Instance.GetParentalControl(userId, profileId);
                if (profileParental != null && !string.IsNullOrEmpty(profileParental.Pin))
                {
                    storedHash = profileParental.Pin;
                    pinFound = true;
                    Debug.Log($"PIN encontrado para el perfil {profileId} del usuario {userId}");
                }
            }
            
            // Si no se encontró PIN para el perfil, buscar para el usuario
            if (!pinFound)
            {
                var parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
                if (parentalControl != null && !string.IsNullOrEmpty(parentalControl.Pin))
                {
                    storedHash = parentalControl.Pin;
                    pinFound = true;
                    Debug.Log("PIN encontrado para el usuario actual");
                }
            }
        
            // Si no hay PIN para el usuario actual o su perfil, verificar usuario por defecto como última opción
            if (!pinFound && userId != AuthManager.DEFAULT_USER_ID)
            {
                var defaultParental = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
                if (defaultParental != null && !string.IsNullOrEmpty(defaultParental.Pin))
                {
                    storedHash = defaultParental.Pin;
                    pinFound = true;
                    Debug.Log("PIN encontrado para el usuario por defecto");
                }
            }
        
            if (!pinFound)
            {
                Debug.Log("No se encontró ningún PIN configurado. Redirigiendo sin verificación.");
                return true;
            }
        
            string inputHash = GetSHA256Hash(inputPin);
            return storedHash.Equals(inputHash);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al verificar PIN: " + e.Message);
            return false;
        }
    }
    
    private int GetCurrentProfileId()
    {
        return ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile() 
            ? ProfileManager.Instance.GetCurrentProfileId() 
            : 0;
    }
    
    private string GetSHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convertir el PIN a bytes
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            
            // Calcular el hash
            byte[] hashBytes = sha256.ComputeHash(bytes);
            
            // Convertir el hash a string hexadecimal
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }
    
    public void Navigate()
    {
        // Desactivar el panel de control parental si está definido
        if (panelToDeactivate != null)
        {
            panelToDeactivate.SetActive(false);
        }
        
        if (prioritizeSceneNavigation && !string.IsNullOrEmpty(targetSceneName))
        {
            LoadTargetScene();
        }
        else
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                // Si no hay nombre de escena, navegamos entre páginas
                ActivatePage();
            }
            else
            {
                // Si hay nombre de escena, cargamos la escena
                LoadTargetScene();
            }
        }
    }
    
    private void ActivatePage()
    {
        // Desactivar página actual y activar página a la que queremos ir
        if (actualPage != null)
        {
            actualPage.SetActive(false);
        }
        
        if (nextPage != null)
        {
            nextPage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("La página de destino es null.");
        }
    }
    
    private void LoadTargetScene()
    {
        try
        {
            // Verificar que la escena existe antes de intentar cargarla
            if (SceneUtility.GetBuildIndexByScenePath("Scenes/" + targetSceneName) != -1 || 
                SceneUtility.GetBuildIndexByScenePath(targetSceneName) != -1)
            {
                // La escena existe, procedemos a cargarla
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning($"La escena '{targetSceneName}' no existe o no está incluida en el Build. Verificando si podemos navegar entre páginas en su lugar.");
                
                // Si la escena no existe pero tenemos referencias a páginas, intentamos navegar entre ellas
                if (nextPage != null)
                {
                    ActivatePage();
                }
                else
                {
                    Debug.LogError($"No se pudo cargar la escena '{targetSceneName}' y no hay una página de destino configurada.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cargar la escena: {e.Message}. Intentando navegar entre páginas si es posible.");
            
            // En caso de error, intentamos la navegación entre páginas como fallback
            if (nextPage != null)
            {
                ActivatePage();
            }
        }
    }
}