using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Realms;

// Esta clase funciona como fachada para acceder a DataService
// Se mantiene por compatibilidad con el código existente
public static class DataAccess
{
    public static Configuration GetConfiguration()
    {
        var userId = GetEffectiveUserId();
        return DataService.Instance.ConfigRepo.GetUserConfiguration(userId);
    }
    
    public static void UpdateConfiguration(int colors, bool autoNarrator)
    {
        var userId = GetEffectiveUserId();
        var configRepo = DataService.Instance.ConfigRepo;
        
        configRepo.UpdateColors(userId, colors);
        configRepo.UpdateAutoNarrator(userId, autoNarrator);
    }
    
    public static void UpdateSoundConfiguration(int colors, bool autoNarrator, bool sound, int generalSound, int musicSound, int effectsSound, int narratorSound, bool vibration)
    {
        var userId = GetCurrentUserId();
        var configRepo = DataService.Instance.ConfigRepo;
        
        configRepo.UpdateFullConfiguration(
            userId, colors, autoNarrator, sound, generalSound, 
            musicSound, effectsSound, narratorSound, vibration
        );
    }
    
    public static ParentalControl GetParentalControl()
    {
        var userId = GetCurrentUserId();
        return DataService.Instance.ParentalRepo.GetUserParentalControl(userId);
    }
    
    public static void UpdateParentalControl(bool activated, string pin, bool soundConf, bool accessibilityConf, bool statisticsConf, bool aboutConf, bool profileConf)
    {
        var userId = GetCurrentUserId();
        DataService.Instance.ParentalRepo.UpdateSettings(
            userId, activated, pin, soundConf, accessibilityConf, 
            statisticsConf, aboutConf, profileConf
        );
    }
    
    public static List<Statistics> GetAllStatistics()
    {
        var userId = GetCurrentUserId();
        return DataService.Instance.StatsRepo.GetUserStatistics(userId);
    }
    
    public static Statistics GetLevelStatistics(int level)
    {
        var userId = GetCurrentUserId();
        return DataService.Instance.StatsRepo.GetLevelStatistics(userId, level);
    }

    public static void SaveLevelAttempt(int level, bool completed, bool helpUsed, int timeSpent)
    {
        var userId = GetCurrentUserId();
        DataService.Instance.StatsRepo.SaveLevelAttempt(userId, level, completed, helpUsed, timeSpent);
    }
    
    public static List<LevelAttempt> GetLevelAttempts(int level)
    {
        var stats = GetLevelStatistics(level);
        if (stats == null) return new List<LevelAttempt>();
        
        return DataService.Instance.StatsRepo.GetLevelAttempts(stats.StatID);
    }
    
    private static string GetCurrentUserId()
    {
        return DataService.Instance.GetCurrentUserId();
    }
    
    private static string GetEffectiveUserId()
    {
        return DataService.Instance.GetEffectiveUserId();
    }
}