using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGame : MonoBehaviour
{
    [SerializeField] private GameObject exitGamePanel;

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.Escape)) || (Application.platform == RuntimePlatform.Android && Input.GetKeyDown(KeyCode.Escape)))
        {
            exitGamePanel.SetActive(true);
        }
    }

    // Método para salir del juego
    public void QuitGame()
    {
        #if UNITY_EDITOR
            // Si estamos en el editor, detenemos el modo de juego
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // En una compilación real, cerramos la aplicación
            Application.Quit();
        #endif
        
        Debug.Log("Saliendo del juego");
    }

    public void ClosePanel()
    {
        exitGamePanel.SetActive(false);
    }
}