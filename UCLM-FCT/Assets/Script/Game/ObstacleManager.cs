using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    // Prefab de obstáculo
    [SerializeField] private GameObject obstaclePrefab;
    
    // Array de sprites para los diferentes obstáculos
    [SerializeField] private Sprite[] obstacleSprites;
    
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private int selectedCharacterIndex = 0;
    
    public void SetSelectedCharacterIndex(int index)
    {
        selectedCharacterIndex = index;
        Debug.Log($"ObstacleManager: Índice de personaje seleccionado: {selectedCharacterIndex}");
    }
    
    public void ClearObstacles()
    {
        foreach (GameObject obstacle in spawnedObstacles)
        {
            Destroy(obstacle);
        }
        spawnedObstacles.Clear();
    }
    
    public void SpawnObstaclesAtWorldPositions(Vector3[] worldPositions, Transform parent)
    {
        // Limpiar obstáculos anteriores
        ClearObstacles();
    
        Debug.Log($"ObstacleManager: Colocando {worldPositions.Length} obstáculos");
    
        if (obstaclePrefab == null)
        {
            Debug.LogError("ObstacleManager: El prefab de obstáculo no está asignado");
            return;
        }
    
        Transform obstaclesParent = new GameObject("Obstacles").transform;
        obstaclesParent.SetParent(parent);
    
        // Crear nuevos obstáculos
        for (int i = 0; i < worldPositions.Length; i++)
        {
            // Instanciar el obstáculo en la posición exacta del mundo
            GameObject obstacle = Instantiate(obstaclePrefab, worldPositions[i], Quaternion.identity, obstaclesParent);
            obstacle.name = $"Obstacle_{i}";
        
            // Asignar el sprite correspondiente
            SpriteRenderer spriteRenderer = obstacle.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && obstacleSprites != null && obstacleSprites.Length > 0)
            {
                // Seleccionar el sprite según el índice del personaje
                int spriteIndex = obstacleSprites.Length > selectedCharacterIndex ? 
                    selectedCharacterIndex : 
                    Random.Range(0, obstacleSprites.Length);
            
                spriteRenderer.sprite = obstacleSprites[spriteIndex];
                Debug.Log($"ObstacleManager: Sprite {spriteIndex} asignado a obstáculo en posición {worldPositions[i]}");
            }
        
            // Intentar asignar el tag si existe
            try {
                obstacle.tag = "Obstacle";
            } catch (System.Exception) {
                // Ignorar si el tag no existe
            }
        
            spawnedObstacles.Add(obstacle);
        }
    
        Debug.Log($"ObstacleManager: Se han colocado {spawnedObstacles.Count} obstáculos");
    }
}