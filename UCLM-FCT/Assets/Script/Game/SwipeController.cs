using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwipeController : MonoBehaviour
{
    private BoardManager boardManager;
    private Camera mainCamera;
    private bool isDragging = false;
    private List<Vector2Int> swipePath = new List<Vector2Int>();
    private Vector2Int lastGridPosition;
    
    [Header("Configuración")]
    [SerializeField] private float gridTolerance = 0.4f;
    
    private void Start()
    {
        boardManager = GetComponent<BoardManager>();
        mainCamera = Camera.main;
        
        if (boardManager == null)
        {
            Debug.LogError("SwipeController: BoardManager no encontrado");
        }
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartSwipe();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndSwipe();
        }
        else if (isDragging && Input.GetMouseButton(0))
        {
            UpdateSwipe();
        }
    }
    
    private void StartSwipe()
    {
        // Verificar si el juego ya terminó
        if (boardManager.IsGameCompleted()) return;
    
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        Vector2Int gridPos = boardManager.WorldToGridPosition(worldPos);
    
        Vector2Int characterPos = boardManager.GetCharacterCurrentPosition();
    
        // Solo empezar deslizamiento si está cerca del personaje o sobre él
        int distX = Mathf.Abs(gridPos.x - characterPos.x);
        int distY = Mathf.Abs(gridPos.y - characterPos.y);
    
        if (distX <= 1 && distY <= 1 && boardManager.IsPositionOnBoard(gridPos))
        {
            isDragging = true;
            swipePath.Clear();
        
            // SIEMPRE empezar desde donde está el personaje
            swipePath.Add(characterPos);
            lastGridPosition = characterPos;
        }
    }
    
    private void UpdateSwipe()
    {
        Vector2 screenPos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        Vector2Int currentGridPos = boardManager.WorldToGridPosition(worldPos);
    
        if (!boardManager.IsPositionOnBoard(currentGridPos))
        {
            EndSwipe();
            return;
        }
    
        if (currentGridPos != lastGridPosition)
        {
            if (swipePath.Count > 0)
            {
                Vector2Int lastPos = swipePath[swipePath.Count - 1];
                int distX = Mathf.Abs(currentGridPos.x - lastPos.x);
                int distY = Mathf.Abs(currentGridPos.y - lastPos.y);
            
                // Solo agregar si es adyacente al último punto del recorrido
                if ((distX == 1 && distY == 0) || (distX == 0 && distY == 1))
                {
                    AddPositionToPath(currentGridPos);
                    lastGridPosition = currentGridPos;
                }
            }
        }
    }
    
    private void AddPositionToPath(Vector2Int newPos)
    {
        if (swipePath.Count == 0)
        {
            swipePath.Add(newPos);
            return;
        }
    
        Vector2Int lastPos = swipePath[swipePath.Count - 1];
    
        if (newPos == lastPos) return;
    
        // SIEMPRE agregar la nueva posición, aunque ya haya estado antes
        swipePath.Add(newPos);
    }
    
    private void EndSwipe()
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        if (swipePath.Count > 1)
        {
            List<Vector2Int> finalPath = ProcessSwipePath();
            
            if (finalPath.Count > 1)
            {
                StartCoroutine(MoveAlongPath(finalPath));
            }
        }
        
        swipePath.Clear();
    }
    
    private List<Vector2Int> ProcessSwipePath()
    {
        List<Vector2Int> finalPath = new List<Vector2Int>();
        
        if (swipePath.Count == 0) return finalPath;
        
        finalPath.Add(swipePath[0]);
        
        for (int i = 1; i < swipePath.Count; i++)
        {
            Vector2Int targetPos = swipePath[i];
            
            if (boardManager.IsValidMovePosition(targetPos))
            {
                finalPath.Add(targetPos);
            }
            else
            {
                break;
            }
        }
        
        return finalPath;
    }
    
    private IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        Vector2Int homePos = boardManager.GetHomePosition();
    
        for (int i = 1; i < path.Count; i++)
        {
            bool moved = boardManager.MoveCharacterToPosition(path[i]);
        
            if (!moved)
            {
                Debug.Log("Movimiento detenido en: " + path[i]);
                break;
            }
        
            yield return new WaitForSeconds(0.25f);
        
            if (path[i] == homePos)
            {
                Debug.Log("¡Llegó a casa! Deteniendo recorrido.");
                break;
            }
        }
    }
}