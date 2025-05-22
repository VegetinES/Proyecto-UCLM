using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.UI;

public class ProfileChecker : MonoBehaviour, IPointerClickHandler
{
    [Header("Navegación normal")]
    public GameObject nextPage;
    public GameObject actualPage;
    
    [Header("Navegación para perfiles")]
    public GameObject profilePage;
    
    [Header("Navegación entre escenas")]
    public string targetSceneName;
    
    [Header("Control Parental")]
    public GameObject parentalPinObject;
    public TMP_InputField pinInput;
    public Button continueButton;
    public Button cancelButton;
    public GameObject errorObject;
    
    private void Start()
    {
        // Configurar botones de control parental
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClick);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);
        
        // Ocultar objetos al inicio
        if (parentalPinObject != null)
            parentalPinObject.SetActive(false);
            
        if (errorObject != null)
            errorObject.SetActive(false);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Verificar control parental primero
        if (HasParentalControl())
        {
            ShowParentalPinDialog();
        }
        else
        {
            NavigateNormally();
        }
    }
    
    private bool HasParentalControl()
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            int profileId = GetCurrentProfileId();
            
            LocalParentalControl parentalControl = null;
            
            // Verificar control parental del perfil primero
            if (profileId > 0)
            {
                parentalControl = SqliteDatabase.Instance.GetParentalControl(userId, profileId);
            }
            
            // Si no hay para el perfil, verificar del usuario
            if (parentalControl == null)
            {
                parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
            }
            
            // Verificar usuario por defecto como última opción
            if (parentalControl == null && userId != AuthManager.DEFAULT_USER_ID)
            {
                parentalControl = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
            }
            
            return parentalControl != null && parentalControl.Activated && !string.IsNullOrEmpty(parentalControl.Pin);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ProfileChecker: Error verificando control parental: {e.Message}");
            return false;
        }
    }
    
    private void ShowParentalPinDialog()
    {
        if (parentalPinObject != null)
            parentalPinObject.SetActive(true);
            
        if (errorObject != null)
            errorObject.SetActive(false);
            
        if (pinInput != null)
            pinInput.text = "";
    }
    
    private void OnContinueClick()
    {
        if (pinInput == null) return;
        
        string pin = pinInput.text.Trim();
        
        if (VerifyPin(pin))
        {
            // PIN correcto, navegar
            if (parentalPinObject != null)
                parentalPinObject.SetActive(false);
                
            if (errorObject != null)
                errorObject.SetActive(false);
                
            NavigateNormally();
        }
        else
        {
            // PIN incorrecto, mostrar error
            if (errorObject != null)
                errorObject.SetActive(true);
        }
    }
    
    private void OnCancelClick()
    {
        if (parentalPinObject != null)
            parentalPinObject.SetActive(false);
            
        if (errorObject != null)
            errorObject.SetActive(false);
    }
    
    private bool VerifyPin(string inputPin)
    {
        try
        {
            string userId = DataManager.Instance?.GetCurrentUserId() ?? AuthManager.DEFAULT_USER_ID;
            int profileId = GetCurrentProfileId();
            string storedHash = "";
            bool pinFound = false;
            
            // Buscar PIN del perfil primero
            if (profileId > 0)
            {
                var profileParental = SqliteDatabase.Instance.GetParentalControl(userId, profileId);
                if (profileParental != null && !string.IsNullOrEmpty(profileParental.Pin))
                {
                    storedHash = profileParental.Pin;
                    pinFound = true;
                }
            }
            
            // Si no hay PIN del perfil, buscar del usuario
            if (!pinFound)
            {
                var parentalControl = SqliteDatabase.Instance.GetParentalControl(userId);
                if (parentalControl != null && !string.IsNullOrEmpty(parentalControl.Pin))
                {
                    storedHash = parentalControl.Pin;
                    pinFound = true;
                }
            }
            
            // Verificar usuario por defecto como última opción
            if (!pinFound && userId != AuthManager.DEFAULT_USER_ID)
            {
                var defaultParental = SqliteDatabase.Instance.GetParentalControl(AuthManager.DEFAULT_USER_ID);
                if (defaultParental != null && !string.IsNullOrEmpty(defaultParental.Pin))
                {
                    storedHash = defaultParental.Pin;
                    pinFound = true;
                }
            }
            
            if (!pinFound) return true;
            
            string inputHash = GetSHA256Hash(inputPin);
            return storedHash.Equals(inputHash);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ProfileChecker: Error verificando PIN: {e.Message}");
            return false;
        }
    }
    
    private void NavigateNormally()
    {
        // Comprobar si es un perfil
        bool isProfile = ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile();
        
        if (isProfile && profilePage != null)
        {
            profilePage.SetActive(true);
        }
        else if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            if (actualPage != null)
                actualPage.SetActive(false);
                
            if (nextPage != null)
                nextPage.SetActive(true);
        }
    }
    
    private int GetCurrentProfileId()
    {
        return ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile() 
            ? ProfileManager.Instance.GetCurrentProfileId() 
            : 0;
    }
    
    private string GetSHA256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }
    
    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClick);
            
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(OnCancelClick);
    }
}