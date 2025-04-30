using System;
using System.Linq;
using Realms;
using UnityEngine;

public class ParentalControlRepository : RealmRepository<ParentalControl>
{
    public ParentalControl GetUserParentalControl(string userId)
    {
        try
        {
            bool isProfileId = int.TryParse(userId, out int profileIdValue);
            
            using (var realm = Realm.GetInstance(realmConfig))
            {
                ParentalControl parentalControl = null;
                
                if (isProfileId)
                {
                    // Primero obtenemos el perfil
                    var profile = realm.All<Profile>().FirstOrDefault(p => p.ProfileID == profileIdValue);
                    
                    if (profile != null)
                    {
                        // Luego obtenemos el control parental asociado a ese perfil
                        parentalControl = realm.All<ParentalControl>().FirstOrDefault(p => p.Profile == profile);
                    }
                }
                else
                {
                    // Buscar control parental por UID (usuario no tutor)
                    parentalControl = realm.Find<ParentalControl>(userId);
                }
                
                // Si no existe, creamos uno por defecto pero con control parental desactivado
                if (parentalControl == null)
                {
                    if (isProfileId)
                    {
                        var profile = realm.All<Profile>().FirstOrDefault(p => p.ProfileID == profileIdValue);
                            
                        if (profile != null)
                        {
                            Debug.Log("Creando control parental por defecto para el perfil " + userId);
                            realm.Write(() => {
                                parentalControl = realm.Add(new ParentalControl {
                                    ID = userId,
                                    Profile = profile,
                                    User = profile.User,
                                    Activated = false,
                                    Pin = "",
                                    SoundConf = true,
                                    AccessibilityConf = true,
                                    StatisticsConf = true,
                                    AboutConf = true,
                                    ProfileConf = true
                                });
                                
                                profile.ParentalControl = parentalControl;
                            });
                        }
                    }
                    else
                    {
                        var user = realm.Find<User>(userId);
                        if (user != null)
                        {
                            Debug.Log("Creando control parental por defecto para el usuario " + userId);
                            realm.Write(() => {
                                parentalControl = realm.Add(new ParentalControl {
                                    ID = userId,
                                    User = user,
                                    Activated = false,
                                    Pin = "",
                                    SoundConf = true,
                                    AccessibilityConf = true,
                                    StatisticsConf = true,
                                    AboutConf = true,
                                    ProfileConf = true
                                });
                                
                                user.ParentalControl = parentalControl;
                            });
                        }
                    }
                }
                
                // Retornar una copia desvinculada para evitar excepciones de RealmClosedException
                if (parentalControl != null)
                {
                    return new ParentalControl {
                        ID = parentalControl.ID,
                        Activated = parentalControl.Activated,
                        Pin = parentalControl.Pin,
                        SoundConf = parentalControl.SoundConf,
                        AccessibilityConf = parentalControl.AccessibilityConf,
                        StatisticsConf = parentalControl.StatisticsConf,
                        AboutConf = parentalControl.AboutConf,
                        ProfileConf = parentalControl.ProfileConf
                    };
                }
                
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener control parental: {e.Message}");
            return null;
        }
    }
    
    public void CreateDefaultParentalControl(string userId, User user)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var parentalControl = realm.Find<ParentalControl>(userId);
                
                if (parentalControl == null)
                {
                    var userObj = realm.Find<User>(userId);
                    
                    if (userObj != null)
                    {
                        realm.Write(() => {
                            var newParentalControl = realm.Add(new ParentalControl {
                                ID = userId,
                                User = userObj,
                                Activated = false, // Inicialmente desactivado
                                Pin = "",
                                SoundConf = true,
                                AccessibilityConf = true,
                                StatisticsConf = true,
                                AboutConf = true,
                                ProfileConf = true
                            });
                            
                            userObj.ParentalControl = newParentalControl;
                        });
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear control parental por defecto: {e.Message}");
        }
    }
    
    public void UpdateSettings(string userId, bool activated, string pin, bool soundConf, bool accessibilityConf, bool statisticsConf, bool aboutConf, bool profileConf)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var parentalControl = realm.Find<ParentalControl>(userId);
                
                if (parentalControl != null)
                {
                    realm.Write(() => {
                        parentalControl.Activated = activated;
                        if (!string.IsNullOrEmpty(pin))
                        {
                            parentalControl.Pin = pin;
                        }
                        parentalControl.SoundConf = soundConf;
                        parentalControl.AccessibilityConf = accessibilityConf;
                        parentalControl.StatisticsConf = statisticsConf;
                        parentalControl.AboutConf = aboutConf;
                        parentalControl.ProfileConf = profileConf;
                    });
                }
                else
                {
                    // Si no existe, crear uno nuevo con estos valores
                    var user = realm.Find<User>(userId);
                    if (user != null)
                    {
                        realm.Write(() => {
                            var newParentalControl = realm.Add(new ParentalControl {
                                ID = userId,
                                User = user,
                                Activated = activated,
                                Pin = pin,
                                SoundConf = soundConf,
                                AccessibilityConf = accessibilityConf,
                                StatisticsConf = statisticsConf,
                                AboutConf = aboutConf,
                                ProfileConf = profileConf
                            });
                            
                            user.ParentalControl = newParentalControl;
                        });
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar control parental: {e.Message}");
        }
    }
    
    // Método auxiliar para verificar si el control parental está activado y tiene PIN
    public bool IsParentalControlConfigured(string userId)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var parentalControl = realm.Find<ParentalControl>(userId);
                
                return parentalControl != null && parentalControl.Activated && !string.IsNullOrEmpty(parentalControl.Pin);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al verificar si el control parental está configurado: {e.Message}");
            return false;
        }
    }
}