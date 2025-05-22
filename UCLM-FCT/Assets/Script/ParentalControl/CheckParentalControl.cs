using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum ParentalSection
{
    Sound,
    Accessibility,
    Statistics,
    Parental,
    About,
    Profile
}

public class CheckParentalControl : MonoBehaviour, IPointerClickHandler
{
    [Header("Navegación en la misma escena")]
    public GameObject nextPage;
    public GameObject actualPage;
    
    [Header("Navegación entre escenas")]
    [Tooltip("Nombre de la escena a cargar (dejar vacío si navegamos en la misma escena)")]
    public string targetSceneName;
    
    [Tooltip("Si es true, intentará primero cargar otra escena. Si es false o hay error, navegará entre páginas de la misma escena")]
    public bool prioritizeSceneNavigation = false;
    
    [Header("Control Parental")]
    [Tooltip("Sección que está intentando acceder")]
    public ParentalSection section;
    
    [Tooltip("GameObject que se activa cuando el control parental está activado")]
    public GameObject parentalControlPanel;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Verificar el control parental primero
        if (IsParentalControlActive())
        {
            // Si el control parental está activo, mostrar el panel de verificación
            if (parentalControlPanel != null)
            {
                parentalControlPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Panel de control parental no asignado. No se puede verificar el PIN.");
            }
        }
        else
        {
            // Si el control parental no está activo, continuar con la navegación
            Navigate();
        }
    }
    
    private bool IsParentalControlActive()
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            int profileId = GetCurrentProfileId();
            bool isActive = false;
            
            // Primero, intentar obtener configuración específica del perfil
            LocalParentalControl parentalControl = null;
            
            if (profileId > 0)
            {
                parentalControl = SqliteDatabase.Instance.GetParentalControl(userId, profileId);
            }
            
            // Si no hay configuración específica para el perfil, usar la configuración del usuario
            if (parentalControl == null)
            {
                parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
            }
            
            if (parentalControl != null && parentalControl.Activated && !string.IsNullOrEmpty(parentalControl.Pin))
            {
                switch (section)
                {
                    case ParentalSection.Sound:
                        isActive = parentalControl.SoundConf;
                        break;
                    case ParentalSection.Accessibility:
                        isActive = parentalControl.AccessibilityConf;
                        break;
                    case ParentalSection.Statistics:
                        isActive = parentalControl.StatisticsConf;
                        break;
                    case ParentalSection.Parental:
                        isActive = parentalControl.Activated;
                        break;
                    case ParentalSection.About:
                        isActive = parentalControl.AboutConf;
                        break;
                    case ParentalSection.Profile:
                        isActive = parentalControl.ProfileConf;
                        break;
                }
            }
            else if (userId != AuthManager.DEFAULT_USER_ID)
            {
                // Si estamos con una cuenta de usuario pero sin configuración de control parental,
                // verificar si hay configuración en el usuario por defecto como última opción
                var defaultParental = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
                if (defaultParental != null && defaultParental.Activated && !string.IsNullOrEmpty(defaultParental.Pin))
                {
                    switch (section)
                    {
                        case ParentalSection.Sound:
                            isActive = defaultParental.SoundConf;
                            break;
                        case ParentalSection.Accessibility:
                            isActive = defaultParental.AccessibilityConf;
                            break;
                        case ParentalSection.Statistics:
                            isActive = defaultParental.StatisticsConf;
                            break;
                        case ParentalSection.Parental:
                            isActive = defaultParental.Activated;
                            break;
                        case ParentalSection.About:
                            isActive = defaultParental.AboutConf;
                            break;
                        case ParentalSection.Profile:
                            isActive = defaultParental.ProfileConf;
                            break;
                    }
                }
            }
            
            return isActive;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al verificar control parental: " + e.Message);
            return false;
        }
    }
    
    private int GetCurrentProfileId()
    {
        return ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile() 
            ? ProfileManager.Instance.GetCurrentProfileId() 
            : 0;
    }
    
    public void Navigate()
    {
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
            if (SceneUtility.GetBuildIndexByScenePath("Scenes/" + targetSceneName) != -1 || SceneUtility.GetBuildIndexByScenePath(targetSceneName) != -1)
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