using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Script para el prefab de solicitud
public class FriendRequestDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text requestText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;

    public string SenderId { get; private set; }
    public string SenderName { get; private set; }

    private System.Action<string, int> onDecision;

    public void Initialize(string senderId, string senderName, System.Action<string, int> decisionCallback)
    {
        SenderId = senderId;
        SenderName = senderName;
        onDecision = decisionCallback;

        requestText.text = $"{senderName} quiere ser tu amigo";

        acceptButton.onClick.AddListener(() => onDecision?.Invoke(SenderId, 1));
        rejectButton.onClick.AddListener(() => onDecision?.Invoke(SenderId, 2));
    }
}