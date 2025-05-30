using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PersistentNotifier : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private Transform notificationsContainer;
    [SerializeField] private int maxNotifications = 5;

    private Queue<GameObject> activeNotifications = new Queue<GameObject>();
    public void OnEnable()
    {
        ClearAllNotifications();
    }

    // Método para mostrar notificación persistente
    public void ShowPersistentMessage(string message)
    {
        if (notificationPrefab == null || notificationsContainer == null)
        {
            Debug.LogWarning("Faltan referencias en PersistentNotifier");
            return;
        }

        // Crear nueva notificación
        GameObject newNotification = Instantiate(notificationPrefab, notificationsContainer);
        TMP_Text textComponent = newNotification.GetComponentInChildren<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = message;
        }

        // Manejar límite de notificaciones
        activeNotifications.Enqueue(newNotification);

        if (activeNotifications.Count > maxNotifications)
        {
            GameObject oldestNotification = activeNotifications.Dequeue();
            Destroy(oldestNotification);
        }
    }

    // Método para limpiar todas las notificaciones manualmente
    public void ClearAllNotifications()
    {
        foreach (GameObject notification in activeNotifications)
        {
            Destroy(notification);
        }
        activeNotifications.Clear();
    }
}
