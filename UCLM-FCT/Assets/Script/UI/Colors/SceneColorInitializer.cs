using UnityEngine;

// Este script debe colocarse en cada escena para asegurar que los colores se actualicen
public class SceneColorInitializer : MonoBehaviour
{
    [SerializeField] private bool applyOnStart = true;
    
    private void Start()
    {
        if (applyOnStart)
        {
            InitializeColors();
        }
    }
    
    public void InitializeColors()
    {
        // Verificar si ya existe un GlobalColorManager
        if (GlobalColorManager.Instance == null)
        {
            // Si no existe, crear uno
            Debug.Log("Creando nueva instancia de GlobalColorManager");
            GameObject managerObj = new GameObject("GlobalColorManager");
            managerObj.AddComponent<GlobalColorManager>();
        }
        else
        {
            // Si ya existe, forzar actualización
            Debug.Log("GlobalColorManager encontrado. Forzando actualización...");
            GlobalColorManager.Instance.ForceRefresh();
        }
    }
}