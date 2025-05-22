using UnityEngine;
using UnityEngine.EventSystems;

public class TileController : MonoBehaviour, IPointerClickHandler
{
    private Vector2Int position;
    private BoardManager boardManager;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    public Color hintColor = new Color(0.5f, 1f, 0.5f, 0.5f);
    
    public void Initialize(Vector2Int pos, BoardManager manager)
    {
        position = pos;
        boardManager = manager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
        
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Asegurarse de que la cámara principal tenga un Physics2DRaycaster para detectar toques en móviles
        EnsureRaycaster();
    }
    
    private void EnsureRaycaster()
    {
        // Añadir Physics2DRaycaster a la cámara principal si no lo tiene
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<Physics2DRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }
        
        // Asegurarse de que existe un EventSystem en la escena
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Tile clicked at position {position}");
        boardManager.MoveCharacter(position);
    }
    
    
    public void HighlightAsHint()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hintColor;
        }
    }
    
    public void ClearHighlight()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}