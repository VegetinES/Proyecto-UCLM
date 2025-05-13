using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public enum SoundToggleType
{
    Sound,
    Vibration,
    AutoNarrator
}

public class SoundToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Slider slider;
    [SerializeField] public SoundToggleType toggleType;
    [SerializeField] private float initDelay = 0.5f;
    
    [SerializeField] private bool isOn = true;
    
    private void Start()
    {
        // Inicializar slider en posición inactiva
        if (slider != null)
        {
            slider.interactable = false;
        }
        
        // Cargar estado con un pequeño retraso para asegurar que otros sistemas estén inicializados
        StartCoroutine(LoadStateWithDelay());
    }
    
    private IEnumerator LoadStateWithDelay()
    {
        yield return new WaitForSeconds(initDelay);
        
        LoadStateFromConfig();
    }
    
    private void LoadStateFromConfig()
    {
        // Cargar el estado desde el GlobalSoundManager si está disponible
        if (GlobalSoundManager.Instance != null)
        {
            // El GlobalSoundManager ya debería tener los valores cargados de la base de datos
            switch (toggleType)
            {
                case SoundToggleType.Sound:
                    isOn = GlobalSoundManager.Instance.GetSoundEnabled();
                    break;
                case SoundToggleType.Vibration:
                    isOn = GlobalSoundManager.Instance.GetVibrationEnabled();
                    break;
                case SoundToggleType.AutoNarrator:
                    isOn = GlobalSoundManager.Instance.GetAutoNarratorEnabled();
                    break;
            }
            
            Debug.Log($"SoundToggle: Estado {toggleType} cargado: {isOn}");
        }
        else
        {
            // Si no hay GlobalSoundManager, cargar directamente de la base de datos
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                switch (toggleType)
                {
                    case SoundToggleType.Sound:
                        isOn = config.Sound;
                        break;
                    case SoundToggleType.Vibration:
                        isOn = config.Vibration;
                        break;
                    case SoundToggleType.AutoNarrator:
                        isOn = config.AutoNarrator;
                        break;
                }
                
                Debug.Log($"SoundToggle: Estado {toggleType} cargado desde base de datos: {isOn}");
            }
            else
            {
                Debug.LogWarning($"SoundToggle: No se pudo cargar configuración, usando valor por defecto para {toggleType}: {isOn}");
            }
        }
        
        // Actualizar el slider con el valor cargado
        UpdateSliderValue();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleState();
    }
    
    public void ToggleState()
    {
        isOn = !isOn;
        UpdateSliderValue();
        
        if (GlobalSoundManager.Instance != null)
        {
            switch (toggleType)
            {
                case SoundToggleType.Sound:
                    GlobalSoundManager.Instance.UpdateSound(isOn);
                    break;
                case SoundToggleType.Vibration:
                    GlobalSoundManager.Instance.UpdateVibration(isOn);
                    break;
                case SoundToggleType.AutoNarrator:
                    GlobalSoundManager.Instance.UpdateAutoNarrator(isOn);
                    break;
            }
            
            Debug.Log($"SoundToggle: Estado {toggleType} cambiado a {isOn} mediante GlobalSoundManager");
        }
        else
        {
            // Si no hay GlobalSoundManager, actualizar directamente en la base de datos
            TryDirectUpdate();
            Debug.Log($"SoundToggle: Estado {toggleType} cambiado a {isOn} directamente en la base de datos");
        }
    }
    
    // Reemplazar método TryDirectUpdate:
    private void TryDirectUpdate()
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            var config = SqliteDatabase.Instance.GetConfiguration(userId);

            if (config != null)
            {
                switch (toggleType)
                {
                    case SoundToggleType.Sound:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator, isOn,
                            config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound,
                            config.Vibration);
                        break;
                    case SoundToggleType.Vibration:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator,
                            config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound,
                            config.NarratorSound, isOn);
                        break;
                    case SoundToggleType.AutoNarrator:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, isOn, config.Sound,
                            config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound,
                            config.Vibration);
                        break;
                }
            }
            else
            {
                switch (toggleType)
                {
                    case SoundToggleType.Sound:
                        SqliteDatabase.Instance.SaveConfiguration(userId, 3, false, isOn);
                        break;
                    case SoundToggleType.Vibration:
                        SqliteDatabase.Instance.SaveConfiguration(userId, 3, false, true, 50, 50, 50, 50, isOn);
                        break;
                    case SoundToggleType.AutoNarrator:
                        SqliteDatabase.Instance.SaveConfiguration(userId, 3, isOn);
                        break;
                }
            }

            Debug.Log($"SoundToggle: Estado {toggleType} actualizado directamente en la base de datos a {isOn}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SoundToggle: Error al actualizar directamente en la base de datos: {e.Message}");
        }
    }

    private void UpdateSliderValue()
    {
        if (slider != null)
        {
            slider.value = isOn ? 1 : 0;
        }
    }
    
    public bool IsOn()
    {
        return isOn;
    }
    
    public void SetState(bool state)
    {
        if (isOn != state)
        {
            isOn = state;
            UpdateSliderValue();
        }
    }
}