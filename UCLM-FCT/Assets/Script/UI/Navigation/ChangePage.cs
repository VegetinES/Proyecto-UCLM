using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ChangePage : MonoBehaviour, IPointerClickHandler
{
    [Header("Navegación en la misma escena")]
    public GameObject nextPage;
    public GameObject actualPage;
    
    [Header("Navegación entre escenas")]
    [Tooltip("Nombre de la escena a cargar (dejar vacío si navegamos en la misma escena)")]
    public string targetSceneName;
    
    [Tooltip("Si es true, intentará primero cargar otra escena. Si es false o hay error, navegará entre páginas de la misma escena")]
    public bool prioritizeSceneNavigation = false;
    
    private async Task PrepareForExit()
    {
        // Si hay un StatisticsManager, preparar para salir
        if (StatisticsManager.Instance != null)
        {
            // Esto esperará a que se guarden las estadísticas
            await StatisticsManager.Instance.PrepareForExit();
        }
    }
    
    public async void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Image clicked");
        
        // Si estamos en una escena de juego, preparar para salir
        if (SceneManager.GetActiveScene().name == "Game")
        {
            await PrepareForExit();
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
        else
        {
            Debug.LogWarning("La página actual es null (si se hace con esa intención ignora esta advertencia)");
        }
        
        if (nextPage != null)
        {
            nextPage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("La página de destino es null (si se hace con esa intención ignora esta advertencia)");
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
                // Se carga la escena entonces
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning($"La escena '{targetSceneName}' no existe o no está incluida en el Build. Verificando si podemos navegar entre páginas en su lugar.");
                
                // Si la escena no existe hay referencias de páginas intentamos navegar entre ellas
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