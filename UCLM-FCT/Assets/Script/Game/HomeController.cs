using UnityEngine;

public class HomeController : MonoBehaviour
{
    // Array de sprites para la casa
    [SerializeField] public Sprite[] homeSprites;
    
    // Índice del sprite seleccionado por el personaje
    private int characterSpriteIndex = 0;
    
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        // Obtener el SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    public void Initialize(int characterIndex)
    {
        characterSpriteIndex = characterIndex;
        Debug.Log($"HomeController: Inicializado con índice de personaje {characterIndex}");
        
        // Asignar el sprite correspondiente si hay sprites disponibles
        if (homeSprites != null && homeSprites.Length > characterIndex && homeSprites[characterIndex] != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = homeSprites[characterIndex];
                Debug.Log($"HomeController: Sprite asignado: {characterIndex}");
            }
            else
            {
                Debug.LogError("HomeController: No hay SpriteRenderer disponible");
            }
        }
        else
        {
            Debug.LogWarning($"HomeController: No hay sprite disponible para el índice {characterIndex}");
        }
    }
}