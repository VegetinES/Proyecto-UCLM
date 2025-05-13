using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalSoundManager : MonoBehaviour
{
    public static GlobalSoundManager Instance { get; private set; }
    
    private bool soundEnabled = true;
    private bool vibrationEnabled = false;
    private int generalSoundLevel = 50;
    private int musicSoundLevel = 50;
    private int effectsSoundLevel = 50;
    private int narratorSoundLevel = 50;
    private bool autoNarrator = false;
    
    private bool configLoaded = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("GlobalSoundManager: Inicializado como singleton");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartCoroutine(WaitForDataManagerAndLoad());
    }
    
    private IEnumerator WaitForDataManagerAndLoad()
    {
        while (DataManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.1f);
        LoadConfiguration();
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"GlobalSoundManager: Nueva escena cargada: {scene.name}");
        StartCoroutine(ApplySoundConfigNextFrame());
    }
    
    private IEnumerator ApplySoundConfigNextFrame()
    {
        yield return null;
        yield return null;
        
        if (!configLoaded)
        {
            LoadConfiguration();
        }
        
        ApplySoundConfigToCurrentScene();
        Debug.Log("GlobalSoundManager: Configuración de sonido aplicada a la escena actual");
    }
    
    private void LoadConfiguration()
    {
        try
        {
            if (DataManager.Instance == null)
            {
                Debug.LogWarning("GlobalSoundManager: DataManager no disponible al cargar configuración");
                return;
            }
            
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                soundEnabled = config.Sound;
                vibrationEnabled = config.Vibration;
                generalSoundLevel = config.GeneralSound;
                musicSoundLevel = config.MusicSound;
                effectsSoundLevel = config.EffectsSound;
                narratorSoundLevel = config.NarratorSound;
                autoNarrator = config.AutoNarrator;
                
                configLoaded = true;
                
                Debug.Log($"GlobalSoundManager: Configuración cargada - Sound: {soundEnabled}, " +
                         $"General: {generalSoundLevel}, Music: {musicSoundLevel}, " +
                         $"Effects: {effectsSoundLevel}, Narrator: {narratorSoundLevel}, " +
                         $"AutoNarrator: {autoNarrator}, Vibration: {vibrationEnabled}");
            }
            else
            {
                Debug.LogWarning("GlobalSoundManager: No se pudo cargar la configuración, usando valores por defecto");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GlobalSoundManager: Error al cargar configuración: {e.Message}");
        }
    }
    
    public void ApplySoundConfigToCurrentScene()
    {
        ApplySoundToggles();
        ApplySoundSliders();
        UpdateSliderInteractability();
    }
    
    private void ApplySoundToggles()
    {
        var soundToggles = FindObjectsOfType<SoundToggle>();
        
        foreach (var toggle in soundToggles)
        {
            if (toggle == null) continue;
            
            switch (toggle.toggleType)
            {
                case SoundToggleType.Sound:
                    toggle.SetState(soundEnabled);
                    break;
                case SoundToggleType.Vibration:
                    toggle.SetState(vibrationEnabled);
                    break;
                case SoundToggleType.AutoNarrator:
                    toggle.SetState(autoNarrator);
                    break;
            }
        }
    }
    
    private void ApplySoundSliders()
    {
        var soundSliders = FindObjectsOfType<SoundSlider>();
        
        foreach (var slider in soundSliders)
        {
            if (slider == null) continue;
            
            switch (slider.sliderType)
            {
                case SoundSliderType.General:
                    slider.SetValue(generalSoundLevel);
                    break;
                case SoundSliderType.Music:
                    slider.SetValue(musicSoundLevel);
                    break;
                case SoundSliderType.Effects:
                    slider.SetValue(effectsSoundLevel);
                    break;
                case SoundSliderType.Narrator:
                    slider.SetValue(narratorSoundLevel);
                    break;
            }
        }
    }
    
    private void UpdateSliderInteractability()
    {
        var soundSliders = FindObjectsOfType<SoundSlider>();
        
        foreach (var slider in soundSliders)
        {
            if (slider == null) continue;
            
            // Deshabilitar sliders de sonido si Sound está desactivado
            slider.SetInteractable(soundEnabled);
        }
    }
    
    public void UpdateSound(bool isEnabled)
    {
        soundEnabled = isEnabled;
        SaveConfiguration();
    
        // Actualizar la interactividad de los sliders inmediatamente
        UpdateSliderInteractability();
    
        // Notificar cambios a los controladores de música
        NotifyVolumeChanges();
    
        Debug.Log($"GlobalSoundManager: Sound actualizado a {isEnabled}");
    }
    
    public void UpdateVibration(bool isEnabled)
    {
        vibrationEnabled = isEnabled;
        SaveConfiguration();
        
        Debug.Log($"GlobalSoundManager: Vibration actualizado a {isEnabled}");
    }
    
    public void UpdateAutoNarrator(bool isEnabled)
    {
        autoNarrator = isEnabled;
        SaveConfiguration();
        
        Debug.Log($"GlobalSoundManager: AutoNarrator actualizado a {isEnabled}");
    }
    
    public void UpdateGeneralSound(int level)
    {
        generalSoundLevel = level;
        SaveConfiguration();
    
        // Notificar cambios a los controladores de música
        NotifyVolumeChanges();
    
        Debug.Log($"GlobalSoundManager: GeneralSound actualizado a {level}");
    }
    
    public void UpdateMusicSound(int level)
    {
        musicSoundLevel = level;
        SaveConfiguration();
    
        // Notificar cambios a los controladores de música
        NotifyVolumeChanges();
    
        Debug.Log($"GlobalSoundManager: MusicSound actualizado a {level}");
    }
    
    public void UpdateEffectsSound(int level)
    {
        effectsSoundLevel = level;
        SaveConfiguration();
        
        Debug.Log($"GlobalSoundManager: EffectsSound actualizado a {level}");
    }
    
    public void UpdateNarratorSound(int level)
    {
        narratorSoundLevel = level;
        SaveConfiguration();
        
        Debug.Log($"GlobalSoundManager: NarratorSound actualizado a {level}");
    }
    
    private void SaveConfiguration()
    {
        try
        {
            if (DataManager.Instance == null)
            {
                Debug.LogError("GlobalSoundManager: DataManager no disponible al guardar configuración");
                return;
            }
    
            var userId = DataManager.Instance.GetCurrentUserId();
    
            var currentConfig = SqliteDatabase.Instance.GetConfiguration(userId);
            int colorsValue = currentConfig != null ? currentConfig.Colors : 3;

            SqliteDatabase.Instance.SaveConfiguration(
                userId,
                colorsValue,
                autoNarrator,
                soundEnabled,
                generalSoundLevel,
                musicSoundLevel,
                effectsSoundLevel,
                narratorSoundLevel,
                vibrationEnabled
            );
    
            Debug.Log($"GlobalSoundManager: Configuración guardada para usuario {userId}");
            PlayerPrefs.Save(); // Asegura que los datos se guarden inmediatamente
            configLoaded = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GlobalSoundManager: Error al guardar configuración: {e.Message}");
        }
    }
    
    public void ForceRefresh()
    {
        Debug.Log("GlobalSoundManager: Forzando actualización de configuración de sonido");
        LoadConfiguration();
        ApplySoundConfigToCurrentScene();
    }
    
    public void NotifyVolumeChanges()
    {
        // Buscar todos los controladores de música en la escena y actualizar su volumen
        GameMusicController[] musicControllers = FindObjectsOfType<GameMusicController>();
        foreach (var controller in musicControllers)
        {
            controller.UpdateVolume();
        }
    }
    
    // Métodos para obtener valores actuales para componentes recién creados
    public bool GetSoundEnabled() => soundEnabled;
    public bool GetVibrationEnabled() => vibrationEnabled;
    public bool GetAutoNarratorEnabled() => autoNarrator;
    public int GetGeneralSoundLevel() => generalSoundLevel;
    public int GetMusicSoundLevel() => musicSoundLevel;
    public int GetEffectsSoundLevel() => effectsSoundLevel;
    public int GetNarratorSoundLevel() => narratorSoundLevel;
}