using Firebase.Auth;
using System;
using UnityEngine;

public class AuthStateHandler : MonoBehaviour
{
    [SerializeField]
    GameObject _panelAuth;
    [SerializeField]
    GameObject _panelScore;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Reset()
    {
        _panelAuth = GameObject.Find("PanelAuth");
        _panelScore = GameObject.Find("PanelScore");

        _panelScore.SetActive(false);
    }

    void Start()
    {
        FirebaseAuth.DefaultInstance.StateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(object sender, EventArgs e)
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            Invoke("SetAuth", 2f);
        }
        else
        {
            _panelAuth.SetActive(true);
            _panelScore.SetActive(false);
        }
    }
    public void SetAuth()
    {
        _panelAuth.SetActive(false);
        _panelScore.SetActive(true);
        Debug.Log(FirebaseAuth.DefaultInstance.CurrentUser.Email);
    }

}
