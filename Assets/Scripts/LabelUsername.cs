using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;

public class LabelUsername : MonoBehaviour
{

    [SerializeField]
    private TMP_Text _label;

    private void Reset()
    {
        _label = GetComponent<TMP_Text>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            FirebaseDatabase.DefaultInstance
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
              });

        }
    }

}
