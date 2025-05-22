using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileButton : MonoBehaviour
{
    private int profileId;
    private string profileName;
    
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }
    
    public void SetProfileData(int id, string name)
    {
        profileId = id;
        profileName = name;
    }
    
    private void OnButtonClicked()
    {
        // Guardar el perfil seleccionado en ProfileManager
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.SetCurrentProfile(profileId, profileName);
            
        // Cargar la escena de configuración de perfil
        SceneManager.LoadScene("ConfigurationProfile");
    }
}