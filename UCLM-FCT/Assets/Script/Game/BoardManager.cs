using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public GameObject tilePrefab;
    public GameObject characterPrefab;
    public GameObject homePrefab;
    public GameObject[] obstaclePrefabs;
    
    [Header("Level Settings")]
    public int levelNumber = 1;
    public int boardWidth = 5;
    public int boardHeight = 5;
    public int maxMoves = 5;
    public int maxTurns = 1;
    public int obstacleCount = 0;
    public bool isSquare = true;
    
    [Header("Sprites")]
    public Sprite[] characterSprites;
    public ObstacleManager obstacleManager;
    
    private GameObject[,] tiles;
    private Vector2Int characterPosition;
    private Vector2Int characterStartPosition;
    private Vector2Int homePosition;
    private List<Vector2Int> obstaclePositions = new List<Vector2Int>();
    private List<Vector2Int> validTilePositions = new List<Vector2Int>();
    
    private GameObject characterObject;
    private GameObject homeObject;
    private float startTime;
    private bool gameCompleted = false;
    
    private int selectedCharacterSpriteIndex = 0;
    private int shapeType = 0;
    
    private StatisticsManager statistics;
    
    public Vector2Int GetCharacterStartPosition() { return characterStartPosition; }
    public Vector2Int GetHomePosition() { return homePosition; }
    public Vector2Int[] GetObstaclePositions() { return obstaclePositions.ToArray(); }
    
    public void SetSelectedCharacterSpriteIndex(int index)
    {
        selectedCharacterSpriteIndex = index;
        
        if (obstacleManager != null)
        {
            obstacleManager.SetSelectedCharacterIndex(index);
        }
        else
        {
            Debug.LogError("BoardManager: obstacleManager no está asignado");
        }
    }
    
    public void InitializeBoard(bool isRetry = false, Vector2Int savedCharStart = default, Vector2Int savedHome = default, Vector2Int[] savedObstacles = null)
    {
        Debug.Log("BoardManager: Inicializando tablero");
        
        statistics = StatisticsManager.Instance;
        if (statistics != null)
        {
            statistics.StartLevel(levelNumber);
        }
        
        startTime = Time.time;
        gameCompleted = false;
        
        if (levelNumber == 3 && !isRetry)
        {
            shapeType = Random.Range(0, 5);
            Debug.Log($"BoardManager: Generando tablero con forma tipo {shapeType}");
        }
        
        Debug.Log("BoardManager: Limpiando tablero anterior");
        ClearBoard();
        
        Debug.Log($"BoardManager: Generando tablero de tamaño {boardWidth}x{boardHeight}");
        GenerateBoard();
        
        Debug.Log("BoardManager: Generando posiciones de objetos");
        if (isRetry && savedObstacles != null)
        {
            characterStartPosition = savedCharStart;
            characterPosition = characterStartPosition;
            homePosition = savedHome;
            obstaclePositions.Clear();
            obstaclePositions.AddRange(savedObstacles);
            Debug.Log("BoardManager: Usando posiciones guardadas para retry");
        }
        else
        {
            Debug.Log("BoardManager: Generando nuevas posiciones estratégicas");
            GenerateStrategicPositions();
        }
        
        Debug.Log("BoardManager: Colocando objetos en el tablero");
        PlaceGameObjects();
        
        Debug.Log($"BoardManager: Tablero inicializado - Nivel {levelNumber}, Tamaño: {boardWidth}x{boardHeight}, Obstáculos: {obstacleCount}");
    }
    
    private void ClearBoard()
    {
        if (tiles != null)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    if (tiles[x, y] != null)
                    {
                        Destroy(tiles[x, y]);
                    }
                }
            }
        }
        
        if (characterObject != null) Destroy(characterObject);
        if (homeObject != null) Destroy(homeObject);
        
        try
        {
            GameObject[] oldObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            foreach (GameObject obstacle in oldObstacles)
            {
                Destroy(obstacle);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("BoardManager: No se pudo encontrar obstáculos con tag 'Obstacle': " + e.Message);
    
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Obstacle"))
                {
                    Destroy(obj);
                }
            }
        }
        
        validTilePositions.Clear();
        obstaclePositions.Clear();
    }
    
    private void GenerateBoard()
    {
        Debug.Log($"BoardManager: Creando array de tiles {boardWidth}x{boardHeight}");
        tiles = new GameObject[boardWidth, boardHeight];
        
        float offsetX = (boardWidth - 1) * 0.5f;
        float offsetY = (boardHeight - 1) * 0.5f;
        Debug.Log($"BoardManager: Offset calculado - X: {offsetX}, Y: {offsetY}");
        
        int tilesCreated = 0;
        
        Transform tileParent = new GameObject("Tiles").transform;
        tileParent.SetParent(transform);
        
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                bool createTile = isSquare || ShouldCreateTile(x, y);
                
                if (createTile)
                {
                    Vector3 position = new Vector3(x - offsetX, y - offsetY, 0);
                    
                    if (tilePrefab == null)
                    {
                        Debug.LogError("BoardManager: ¡El prefab de Tile no está asignado!");
                        return;
                    }
                    
                    tiles[x, y] = Instantiate(tilePrefab, position, Quaternion.identity, tileParent);
                    tiles[x, y].name = $"Tile_{x}_{y}";
                    
                    TileController tileController = tiles[x, y].GetComponent<TileController>();
                    if (tileController == null)
                    {
                        tileController = tiles[x, y].AddComponent<TileController>();
                    }
                    tileController.Initialize(new Vector2Int(x, y), this);
                    
                    validTilePositions.Add(new Vector2Int(x, y));
                    tilesCreated++;
                }
            }
        }
        
        Debug.Log($"BoardManager: Se crearon {tilesCreated} tiles");
    }
    
    private bool ShouldCreateTile(int x, int y)
    {
        if (levelNumber < 3) return true;
        
        switch (shapeType)
        {
            case 0:
                return (x < boardWidth/2 + 1) || (y < boardHeight/2 + 1);
            case 1:
                return (x < boardWidth/4) || (x >= 3*boardWidth/4) || (y < boardHeight/2);
            case 2:
                return (y < boardHeight/2 && x < 2*boardWidth/3) || 
                       (y >= boardHeight/2 && x >= boardWidth/3);
            case 3:
                return (y < boardHeight/2) || (x >= boardWidth/3 && x < 2*boardWidth/3);
            case 4:
                return (x >= boardWidth/4 && x < 3*boardWidth/4) || 
                       (y >= boardHeight/4 && y < 3*boardHeight/4);
            default:
                return (x < boardWidth/2 + 1) || (y < boardHeight/2 + 1);
        }
    }
    
    private void GenerateStrategicPositions()
    {
        obstaclePositions.Clear();
        
        if (validTilePositions.Count < 2)
        {
            Debug.LogError("BoardManager: No hay suficientes casillas para colocar personaje y casa");
            return;
        }
        
        // Colocar personaje y casa con distancia mínima
        PlaceCharacterAndHome();
        
        // Solo generar obstáculos si es necesario
        if (obstacleCount <= 0) 
        {
            Debug.Log("BoardManager: No se generarán obstáculos (obstacleCount = 0)");
            return;
        }
        
        // Generar obstáculos estratégicamente
        GenerateStrategicObstacles();
    }
    
    private void PlaceCharacterAndHome()
    {
        int maxAttempts = 50;
        int attempts = 0;
        
        do {
            // Elegir posición aleatoria para el personaje
            int charIndex = Random.Range(0, validTilePositions.Count);
            characterStartPosition = validTilePositions[charIndex];
            
            // Buscar casa con distancia mínima adecuada
            List<Vector2Int> suitableHomePositions = new List<Vector2Int>();
            
            for (int i = 0; i < validTilePositions.Count; i++)
            {
                if (i == charIndex) continue;
                
                Vector2Int pos = validTilePositions[i];
                float distance = Vector2Int.Distance(characterStartPosition, pos);
                
                // Distancia mínima según el nivel
                float minDistance = levelNumber == 1 ? 2f : (levelNumber == 2 ? 3f : 4f);
                
                if (distance >= minDistance)
                {
                    suitableHomePositions.Add(pos);
                }
            }
            
            if (suitableHomePositions.Count > 0)
            {
                homePosition = suitableHomePositions[Random.Range(0, suitableHomePositions.Count)];
                characterPosition = characterStartPosition;
                Debug.Log($"BoardManager: Personaje en {characterStartPosition}, Casa en {homePosition}, Distancia: {Vector2Int.Distance(characterStartPosition, homePosition)}");
                return;
            }
            
            attempts++;
        } while (attempts < maxAttempts);
        
        // Fallback si no se encuentra una buena combinación
        int charIndex2 = Random.Range(0, validTilePositions.Count);
        characterStartPosition = validTilePositions[charIndex2];
        
        int homeIndex2;
        do {
            homeIndex2 = Random.Range(0, validTilePositions.Count);
        } while (homeIndex2 == charIndex2);
        
        homePosition = validTilePositions[homeIndex2];
        characterPosition = characterStartPosition;
        
        Debug.Log($"BoardManager: Fallback - Personaje en {characterStartPosition}, Casa en {homePosition}");
    }
    
    private void GenerateStrategicObstacles()
    {
        // Calcular el camino óptimo inicial
        List<Vector2Int> optimalPath = FindShortestPath(characterStartPosition, homePosition);
        
        if (optimalPath == null || optimalPath.Count < 2)
        {
            Debug.LogWarning("BoardManager: No se pudo calcular un camino óptimo válido");
            GenerateFallbackObstacles();
            return;
        }
        
        Debug.Log($"BoardManager: Camino óptimo calculado con {optimalPath.Count} pasos");
        
        // Identificar posiciones estratégicas cerca del camino
        List<Vector2Int> strategicPositions = FindStrategicPositions(optimalPath);
        
        Debug.Log($"BoardManager: Se encontraron {strategicPositions.Count} posiciones estratégicas");
        
        // Colocar obstáculos estratégicamente
        int obstaclesPlaced = 0;
        int maxAttempts = obstacleCount * 5;
        int attempts = 0;
        
        while (obstaclesPlaced < obstacleCount && attempts < maxAttempts)
        {
            Vector2Int obstaclePos;
            
            // Primero intentar con posiciones estratégicas
            if (strategicPositions.Count > 0)
            {
                int randomIndex = Random.Range(0, strategicPositions.Count);
                obstaclePos = strategicPositions[randomIndex];
                strategicPositions.RemoveAt(randomIndex);
            }
            else
            {
                // Si no hay más posiciones estratégicas, usar posiciones aleatorias válidas
                List<Vector2Int> availablePositions = GetAvailablePositions();
                if (availablePositions.Count == 0) break;
                
                obstaclePos = availablePositions[Random.Range(0, availablePositions.Count)];
            }
            
            // Añadir temporalmente el obstáculo
            obstaclePositions.Add(obstaclePos);
            
            // Verificar que sigue habiendo un camino
            if (IsPathPossible())
            {
                // El obstáculo es válido
                obstaclesPlaced++;
                Debug.Log($"BoardManager: Obstáculo añadido en {obstaclePos}");
            }
            else
            {
                // El obstáculo bloquea completamente, quitarlo
                obstaclePositions.RemoveAt(obstaclePositions.Count - 1);
                Debug.Log($"BoardManager: Obstáculo rechazado en {obstaclePos} (bloquea camino)");
            }
            
            attempts++;
        }
        
        // Si no se colocaron suficientes obstáculos, usar método de respaldo
        if (obstaclesPlaced < obstacleCount)
        {
            Debug.Log($"BoardManager: Solo se colocaron {obstaclesPlaced} de {obstacleCount} obstáculos. Usando método de respaldo.");
            GenerateRemainingObstacles(obstacleCount - obstaclesPlaced);
        }
        
        Debug.Log($"BoardManager: Se colocaron {obstaclePositions.Count} obstáculos en total");
    }
    
    private void GenerateFallbackObstacles()
    {
        Debug.Log("BoardManager: Generando obstáculos con método de respaldo");
        
        List<Vector2Int> availablePositions = GetAvailablePositions();
        int obstaclesPlaced = 0;
        
        while (obstaclesPlaced < obstacleCount && availablePositions.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2Int obstaclePos = availablePositions[randomIndex];
            
            obstaclePositions.Add(obstaclePos);
            
            if (IsPathPossible())
            {
                obstaclesPlaced++;
                Debug.Log($"BoardManager: Obstáculo de respaldo añadido en {obstaclePos}");
            }
            else
            {
                obstaclePositions.RemoveAt(obstaclePositions.Count - 1);
            }
            
            availablePositions.RemoveAt(randomIndex);
        }
    }
    
    private void GenerateRemainingObstacles(int remainingCount)
    {
        List<Vector2Int> availablePositions = GetAvailablePositions();
        int obstaclesPlaced = 0;
        
        while (obstaclesPlaced < remainingCount && availablePositions.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2Int obstaclePos = availablePositions[randomIndex];
            
            obstaclePositions.Add(obstaclePos);
            
            if (IsPathPossible())
            {
                obstaclesPlaced++;
                Debug.Log($"BoardManager: Obstáculo restante añadido en {obstaclePos}");
            }
            else
            {
                obstaclePositions.RemoveAt(obstaclePositions.Count - 1);
            }
            
            availablePositions.RemoveAt(randomIndex);
        }
    }
    
    private List<Vector2Int> GetAvailablePositions()
    {
        List<Vector2Int> available = new List<Vector2Int>();
        
        foreach (Vector2Int pos in validTilePositions)
        {
            if (!pos.Equals(characterStartPosition) && 
                !pos.Equals(homePosition) && 
                !obstaclePositions.Contains(pos))
            {
                available.Add(pos);
            }
        }
        
        return available;
    }
    
    private List<Vector2Int> FindStrategicPositions(List<Vector2Int> path)
    {
        List<Vector2Int> strategic = new List<Vector2Int>();
        
        if (path == null || path.Count < 2) return strategic;
        
        // Posiciones adyacentes al camino (más fáciles de colocar)
        foreach (Vector2Int pathPos in path)
        {
            Vector2Int[] directions = {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjacentPos = pathPos + dir;
                
                if (IsPositionValid(adjacentPos) && 
                    !path.Contains(adjacentPos) && 
                    adjacentPos != characterStartPosition && 
                    adjacentPos != homePosition &&
                    !strategic.Contains(adjacentPos))
                {
                    strategic.Add(adjacentPos);
                }
            }
        }
        
        // Añadir algunas posiciones del camino intermedio (más desafiantes)
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int pathPos = path[i];
            if (IsPositionValid(pathPos) && 
                pathPos != characterStartPosition && 
                pathPos != homePosition &&
                !strategic.Contains(pathPos))
            {
                strategic.Add(pathPos);
            }
        }
        
        // Barajar la lista para variedad
        for (int i = strategic.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector2Int temp = strategic[i];
            strategic[i] = strategic[randomIndex];
            strategic[randomIndex] = temp;
        }
        
        return strategic;
    }
    
    private List<Vector2Int> FindShortestPath(Vector2Int start, Vector2Int goal)
    {
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
        
        var frontier = new System.Collections.Generic.SortedDictionary<int, Queue<Vector2Int>>();
        
        costSoFar[start] = 0;
        AddToFrontier(frontier, 0, start);
        
        while (frontier.Count > 0)
        {
            var firstKey = GetFirstKey(frontier);
            var queue = frontier[firstKey];
            Vector2Int current = queue.Dequeue();
            
            if (queue.Count == 0)
                frontier.Remove(firstKey);
            
            if (current.Equals(goal))
            {
                // Reconstruir camino
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int step = goal;
                
                while (!step.Equals(start))
                {
                    path.Add(step);
                    step = cameFrom[step];
                }
                path.Add(start);
                path.Reverse();
                
                return path;
            }
            
            Vector2Int[] directions = {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                
                if (IsPositionValid(next) && !obstaclePositions.Contains(next))
                {
                    int newCost = costSoFar[current] + 1;
                    
                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        int priority = newCost + ManhattanDistance(next, goal);
                        AddToFrontier(frontier, priority, next);
                        cameFrom[next] = current;
                    }
                }
            }
        }
        
        return null; // No se encontró camino
    }
    
    private void AddToFrontier(SortedDictionary<int, Queue<Vector2Int>> frontier, int priority, Vector2Int item)
    {
        if (!frontier.ContainsKey(priority))
            frontier[priority] = new Queue<Vector2Int>();
        frontier[priority].Enqueue(item);
    }
    
    private int GetFirstKey(SortedDictionary<int, Queue<Vector2Int>> frontier)
    {
        foreach (var key in frontier.Keys)
            return key;
        return 0;
    }
    
    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool IsPathPossible()
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        queue.Enqueue(characterStartPosition);
        visited.Add(characterStartPosition);
        
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            
            if (current.Equals(homePosition))
                return true;
            
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0)
            };
            
            foreach (var dir in directions)
            {
                Vector2Int neighbor = new Vector2Int(current.x + dir.x, current.y + dir.y);
                
                if (IsPositionValid(neighbor) && !obstaclePositions.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }
        
        return false;
    }
    
    private void PlaceGameObjects()
    {
        Debug.Log("BoardManager: Colocando objetos...");
        
        if (validTilePositions.Count == 0)
        {
            Debug.LogError("BoardManager: No hay posiciones válidas para colocar objetos");
            return;
        }
        
        if (characterPrefab != null && IsPositionValid(characterStartPosition))
        {
            Vector3 characterPos = ConvertToWorldPosition(characterStartPosition);
            characterPos.z = -1f;
            
            characterObject = Instantiate(characterPrefab, characterPos, Quaternion.identity);
            characterObject.name = "Character";
            
            CharacterController controller = characterObject.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.Initialize(this, characterStartPosition);
                selectedCharacterSpriteIndex = controller.GetSelectedSpriteIndex();
            }
            
            Debug.Log($"BoardManager: Personaje colocado en posición grid {characterStartPosition}, mundo: {characterPos}");
        }
        else
        {
            Debug.LogError($"BoardManager: No se puede colocar el personaje. Prefab válido: {characterPrefab != null}, Posición válida: {IsPositionValid(characterStartPosition)}");
        }
        
        if (homePrefab != null && IsPositionValid(homePosition))
        {
            Vector3 homePos = ConvertToWorldPosition(homePosition);
            homePos.z = -0.5f;
            
            homeObject = Instantiate(homePrefab, homePos, Quaternion.identity);
            homeObject.name = "Home";
            
            HomeController homeController = homeObject.GetComponent<HomeController>();
            if (homeController != null)
            {
                homeController.Initialize(selectedCharacterSpriteIndex);
                Debug.Log($"BoardManager: HomeController inicializado con índice {selectedCharacterSpriteIndex}");
            }
            
            Debug.Log($"BoardManager: Casa colocada en posición grid {homePosition}, mundo: {homePos}");
        }
        else
        {
            Debug.LogError($"BoardManager: No se puede colocar la casa. Prefab válido: {homePrefab != null}, Posición válida: {IsPositionValid(homePosition)}");
        }
        
        if (obstacleManager != null && obstaclePositions.Count > 0)
        {
            Debug.Log($"BoardManager: Pasando {obstaclePositions.Count} obstáculos al ObstacleManager");
            obstacleManager.SetSelectedCharacterIndex(selectedCharacterSpriteIndex);
            
            List<Vector3> worldPositions = new List<Vector3>();
            
            foreach (Vector2Int obsPos in obstaclePositions)
            {
                if (IsPositionValid(obsPos))
                {
                    Vector3 obstaclePos = ConvertToWorldPosition(obsPos);
                    obstaclePos.z = -0.5f;
                    worldPositions.Add(obstaclePos);
                }
            }
            
            obstacleManager.SpawnObstaclesAtWorldPositions(worldPositions.ToArray(), transform);
        }
        else if (obstaclePositions.Count > 0)
        {
            Debug.LogWarning("BoardManager: No hay ObstacleManager asignado para generar obstáculos");
        }
        else
        {
            Debug.Log("BoardManager: No hay obstáculos que colocar");
        }
    }

    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < boardWidth &&
               position.y >= 0 && position.y < boardHeight &&
               tiles[position.x, position.y] != null;
    }
    
    public bool MoveCharacter(Vector2Int tilePosition)
    {
        if (gameCompleted) return false;
        if (!IsValidMove(tilePosition)) return false;
    
        CharacterController controller = characterObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            bool moved = controller.TryMove(tilePosition);
            
            if (moved && statistics != null)
            {
                statistics.RegisterMove();
            }
            
            return moved;
        }
    
        return false;
    }

    public bool IsValidMove(Vector2Int tilePosition)
    {
        if (tilePosition.x < 0 || tilePosition.x >= boardWidth ||
            tilePosition.y < 0 || tilePosition.y >= boardHeight)
        {
            return false;
        }
    
        if (tiles[tilePosition.x, tilePosition.y] == null)
        {
            return false;
        }
    
        if (obstaclePositions.Contains(tilePosition))
        {
            return false;
        }
    
        int distX = Mathf.Abs(tilePosition.x - characterPosition.x);
        int distY = Mathf.Abs(tilePosition.y - characterPosition.y);
    
        return (distX == 1 && distY == 0) || (distX == 0 && distY == 1);
    }
    
    public void UpdateCharacterPosition(Vector2Int newPosition)
    {
        characterPosition = newPosition;
        CheckWinCondition(newPosition);
    }
    
    public void CheckWinCondition(Vector2Int position)
    {
        if (position.Equals(homePosition) && !gameCompleted)
        {
            gameCompleted = true;
            Debug.Log("¡Nivel completado!");
            
            if (statistics != null)
            {
                statistics.CompletedLevel();
            }
            
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowWinPanel();
            }
        }
    }
    
    public void ShowHint()
    {
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (tiles[x, y] != null)
                {
                    Vector2Int tilePos = new Vector2Int(x, y);
                    if (IsValidMove(tilePos))
                    {
                        TileController tileController = tiles[x, y].GetComponent<TileController>();
                        if (tileController != null)
                        {
                            tileController.HighlightAsHint();
                        }
                    }
                }
            }
        }
        
        StartCoroutine(ClearHintsAfterDelay(3.0f));
    }
    
    private IEnumerator ClearHintsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                if (tiles[x, y] != null)
                {
                    TileController tileController = tiles[x, y].GetComponent<TileController>();
                    if (tileController != null)
                    {
                        tileController.ClearHighlight();
                    }
                }
            }
        }
    }
    
    public Vector3 ConvertToWorldPosition(Vector2Int gridPosition)
    {
        float offsetX = (boardWidth - 1) * 0.5f;
        float offsetY = (boardHeight - 1) * 0.5f;
        return new Vector3(gridPosition.x - offsetX, gridPosition.y - offsetY, 0);
    }
}