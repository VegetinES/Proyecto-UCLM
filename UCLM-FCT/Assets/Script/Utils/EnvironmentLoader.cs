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

        // También intentamos obtener desde las variables de entorno del sistema
        string envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        return defaultValue;
    }

    private static void LoadEnvFile()
    {
        // Ruta al archivo .env en la raíz del proyecto
        string envPath = Path.Combine(Application.dataPath, "..", ".env");

        try
        {
            if (File.Exists(envPath))
            {
                string[] lines = File.ReadAllLines(envPath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // Ignorar líneas vacías o comentarios
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    // Separar clave y valor
                    int separatorIndex = trimmedLine.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = trimmedLine.Substring(0, separatorIndex).Trim();
                        string value = trimmedLine.Substring(separatorIndex + 1).Trim();

                        // Remover comillas si existen
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        _envVars[key] = value;
                        Debug.Log($"EnvironmentLoader: Cargada variable {key}");
                    }
                }
                
                Debug.Log($"EnvironmentLoader: Cargadas {_envVars.Count} variables de entorno");
            }
            else
            {
                Debug.LogWarning("EnvironmentLoader: No se encontró el archivo .env");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EnvironmentLoader: Error al cargar .env: {e.Message}");
        }
    }
}