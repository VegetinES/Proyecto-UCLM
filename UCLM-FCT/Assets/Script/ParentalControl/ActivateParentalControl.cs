using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ActivateParentalControl : MonoBehaviour, IPointerClickHandler
{
    public GameObject activated;
    public GameObject activate;

    public TMP_InputField setPin;
    public TMP_InputField repeatPin;
    
    // Mensaje de error para mostrar al usuario
    public GameObject errorMessage;
    
    private string setPinText;
    private string repeatPinText;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        setPinText = setPin.text.Replace(" ", "");
        repeatPinText = repeatPin.text.Replace(" ", "");

        if (setPinText.Equals(repeatPinText) && (Regex.IsMatch(setPinText, @"^\d{4}$") && Regex.IsMatch(repeatPinText, @"^\d{4}$")))
        {
            // Guardar hash del PIN en el control parental
            SavePinHash(setPinText);
            ActivatePage();
            
            // Ocultar mensaje de error si está visible
            if (errorMessage != null)
                errorMessage.SetActive(false);
        }
        else
        {
            // Mostrar mensaje de error
            if (errorMessage != null)
                errorMessage.SetActive(true);
        }
    }
    
    private void SavePinHash(string pin)
    {
        string pinHash = GetSHA256Hash(pin);
    
        string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;

        var parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);

        SqliteDatabase.Instance.SaveParentalControl(
            userId,
            true,
            pinHash
        );
    
        Debug.Log("Control parental creado con PIN hasheado");
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
    
    private void ActivatePage()
    {
        if (activate != null)
        {
            activate.SetActive(false);
        }

        if (activated != null)
        {
            activated.SetActive(true);
        }
    }
}