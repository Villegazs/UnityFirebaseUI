using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using System;
using static UnityEngine.Rendering.GPUSort;

public class UsersOnline : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnEnable()
    {
        var reference = FirebaseDatabase.DefaultInstance.GetReference("users-online");

        reference.ChildAdded += HandleChildAdded;
        reference.ChildRemoved += HandleChildRemoved;
    }

    private void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        Debug.Log($"User disconnected: {snapshot.Key}");

        try
        {
            // Handle the removed user data here
            if (snapshot.HasChildren)
            {
                var userData = snapshot.Value as Dictionary<string, object>;
                if (userData != null)
                {
                    // Process user data
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing removed user: {e.Message}");
        }
    }

    private void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }


        DataSnapshot snapshot = args.Snapshot;
        

        Debug.Log($"User added: {snapshot.Key}");

        try
        {

            // Handle the new user data here
            if (snapshot.HasChildren)
            {
                var userData = snapshot.Value as Dictionary<string, object>;
                if (userData != null)
                {
                    // Process user data
                    if (userData.TryGetValue("username", out object username))
                    {
                        Debug.Log($"New user online: {username}");
                    }
                }
                else
                {
                    // Handle case where value is not a dictionary
                    Debug.Log($"User data is: {snapshot.Value}");
                }
            }
            else
            {
                // Handle case where snapshot has no children
                Debug.Log($"User data is simple value: {snapshot.Value}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing new user: {e.Message}");
        }
    }

    private void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {

        if (args.DatabaseError != null)
        {
            Debug.Log(args.DatabaseError.Message);
        }


         DataSnapshot snapshot = args.Snapshot;
         
         foreach (var userDoc in (Dictionary<string, object>)snapshot.Value)
         {
             var userObject = (Dictionary<string, object>)userDoc.Value;
             Debug.Log(userDoc.Key);

             Debug.Log(userObject["username"] + " | " + userDoc.Key);

         }
    }

    private void OnDisable()
    {
        var reference = FirebaseDatabase.DefaultInstance.GetReference("users-online");
        reference.ChildAdded -= HandleChildAdded;
        reference.ChildRemoved -= HandleChildRemoved;
    }
}
