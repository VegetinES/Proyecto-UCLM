using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ProfileSelector : MonoBehaviour
{
    [Header("Botones de perfil")]
    public Button[] profileButtons;
    
    [Header("Botón de tutor")]
    public Button tutorButton;
    
    private void Start()
    {
        LoadProfiles();
        
        // Configurar botón de tutor
        if (tutorButton != null)
            tutorButton.onClick.AddListener(OnTutorButtonClicked);
    }

    public void LoadProfiles()
    {
        if (ProfileManager.Instance == null || DataManager.Instance == null) return;
        
        string userId = DataManager.Instance.GetCurrentUserId();
        var profiles = SqliteDatabase.Instance.GetProfiles(userId);
        
        // Desactivar todos los botones inicialmente
        foreach (Button button in profileButtons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
        
        // Activar botones según los perfiles encontrados
        for (int i = 0; i < profiles.Count && i < profileButtons.Length; i++)
        {
            if (profileButtons[i] != null)
            {
                profileButtons[i].gameObject.SetActive(true);
                
                // Configurar texto del botón
                TMP_Text buttonText = profileButtons[i].GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                    buttonText.text = profiles[i].Name;
                
                // Configurar el botón de perfil
                int profileId = profiles[i].ProfileID;
                string profileName = profiles[i].Name;
                
                profileButtons[i].onClick.RemoveAllListeners();
                profileButtons[i].onClick.AddListener(() => OnProfileButtonClicked(profileId, profileName));
            }
        }
    }
    
    private void OnProfileButtonClicked(int profileId, string profileName)
    {
        // Guardar el perfil seleccionado
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SetCurrentProfile(profileId, profileName);
            
        // Desactivar el panel y continuar al juego
        gameObject.SetActive(false);
    }
    
    private void OnTutorButtonClicked()
    {
        // Cargar la escena de perfil para gestión de perfiles
        SceneManager.LoadScene("Profile");
    }
}