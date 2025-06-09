using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionUIManager : MonoBehaviour
{
    public GameObject TutorLoggedIn;
    public GameObject ProfileLoggedIn;
    public GameObject UserLoggedIn;
    public GameObject NotLoggedIn;
    
    [SerializeField] private float maxWaitTime = 10.0f;
    [SerializeField] private float monitorInterval = 0.1f;
    
    private bool isMonitoring = false;
    private bool lastTutorState = false;
    
    private void Start()
    {
        SetAllInactive();
        if (NotLoggedIn != null) NotLoggedIn.SetActive(true);
        
        StartCoroutine(CheckAuthState());
    }
    
    private IEnumerator CheckAuthState()
    {
        float elapsed = 0f;
        
        while (elapsed < maxWaitTime)
        {
            if (AuthManager.Instance != null && AuthManager.Instance.IsInitialized())
            {
                AuthManager.Instance.ValidateCurrentState();
                UpdateUI();
                yield break;
            }
            
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogError("AuthManager no disponible después del tiempo de espera");
    }
    
    public void UpdateUI()
    {
        bool isLoggedIn = AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn;
    
        if (!isLoggedIn)
        {
            SetAllInactive();
            if (NotLoggedIn != null) NotLoggedIn.SetActive(true);
            return;
        }
        
        string userId = AuthManager.Instance.UserID;
        var user = SqliteDatabase.Instance.GetUser(userId);
        
        if (user != null && user.IsTutor)
        {
            SetAllInactive();
            if (TutorLoggedIn != null) 
            {
                TutorLoggedIn.SetActive(true);
                StartMonitoring();
            }
        }
        else if (ProfileManager.Instance != null && ProfileManager.Instance.IsUsingProfile())
        {
            SetAllInactive();
            if (ProfileLoggedIn != null) ProfileLoggedIn.SetActive(true);
        }
        else
        {
            SetAllInactive();
            if (UserLoggedIn != null) UserLoggedIn.SetActive(true);
        }
    }
    
    private void StartMonitoring()
    {
        if (!isMonitoring && TutorLoggedIn != null)
        {
            isMonitoring = true;
            lastTutorState = TutorLoggedIn.activeSelf;
            StartCoroutine(MonitorTutorLoggedIn());
        }
    }
    
    private IEnumerator MonitorTutorLoggedIn()
    {
        while (isMonitoring && TutorLoggedIn != null)
        {
            bool currentState = TutorLoggedIn.activeSelf;
            
            if (currentState != lastTutorState)
            {
                if (!currentState)
                {
                    TutorLoggedIn.SetActive(true);
                }
                lastTutorState = currentState;
            }
            
            yield return new WaitForSeconds(monitorInterval);
        }
    }
    
    private void StopMonitoring()
    {
        isMonitoring = false;
    }
    
    private void SetAllInactive()
    {
        if (TutorLoggedIn != null) TutorLoggedIn.SetActive(false);
        if (ProfileLoggedIn != null) ProfileLoggedIn.SetActive(false);
        if (UserLoggedIn != null) UserLoggedIn.SetActive(false);
        if (NotLoggedIn != null) NotLoggedIn.SetActive(false);
    }
    
    public void GoToLogin()
    {
        SceneManager.LoadScene("Login");
    }
    
    private void OnDisable()
    {
        StopMonitoring();
    }
    
    private void OnDestroy()
    {
        StopMonitoring();
    }
}