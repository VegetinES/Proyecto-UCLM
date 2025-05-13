using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    // Array de sprites para el obstáculo
    [SerializeField] public Sprite[] obstacleSprites;
    
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
        Debug.Log($"ObstacleController: Inicializado con índice de personaje {characterIndex}");
        
        // Asignar el sprite correspondiente si hay sprites disponibles
        if (obstacleSprites != null && obstacleSprites.Length > characterIndex && obstacleSprites[characterIndex] != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = obstacleSprites[characterIndex];
                Debug.Log($"ObstacleController: Sprite asignado: {characterIndex}");
            }
            else
            {
                Debug.LogError("ObstacleController: No hay SpriteRenderer disponible");
            }
        }
        else
        {
            Debug.LogWarning($"ObstacleController: No hay sprite disponible para el índice {characterIndex}");
        }
    }
}