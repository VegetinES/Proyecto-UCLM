using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class EnvironmentLoader
{
    private static Dictionary<string, string> _envVars = new Dictionary<string, string>();
    private static bool _initialized = false;

    public static void Initialize()
    {
        if (_initialized) return;

        LoadEnvFile();
        _initialized = true;
    }

    public static string GetVariable(string key, string defaultValue = "")
    {
        if (!_initialized) Initialize();

        if (_envVars.TryGetValue(key, out string value))
        {
            return value;
        }

        string envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        return defaultValue;
    }

    private static void LoadEnvFile()
    {
        string envPath = GetEnvFilePath();

        try
        {
            if (File.Exists(envPath))
            {
                string[] lines = File.ReadAllLines(envPath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();
                        string value = trimmedLine.Substring(separatorIndex + 1).Trim();

                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        _envVars[key] = value;
                        Debug.Log($"EnvironmentLoader: Cargada variable {key}");
                    }
                }
                
                Debug.Log($"EnvironmentLoader: Cargadas {_envVars.Count} variables de entorno desde {envPath}");
            }
            else
            {
                Debug.LogWarning($"EnvironmentLoader: No se encontró el archivo .env en {envPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EnvironmentLoader: Error al cargar .env: {e.Message}");
        }
    }

    private static string GetEnvFilePath()
    {
        // Buscar en StreamingAssets (para builds y editor)
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, ".env");
        if (File.Exists(streamingAssetsPath))
        {
            return streamingAssetsPath;
        }

        // Buscar en la raíz del proyecto (solo para editor)
        if (Application.isEditor)
        {
            string projectRootPath = Path.Combine(Application.dataPath, "..", ".env");
            if (File.Exists(projectRootPath))
            {
                return projectRootPath;
            }
        }

        // Buscar en persistentDataPath (para casos especiales)
        string persistentPath = Path.Combine(Application.persistentDataPath, ".env");
        if (File.Exists(persistentPath))
        {
            return persistentPath;
        }

        // Devolver la ruta de StreamingAssets como predeterminada
        return streamingAssetsPath;
    }
}