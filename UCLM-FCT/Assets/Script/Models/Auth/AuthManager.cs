using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Supabase;
using Postgrest.Responses;
using Supabase.Gotrue;
using TMPro;
using Client = Supabase.Client;


public class AuthManager : MonoBehaviour
{
    public const string SUPABASE_URL = "https://ujbqtvsbrwcgnxveufto.supabase.co";
    
    public const string SUPABASE_PUBLIC_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVqYnF0dnNicndjZ254dmV1ZnRvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDI0NjkwOTksImV4cCI6MjA1ODA0NTA5OX0.HmK9xGLnFLHJMZaOPVAHPYA1LKH2-LE9Kz8dwsb17Es";
    
    public TMP_InputField email;
    public TMP_InputField password;
    public TMP_Text text;

    private static Client _supabase;
    private string id;
    private string _nonce;

    private async void Start()
    {
        if (_supabase == null)
        {
            _supabase = new Client(SUPABASE_URL, SUPABASE_PUBLIC_KEY);
            await _supabase.InitializeAsync();
        }
    }

    public async void LogInUser()
    {
        Debug.Log("Starting sign in");
        Task<Session> signIn = _supabase.Auth.SignInWithPassword(email.text, password.text);

        try
        {
            await signIn;
        }
        catch (Exception e)
        {
            Debug.Log(e);
            text.text = $"Error: {e.Message}";
            return;
        }

        if (!signIn.IsCompletedSuccessfully)
        {
            text.text = JsonUtility.ToJson(signIn.Exception);
            return;
        }
        
        Session session = signIn.Result;

        if (session == null)
        {
            text.text = "Nope";
        }
        else
        {
            // Obtener el UID del usuario de la sesión
            string userId = session.User.Id;
            
            // Mostrar mensaje de éxito junto con el UID
            text.text = $"Logged in successfully\nUID: {userId}";
            
            // También lo mostramos en la consola para debugging
            Debug.Log($"User logged in with ID: {userId}");
        }
    }
}
