using UnityEngine;
using System.Collections;

public class CharacterController : MonoBehaviour
{
    private BoardManager boardManager;
    private Vector2Int currentPosition;
    private bool isMoving = false;
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] public Sprite[] characterSprites;
    
    private int selectedSpriteIndex = 0;
    private SpriteRenderer spriteRenderer;
    private int currentDirection = 0;
    private int turnsCount = 0;
    private int movesCount = 0;
    
    private void Awake()
    {
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
        turnsCount = 0;
        movesCount = 0;
        currentDirection = 0;
        spriteRenderer.flipX = false;
        
        if (characterSprites != null && characterSprites.Length > 0)
        {
            selectedSpriteIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);
            selectedSpriteIndex = Mathf.Clamp(selectedSpriteIndex, 0, characterSprites.Length - 1);
            
            if (spriteRenderer != null && characterSprites[selectedSpriteIndex] != null)
            {
                spriteRenderer.sprite = characterSprites[selectedSpriteIndex];
                Debug.Log($"CharacterController: Sprite seleccionado: {selectedSpriteIndex}");
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
    
        int newDirection = CalculateDirection(newPosition);
        bool needsToTurn = newDirection != currentDirection;
    
        if (needsToTurn)
        {
            turnsCount++;
            Debug.Log($"Giro realizado: {turnsCount}");
        }
    
        movesCount++;
        Debug.Log($"Movimiento realizado: {movesCount}");
        currentDirection = newDirection;
        StartCoroutine(MoveToPosition(newPosition));
        return true;
    }
    
    private int CalculateDirection(Vector2Int newPosition)
    {
        if (newPosition.y > currentPosition.y) return 0;
        if (newPosition.x > currentPosition.x) return 1;
        if (newPosition.y < currentPosition.y) return 2;
        if (newPosition.x < currentPosition.x) return 3;
        return currentDirection;
    }
    
    private IEnumerator MoveToPosition(Vector2Int newPosition)
    {
        isMoving = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = boardManager.ConvertToWorldPosition(newPosition);
        targetPos.z = transform.position.z;
        ApplyDirectionVisuals();
        
        float elapsedTime = 0f;
        float duration = 1f / moveSpeed;
    
        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        transform.position = targetPos;
        currentPosition = newPosition;
        boardManager.UpdateCharacterPosition(currentPosition);
        isMoving = false;
    }
    
    private void ApplyDirectionVisuals()
    {
        switch (currentDirection)
        {
            case 0:
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case 1:
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 270);
                break;
            case 2:
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 180);
                break;
            case 3:
                spriteRenderer.flipX = false;
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
        }
    }
}