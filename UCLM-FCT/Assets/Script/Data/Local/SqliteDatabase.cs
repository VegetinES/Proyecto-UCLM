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
    
    public void SaveConfiguration(string uid, int colors, bool autoNarrator, bool sound = true, int generalSound = 50, int musicSound = 50, int effectsSound = 50, int narratorSound = 50, bool vibration = false)
    {
        var config = _db.Table<LocalConfiguration>().FirstOrDefault(c => c.UserID == uid);
        if (config == null)
        {
            _db.Insert(new LocalConfiguration { 
                UserID = uid, 
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
    
    public void SaveParentalControl(string uid, bool activated, string pin, bool soundConf = true, bool accessibilityConf = true, bool statisticsConf = true, bool aboutConf = true, bool profileConf = true)
    {
        var pc = _db.Table<LocalParentalControl>().FirstOrDefault(p => p.UserID == uid);
        if (pc == null)
        {
            _db.Insert(new LocalParentalControl 
            { 
                UserID = uid, 
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
    public LocalConfiguration GetConfiguration(string uid) => _db.Table<LocalConfiguration>().FirstOrDefault(c => c.UserID == uid);
    public LocalParentalControl GetParentalControl(string uid) => _db.Table<LocalParentalControl>().FirstOrDefault(p => p.UserID == uid);
}