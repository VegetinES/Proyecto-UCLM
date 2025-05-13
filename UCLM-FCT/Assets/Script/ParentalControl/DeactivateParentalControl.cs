using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DeactivateParentalControl : MonoBehaviour, IPointerClickHandler
{
    [Header("Campos de entrada")]
    public TMP_InputField pinInput;
    
    [Header("Objetos a manipular")]
    public GameObject objectToActivate;      // Objeto que se activará al desactivar el control parental
    public GameObject objectToDeactivate1;   // Primer objeto a desactivar
    public GameObject objectToDeactivate2;   // Segundo objeto a desactivar
    
    [Header("Mensajes de error")]
    public GameObject errorMessage;          // Objeto que se activa cuando el PIN es incorrecto
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (pinInput == null)
        {
            Debug.LogError("El campo de PIN no está asignado");
            return;
        }
        
        // Ocultar mensaje de error si está visible
        if (errorMessage != null)
            errorMessage.SetActive(false);
        
        string pin = pinInput.text.Trim();
        
        // Verificar PIN
        if (VerifyPin(pin))
        {
            // PIN correcto, desactivar el control parental
            DeactivateParental();
            
            // Gestionar la activación/desactivación de GameObjects
            if (objectToDeactivate1 != null)
                objectToDeactivate1.SetActive(false);
                
            if (objectToDeactivate2 != null)
                objectToDeactivate2.SetActive(false);
                
            if (objectToActivate != null)
                objectToActivate.SetActive(true);
                
            // Limpiar el campo de PIN
            pinInput.text = "";
        }
        else
        {
            // PIN incorrecto, mostrar mensaje de error
            if (errorMessage != null)
                errorMessage.SetActive(true);
                
            Debug.Log("PIN incorrecto. No se pudo desactivar el control parental.");
        }
    }
    
    private bool VerifyPin(string inputPin)
    {
        try
        {
            // Obtener ID del usuario actual
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            string storedHash = "";
            bool pinFound = false;
        
            // Obtener el hash del PIN desde SQLite
            var parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
            if (parentalControl != null && !string.IsNullOrEmpty(parentalControl.Pin))
            {
                storedHash = parentalControl.Pin;
                pinFound = true;
                Debug.Log("PIN encontrado para el usuario actual");
            }
        
            // Si no encontramos un PIN para el usuario actual, intentamos con el usuario por defecto
            if (!pinFound && userId != AuthManager.DEFAULT_USER_ID)
            {
                var defaultParentalControl = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
                if (defaultParentalControl != null && !string.IsNullOrEmpty(defaultParentalControl.Pin))
                {
                    storedHash = defaultParentalControl.Pin;
                    pinFound = true;
                    Debug.Log("PIN encontrado para el usuario por defecto");
                }
            }
        
            if (!pinFound)
            {
                Debug.Log("No se encontró ningún PIN configurado.");
                return false;
            }
        
            // Calcular el hash del PIN introducido
            string inputHash = GetSHA256Hash(inputPin);
        
            // Comparar los hashes
            return storedHash.Equals(inputHash);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al verificar PIN: " + e.Message);
            return false;
        }
    }
    
    private void DeactivateParental()
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
        
            var parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
        
            if (parentalControl != null)
            {
                SqliteDatabase.Instance.SaveParentalControl(
                    userId,
                    false,
                    parentalControl.Pin,
                    parentalControl.SoundConf,
                    parentalControl.AccessibilityConf,
                    parentalControl.StatisticsConf,
                    parentalControl.AboutConf,
                    parentalControl.ProfileConf
                );
            
                Debug.Log("Control parental desactivado correctamente");
            }
            else
            {
                Debug.LogError("No se pudo obtener la configuración del control parental");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al desactivar el control parental: " + e.Message);
        }
    }
    
    private string GetSHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convertir el PIN a bytes
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            
            // Calcular el hash
            byte[] hashBytes = sha256.ComputeHash(bytes);
            
            // Convertir el hash a string hexadecimal
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }
}