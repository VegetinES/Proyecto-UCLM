using System.Collections;
using UnityEngine;

public class GameMusicController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip gameMusic;
    
    [Header("Configuración")]
    [SerializeField] private float initialVolume = 0.5f;
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private bool playOnStart = true;
    
    private void Start()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (gameMusic != null)
        {
            musicSource.clip = gameMusic;
        }
        
        // Cargar el volumen desde la configuración
        LoadVolumeSettings();
        
        // Iniciar reproducción si está configurada
        if (playOnStart)
        {
            StartCoroutine(FadeInMusic());
        }
    }
    
    private void LoadVolumeSettings()
    {
        bool soundEnabled = true;
        float generalVolume = initialVolume;
        float musicVolume = initialVolume;
        
        // Intentar cargar desde GlobalSoundManager si está disponible
        if (GlobalSoundManager.Instance != null)
        {
            soundEnabled = GlobalSoundManager.Instance.GetSoundEnabled();
            generalVolume = GlobalSoundManager.Instance.GetGeneralSoundLevel() / 100f;
            musicVolume = GlobalSoundManager.Instance.GetMusicSoundLevel() / 100f;
            
            Debug.Log($"GameMusicController: Configuración cargada - Sound: {soundEnabled}, " +
                    $"General: {generalVolume}, Music: {musicVolume}");
        }
        else
        {
            // Intentar cargar directamente desde la configuración
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                soundEnabled = config.Sound;
                generalVolume = config.GeneralSound / 100f;
                musicVolume = config.MusicSound / 100f;
                
                Debug.Log($"GameMusicController: Configuración cargada directamente - Sound: {soundEnabled}, " +
                        $"General: {generalVolume}, Music: {musicVolume}");
            }
            else
            {
                Debug.LogWarning("GameMusicController: No se pudo cargar la configuración, usando valores por defecto");
            }
        }
        
        // Aplicar configuración
        musicSource.mute = !soundEnabled;
        
        // El volumen final es el producto del volumen general y el volumen de música
        float finalVolume = generalVolume * musicVolume;
        musicSource.volume = finalVolume;
        
        Debug.Log($"GameMusicController: Volumen aplicado: {finalVolume} (General: {generalVolume} x Música: {musicVolume})");
    }
    
    private IEnumerator FadeInMusic()
    {
        // Guardar el volumen final deseado
        float targetVolume = musicSource.volume;
        
        // Empezar desde volumen cero
        musicSource.volume = 0f;
        musicSource.Play();
        
        // Fade in gradual
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Asegurar que llegue al volumen final exacto
        musicSource.volume = targetVolume;
    }
    
    // Método para actualizar el volumen si cambia durante el juego
    public void UpdateVolume()
    {
        LoadVolumeSettings();
    }
    
    // Para cuando la escena se recarga o destruye
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}