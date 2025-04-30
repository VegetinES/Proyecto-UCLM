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
        yield return null; // Esperar dos frames para asegurar que los toggles y sliders estén inicializados
        
        // Recargar la configuración si es necesario
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
            if (DataService.Instance == null)
            {
                Debug.LogWarning("GlobalSoundManager: DataService no disponible al cargar configuración");
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
        
        Debug.Log($"GlobalSoundManager: GeneralSound actualizado a {level}");
    }
    
    public void UpdateMusicSound(int level)
    {
        musicSoundLevel = level;
        SaveConfiguration();
        
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
            if (DataService.Instance == null)
            {
                Debug.LogError("GlobalSoundManager: DataService no disponible al guardar configuración");
                return;
            }
            
            var userId = DataService.Instance.GetCurrentUserId();
            
            // Obtener la configuración actual para preservar el valor de Colors
            var currentConfig = DataService.Instance.ConfigRepo.GetUserConfiguration(userId);
            int colorsValue = currentConfig != null ? currentConfig.Colors : 3;
        
            DataService.Instance.ConfigRepo.UpdateFullConfiguration(
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
            
            // Marcar la configuración como cargada
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
    
    // Métodos para obtener valores actuales para componentes recién creados
    public bool GetSoundEnabled() => soundEnabled;
    public bool GetVibrationEnabled() => vibrationEnabled;
    public bool GetAutoNarratorEnabled() => autoNarrator;
    public int GetGeneralSoundLevel() => generalSoundLevel;
    public int GetMusicSoundLevel() => musicSoundLevel;
    public int GetEffectsSoundLevel() => effectsSoundLevel;
    public int GetNarratorSoundLevel() => narratorSoundLevel;
}