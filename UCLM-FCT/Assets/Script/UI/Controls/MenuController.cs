using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuController : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    [SerializeField] private Button level3Button;
    
    [Header("Paneles de selección de perfil")]
    public GameObject profileSelectionPanel;
    
    [Header("Selección de personaje")]
    [SerializeField] private Image[] characterImages; // Las 6 imágenes para seleccionar
    [SerializeField] private Color selectedColor = new Color(0.7f, 0.7f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    
    [Header("Opciones de depuración")]
    [SerializeField] private bool fixTutorStatus = true;

    private int selectedCharacterIndex = -1; // -1 significa que no hay selección, será aleatorio

    private void Start()
    {
        // Asignar listeners a los botones de nivel
        if (level1Button != null)
            level1Button.onClick.AddListener(OnLevel1ButtonClick);
        if (level2Button != null)
            level2Button.onClick.AddListener(OnLevel2ButtonClick);
        if (level3Button != null)
            level3Button.onClick.AddListener(OnLevel3ButtonClick);

        // Configurar botones de personajes
        SetupCharacterButtons();

        // Esperar antes de verificar usuario para dar tiempo a que todo se inicialice
        Invoke("CheckUserAfterDelay", 2.0f);
    }
    
    private void SetupCharacterButtons()
    {
        // Configurar imágenes de personajes
        if (characterImages != null)
        {
            for (int i = 0; i < characterImages.Length; i++)
            {
                if (characterImages[i] != null)
                {
                    int index = i; // Necesario para capturar el valor correcto en el lambda
                    
                    // Añadir componente Button si no existe
                    Button button = characterImages[i].GetComponent<Button>();
                    if (button == null)
                        button = characterImages[i].gameObject.AddComponent<Button>();
                    
                    // Configurar colores normales del botón
                    ColorBlock colors = button.colors;
                    colors.normalColor = normalColor;
                    colors.selectedColor = selectedColor;
                    button.colors = colors;
                    
                    // Asignar listener
                    button.onClick.AddListener(() => SelectCharacter(index));
                }
            }
        }
    }
    
    private void SelectCharacter(int index)
    {
        // Guardar el índice seleccionado
        selectedCharacterIndex = index;
        
        // Actualizar colores de las imágenes
        for (int i = 0; i < characterImages.Length; i++)
        {
            if (characterImages[i] != null)
            {
                characterImages[i].color = (i == index) ? selectedColor : normalColor;
            }
        }
        
        // Guardar selección en PlayerPrefs para que persista
        PlayerPrefs.SetInt("SelectedCharacterIndex", index);
        PlayerPrefs.Save();
        
        Debug.Log($"Personaje seleccionado: {index}");
    }
    
    private void CheckUserAfterDelay()
    {
        // Simple comprobación de seguridad
        if (AuthManager.Instance == null)
        {
            Debug.LogWarning("AuthManager no está disponible todavía. No se puede verificar usuario.");
            return;
        }
        
        if (SqliteDatabase.Instance == null)
        {
            Debug.LogWarning("SqliteDatabase no está disponible todavía. No se puede verificar usuario.");
            return;
        }
        
        try
        {
            string currentUserId = AuthManager.Instance.UserID;
            
            // Verificar si es el usuario por defecto
            if (currentUserId == AuthManager.DEFAULT_USER_ID)
            {
                Debug.Log("Usuario actual es el usuario por defecto");
                return;
            }
            
            // Buscar usuario en SQLite
            var currentUser = SqliteDatabase.Instance.GetUser(currentUserId);
            
            // Si el usuario existe y es tutor
            if (currentUser != null && currentUser.IsTutor)
            {
                Debug.Log("Usuario es tutor, verificando perfiles");
                
                // Verificar perfiles
                var profiles = SqliteDatabase.Instance.GetProfiles(currentUserId);
                
                if (profiles.Count > 0 && profileSelectionPanel != null)
                {
                    // Activar el panel de selección de perfiles
                    profileSelectionPanel.SetActive(true);
                }
                else
                {
                    // Si no hay perfiles, ir a la página de perfil
                    SceneManager.LoadScene("Profile");
                }
            }
            else if (currentUser != null && !currentUser.IsTutor && fixTutorStatus)
            {
                // Si el usuario existe pero no es tutor, forzar tutor si está habilitado
                Debug.Log("Corrigiendo estado de tutor");
                SqliteDatabase.Instance.SaveUser(currentUserId, currentUser.Email, true);
                
                // Recargar esta misma escena para que tome efecto el cambio
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al verificar usuario: " + e.Message);
        }
    }
    
    public void OnLevel1ButtonClick()
    {
        // Guardar el índice del personaje antes de cargar el nivel
        SaveCharacterSelection();
        LevelLoader.LoadLevel(1);
    }

    public void OnLevel2ButtonClick()
    {
        // Guardar el índice del personaje antes de cargar el nivel
        SaveCharacterSelection();
        LevelLoader.LoadLevel(2);
    }

    public void OnLevel3ButtonClick()
    {
        // Guardar el índice del personaje antes de cargar el nivel
        SaveCharacterSelection();
        LevelLoader.LoadLevel(3);
    }
    
    private void SaveCharacterSelection()
    {
        // Si no se ha seleccionado ningún personaje, elegir uno aleatoriamente
        if (selectedCharacterIndex == -1)
        {
            selectedCharacterIndex = Random.Range(0, characterImages.Length);
            Debug.Log($"No había personaje seleccionado. Seleccionando aleatoriamente: {selectedCharacterIndex}");
        }
        
        // Guardar la selección para que el nivel la pueda leer
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedCharacterIndex);
        PlayerPrefs.Save();
    }
    
    private void OnDestroy()
    {
        // Limpiar listeners de botones de nivel
        if (level1Button != null)
            level1Button.onClick.RemoveListener(OnLevel1ButtonClick);
        if (level2Button != null)
            level2Button.onClick.RemoveListener(OnLevel2ButtonClick);
        if (level3Button != null)
            level3Button.onClick.RemoveListener(OnLevel3ButtonClick);
        
        // Limpiar listeners de botones de personajes
        if (characterImages != null)
        {
            for (int i = 0; i < characterImages.Length; i++)
            {
                if (characterImages[i] != null)
                {
                    Button button = characterImages[i].GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                    }
                }
            }
        }
    }
}