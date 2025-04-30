using UnityEngine;
using UnityEngine.UI;

public class ChangeColors : MonoBehaviour
{
    [SerializeField] public Slider slider;
    [SerializeField] public Image image1;
    [SerializeField] public Image image2;
    [SerializeField] public Image image3;
    [SerializeField] public Image image4;
    
    void Start()
    {
        // Cargar el valor desde la base de datos
        var config = DataAccess.GetConfiguration();
        if (config != null)
        {
            slider.value = config.Colors;
            Debug.Log($"ChangeColors: Valor cargado de la base de datos: {config.Colors}");
        }
        else
        {
            Debug.LogWarning("ChangeColors: No se pudo cargar la configuración, usando valor por defecto");
        }
        
        // Aplicar colores iniciales
        UpdateColors((int)slider.value);
        
        // Añadir el listener para el slider
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }
    
    void OnSliderValueChanged(float value)
    {
        int colorValue = (int)value;
    
        // Actualizar colores
        UpdateColors(colorValue);
    
        // Guardar en la base de datos
        try
        {
            Debug.Log($"ChangeColors: Actualizando color a {colorValue}");
            
            // Si GlobalColorManager está disponible, usar su método
            if (GlobalColorManager.Instance != null)
            {
                GlobalColorManager.Instance.UpdateColorIntensity(colorValue);
                Debug.Log("ChangeColors: Color actualizado a través de GlobalColorManager");
            }
            else
            {
                // Usar el método directo si el manager no está disponible
                var userId = DataService.Instance.GetCurrentUserId();
                DataService.Instance.ConfigRepo.UpdateColors(userId, colorValue);
                Debug.Log($"ChangeColors: Color actualizado directamente en la base de datos para {userId}");
            }
            
            // Busca el inicializador de colores y fuerza la actualización
            ColorAdapterInitializer initializer = FindObjectOfType<ColorAdapterInitializer>();
            if (initializer != null)
            {
                initializer.ForceInitialization();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ChangeColors: Error al guardar en la base de datos: {e.Message}");
        }
    }
    
    void UpdateColors(int colorValue)
    {
        switch (colorValue)
        {
            case 1:
                image1.color = new Color(252f / 255f, 161f / 255f, 161f / 255f);
                image2.color = new Color(166f / 255f, 190f / 255f, 255f / 255f);
                image3.color = new Color(180f / 255f, 255f / 255f, 161f / 255f);
                image4.color = new Color(255f / 255f, 245f / 255f, 157f / 255f);
                break;
            case 2:
                image1.color = new Color(252f / 255f, 121f / 255f, 121f / 255f);
                image2.color = new Color(126f / 255f, 160f / 255f, 255f / 255f);
                image3.color = new Color(146f / 255f, 250f / 255f, 120f / 255f);
                image4.color = new Color(255f / 255f, 228f / 255f, 120f / 255f);
                break;
            case 3:
                image1.color = new Color(255f / 255f, 79f / 255f, 79f / 255f);
                image2.color = new Color(84f / 255f, 130f / 255f, 255f / 255f);
                image3.color = new Color(118f / 255f, 252f / 255f, 82f / 255f);
                image4.color = new Color(255f / 255f, 220f / 255f, 79f / 255f);
                break;
            case 4:
                image1.color = new Color(255f / 255f, 41f / 255f, 41f / 255f);
                image2.color = new Color(41f / 255f, 98f / 255f, 255f / 255f);
                image3.color = new Color(84f / 255f, 255f / 255f, 41f / 255f);
                image4.color = new Color(255f / 255f, 212f / 255f, 41f / 255f);
                break;
            case 5:
                image1.color = new Color(255f / 255f, 0f / 255f, 0f / 255f);
                image2.color = new Color(0f / 255f, 68f / 255f, 255f / 255f);
                image3.color = new Color(51f / 255f, 255f / 255f, 0f / 255f);
                image4.color = new Color(255f / 255f, 204f / 255f, 0f / 255f);
                break;
        }
    }
}