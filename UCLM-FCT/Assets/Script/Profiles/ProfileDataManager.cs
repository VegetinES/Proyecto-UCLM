using UnityEngine;

public class ProfileDataManager : MonoBehaviour
{
    public static ProfileDataManager Instance { get; private set; }
    
    // ID del perfil actual
    private int currentProfileId = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSelectedProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadSelectedProfile()
    {
        // Cargar el ID del perfil desde PlayerPrefs
        currentProfileId = PlayerPrefs.GetInt("SelectedProfileId", 0);
        Debug.Log($"Perfil cargado: {currentProfileId}");
    }
}