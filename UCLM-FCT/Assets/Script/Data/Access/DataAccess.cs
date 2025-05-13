using System;
using UnityEngine;

public static class DataAccess
{
    public static SharedModels.Configuration GetConfiguration()
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        var config = SqliteDatabase.Instance.GetConfiguration(userId);
        
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
    
    public static void UpdateConfiguration(int colors, bool autoNarrator)
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        var config = SqliteDatabase.Instance.GetConfiguration(userId);
        
        if (config != null)
        {
            SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, config.Sound, config.GeneralSound, config.MusicSound, config.EffectsSound, config.NarratorSound, config.Vibration);
        }
        else
        {
            SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator);
        }
    }
    
    public static void UpdateSoundConfiguration(int colors, bool autoNarrator, bool sound, int generalSound, int musicSound, int effectsSound, int narratorSound, bool vibration)
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        SqliteDatabase.Instance.SaveConfiguration(userId, colors, autoNarrator, sound, generalSound, musicSound, effectsSound, narratorSound, vibration);
    }
    
    public static SharedModels.ParentalControl GetParentalControl()
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        var pc = SqliteDatabase.Instance.GetParentalControl(userId);
        
        if (pc == null)
        {
            return new SharedModels.ParentalControl { Activated = false, Pin = "", SoundConf = true, AccessibilityConf = true, StatisticsConf = true, AboutConf = true, ProfileConf = true };
        }
        
        return new SharedModels.ParentalControl 
        { 
            Activated = pc.Activated, 
            Pin = pc.Pin,
            SoundConf = pc.SoundConf,
            AccessibilityConf = pc.AccessibilityConf,
            StatisticsConf = pc.StatisticsConf,
            AboutConf = pc.AboutConf,
            ProfileConf = pc.ProfileConf
        };
    }
    
    public static void UpdateParentalControl(bool activated, string pin, bool soundConf, bool accessibilityConf, bool statisticsConf, bool aboutConf, bool profileConf)
    {
        var userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        SqliteDatabase.Instance.SaveParentalControl(userId, activated, pin, soundConf, accessibilityConf, statisticsConf, aboutConf, profileConf);
    }
}