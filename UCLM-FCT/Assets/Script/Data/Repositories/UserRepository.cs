using System;
using System.Linq;
using Realms;
using UnityEngine;

public class UserRepository : RealmRepository<User>
{
    public void CreateDefaultUser(string defaultUserId)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var defaultUser = realm.Find<User>(defaultUserId);
                
                if (defaultUser == null)
                {
                    realm.Write(() => {
                        var newUser = realm.Add(new User {
                            UID = defaultUserId,
                            Email = "",
                            CreationDate = DateTimeOffset.Now,
                            LastLogin = DateTimeOffset.Now
                        });
                        
                        Debug.Log("Usuario por defecto creado correctamente");
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear usuario por defecto: {e.Message}");
        }
    }
    
    public User GetUserByEmail(string email)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                return realm.All<User>().FirstOrDefault(u => u.Email == email);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al buscar usuario por email: {e.Message}");
            return null;
        }
    }
    
    public void RegisterLogin(string userId)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var user = realm.Find<User>(userId);
                
                if (user != null)
                {
                    realm.Write(() => {
                        // Actualizar fecha de último login
                        user.LastLogin = DateTimeOffset.Now;
                        
                        // Crear registro de login
                        var nextLoginId = realm.All<UserLogin>().Any() ? realm.All<UserLogin>().Max(l => l.ID) + 1 : 1;
                        
                        realm.Add(new UserLogin {
                            ID = nextLoginId,
                            User = user,
                            LoginDate = DateTimeOffset.Now
                        });
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al registrar login: {e.Message}");
        }
    }
}