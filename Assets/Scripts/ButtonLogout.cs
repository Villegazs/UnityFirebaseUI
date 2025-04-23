using Firebase.Auth;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonLogout : MonoBehaviour,IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
       FirebaseAuth.DefaultInstance.SignOut();
    }

  
}
