using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorProfileManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject tutorPanel;
    public GameObject createProfilePanel;

    [Header("Buttons")]
    public Button createProfileButton;
    public Button deleteProfileButton;
    public Button[] profileButtons; // Array de botones de perfil (máximo 4)

    [Header("Create Profile Panel")]
    public TMP_InputField profileNameInput;
    public ToggleSwitch parentalControlSwitch; // Cambiado de Toggle a ToggleSwitch
    public Image boyImage;
    public Image girlImage;
    public Button confirmCreateButton;

    private string selectedGender = "Male"; // Valor por defecto
    private List<UserProfile> profiles = new List<UserProfile>();
    private int maxProfiles = 4;

    [System.Serializable]
    public class UserProfile
    {
        public string name;
        public string gender;
        public bool parentalControlEnabled;
    }

    private void Start()
    {
        InitializeUI();
        SetupListeners();
    }

    private void InitializeUI()
    {
        // Panel inicial
        if (tutorPanel != null) tutorPanel.SetActive(true);
        if (createProfilePanel != null) createProfilePanel.SetActive(false);

        // Botones
        if (deleteProfileButton != null) deleteProfileButton.interactable = false;

        // Desactivar botones de perfil inicialmente
        foreach (Button btn in profileButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
    }

    private void SetupListeners()
    {
        // Botón para abrir panel de creación
        if (createProfileButton != null)
            createProfileButton.onClick.AddListener(ShowCreateProfilePanel);

        // Botón para confirmar creación
        if (confirmCreateButton != null)
            confirmCreateButton.onClick.AddListener(CreateNewProfile);

        // Botón para eliminar perfil
        if (deleteProfileButton != null)
            deleteProfileButton.onClick.AddListener(DeleteLastProfile);

        // Imágenes de género
        if (boyImage != null)
            boyImage.GetComponent<Button>().onClick.AddListener(() => SelectGender("Male"));

        if (girlImage != null)
            girlImage.GetComponent<Button>().onClick.AddListener(() => SelectGender("Female"));
    }

    private void SelectGender(string gender)
    {
        selectedGender = gender;
        
        // Resaltar la imagen seleccionada
        if (gender == "Male")
        {
            boyImage.color = new Color(0.7f, 0.7f, 1f);
            girlImage.color = Color.white;
        }
        else
        {
            boyImage.color = Color.white;
            girlImage.color = new Color(1f, 0.7f, 0.7f);
        }
    }

    private void ShowCreateProfilePanel()
    {
        // Verificar si podemos crear más perfiles
        if (profiles.Count >= maxProfiles)
        {
            Debug.Log("Número máximo de perfiles alcanzado");
            return;
        }

        tutorPanel.SetActive(false);
        createProfilePanel.SetActive(true);
        
        // Reiniciar formulario
        profileNameInput.text = "";
        if (parentalControlSwitch != null)
            parentalControlSwitch.SetState(false); // Usar SetState en lugar de isOn
        SelectGender("Male");
    }

    private void CreateNewProfile()
    {
        string profileName = profileNameInput.text.Trim();
        
        if (string.IsNullOrEmpty(profileName))
        {
            Debug.LogWarning("El nombre del perfil no puede estar vacío");
            return;
        }

        // Crear nuevo perfil
        UserProfile newProfile = new UserProfile
        {
            name = profileName,
            gender = selectedGender,
            parentalControlEnabled = parentalControlSwitch != null ? parentalControlSwitch.IsOn() : false
        };
        
        // Añadir a la lista
        profiles.Add(newProfile);
        
        // Guardar en base de datos
        SaveProfileToDatabase(newProfile);
        
        // Actualizar UI
        UpdateProfileButtons();
        
        // Volver al panel de tutor
        tutorPanel.SetActive(true);
        createProfilePanel.SetActive(false);
    }

    private async void SaveProfileToDatabase(UserProfile profile)
    {
        // Verificar que DataManager está disponible
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager no disponible");
            return;
        }

        string userId = DataManager.Instance.GetCurrentUserId();
        
        try
        {
            // Guardar localmente en SQLite
            int profileId = SqliteDatabase.Instance.SaveProfile(userId, profile.name, profile.gender);
            
            // Configurar el control parental si está activado
            if (profile.parentalControlEnabled)
            {
                SqliteDatabase.Instance.SaveParentalControl(userId, true, "", true, true, true, true, true, profileId);
            }
            
            Debug.Log($"Perfil guardado localmente con ID: {profileId}");
            
            // Sincronizar con MongoDB si está disponible y el usuario no es el predeterminado
            if (DataManager.Instance.IsOnline() && !AuthManager.IsDefaultUser(userId))
            {
                try
                {
                    await SyncProfileToMongoDB(userId, profileId, profile);
                    Debug.Log($"Perfil sincronizado con MongoDB: {profileId}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error al sincronizar perfil con MongoDB: {e.Message}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar perfil: {e.Message}");
        }
    }

    private async Task SyncProfileToMongoDB(string userId, int profileId, UserProfile profile)
    {
        if (MongoDbService.Instance == null || !MongoDbService.Instance.IsConnected())
        {
            Debug.LogWarning("MongoDB no está disponible para sincronización");
            return;
        }
        
        // Crear documento para el perfil
        var profileData = new Dictionary<string, object>
        {
            { "profileId", profileId },
            { "name", profile.name },
            { "gender", profile.gender },
            { "parentalControlEnabled", profile.parentalControlEnabled },
            { "createdAt", DateTime.UtcNow.ToString("o") }
        };
        
        // Obtener configuración por defecto si existe
        var config = SqliteDatabase.Instance.GetConfiguration(userId, profileId);
        var configData = config != null ? new Dictionary<string, object>
        {
            { "colors", config.Colors },
            { "autoNarrator", config.AutoNarrator },
            { "sound", config.Sound },
            { "generalSound", config.GeneralSound },
            { "musicSound", config.MusicSound },
            { "effectsSound", config.EffectsSound },
            { "narratorSound", config.NarratorSound },
            { "vibration", config.Vibration }
        } : null;
        
        // Guardar en MongoDB
        await MongoDbService.Instance.SaveProfileDataAsync(userId, profileId, profileData, configData);
    }

    private void UpdateProfileButtons()
    {
        // Activar botones según número de perfiles
        for (int i = 0; i < profileButtons.Length; i++)
        {
            if (profileButtons[i] != null)
            {
                bool shouldBeActive = i < profiles.Count;
                profileButtons[i].gameObject.SetActive(shouldBeActive);
                
                // Actualizar texto del botón con el nombre del perfil
                if (shouldBeActive)
                {
                    TMP_Text buttonText = profileButtons[i].GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = profiles[i].name;
                    }
                }
            }
        }
        
        // Activar botón de eliminar si hay perfiles
        if (deleteProfileButton != null)
        {
            deleteProfileButton.interactable = profiles.Count > 0;
        }
        
        // Desactivar botón de crear si se alcanzó el máximo
        if (createProfileButton != null)
        {
            createProfileButton.interactable = profiles.Count < maxProfiles;
        }
    }

    private void DeleteLastProfile()
    {
        if (profiles.Count == 0) return;
        
        // Eliminar el último perfil
        int lastIndex = profiles.Count - 1;
        UserProfile profileToDelete = profiles[lastIndex];
        
        // Eliminar de la base de datos
        DeleteProfileFromDatabase(profileToDelete);
        
        // Eliminar de la lista
        profiles.RemoveAt(lastIndex);
        
        // Actualizar UI
        UpdateProfileButtons();
    }

    private async void DeleteProfileFromDatabase(UserProfile profile)
    {
        // Verificar que DataManager está disponible
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager no disponible");
            return;
        }

        string userId = DataManager.Instance.GetCurrentUserId();
        
        try
        {
            // Buscar el ProfileID del perfil que queremos eliminar
            List<LocalProfile> dbProfiles = SqliteDatabase.Instance.GetProfiles(userId);
            
            // Buscar el perfil por nombre y género (ya que no tenemos el ID guardado en nuestra lista local)
            foreach (var dbProfile in dbProfiles)
            {
                if (dbProfile.Name == profile.name && dbProfile.Gender == profile.gender)
                {
                    int profileId = dbProfile.ProfileID;
                    
                    // Eliminar el perfil localmente
                    SqliteDatabase.Instance.DeleteProfile(profileId);
                    Debug.Log($"Perfil eliminado localmente: {profileId} - {dbProfile.Name}");
                    
                    // Sincronizar eliminación con MongoDB
                    if (DataManager.Instance.IsOnline() && !AuthManager.IsDefaultUser(userId))
                    {
                        try
                        {
                            await DeleteProfileFromMongoDB(userId, profileId);
                            Debug.Log($"Perfil eliminado de MongoDB: {profileId}");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Error al eliminar perfil de MongoDB: {e.Message}");
                        }
                    }
                    
                    return;
                }
            }
            
            Debug.LogWarning($"No se encontró el perfil '{profile.name}' en la base de datos para eliminar");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al eliminar perfil: {e.Message}");
        }
    }

    private async Task DeleteProfileFromMongoDB(string userId, int profileId)
    {
        if (MongoDbService.Instance == null || !MongoDbService.Instance.IsConnected())
        {
            Debug.LogWarning("MongoDB no está disponible para sincronización");
            return;
        }
        
        await MongoDbService.Instance.DeleteProfileDataAsync(userId, profileId);
    }
}