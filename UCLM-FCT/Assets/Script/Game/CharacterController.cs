using UnityEngine;
using System.Collections;

public class CharacterController : MonoBehaviour
{
    private BoardManager boardManager;
    private Vector2Int currentPosition;
    private bool isMoving = false;
    
    [SerializeField] private float moveSpeed = 5f;
    
    // Array de sprites para el personaje
    [SerializeField] public Sprite[] characterSprites;
    
    // Índice del sprite seleccionado
    private int selectedSpriteIndex = 0;
    
    private SpriteRenderer spriteRenderer;
    
    // Dirección actual del personaje (0 = arriba, 1 = derecha, 2 = abajo, 3 = izquierda)
    private int currentDirection = 0;
    
    // Contador de giros realizados en este nivel
    private int turnsCount = 0;
    
    // Contador de movimientos realizados en este nivel
    private int movesCount = 0;
    
    private void Awake()
    {
        // Obtener el SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    public void Initialize(BoardManager board, Vector2Int startPos)
    {
        boardManager = board;
        currentPosition = startPos;
    
        // Reiniciar contadores
        turnsCount = 0;
        movesCount = 0;
    
        // Establecer dirección inicial: mirando hacia arriba
        currentDirection = 0;
        spriteRenderer.flipX = false;
    
        // Seleccionar un sprite aleatorio si hay sprites disponibles
        if (characterSprites != null && characterSprites.Length > 0)
        {
            selectedSpriteIndex = Random.Range(0, characterSprites.Length);
            if (spriteRenderer != null && characterSprites[selectedSpriteIndex] != null)
            {
                spriteRenderer.sprite = characterSprites[selectedSpriteIndex];
                Debug.Log($"CharacterController: Sprite seleccionado: {selectedSpriteIndex}");
            
                // Informar al BoardManager sobre el sprite seleccionado
                boardManager.SetSelectedCharacterSpriteIndex(selectedSpriteIndex);
            }
        }
    }
    
    public int GetSelectedSpriteIndex()
    {
        return selectedSpriteIndex;
    }
    
    public bool TryMove(Vector2Int newPosition)
    {
        if (isMoving) return false;
    
        // Calcular dirección del movimiento
        int newDirection = CalculateDirection(newPosition);
        bool needsToTurn = newDirection != currentDirection;
    
        // Actualizar contadores (solo para estadísticas, ya no limitan el movimiento)
        if (needsToTurn)
        {
            turnsCount++;
            Debug.Log($"Giro realizado: {turnsCount}");
        }
    
        movesCount++;
        Debug.Log($"Movimiento realizado: {movesCount}");
    
        // Actualizar dirección actual
        currentDirection = newDirection;
    
        // Iniciar movimiento
        StartCoroutine(MoveToPosition(newPosition));
        return true;
    }
    
    private int CalculateDirection(Vector2Int newPosition)
    {
        // 0 = arriba, 1 = derecha, 2 = abajo, 3 = izquierda
        if (newPosition.y > currentPosition.y) return 0; // Arriba
        if (newPosition.x > currentPosition.x) return 1; // Derecha
        if (newPosition.y < currentPosition.y) return 2; // Abajo
        if (newPosition.x < currentPosition.x) return 3; // Izquierda
        
        return currentDirection; // Mantener dirección actual si no hay cambio
    }
    
    private IEnumerator MoveToPosition(Vector2Int newPosition)
    {
        isMoving = true;
    
        // Obtener las posiciones en el mundo
        Vector3 startPos = transform.position;
    
        // En lugar de calcular manualmente, pedimos al BoardManager que nos convierta la posición
        Vector3 targetPos = boardManager.ConvertToWorldPosition(newPosition);
        targetPos.z = transform.position.z;  // Mantener la misma profundidad
    
        // Aplicar flip según la dirección
        ApplyDirectionVisuals();
    
        float elapsedTime = 0f;
        float duration = 1f / moveSpeed;
    
        // Animación de movimiento suave
        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        // Asegurar posición final exacta
        transform.position = targetPos;
    
        // Actualizar posición en el BoardManager
        currentPosition = newPosition;
        boardManager.UpdateCharacterPosition(currentPosition);
    
        isMoving = false;
    }
    
    private void ApplyDirectionVisuals()
    {
        // Aplicar rotación y flip según la dirección
        switch (currentDirection)
        {
            case 0: // Arriba
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
                
            case 1: // Derecha
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 270);
                break;
                
            case 2: // Abajo
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 180);
                break;
                
            case 3: // Izquierda
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
        }
    }
    
    public int GetTurnsCount()
    {
        return turnsCount;
    }
    
    public int GetMovesCount()
    {
        return movesCount;
    }
}