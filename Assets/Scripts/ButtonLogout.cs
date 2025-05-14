using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonLogout : MonoBehaviour,IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        mDatabaseRef.Child("users-online").Child(userId).SetValueAsync(null);

        FirebaseAuth.DefaultInstance.SignOut();
       

    }


    private void OnApplicationQuit()
    {
        var mDatabaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        mDatabaseRef.Child("users-online").Child(userId).SetValueAsync(null);
    }
}
