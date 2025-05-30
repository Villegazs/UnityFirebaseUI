using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LocalUsername : MonoBehaviour
{

    [SerializeField]
    private TMP_Text _label;
    [SerializeField] private Image profileImage;
    [SerializeField] private Image statsImage;
    private int characterIndex;

    private void Reset()
    {
        _label = GetComponent<TMP_Text>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            var reference = mDatabaseRef.Child("users").Child(userId);

            reference.Child("username").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    _label.text = snapshot.Value.ToString();
                    // Do something with snapshot...
                }
            });
            reference.Child("character").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.Log("Error con personaje");
                    // Handle the error...
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    Debug.Log(snapshot.Value.ToString());
                    characterIndex = Convert.ToInt32(snapshot.Value);
                    profileImage.sprite = CharacterManager.Instance.characters[characterIndex].ClassImage;
                    statsImage.sprite = CharacterManager.Instance.characters[characterIndex].ClassStatsImageLobby;
                    // Do something with snapshot...
                }
            });

        }
    }

}

/*FirebaseDatabase.DefaultInstance
  .GetReference("users/" + userId + "/username")
  .GetValueAsync().ContinueWithOnMainThread(task => {
      if (task.IsFaulted)
      {
          // Handle the error...
      }
      else if (task.IsCompleted)
      {
          DataSnapshot snapshot = task.Result;
          _label.text = snapshot.Value.ToString();
          // Do something with snapshot...
      }
  });*/
