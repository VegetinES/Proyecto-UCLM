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
    
    private void TryDirectUpdate()
    {
        try
        {
            if (DataService.Instance == null)
            {
                Debug.LogError("SoundToggle: DataService no disponible para actualización directa");
                return;
            }
            
            string userId = DataService.Instance.GetCurrentUserId();
            var config = DataService.Instance.ConfigRepo.GetUserConfiguration(userId);
            
            if (config != null)
            {
                // Obtener valores actuales
                bool sound = config.Sound;
                bool autoNarrator = config.AutoNarrator;
                bool vibration = config.Vibration;
                
                // Actualizar valor específico
                switch (toggleType)
                {
                    case SoundToggleType.Sound:
                        sound = isOn;
                        break;
                    case SoundToggleType.Vibration:
                        vibration = isOn;
                        break;
                    case SoundToggleType.AutoNarrator:
                        autoNarrator = isOn;
                        break;
                }
                
                // Actualizar configuración
                if (toggleType == SoundToggleType.Sound || toggleType == SoundToggleType.Vibration)
                {
                    DataService.Instance.ConfigRepo.UpdateSoundSettings(
                        userId, sound, config.GeneralSound, config.MusicSound, 
                        config.EffectsSound, config.NarratorSound, vibration);
                }
                else if (toggleType == SoundToggleType.AutoNarrator)
                {
                    DataService.Instance.ConfigRepo.UpdateAutoNarrator(userId, autoNarrator);
                }
            }
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