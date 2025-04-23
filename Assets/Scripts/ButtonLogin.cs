using Firebase.Auth;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonLogin : MonoBehaviour
{

    [SerializeField]
    private Button _loginButton;
    [SerializeField]
    private TMP_InputField _emailInputField;
    [SerializeField]
    private TMP_InputField _passwordInputField;


    private void Reset()
    {
        _loginButton = GetComponent<Button>();
        _emailInputField = GameObject.Find("InputFieldEmail").GetComponent<TMP_InputField>();
        _passwordInputField = GameObject.Find("InputFieldPassword").GetComponent<TMP_InputField>(); ;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        _loginButton.onClick.AddListener(RegisterUser);
    }


    void RegisterUser()
    {
        var auth = FirebaseAuth.DefaultInstance;
        string email = _emailInputField.text;
        string password = _passwordInputField.text; 

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
        });
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
