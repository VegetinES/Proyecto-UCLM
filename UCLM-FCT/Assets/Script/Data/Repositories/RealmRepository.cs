using System;
using System.Collections.Generic;
using System.Linq;
using Realms;
using UnityEngine;

public abstract class RealmRepository<T> : IRepository<T> where T : RealmObject
{
    protected RealmConfiguration realmConfig;
    
    protected RealmRepository()
    {
        InitializeRealmConfig();
    }
    
    private void InitializeRealmConfig()
    {
        try
        {
            // Creamos una configuración básica con versión de esquema
            realmConfig = new RealmConfiguration { SchemaVersion = 1 };
            
            // La ruta se configura automáticamente por Realm en Application.persistentDataPath
            Debug.Log($"RealmRepository: Configuración de Realm inicializada con SchemaVersion = 1");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al inicializar la configuración de Realm: {e.Message}");
            
            // Fallback a configuración básica
            realmConfig = new RealmConfiguration { SchemaVersion = 1 };
        }
    }
    
    public virtual T GetById(string id)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var result = realm.Find<T>(id);
                if (result == null)
                {
                    Debug.LogWarning($"RealmRepository: No se encontró {typeof(T).Name} con ID: {id}");
                }
                return result;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener {typeof(T).Name} por ID ({id}): {e.Message}");
            return null;
        }
    }
    
    public virtual List<T> GetAll()
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var results = realm.All<T>().ToList();
                Debug.Log($"RealmRepository: Obtenidos {results.Count} registros de {typeof(T).Name}");
                return results;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener todos los {typeof(T).Name}: {e.Message}");
            return new List<T>();
        }
    }
    
    public virtual void Add(T entity)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                realm.Write(() => {
                    realm.Add(entity);
                });
                Debug.Log($"RealmRepository: {typeof(T).Name} añadido correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al añadir {typeof(T).Name}: {e.Message}");
        }
    }
    
    public virtual void Update(T entity, Action<T> updateAction)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                realm.Write(() => {
                    updateAction(entity);
                });
                Debug.Log($"RealmRepository: {typeof(T).Name} actualizado correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al actualizar {typeof(T).Name}: {e.Message}");
        }
    }
    
    public virtual void Delete(T entity)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                realm.Write(() => {
                    realm.Remove(entity);
                });
                Debug.Log($"RealmRepository: {typeof(T).Name} eliminado correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al eliminar {typeof(T).Name}: {e.Message}");
        }
    }
    
    public virtual void DeleteById(string id)
    {
        try
        {
            using (var realm = Realm.GetInstance(realmConfig))
            {
                var entity = realm.Find<T>(id);
                
                if (entity != null)
                {
                    realm.Write(() => {
                        realm.Remove(entity);
                    });
                    Debug.Log($"RealmRepository: {typeof(T).Name} con ID {id} eliminado correctamente");
                }
                else
                {
                    Debug.LogWarning($"RealmRepository: No se encontró {typeof(T).Name} con ID {id} para eliminar");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al eliminar {typeof(T).Name} por ID ({id}): {e.Message}");
        }
    }
}