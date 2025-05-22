using System;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

public class SqliteDatabase
{
    private SQLiteConnection _db;
    private static SqliteDatabase _instance;
    public static SqliteDatabase Instance => _instance ??= new SqliteDatabase();
    
    private SqliteDatabase()
    {
        string dbPath = System.IO.Path.Combine(Application.persistentDataPath, "local_data.db");
        _db = new SQLiteConnection(dbPath);
        CreateTables();
    }
    
    private void CreateTables()
    {
        _db.CreateTable<LocalUser>();
        _db.CreateTable<LocalConfiguration>();
        _db.CreateTable<LocalParentalControl>();
        _db.CreateTable<LocalProfile>();
    }
    
    public void SaveUser(string uid, string email, bool isTutor)
    {
        var user = _db.Table<LocalUser>().FirstOrDefault(u => u.UID == uid);
        if (user == null)
        {
            _db.Insert(new LocalUser { 
                UID = uid, 
                Email = email, 
                IsTutor = isTutor,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                LastLogin = DateTime.UtcNow.ToString("o")
            });
        }
        else
        {
            user.Email = email;
            user.IsTutor = isTutor;
            user.LastLogin = DateTime.UtcNow.ToString("o");
            _db.Update(user);
        }
    }

    public void UpdateLastLogin(string uid)
    {
        var user = _db.Table<LocalUser>().FirstOrDefault(u => u.UID == uid);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow.ToString("o");
            _db.Update(user);
        }
    }
    
    public void SaveConfiguration(string userId, int colors, bool autoNarrator, bool sound = true, int generalSound = 50, int musicSound = 50, int effectsSound = 50, int narratorSound = 50, bool vibration = false, int profileId = 0)
    {
        var config = _db.Table<LocalConfiguration>().FirstOrDefault(c => c.UserID == userId && c.ProfileID == profileId);
        
        if (config == null)
        {
            _db.Insert(new LocalConfiguration { 
                UserID = userId, 
                ProfileID = profileId,
                Colors = colors, 
                AutoNarrator = autoNarrator,
                Sound = sound,
                GeneralSound = generalSound,
                MusicSound = musicSound,
                EffectsSound = effectsSound,
                NarratorSound = narratorSound,
                Vibration = vibration
            });
        }
        else
        {
            config.Colors = colors;
            config.AutoNarrator = autoNarrator;
            config.Sound = sound;
            config.GeneralSound = generalSound;
            config.MusicSound = musicSound;
            config.EffectsSound = effectsSound;
            config.NarratorSound = narratorSound;
            config.Vibration = vibration;
            _db.Update(config);
        }
    }
    
    public void SaveParentalControl(string userId, bool activated, string pin, bool soundConf = true, bool accessibilityConf = true, bool statisticsConf = true, bool aboutConf = true, bool profileConf = true, int profileId = 0)
    {
        var pc = _db.Table<LocalParentalControl>().FirstOrDefault(p => p.UserID == userId && p.ProfileID == profileId);
        
        if (pc == null)
        {
            _db.Insert(new LocalParentalControl 
            { 
                UserID = userId, 
                ProfileID = profileId,
                Activated = activated, 
                Pin = pin,
                SoundConf = soundConf,
                AccessibilityConf = accessibilityConf,
                StatisticsConf = statisticsConf,
                AboutConf = aboutConf,
                ProfileConf = profileConf
            });
        }
        else
        {
            pc.Activated = activated;
            pc.Pin = pin;
            pc.SoundConf = soundConf;
            pc.AccessibilityConf = accessibilityConf;
            pc.StatisticsConf = statisticsConf;
            pc.AboutConf = aboutConf;
            pc.ProfileConf = profileConf;
            _db.Update(pc);
        }
    }
    
    public LocalUser GetUser(string uid) => _db.Table<LocalUser>().FirstOrDefault(u => u.UID == uid);
    public LocalConfiguration GetConfiguration(string userId)
    {
        return _db.Table<LocalConfiguration>().FirstOrDefault(c => c.UserID == userId && c.ProfileID == 0);
    }

    // Nueva sobrecarga que acepta el parámetro profileId
    public LocalConfiguration GetConfiguration(string userId, int profileId)
    {
        return _db.Table<LocalConfiguration>()
            .FirstOrDefault(c => c.UserID == userId && c.ProfileID == profileId);
    }
    public LocalParentalControl GetParentalControl(string userId)
    {
        return _db.Table<LocalParentalControl>().FirstOrDefault(p => p.UserID == userId && p.ProfileID == 0);
    }

    // Nueva sobrecarga que acepta el parámetro profileId
    public LocalParentalControl GetParentalControl(string userId, int profileId)
    {
        return _db.Table<LocalParentalControl>()
            .FirstOrDefault(p => p.UserID == userId && p.ProfileID == profileId);
    }
    
    public int SaveProfile(string userId, string name, string gender)
    {
        var profile = new LocalProfile
        {
            UserID = userId,
            Name = name,
            Gender = gender
        };
    
        _db.Insert(profile);
    
        // Obtener el ID generado
        var savedProfile = _db.Table<LocalProfile>().Where(p => p.UserID == userId && p.Name == name && p.Gender == gender).OrderByDescending(p => p.ProfileID).FirstOrDefault();
        
        return savedProfile?.ProfileID ?? 0;
    }

    public void DeleteProfile(int profileId)
    {
        _db.Delete<LocalProfile>(profileId);
    
        // También eliminar configuraciones asociadas a este perfil
        var configs = _db.Table<LocalConfiguration>().Where(c => c.ProfileID == profileId).ToList();
        foreach (var config in configs)
        {
            _db.Delete<LocalConfiguration>(config.ID);
        }
    
        var parentalControls = _db.Table<LocalParentalControl>().Where(p => p.ProfileID == profileId).ToList();
        foreach (var pc in parentalControls)
        {
            _db.Delete<LocalParentalControl>(pc.ID);
        }
    
        Debug.Log($"Perfil {profileId} y sus configuraciones asociadas eliminados de la base de datos");
    }

    public List<LocalProfile> GetProfiles(string userId)
    {
        return _db.Table<LocalProfile>().Where(p => p.UserID == userId).ToList();
    }
    
    public LocalProfile GetProfileById(int profileId)
    {
        return _db.Table<LocalProfile>().FirstOrDefault(p => p.ProfileID == profileId);
    }
}