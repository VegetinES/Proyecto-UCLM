using System;
using UnityEngine;

public static class DataAccess
{
    public static SharedModels.Configuration GetConfiguration()
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        int profileId = GetCurrentProfileId();
        
        // Intentar obtener configuración específica del perfil primero
        var config = profileId > 0 
            ? SqliteDatabase.Instance.GetConfiguration(userId, profileId) 
            : SqliteDatabase.Instance.GetConfiguration(userId);
        
        if (config == null)
        {
            return new SharedModels.Configuration { Colors = 3, AutoNarrator = false, Sound = true, GeneralSound = 50, MusicSound = 50, EffectsSound = 50, NarratorSound = 50, Vibration = false };
        }
        
        return new SharedModels.Configuration 
        { 
            Colors = config.Colors, 
            AutoNarrator = config.AutoNarrator,
            Sound = config.Sound,
            GeneralSound = config.GeneralSound,
            MusicSound = config.MusicSound,
            EffectsSound = config.EffectsSound,
            NarratorSound = config.NarratorSound,
            Vibration = config.Vibration
        };
    }
    
    private static int GetCurrentProfileId()
    {
        // Obtener el ID del perfil actual si existe
        return ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile() 
            ? ProfileManager.Instance.GetCurrentProfileId() 
            : 0;
    }
}