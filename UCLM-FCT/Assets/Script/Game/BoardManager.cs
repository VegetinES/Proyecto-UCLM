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
    public Sprite[] characterSprites; // Los 6 sprites posibles para el personaje
    public ObstacleManager obstacleManager; // Referencia al ObstacleManager
    
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
    
    private int selectedCharacterSpriteIndex = 0; // Índice del sprite seleccionado para el personaje
    
    // Accesores para posiciones guardadas
    public Vector2Int GetCharacterStartPosition() { return characterStartPosition; }
    public Vector2Int GetHomePosition() { return homePosition; }
    public Vector2Int[] GetObstaclePositions() { return obstaclePositions.ToArray(); }
    
    public void SetSelectedCharacterSpriteIndex(int index)
    {
        selectedCharacterSpriteIndex = index;
        Debug.Log($"BoardManager: Índice de sprite seleccionado para Character: {index}");
        
        // Actualizar el ObstacleManager con el índice seleccionado
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
        
        // Reiniciar variables
        startTime = Time.time;
        gameCompleted = false;
        
        // Limpiar el tablero anterior
        Debug.Log("BoardManager: Limpiando tablero anterior");
        ClearBoard();
        
        // Generar forma del tablero
        Debug.Log($"BoardManager: Generando tablero de tamaño {boardWidth}x{boardHeight}");
        GenerateBoard();
        
        // Generar posiciones válidas
        Debug.Log("BoardManager: Generando posiciones de objetos");
        if (isRetry && savedObstacles != null)
        {
            // Usar posiciones guardadas
            characterStartPosition = savedCharStart;
            characterPosition = characterStartPosition;
            homePosition = savedHome;
            obstaclePositions.Clear();
            obstaclePositions.AddRange(savedObstacles);
            Debug.Log("BoardManager: Usando posiciones guardadas para retry");
        }
        else
        {
            // Generar nuevas posiciones aleatorias
            Debug.Log("BoardManager: Generando nuevas posiciones aleatorias");
            GenerateValidPositions();
        }
        
        // Colocar objetos en el tablero
        Debug.Log("BoardManager: Colocando objetos en el tablero");
        PlaceGameObjects();
        
        Debug.Log($"BoardManager: Tablero inicializado - Nivel {levelNumber}, Tamaño: {boardWidth}x{boardHeight}, Obstáculos: {obstacleCount}");
    }
    
    private void ClearBoard()
    {
        // Destruir todas las casillas existentes
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
        
        // Destruir personaje y casa
        if (characterObject != null) Destroy(characterObject);
        if (homeObject != null) Destroy(homeObject);
        
        // Limpiar obstáculos - utilizar GameObject.FindGameObjectsWithTag solo si el tag existe
        // Primero verificamos si el tag "Obstacle" está definido
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
            Debug.LogWarning("BoardManager: No se pudo encontrar obstáculos con tag 'Obstacle'. Asegúrate de que el tag está definido: " + e.Message);
            
            // Alternativa: buscar por nombre
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Obstacle"))
                {
                    Destroy(obj);
                }
            }
        }
        
        // Limpiar lista de posiciones válidas
        validTilePositions.Clear();
        obstaclePositions.Clear();
    }
    
    private void GenerateBoard()
    {
        Debug.Log($"BoardManager: Creando array de tiles {boardWidth}x{boardHeight}");
        tiles = new GameObject[boardWidth, boardHeight];
        
        // Calcular el offset para centrar el tablero
        float offsetX = (boardWidth - 1) * 0.5f;
        float offsetY = (boardHeight - 1) * 0.5f;
        Debug.Log($"BoardManager: Offset calculado - X: {offsetX}, Y: {offsetY}");
        
        int tilesCreated = 0;
        
        Transform tileParent = new GameObject("Tiles").transform;
        tileParent.SetParent(transform);
        
        // Crear tiles
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                // Si no es cuadrado, determinar si creamos este tile
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
                    
                    // Añadir TileController
                    TileController tileController = tiles[x, y].GetComponent<TileController>();
                    if (tileController == null)
                    {
                        tileController = tiles[x, y].AddComponent<TileController>();
                    }
                    tileController.Initialize(new Vector2Int(x, y), this);
                    
                    // Añadir a lista de posiciones válidas
                    validTilePositions.Add(new Vector2Int(x, y));
                    tilesCreated++;
                }
            }
        }
        
        Debug.Log($"BoardManager: Se crearon {tilesCreated} tiles");
    }
    
    private bool ShouldCreateTile(int x, int y)
    {
        // Para nivel 1 y 2, siempre creamos tiles (cuadrado 5x5)
        if (levelNumber < 3) return true;
        
        // Para nivel 3, creamos forma no cuadrada (este es solo un ejemplo)
        
        // Ejemplos de formas no cuadradas:
        // 1. Forma de L
        if (levelNumber == 3)
        {
            // Crear forma de L
            return (x < boardWidth/2 + 1) || (y < boardHeight/2 + 1);
        }
        
        // Por defecto, creamos el tile
        return true;
    }
    
    private void GenerateValidPositions()
    {
        obstaclePositions.Clear();
        
        if (validTilePositions.Count < 2)
        {
            Debug.LogError("BoardManager: No hay suficientes casillas para colocar personaje y casa");
            return;
        }
        
        // Elegir posición aleatoria para el personaje
        int charIndex = Random.Range(0, validTilePositions.Count);
        characterStartPosition = validTilePositions[charIndex];
        characterPosition = characterStartPosition;
        Debug.Log($"BoardManager: Posición del personaje: {characterStartPosition}, índice: {charIndex}");
        
        // Elegir posición para la casa (diferente al personaje)
        int homeIndex;
        do
        {
            homeIndex = Random.Range(0, validTilePositions.Count);
        } while (homeIndex == charIndex);
        
        homePosition = validTilePositions[homeIndex];
        Debug.Log($"BoardManager: Posición de la casa: {homePosition}, índice: {homeIndex}");
        
        // Generar obstáculos solo si obstacleCount > 0
        if (obstacleCount <= 0) 
        {
            Debug.Log("BoardManager: No se generarán obstáculos (obstacleCount = 0)");
            return;
        }
        
        Debug.Log($"BoardManager: Generando {obstacleCount} obstáculos");
        
        // Filtrar posiciones disponibles (que no sean ni personaje ni casa)
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        foreach (Vector2Int pos in validTilePositions)
        {
            if (!pos.Equals(characterStartPosition) && !pos.Equals(homePosition))
            {
                availablePositions.Add(pos);
            }
        }
        
        Debug.Log($"BoardManager: Hay {availablePositions.Count} posiciones disponibles para obstáculos");
        
        // Limitar el número de obstáculos al número de posiciones disponibles
        int numObstacles = Mathf.Min(obstacleCount, availablePositions.Count);
        
        // Elegir posiciones aleatorias para obstáculos
        for (int i = 0; i < numObstacles; i++)
        {
            if (availablePositions.Count == 0) break;
            
            int randomIndex = Random.Range(0, availablePositions.Count);
            obstaclePositions.Add(availablePositions[randomIndex]);
            Debug.Log($"BoardManager: Obstáculo añadido en posición {availablePositions[randomIndex]}");
            availablePositions.RemoveAt(randomIndex);
        }
        
        Debug.Log($"BoardManager: Se han generado {obstaclePositions.Count} posiciones para obstáculos");
    }
    
    // Reemplaza solo el método PlaceGameObjects en el BoardManager.cs
    private void PlaceGameObjects()
    {
        Debug.Log("BoardManager: Colocando objetos...");
        
        // Verificar si hay posiciones válidas
        if (validTilePositions.Count == 0)
        {
            Debug.LogError("BoardManager: No hay posiciones válidas para colocar objetos");
            return;
        }
        
        // Colocar el personaje
        if (characterPrefab != null && IsPositionValid(characterStartPosition))
        {
            // Convertir posición de grid a mundo
            Vector3 characterPos = ConvertToWorldPosition(characterStartPosition);
            characterPos.z = -1f;  // Ajustar Z para que esté por encima de los tiles
            
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
        
        // Colocar la casa
        if (homePrefab != null && IsPositionValid(homePosition))
        {
            // Convertir posición de grid a mundo
            Vector3 homePos = ConvertToWorldPosition(homePosition);
            homePos.z = -0.5f;  // Ajustar Z para que esté entre personaje y obstáculos
            
            homeObject = Instantiate(homePrefab, homePos, Quaternion.identity);
            homeObject.name = "Home";
            
            // Inicializar HomeController si existe
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
        
        // Colocar obstáculos
        if (obstacleManager != null && obstaclePositions.Count > 0)
        {
            Debug.Log($"BoardManager: Pasando {obstaclePositions.Count} obstáculos al ObstacleManager");
            
            // Convertir las posiciones del grid a posiciones en el mundo
            List<Vector3> worldPositions = new List<Vector3>();
            
            foreach (Vector2Int obsPos in obstaclePositions)
            {
                if (IsPositionValid(obsPos))
                {
                    Vector3 obstaclePos = ConvertToWorldPosition(obsPos);
                    obstaclePos.z = -0.5f;  // Ajustar Z igual que la casa
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

    // Método auxiliar para verificar si una posición es válida
    private bool IsPositionValid(Vector2Int position)
    {
        return position.x >= 0 && position.x < boardWidth &&
               position.y >= 0 && position.y < boardHeight &&
               tiles[position.x, position.y] != null;
    }
        
    // Método auxiliar para obtener la posición en el mundo a partir de coordenadas de grid
    private Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        // Calcular el offset para centrar el tablero
        float offsetX = (boardWidth - 1) * 0.5f;
        float offsetY = (boardHeight - 1) * 0.5f;
    
        // Calcular la posición en el mundo
        return new Vector3(gridPosition.x - offsetX, gridPosition.y - offsetY, 0);
    }
    
    // Método auxiliar para generar posiciones de obstáculos
    private void GenerateObstaclePositions()
    {
        obstaclePositions.Clear();
        
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        foreach (Vector2Int pos in validTilePositions)
        {
            if (!pos.Equals(characterStartPosition) && !pos.Equals(homePosition))
            {
                availablePositions.Add(pos);
            }
        }
        
        // Elegir posiciones aleatorias para obstáculos
        int count = Mathf.Min(obstacleCount, availablePositions.Count);
        for (int i = 0; i < count; i++)
        {
            if (availablePositions.Count == 0) break;
            
            int randomIndex = Random.Range(0, availablePositions.Count);
            obstaclePositions.Add(availablePositions[randomIndex]);
            availablePositions.RemoveAt(randomIndex);
        }
        
        Debug.Log($"BoardManager: Generadas {obstaclePositions.Count} posiciones de obstáculos");
    }
    
    public bool MoveCharacter(Vector2Int tilePosition)
    {
        // Verificar si el juego ya está completado
        if (gameCompleted) return false;
    
        // Verificar si la posición es válida
        if (!IsValidMove(tilePosition)) return false;
    
        // Mover personaje
        CharacterController controller = characterObject.GetComponent<CharacterController>();
        if (controller != null)
        {
            return controller.TryMove(tilePosition);
        }
    
        return false;
    }

    public bool IsValidMove(Vector2Int tilePosition)
    {
        // Verificar límites del tablero
        if (tilePosition.x < 0 || tilePosition.x >= boardWidth ||
            tilePosition.y < 0 || tilePosition.y >= boardHeight)
        {
            return false;
        }
    
        // Verificar si hay un tile en esa posición
        if (tiles[tilePosition.x, tilePosition.y] == null)
        {
            return false;
        }
    
        // Verificar si hay un obstáculo
        if (obstaclePositions.Contains(tilePosition))
        {
            return false;
        }
    
        // Verificar que sea una casilla adyacente
        int distX = Mathf.Abs(tilePosition.x - characterPosition.x);
        int distY = Mathf.Abs(tilePosition.y - characterPosition.y);
    
        // Sólo permitir movimientos ortogonales a casillas adyacentes
        if ((distX == 1 && distY == 0) || (distX == 0 && distY == 1))
        {
            return true;
        }
    
        return false;
    }
    
    public void UpdateCharacterPosition(Vector2Int newPosition)
    {
        characterPosition = newPosition;
        
        // Verificar victoria
        CheckWinCondition(newPosition);
    }
    
    public void CheckWinCondition(Vector2Int position)
    {
        // Verificar si el personaje ha llegado a la casa
        if (position.Equals(homePosition) && !gameCompleted)
        {
            gameCompleted = true;
            Debug.Log("¡Nivel completado!");
            
            // Calcular tiempo
            float completionTime = Time.time - startTime;
            
            // Guardar estadísticas
            SaveLevelStatistics(true, completionTime);
            
            // Mostrar panel de victoria
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowWinPanel();
            }
        }
    }
    
    private void SaveLevelStatistics(bool completed, float timeSpent)
    {
        Debug.Log($"Nivel {levelNumber} completado: {completed}, Tiempo: {timeSpent}s");
        
        if (DataManager.Instance != null)
        {
            string userId = DataManager.Instance.GetCurrentUserId();
            
            // Guardar estadísticas en la base de datos
            DataManager.Instance.SaveStatisticsAsync(userId, levelNumber, completed, (int)timeSpent)
                .ConfigureAwait(false);
        }
    }
    
    public void ShowHint()
    {
        // Resaltar casillas válidas para moverse
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
        
        // Desactivar resaltado después de unos segundos
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
        return new Vector3(
            gridPosition.x - 2f,
            gridPosition.y - 2f,
            0f
        );
    }

    private Vector2Int ConvertToGridPosition(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x + 2f),  // Invertir el ajuste horizontal
            Mathf.RoundToInt(worldPosition.y + 2f)   // Invertir el ajuste vertical
        );
    }
}