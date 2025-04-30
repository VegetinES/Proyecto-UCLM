using System.Collections;
using UnityEngine;

public class SceneSoundInitializer : MonoBehaviour
{
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private float initializationDelay = 0.1f;
    
    private void Start()
    {
        if (applyOnStart)
        {
            StartCoroutine(InitializeSoundConfigWithDelay());
        }
    }
    
    private IEnumerator InitializeSoundConfigWithDelay()
    {
        // Pequeño retraso para asegurar que todos los componentes estén cargados
        yield return new WaitForSeconds(initializationDelay);
        InitializeSoundConfig();
    }
    
    public void InitializeSoundConfig()
    {
        if (GlobalSoundManager.Instance == null)
        {
            GameObject managerObj = new GameObject("GlobalSoundManager");
            managerObj.AddComponent<GlobalSoundManager>();
            
            // Esperamos unos frames para que se inicialice completamente
            StartCoroutine(ApplyConfigAfterFrames(2));
        }
        else
        {
            GlobalSoundManager.Instance.ForceRefresh();
        }
    }
    
    private IEnumerator ApplyConfigAfterFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
        
        if (GlobalSoundManager.Instance != null)
        {
            GlobalSoundManager.Instance.ApplySoundConfigToCurrentScene();
        }
    }
}