using System;
using System.Collections.Generic;
using System.Linq;
using Realms;
using UnityEngine;

public class StatisticsRepository : RealmRepository<Statistics>
{
    public List<Statistics> GetUserStatistics(string userId)
    {
        try
        {
            bool isProfileId = int.TryParse(userId, out int profileIdValue);
            
            using (var realm = Realm.GetInstance(realmConfig))
            {
                if (isProfileId)
                {
                    // Primero obtenemos el perfil
                    var profile = realm.All<Profile>().FirstOrDefault(p => p.ProfileID == profileIdValue);
                    
                    if (profile != null)
                    {
                        // Luego obtenemos las estadísticas asociadas a ese perfil
                        return realm.All<Statistics>().Where(s => s.Profile == profile).ToList();
                    }
                    return new List<Statistics>();
                }
                else
                {
                    // Buscar estadísticas por UID (usuario no tutor)
                    return realm.All<Statistics>().Where(s => s.User.UID == userId && s.Profile == null).ToList();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener estadísticas: {e.Message}");
            return new List<Statistics>();
        }
    }
    
    public Statistics GetLevelStatistics(string userId, int level)
    {
        try
        {
            bool isProfileId = int.TryParse(userId, out int profileIdValue);
            
            using (var realm = Realm.GetInstance(realmConfig))
            {
                if (isProfileId)
                {
                    // Primero obtenemos el perfil
                    var profile = realm.All<Profile>().FirstOrDefault(p => p.ProfileID == profileIdValue);
                    
                    if (profile != null)
                    {
                        // Luego obtenemos las estadísticas del nivel específico para ese perfil
                        return realm.All<Statistics>().Where(s => s.Profile == profile && s.Level == level).FirstOrDefault();
                    }
                    return null;
                }
                else
                {
                    // Buscar estadísticas por UID (usuario no tutor)
                    return realm.All<Statistics>().Where(s => s.User.UID == userId && s.Profile == null && s.Level == level).FirstOrDefault();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener estadísticas de nivel: {e.Message}");
            return null;
        }
    }
    
    public List<LevelAttempt> GetLevelAttempts(int statisticsId)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                return realm.All<LevelAttempt>().Where(a => a.Statistics.StatID == statisticsId).ToList();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener intentos de nivel: {e.Message}");
            return new List<LevelAttempt>();
        }
    }
    
    public void SaveLevelAttempt(string userId, int level, bool completed, bool helpUsed, int timeSpent)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var user = realm.Find<User>(userId);
                if (user == null) return;
                
                var stats = realm.All<Statistics>().Where(s => s.User.UID == userId && s.Level == level).FirstOrDefault();
                
                realm.Write(() => {
                    // Si no existen estadísticas, crear
                    if (stats == null)
                    {
                        var nextId = realm.All<Statistics>().Any() ? realm.All<Statistics>().Max(s => s.StatID) + 1 : 1;
                        
                        stats = realm.Add(new Statistics {
                            StatID = nextId,
                            User = user,
                            Level = level,
                            Completed = completed,
                            Failed = !completed ? 1 : 0
                        });
                    }
                    else
                    {
                        // Actualizar estadísticas existentes
                        if (completed && !stats.Completed)
                        {
                            stats.Completed = true;
                        }
                        else if (!completed)
                        {
                            stats.Failed += 1;
                        }
                    }
                    
                    // Crear nuevo intento
                    var nextAttemptId = realm.All<LevelAttempt>().Any() ? realm.All<LevelAttempt>().Max(a => a.AttemptID) + 1 : 1;
                    
                    realm.Add(new LevelAttempt {
                        AttemptID = nextAttemptId,
                        Statistics = stats,
                        Completed = completed,
                        HelpUsed = helpUsed,
                        TimeSpent = timeSpent,
                        CompletionDate = DateTimeOffset.Now
                    });
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar intento de nivel: {e.Message}");
        }
    }
}