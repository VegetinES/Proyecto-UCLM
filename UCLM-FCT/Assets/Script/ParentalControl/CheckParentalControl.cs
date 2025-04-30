using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Realms;

public enum ParentalSection
{
    Sound,
    Accessibility,
    Statistics,
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
            // Obtener ID del usuario actual
            string userId = DataService.Instance.GetCurrentUserId();
            bool isActive = false;
            
            using (var realm = Realm.GetInstance(new RealmConfiguration { SchemaVersion = 1 }))
            {
                // Primero verificamos si el control parental está configurado (tiene un PIN)
                bool parentalControlConfigured = false;
                
                // Verificar usuario actual
                var user = realm.Find<User>(userId);
                if (user != null && user.ParentalControl != null)
                {
                    // Un control parental está configurado si tiene un PIN y está activado
                    if (user.ParentalControl.Activated && !string.IsNullOrEmpty(user.ParentalControl.Pin))
                    {
                        parentalControlConfigured = true;
                        
                        // Verificar la sección específica
                        switch (section)
                        {
                            case ParentalSection.Sound:
                                isActive = user.ParentalControl.SoundConf;
                                break;
                            case ParentalSection.Accessibility:
                                isActive = user.ParentalControl.AccessibilityConf;
                                break;
                            case ParentalSection.Statistics:
                                isActive = user.ParentalControl.StatisticsConf;
                                break;
                            case ParentalSection.About:
                                isActive = user.ParentalControl.AboutConf;
                                break;
                            case ParentalSection.Profile:
                                isActive = user.ParentalControl.ProfileConf;
                                break;
                        }
                    }
                }
                
                // Verificar usuario por defecto si es necesario
                if (!parentalControlConfigured && userId != AuthManager.DEFAULT_USER_ID)
                {
                    var defaultUser = realm.Find<User>(AuthManager.DEFAULT_USER_ID);
                    if (defaultUser != null && defaultUser.ParentalControl != null)
                    {
                        // Verificar que tiene PIN y está activado
                        if (defaultUser.ParentalControl.Activated && !string.IsNullOrEmpty(defaultUser.ParentalControl.Pin))
                        {
                            parentalControlConfigured = true;
                            
                            // Verificar la sección específica para el usuario por defecto
                            switch (section)
                            {
                                case ParentalSection.Sound:
                                    isActive = defaultUser.ParentalControl.SoundConf;
                                    break;
                                case ParentalSection.Accessibility:
                                    isActive = defaultUser.ParentalControl.AccessibilityConf;
                                    break;
                                case ParentalSection.Statistics:
                                    isActive = defaultUser.ParentalControl.StatisticsConf;
                                    break;
                                case ParentalSection.About:
                                    isActive = defaultUser.ParentalControl.AboutConf;
                                    break;
                                case ParentalSection.Profile:
                                    isActive = defaultUser.ParentalControl.ProfileConf;
                                    break;
                            }
                        }
                    }
                }
                
                // Si el control parental no está configurado, simplemente devolver false
                if (!parentalControlConfigured)
                {
                    return false;
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