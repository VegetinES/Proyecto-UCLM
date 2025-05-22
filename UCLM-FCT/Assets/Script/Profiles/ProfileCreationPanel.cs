using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileCreationPanel : MonoBehaviour
{
    [Header("Campos de entrada")]
    public TMP_InputField nameInput;
    
    [Header("Selección de género")]
    public Image boyImage;
    public Image girlImage;
    
    [Header("Botones")]
    public Button createButton;
    
    [Header("Paneles")]
    public GameObject tutorProfilePanel;
    
    private string selectedGender = "Male"; // Valor por defecto
    
    private void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Configurar imágenes de género
        if (boyImage != null)
        {
            Button boyButton = boyImage.GetComponent<Button>();
            if (boyButton == null)
                boyButton = boyImage.gameObject.AddComponent<Button>();
            
            boyButton.onClick.AddListener(() => SelectGender("Male"));
            boyImage.color = new Color(0.7f, 0.7f, 1f); // Seleccionado por defecto
        }
        
        if (girlImage != null)
        {
            Button girlButton = girlImage.GetComponent<Button>();
            if (girlButton == null)
                girlButton = girlImage.gameObject.AddComponent<Button>();
            
            girlButton.onClick.AddListener(() => SelectGender("Female"));
            girlImage.color = Color.white;
        }
        
        // Configurar botón de crear
        if (createButton != null)
            createButton.onClick.AddListener(OnCreateButtonClicked);
    }
    
    private void SelectGender(string gender)
    {
        selectedGender = gender;
        
        // Actualizar colores para mostrar selección
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
    
    private void OnCreateButtonClicked()
    {
        string name = nameInput.text.Trim();
        
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("El nombre del perfil no puede estar vacío");
            return;
        }
        
        // Crear perfil
        if (ProfileManager.Instance != null)
            ProfileManager.Instance.CreateProfile(name, selectedGender);
        
        // Volver al panel de perfiles
        if (tutorProfilePanel != null)
        {
            gameObject.SetActive(false);
            tutorProfilePanel.SetActive(true);
            
            // Actualizar el panel de perfiles
            TutorProfilePanel panel = tutorProfilePanel.GetComponent<TutorProfilePanel>();
            if (panel != null)
                panel.LoadProfiles();
        }
        
        // Limpiar campos
        nameInput.text = "";
        SelectGender("Male");
    }
}