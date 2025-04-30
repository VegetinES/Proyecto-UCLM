using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Este componente adapta el color de un objeto UI según la intensidad configurada.
/// Se puede añadir a cualquier objeto que tenga un componente que maneje color.
/// </summary>
public class ColorAdapter : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Si es true, aplicará el color automáticamente al inicio")]
    [SerializeField] private bool applyOnStart = true;
    
    [Tooltip("Si es true, se actualizará cuando cambie la intensidad global")]
    [SerializeField] private bool listenToGlobalChanges = true;
    
    [Tooltip("Color base a partir del cual se generará el degradado")]
    [SerializeField] private Color baseColor;
    
    [Header("Modo de cambio")]
    [Tooltip("Si es true, cambiará la opacidad (Alpha) en lugar del color (RGB)")]
    [SerializeField] private bool changeAlphaInsteadOfColor = false;
    
    [Tooltip("Alpha mínimo para el nivel 1 de intensidad (como porcentaje)")]
    [Range(0, 100)]
    [SerializeField] private float minAlphaPercent = 33;
    
    [Header("Depuración")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Componentes que pueden tener colores
    private Graphic graphic;        // Para Image, Text, etc.
    private TMP_Text tmpText;       // Para TextMeshPro
    private SpriteRenderer sprite;  // Para sprites en la escena
    private Material material;      // Para otros renderers con materiales
    private CanvasGroup canvasGroup; // Para controlar alpha a nivel de grupo
    
    private int currentIntensity = 3;
    private bool componentIdentified = false;
    private bool subscribedToGlobalEvents = false;
    
    private void Start()
    {
        if (applyOnStart)
        {
            // Pequeño retraso para asegurarnos que todo está inicializado
            StartCoroutine(InitializeWithDelay(0.1f));
        }
    }
    
    private IEnumerator InitializeWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Initialize();
    }
    
    private void OnEnable()
    {
        SubscribeToGlobalEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromGlobalEvents();
    }
    
    private void SubscribeToGlobalEvents()
    {
        if (listenToGlobalChanges && GlobalColorManager.Instance != null && !subscribedToGlobalEvents)
        {
            // Suscribirse al evento de cambio de color global
            GlobalColorManager.Instance.OnColorIntensityChanged += HandleColorIntensityChanged;
            subscribedToGlobalEvents = true;
            DebugLog("Suscrito a eventos globales de color");
        }
    }
    
    private void UnsubscribeFromGlobalEvents()
    {
        if (subscribedToGlobalEvents && GlobalColorManager.Instance != null)
        {
            // Desuscribirse al desactivar el objeto
            GlobalColorManager.Instance.OnColorIntensityChanged -= HandleColorIntensityChanged;
            subscribedToGlobalEvents = false;
            DebugLog("Desuscrito de eventos globales de color");
        }
    }
    
    private void HandleColorIntensityChanged(int intensity)
    {
        DebugLog($"<color=green>Evento recibido: Intensidad cambiada a {intensity}</color>");
        ApplyColorWithIntensity(intensity);
    }
    
    public void Initialize()
    {
        // Identificar el componente que maneja color
        IdentifyColorComponent();
        
        // Guardar el color base si no se ha especificado uno
        if (baseColor == Color.clear)
        {
            CaptureBaseColor();
        }
        
        // Suscribirse a eventos globales
        SubscribeToGlobalEvents();
        
        // Cargar la intensidad actual
        LoadCurrentIntensity();
        
        // Aplicar el color con la intensidad actual
        ApplyColorWithIntensity(currentIntensity);
        
        DebugLog($"Inicializado: Modo={(changeAlphaInsteadOfColor ? "Alpha" : "Color")}, " +
                $"Intensidad={currentIntensity}, " +
                $"Alpha calculado={CalculateAlphaForIntensity(currentIntensity)}");
    }
    
    private void IdentifyColorComponent()
    {
        // Intentar obtener los componentes en orden de probabilidad
        graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            componentIdentified = true;
        }
        
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            componentIdentified = true;
        }
        
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            componentIdentified = true;
        }
        
        // Para otros renderers (MeshRenderer, etc.)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            material = renderer.material;
            componentIdentified = true;
        }
        
        // CanvasGroup para control de alpha
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (!componentIdentified && canvasGroup == null)
        {
            Debug.LogWarning($"ColorAdapter en {gameObject.name}: No se encontró ningún componente que maneje color o alpha");
        }
    }
    
    private void CaptureBaseColor()
    {
        if (!componentIdentified) return;
        
        if (graphic != null)
        {
            baseColor = graphic.color;
        }
        else if (tmpText != null)
        {
            baseColor = tmpText.color;
        }
        else if (sprite != null)
        {
            baseColor = sprite.color;
        }
        else if (material != null)
        {
            baseColor = material.color;
        }
        
        DebugLog($"Color base capturado: {baseColor}");
    }
    
    private void LoadCurrentIntensity()
    {
        int previousIntensity = currentIntensity;
        
        if (GlobalColorManager.Instance != null)
        {
            currentIntensity = GlobalColorManager.Instance.GetCurrentIntensity();
            DebugLog($"Intensidad cargada desde GlobalColorManager: {currentIntensity}");
        }
        else
        {
            // Si no hay GlobalColorManager, intentar obtenerlo directamente
            var config = DataAccess.GetConfiguration();
            if (config != null)
            {
                currentIntensity = config.Colors;
                currentIntensity = Mathf.Clamp(currentIntensity, 1, 5);
                DebugLog($"Intensidad cargada desde DataAccess: {currentIntensity}");
            }
        }
        
        if (previousIntensity != currentIntensity)
        {
            DebugLog($"Intensidad cambiada: {previousIntensity} -> {currentIntensity}");
        }
    }
    
    public void ApplyColorWithIntensity(int intensity)
    {
        if (!componentIdentified && canvasGroup == null) return;
        
        // Guardar la intensidad actual
        currentIntensity = intensity;
        
        DebugLog($"<color=yellow>Aplicando intensidad {intensity}</color>");
        
        // Manejar el cambio de alpha en CanvasGroup si existe y está en modo alpha
        if (changeAlphaInsteadOfColor && canvasGroup != null)
        {
            float newAlpha = CalculateAlphaForIntensity(intensity);
            canvasGroup.alpha = newAlpha;
            DebugLog($"CanvasGroup Alpha ajustado a {newAlpha}");
            return;
        }
        
        // Para componentes que manejan color directamente
        Color newColor;
        
        // Decidir qué tipo de cambio aplicar
        if (changeAlphaInsteadOfColor)
        {
            // Cambiar solo el canal Alpha
            newColor = ChangeAlphaWithIntensity(baseColor, intensity);
            DebugLog($"Aplicando alpha {newColor.a} (valor calculado para intensidad {intensity})");
        }
        else
        {
            // Cambiar el color RGB (degradado)
            newColor = GenerateColorWithIntensity(baseColor, intensity);
            DebugLog($"Aplicando color RGB: {newColor}");
        }
        
        // Aplicar el color al componente correspondiente
        if (graphic != null)
        {
            graphic.color = newColor;
            DebugLog($"Color aplicado a Graphic: {newColor}");
        }
        
        if (tmpText != null)
        {
            tmpText.color = newColor;
            DebugLog($"Color aplicado a TMP_Text: {newColor}");
        }
        
        if (sprite != null)
        {
            sprite.color = newColor;
            DebugLog($"Color aplicado a SpriteRenderer: {newColor}");
        }
        
        if (material != null)
        {
            material.color = newColor;
            DebugLog($"Color aplicado a Material: {newColor}");
        }
    }
    
    private Color GenerateColorWithIntensity(Color baseColor, int intensity)
    {
        // Conversión a enteros para facilitar los cálculos
        int r = Mathf.RoundToInt(baseColor.r * 255);
        int g = Mathf.RoundToInt(baseColor.g * 255);
        int b = Mathf.RoundToInt(baseColor.b * 255);
        float a = baseColor.a;
        
        // Los factores para los 5 niveles de intensidad
        double[] factores = { 0.33, 0.5, 0.67, 0.83, 1.0 };
        
        // Asegurarse de que la intensidad esté en el rango correcto
        intensity = Mathf.Clamp(intensity, 1, 5);
        
        // Interpolar entre blanco (255,255,255) y el color base según el factor
        double factor = factores[intensity - 1];
        int newR = Interpolar(255, r, factor);
        int newG = Interpolar(255, g, factor);
        int newB = Interpolar(255, b, factor);
        
        // Crear y devolver el nuevo color
        return new Color(newR / 255f, newG / 255f, newB / 255f, a);
    }
    
    private Color ChangeAlphaWithIntensity(Color baseColor, int intensity)
    {
        // Conservar los valores RGB originales
        float r = baseColor.r;
        float g = baseColor.g;
        float b = baseColor.b;
        
        // Calcular el valor alpha basado en la intensidad
        float newAlpha = CalculateAlphaForIntensity(intensity);
        
        // Crear y devolver el nuevo color
        return new Color(r, g, b, newAlpha);
    }
    
    private float CalculateAlphaForIntensity(int intensity)
    {
        // Los factores para los 5 niveles de intensidad (de menor a mayor)
        float[] factores = { minAlphaPercent / 100f, 0.5f, 0.67f, 0.83f, 1.0f };
        
        // Asegurarse de que la intensidad esté en el rango correcto
        intensity = Mathf.Clamp(intensity, 1, 5);
        
        // La intensidad 1 corresponde al índice 0 del array
        return factores[intensity - 1];
    }
    
    private int Interpolar(int c1, int c2, double factor)
    {
        return (int)(c1 + (c2 - c1) * factor);
    }
    
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        ApplyColorWithIntensity(currentIntensity);
    }
    
    // Para usos de editor y debug
    public Color GetBaseColor()
    {
        return baseColor;
    }
    
    public int GetCurrentIntensity()
    {
        return currentIntensity;
    }
    
    public bool IsChangingAlpha()
    {
        return changeAlphaInsteadOfColor;
    }
    
    public float GetMinAlphaPercent()
    {
        return minAlphaPercent;
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ColorAdapter] {gameObject.name}: {message}");
        }
    }
    
    // Método público para forzar la recarga de la intensidad actual
    public void ForceRefresh()
    {
        LoadCurrentIntensity();
        ApplyColorWithIntensity(currentIntensity);
        DebugLog("Refresh forzado completado");
    }
}