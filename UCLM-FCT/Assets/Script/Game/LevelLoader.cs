using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    // Nombre de la variable en PlayerPrefs para guardar el nivel
    private const string LEVEL_KEY = "SelectedLevel";
    
    // Nombre de la escena del juego
    private const string GAME_SCENE = "Game";
    
    public static void LoadLevel(int levelNumber)
    {
        // Guardar el nivel seleccionado
        PlayerPrefs.SetInt(LEVEL_KEY, levelNumber);
        PlayerPrefs.Save();
        
        Debug.Log("LevelLoader: Guardando nivel seleccionado: " + levelNumber);
        Debug.Log("LevelLoader: Intentando cargar escena: " + GAME_SCENE);
        
        // Carga la escena del juego
        SceneManager.LoadScene(GAME_SCENE);
    }
    
    public static int GetSelectedLevel()
    {
        int level = PlayerPrefs.GetInt(LEVEL_KEY, 1); // Default to level 1
        Debug.Log("LevelLoader: Obteniendo nivel seleccionado: " + level);
        return level;
    }
}