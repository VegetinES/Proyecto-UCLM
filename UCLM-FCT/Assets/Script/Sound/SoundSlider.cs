using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum SoundSliderType
{
    General,
    Music,
    Effects,
    Narrator
}

public class SoundSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] public SoundSliderType sliderType;
    [SerializeField] private float initDelay = 0.5f;
    
    private int cachedValue = 50;
    private bool valueLoaded = false;
    
    private void Start()
    {
        slider.minValue = 0;
        slider.maxValue = 100;
        
        // Cargar el valor con un pequeño retraso para asegurar que otros sistemas estén inicializados
        StartCoroutine(LoadValueWithDelay());
    }
    
    private IEnumerator LoadValueWithDelay()
    {
        yield return new WaitForSeconds(initDelay);
        
        LoadValueFromConfig();
        
        // Asignar el listener para cambios después de establecer el valor inicial
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }
    
    private void LoadValueFromConfig()
    {
        // Cargar el valor desde el GlobalSoundManager si está disponible
        if (GlobalSoundManager.Instance != null)
        {
            int value = 50; // Valor por defecto
            
            // Intentar obtener valores del GlobalSoundManager primero
            switch (sliderType)
            {
                case SoundSliderType.General:
                    value = GlobalSoundManager.Instance.GetGeneralSoundLevel();
                    break;
                case SoundSliderType.Music:
                    value = GlobalSoundManager.Instance.GetMusicSoundLevel();
                    break;
                case SoundSliderType.Effects:
                    value = GlobalSoundManager.Instance.GetEffectsSoundLevel();
                    break;
                case SoundSliderType.Narrator:
                    value = GlobalSoundManager.Instance.GetNarratorSoundLevel();
                    break;
            }
            
            // Si no conseguimos valores válidos del GlobalSoundManager, intentar obtenerlos directamente de la configuración
            if (value <= 0)
            {
                // Obtener la configuración directamente de la base de datos
                var config = DataAccess.GetConfiguration();
                if (config != null)
                {
                    switch (sliderType)
                    {
                        case SoundSliderType.General:
                            value = config.GeneralSound;
                            break;
                        case SoundSliderType.Music:
                            value = config.MusicSound;
                            break;
                        case SoundSliderType.Effects:
                            value = config.EffectsSound;
                            break;
                        case SoundSliderType.Narrator:
                            value = config.NarratorSound;
                            break;
                    }
                }
            }
            
            // Guardar valor en caché
            cachedValue = value;
            valueLoaded = true;
            
            // Asignar el valor cargado al slider
            slider.value = value;
            
            // Configurar interactividad según estado del sonido
            bool soundEnabled = GlobalSoundManager.Instance.GetSoundEnabled();
            slider.interactable = soundEnabled;
            
            Debug.Log($"SoundSlider: Slider {sliderType} iniciado con valor {value}, interactivo: {soundEnabled}");
        }
        else
        {
            Debug.LogWarning("SoundSlider: GlobalSoundManager no disponible al cargar valores");
            
            // Si no hay GlobalSoundManager, intentar cargar directamente de la base de datos
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                int value = 50;
                switch (sliderType)
                {
                    case SoundSliderType.General:
                        value = config.GeneralSound;
                        break;
                    case SoundSliderType.Music:
                        value = config.MusicSound;
                        break;
                    case SoundSliderType.Effects:
                        value = config.EffectsSound;
                        break;
                    case SoundSliderType.Narrator:
                        value = config.NarratorSound;
                        break;
                }
                
                cachedValue = value;
                valueLoaded = true;
                
                slider.value = value;
                slider.interactable = config.Sound;
                
                Debug.Log($"SoundSlider: Slider {sliderType} iniciado con valor {value} desde base de datos");
            }
            else
            {
                // Si no hay configuración, usar valor por defecto
                slider.value = 50;
                slider.interactable = true;
                
                Debug.Log($"SoundSlider: Usando valor por defecto 50 para slider {sliderType}");
            }
        }
    }
    
    private void OnSliderValueChanged(float value)
    {
        int intValue = (int)value;
        cachedValue = intValue;
        
        if (GlobalSoundManager.Instance != null)
        {
            switch (sliderType)
            {
                case SoundSliderType.General:
                    GlobalSoundManager.Instance.UpdateGeneralSound(intValue);
                    break;
                case SoundSliderType.Music:
                    GlobalSoundManager.Instance.UpdateMusicSound(intValue);
                    break;
                case SoundSliderType.Effects:
                    GlobalSoundManager.Instance.UpdateEffectsSound(intValue);
                    break;
                case SoundSliderType.Narrator:
                    GlobalSoundManager.Instance.UpdateNarratorSound(intValue);
                    break;
            }
            
            Debug.Log($"SoundSlider: Valor de {sliderType} actualizado a {intValue}");
        }
        else
        {
            Debug.LogWarning($"SoundSlider: GlobalSoundManager no disponible al actualizar valor a {intValue}");
            
            // Intentar actualizar directamente en la base de datos
            TryDirectUpdate(intValue);
        }
    }
    
    private void TryDirectUpdate(int value)
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            var config = SqliteDatabase.Instance.GetConfiguration(userId);
        
            if (config != null)
            {
                switch (sliderType)
                {
                    case SoundSliderType.General:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator, config.Sound, value, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration);
                        break;
                    case SoundSliderType.Music:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator, config.Sound, config.GeneralSound, value, config.EffectsSound, config.NarratorSound, config.Vibration);
                        break;
                    case SoundSliderType.Effects:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator, config.Sound, config.GeneralSound, config.MusicSound, value, config.NarratorSound, config.Vibration);
                        break;
                    case SoundSliderType.Narrator:
                        SqliteDatabase.Instance.SaveConfiguration(userId, config.Colors, config.AutoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, value, config.Vibration);
                        break;
                }
            
                Debug.Log($"SoundSlider: Valor {sliderType} actualizado directamente en la base de datos a {value}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SoundSlider: Error al actualizar directamente en la base de datos: {e.Message}");
        }
    }
    
    public void SetValue(int value)
    {
        // Actualizar valor solo si es diferente, para evitar bucles
        if (slider.value != value)
        {
            slider.value = value;
            cachedValue = value;
        }
    }
    
    public void SetInteractable(bool interactable)
    {
        slider.interactable = interactable;
    }
}