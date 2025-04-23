using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSignup : MonoBehaviour
{
    [SerializeField]
    private Button _registrationButton;

    private Coroutine _registrationCoroutine;

    private void Reset()
    {
        _registrationButton = GetComponent<Button>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _registrationButton.onClick.AddListener(HandleRegistrationButtonClick);
    }

    private void HandleRegistrationButtonClick()
    {
        string email = GameObject.Find("InputFieldEmail").GetComponent<TMP_InputField>().text;
        string password = GameObject.Find("InputFieldPassword").GetComponent<TMP_InputField>().text;

        _registrationCoroutine = StartCoroutine(RegisterUser(email, password));

    }

    IEnumerator RegisterUser(string email, string password)
    {
        var auth = FirebaseAuth.DefaultInstance;
        string username = GameObject.Find("InputFieldUsername").GetComponent<TMP_InputField>().text;

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsCanceled)
        {
            Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");

        }
        else if (registerTask.IsFaulted)
        {
            Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + registerTask.Exception);
        }
        else
        {

            // Firebase user has been created.
            AuthResult result = registerTask.Result;

            // insertar despues de registro exitoso
            FirebaseDatabase.DefaultInstance.RootReference
                 .Child("users").Child(result.User.UserId).Child("username").SetValueAsync(username);

            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);
        }
    }


    // Update is called once per frame
    void Update()
    {

    }
}
