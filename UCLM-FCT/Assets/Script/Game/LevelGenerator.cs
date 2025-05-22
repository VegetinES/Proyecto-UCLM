using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [System.Serializable]
    public class LevelConfig
    {
        public int width;
        public int height;
        public int maxMoves;
        public int maxTurns;
        public int minObstacles;
        public int maxObstacles;
        public bool isSquare = true;
    }
    
    public LevelConfig[] levelConfigs;
    
    private void Awake()
    {
        Debug.Log($"LevelGenerator: Awake llamado - Configuraciones cargadas: {(levelConfigs != null ? levelConfigs.Length : 0)}");
        
        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            Debug.LogError("LevelGenerator: ¡No hay configuraciones de nivel definidas!");
            // Crear configuraciones por defecto
            CreateDefaultConfigs();
        }
    }
    
    private void CreateDefaultConfigs()
    {
        levelConfigs = new LevelConfig[3];
        
        // Nivel 1: Tablero 5x5 simple sin obstáculos
        levelConfigs[0] = new LevelConfig
        {
            width = 5,
            height = 5,
            maxMoves = 5,
            maxTurns = 1,
            minObstacles = 0,
            maxObstacles = 0,
            isSquare = true
        };
        
        // Nivel 2: Tablero 5x5 con algunos obstáculos
        levelConfigs[1] = new LevelConfig
        {
            width = 5,
            height = 5,
            maxMoves = 10,
            maxTurns = 2,
            minObstacles = 2,
            maxObstacles = 4,
            isSquare = true
        };
        
        // Nivel 3: Tablero 6x6 no cuadrado con más obstáculos
        levelConfigs[2] = new LevelConfig
        {
            width = 6,
            height = 6,
            maxMoves = 15,
            maxTurns = 3,
            minObstacles = 3,
            maxObstacles = 6,
            isSquare = false
        };
        
        Debug.Log("LevelGenerator: Configuraciones por defecto creadas");
    }
    
    public LevelConfig GetLevelConfig(int level)
    {
        Debug.Log($"LevelGenerator: Obteniendo configuración para nivel {level}");
        
        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            Debug.LogError("LevelGenerator: No hay configuraciones de nivel definidas");
            CreateDefaultConfigs();
        }
        
        if (level <= 0 || level > levelConfigs.Length)
        {
            Debug.LogError($"LevelGenerator: Nivel inválido: {level}. Solo hay {levelConfigs.Length} configuraciones");
            
            // Devolver primer nivel como fallback si existe
            if (levelConfigs.Length > 0)
            {
                Debug.Log("LevelGenerator: Devolviendo configuración del nivel 1 como fallback");
                return levelConfigs[0];
            }
            return null;
        }
        
        Debug.Log($"LevelGenerator: Devolviendo configuración para nivel {level}");
        return levelConfigs[level - 1];
    }
}