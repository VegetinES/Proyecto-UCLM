using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorProfilePanel : MonoBehaviour
{
    [Header("Botones")]
    public Button createProfileButton;
    public Button deleteProfileButton;
    public Button backToMenuButton;
    public Button[] profileButtons;
    
    [Header("Paneles")]
    public GameObject profileCreationPanel;
    
    private void Start()
    {
        InitializeButtons();
        LoadProfiles();
    }
    
    private void InitializeButtons()
    {
        // Configurar botón de crear perfil
        if (createProfileButton != null)
            createProfileButton.onClick.AddListener(OnCreateProfileClicked);
        
        // Configurar botón de eliminar perfil
        if (deleteProfileButton != null)
        {
            deleteProfileButton.onClick.AddListener(OnDeleteProfileClicked);
            deleteProfileButton.interactable = false; // Inicialmente desactivado
        }
        
        // Configurar botón de volver al menú
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            
        // Desactivar todos los botones de perfil inicialmente
        foreach (Button button in profileButtons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
    }

    public void LoadProfiles()
    {
        if (ProfileManager.Instance == null || DataManager.Instance == null) return;
        
        string userId = DataManager.Instance.GetCurrentUserId();
        var profiles = SqliteDatabase.Instance.GetProfiles(userId);
        
        // Activar botones según los perfiles encontrados
        for (int i = 0; i < profiles.Count && i < profileButtons.Length; i++)
        {
            if (profileButtons[i] != null)
            {
                profileButtons[i].gameObject.SetActive(true);
                
                // Configurar botón con datos del perfil
                TMP_Text buttonText = profileButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = profiles[i].Name;
                
                // Añadir el componente ProfileButton si no existe
                ProfileButton profileButton = profileButtons[i].GetComponent<ProfileButton>();
                if (profileButton == null)
                    profileButton = profileButtons[i].gameObject.AddComponent<ProfileButton>();
                
                // Configurar ID y nombre del perfil
                profileButton.SetProfileData(profiles[i].ProfileID, profiles[i].Name);
            }
        }
        
        // Activar botón de eliminar si hay al menos un perfil
        if (deleteProfileButton != null)
            deleteProfileButton.interactable = profiles.Count > 0;
    }
    
    private void OnCreateProfileClicked()
    {
        if (profileCreationPanel != null)
        {
            gameObject.SetActive(false);
            profileCreationPanel.SetActive(true);
        }
    }
    
    private void OnDeleteProfileClicked()
    {
        // Implementar lógica para eliminar el último perfil
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.DeleteLastProfile();
            
        // Recargar perfiles para actualizar la UI
        LoadProfiles();
    }
    
    private void OnBackToMenuClicked()
    {
        SceneManager.LoadScene("Menu");
    }
}