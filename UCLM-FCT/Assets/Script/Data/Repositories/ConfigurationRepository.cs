using System;
using System.Linq;
using Realms;
using UnityEngine;

public class ConfigurationRepository : RealmRepository<Configuration>
{
    public Configuration GetUserConfiguration(string userId)
    {
        try
        {
            bool isProfileId = int.TryParse(userId, out int profileIdValue);
        
            using (var realm = Realm.GetInstance(realmConfig))
            {
                Configuration config = null;
            
                if (isProfileId)
                {
                    // Primero obtenemos el perfil
                    var profile = realm.All<Profile>().FirstOrDefault(p => p.ProfileID == profileIdValue);
                    
                    if (profile != null)
                    {
                        // Luego obtenemos la configuración asociada a ese perfil
                        config = realm.All<Configuration>().FirstOrDefault(c => c.Profile == profile);
                    }
                }
                else
                {
                    // Buscar configuración por UID (en caso de ser un usuario no tutor)
                    config = realm.Find<Configuration>(userId);
                }
            
                if (config != null)
                {
                    // Crear una copia desvinculada del objeto Realm
                    return new Configuration {
                        ID = config.ID,
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
                
                // Si no se encuentra la configuración, crear una por defecto
                var user = realm.Find<User>(userId);
                if (user != null)
                {
                    Debug.Log($"ConfigurationRepository: Configuración no encontrada para {userId}, creando configuración por defecto");
                    CreateDefaultConfiguration(userId, user);
                    
                    // Intentar obtener la configuración recién creada
                    config = realm.Find<Configuration>(userId);
                    if (config != null)
                    {
                        return new Configuration {
                            ID = config.ID,
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
                }
                
                Debug.LogWarning($"ConfigurationRepository: No se pudo obtener ni crear configuración para {userId}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener configuración de usuario: {e.Message}");
            return null;
        }
    }
    
    public void CreateDefaultConfiguration(string userId, User user)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var config = realm.Find<Configuration>(userId);
                
                if (config == null)
                {
                    var userObj = realm.Find<User>(userId);
                    
                    if (userObj != null)
                    {
                        realm.Write(() => {
                            var newConfig = realm.Add(new Configuration {
                                ID = userId,
                                User = userObj,
                                Colors = 3,
                                AutoNarrator = false,
                                Sound = true,
                                GeneralSound = 50,
                                MusicSound = 50,
                                EffectsSound = 50,
                                NarratorSound = 50,
                                Vibration = false
                            });
                            
                            userObj.Configuration = newConfig;
                            Debug.Log($"ConfigurationRepository: Configuración por defecto creada para {userId}");
                        });
                    }
                    else
                    {
                        Debug.LogError($"ConfigurationRepository: No se encontró el usuario {userId} al crear configuración por defecto");
                    }
                }
                else
                {
                    Debug.Log($"ConfigurationRepository: La configuración para {userId} ya existe");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear configuración por defecto: {e.Message}");
        }
    }
    
    public void UpdateColors(string userId, int colors)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var config = realm.Find<Configuration>(userId);
                
                if (config != null)
                {
                    realm.Write(() => {
                        config.Colors = colors;
                    });
                    Debug.Log($"ConfigurationRepository: Color actualizado a {colors} para {userId}");
                }
                else
                {
                    Debug.LogWarning($"ConfigurationRepository: No se encontró configuración para {userId} al actualizar colores");
                    var user = realm.Find<User>(userId);
                    if (user != null)
                    {
                        CreateDefaultConfiguration(userId, user);
                        Debug.Log($"ConfigurationRepository: Creada configuración por defecto y se intentará actualizar los colores nuevamente");
                        UpdateColors(userId, colors);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar colores: {e.Message}");
        }
    }
    
    public void UpdateAutoNarrator(string userId, bool autoNarrator)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var config = realm.Find<Configuration>(userId);
                
                if (config != null)
                {
                    realm.Write(() => {
                        config.AutoNarrator = autoNarrator;
                    });
                    Debug.Log($"ConfigurationRepository: AutoNarrator actualizado a {autoNarrator} para {userId}");
                }
                else
                {
                    Debug.LogWarning($"ConfigurationRepository: No se encontró configuración para {userId} al actualizar narrador automático");
                    var user = realm.Find<User>(userId);
                    if (user != null)
                    {
                        CreateDefaultConfiguration(userId, user);
                        Debug.Log($"ConfigurationRepository: Creada configuración por defecto y se intentará actualizar el narrador nuevamente");
                        UpdateAutoNarrator(userId, autoNarrator);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar narrador automático: {e.Message}");
        }
    }
    
    public void UpdateSoundSettings(string userId, bool sound, int generalSound, int musicSound, int effectsSound, int narratorSound, bool vibration)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var config = realm.Find<Configuration>(userId);
                
                if (config != null)
                {
                    realm.Write(() => {
                        config.Sound = sound;
                        config.GeneralSound = generalSound;
                        config.MusicSound = musicSound;
                        config.EffectsSound = effectsSound;
                        config.NarratorSound = narratorSound;
                        config.Vibration = vibration;
                    });
                    Debug.Log($"ConfigurationRepository: Configuración de sonido actualizada para {userId}");
                }
                else
                {
                    Debug.LogWarning($"ConfigurationRepository: No se encontró configuración para {userId} al actualizar configuración de sonido");
                    var user = realm.Find<User>(userId);
                    if (user != null)
                    {
                        CreateDefaultConfiguration(userId, user);
                        Debug.Log($"ConfigurationRepository: Creada configuración por defecto y se intentará actualizar el sonido nuevamente");
                        UpdateSoundSettings(userId, sound, generalSound, musicSound, effectsSound, narratorSound, vibration);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar configuración de sonido: {e.Message}");
        }
    }
    
    public void UpdateFullConfiguration(string userId, int colors, bool autoNarrator, bool sound, int generalSound, int musicSound, int effectsSound, int narratorSound, bool vibration)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var config = realm.Find<Configuration>(userId);
                
                if (config != null)
                {
                    realm.Write(() => {
                        config.Colors = colors;
                        config.AutoNarrator = autoNarrator;
                        config.Sound = sound;
                        config.GeneralSound = generalSound;
                        config.MusicSound = musicSound;
                        config.EffectsSound = effectsSound;
                        config.NarratorSound = narratorSound;
                        config.Vibration = vibration;
                    });
                    Debug.Log($"ConfigurationRepository: Configuración completa actualizada para {userId}");
                }
                else
                {
                    Debug.LogWarning($"ConfigurationRepository: No se encontró configuración para {userId} al actualizar configuración completa");
                    var user = realm.Find<User>(userId);
                    if (user != null)
                    {
                        CreateDefaultConfiguration(userId, user);
                        // Intentar actualizar nuevamente
                        UpdateFullConfiguration(userId, colors, autoNarrator, sound, generalSound, musicSound, effectsSound, narratorSound, vibration);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar configuración completa: {e.Message}");
        }
    }
}