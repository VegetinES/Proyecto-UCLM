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
        }
    }
    
    public LevelConfig GetLevelConfig(int level)
    {
        Debug.Log($"LevelGenerator: Obteniendo configuración para nivel {level}");
        
        if (levelConfigs == null || levelConfigs.Length == 0)
        {
            Debug.LogError("LevelGenerator: No hay configuraciones de nivel definidas");
            return null;
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