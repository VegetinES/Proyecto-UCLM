using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MongoDB.Bson;
using TMPro;

public class StatisticsPanel : MonoBehaviour
{
    [Header("Estados")]
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private GameObject noStatsMessage;
    [SerializeField] private GameObject statsContainer;
    
    [Header("Elementos de filtro")]
    [SerializeField] private TMP_Dropdown levelFilter;
    
    [Header("Estadísticas")]
    [SerializeField] private TMP_Text tiempoJuegoText;
    [SerializeField] private TMP_Text nivelesCompletadosText;
    [SerializeField] private TMP_Text nivelesNoCompletadosText;
    [SerializeField] private TMP_Text vecesAyudaText;
    [SerializeField] private TMP_Text movimientosText;
    [SerializeField] private TMP_Text nivelText;
    
    [Header("Debug")]
    [SerializeField] private bool useTestData = true;
    
    private List<BsonDocument> loadedStats = new List<BsonDocument>();
    private int selectedLevel = 0;
    
    private void OnEnable()
    {
        ShowLoading();
        SetupLevelDropdown();
        
        if (useTestData)
        {
            CreateTestData();
            ShowStats();
        }
        else
        {
            LoadStatistics();
        }
    }
    
    private void ShowLoading()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (noStatsMessage != null) noStatsMessage.SetActive(false);
        if (statsContainer != null) statsContainer.SetActive(false);
    }
    
    private void ShowNoStats()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (noStatsMessage != null) noStatsMessage.SetActive(true);
        if (statsContainer != null) statsContainer.SetActive(false);
    }
    
    private void ShowStats()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (noStatsMessage != null) noStatsMessage.SetActive(false);
        if (statsContainer != null) statsContainer.SetActive(true);
        UpdateStatisticsDisplay();
    }
    
    private void CreateTestData()
    {
        loadedStats.Clear();
        
        for (int i = 0; i < 15; i++)
        {
            var testStat = new BsonDocument
            {
                { "level", (i % 3) + 1 },
                { "completed", i % 2 == 0 },
                { "moves", UnityEngine.Random.Range(3, 15) },
                { "timeSpent", UnityEngine.Random.Range(10, 120) },
                { "helpUsed", i % 3 == 0 },
                { "timestamp", DateTime.Now.AddHours(-i).ToString("o") }
            };
            
            loadedStats.Add(testStat);
        }
        
        Debug.Log($"StatisticsPanel: Creados {loadedStats.Count} datos de prueba");
    }
    
    private void SetupLevelDropdown()
    {
        if (levelFilter != null)
        {
            levelFilter.ClearOptions();
            
            List<string> options = new List<string>
            {
                "Todos los niveles",
                "Nivel 1", 
                "Nivel 2",
                "Nivel 3"
            };
            
            levelFilter.AddOptions(options);
            levelFilter.onValueChanged.RemoveAllListeners();
            levelFilter.onValueChanged.AddListener(OnLevelFilterChanged);
            levelFilter.value = 0;
        }
    }
    
    private void OnLevelFilterChanged(int value)
    {
        selectedLevel = value;
        Debug.Log($"StatisticsPanel: Filtro cambiado a nivel {selectedLevel}");
        
        if (loadedStats.Count > 0)
        {
            UpdateStatisticsDisplay();
        }
    }
    
    private async void LoadStatistics()
    {
        try
        {
            string userId = AuthManager.Instance?.UserID ?? AuthManager.DEFAULT_USER_ID;
            int profileId = 0;
            
            if (ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile())
            {
                profileId = ProfileManager.Instance.GetCurrentProfileId();
            }
            
            Debug.Log($"StatisticsPanel: Cargando estadísticas para usuario {userId}, perfil {profileId}");
            
            if (MongoDbService.Instance != null && MongoDbService.Instance.IsConnected() 
                && userId != AuthManager.DEFAULT_USER_ID)
            {
                loadedStats = await MongoDbService.Instance.GetGameStatisticsAsync(userId, profileId);
                Debug.Log($"StatisticsPanel: Cargadas {loadedStats.Count} estadísticas desde MongoDB");
                
                if (loadedStats.Count == 0)
                {
                    CreateTestData();
                }
            }
            else
            {
                CreateTestData();
            }
            
            if (loadedStats.Count == 0)
            {
                ShowNoStats();
            }
            else
            {
                ShowStats();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"StatisticsPanel: Error al cargar estadísticas: {e.Message}");
            CreateTestData();
            
            if (loadedStats.Count == 0)
            {
                ShowNoStats();
            }
            else
            {
                ShowStats();
            }
        }
    }
    
    private void UpdateStatisticsDisplay()
    {
        List<BsonDocument> filteredStats = FilterStatsByLevel(loadedStats, selectedLevel);
        
        int tiempoTotal = 0;
        int completados = 0;
        int noCompletados = 0;
        int vecesAyuda = 0;
        int movimientosTotal = 0;
        
        foreach (var stat in filteredStats)
        {
            if (stat.Contains("timeSpent"))
                tiempoTotal += GetSafeInt(stat, "timeSpent", 0);
                
            if (stat.Contains("completed") && stat["completed"].AsBoolean)
                completados++;
            else
                noCompletados++;
                
            if (stat.Contains("helpUsed") && stat["helpUsed"].AsBoolean)
                vecesAyuda++;
                
            if (stat.Contains("moves"))
                movimientosTotal += GetSafeInt(stat, "moves", 0);
        }
        
        if (tiempoJuegoText != null)
            tiempoJuegoText.text = FormatTime(tiempoTotal);
            
        if (nivelesCompletadosText != null)
            nivelesCompletadosText.text = completados.ToString();
            
        if (nivelesNoCompletadosText != null)
            nivelesNoCompletadosText.text = noCompletados.ToString();
            
        if (vecesAyudaText != null)
            vecesAyudaText.text = vecesAyuda.ToString();
            
        if (movimientosText != null)
            movimientosText.text = movimientosTotal.ToString();
        
        if (nivelText != null)
        {
            if (selectedLevel == 0)
                nivelText.text = "";
            else
                nivelText.text = selectedLevel.ToString();
        }
        
        Debug.Log($"StatisticsPanel: Stats actualizadas - Completados: {completados}, No completados: {noCompletados}, Tiempo: {tiempoTotal}s");
    }
    
    private List<BsonDocument> FilterStatsByLevel(List<BsonDocument> stats, int level)
    {
        if (level == 0)
            return stats;
        
        List<BsonDocument> filtered = new List<BsonDocument>();
        
        foreach (var stat in stats)
        {
            if (stat.Contains("level") && stat["level"].AsInt32 == level)
            {
                filtered.Add(stat);
            }
        }
        
        return filtered;
    }
    
    private int GetSafeInt(BsonDocument doc, string key, int defaultValue)
    {
        try
        {
            if (doc != null && doc.Contains(key))
            {
                var value = doc[key];
                if (value.IsInt32) return value.AsInt32;
                if (value.IsInt64) return (int)value.AsInt64;
                if (value.IsDouble) return (int)value.AsDouble;
            }
        }
        catch
        {
        }
        
        return defaultValue;
    }
    
    private string FormatTime(int seconds)
    {
        if (seconds <= 0) return "0s";
        
        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        
        if (minutes > 0)
            return $"{minutes}m {remainingSeconds}s";
        else
            return $"{remainingSeconds}s";
    }
}