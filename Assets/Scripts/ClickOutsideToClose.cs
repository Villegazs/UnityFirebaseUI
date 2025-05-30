using UnityEngine;
using UnityEngine.EventSystems;

public class ClickOutsideToClose : MonoBehaviour
{
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Verificar si se hizo clic con el botón izquierdo del mouse
        if (Input.GetMouseButtonDown(0))
        {
            // Verificar si el clic fue fuera del RectTransform
            if (!IsClickInsideRectTransform(rectTransform, Input.mousePosition))
            {
                // Desactivar el objeto
                gameObject.SetActive(false);
            }
        }
    }

    private bool IsClickInsideRectTransform(RectTransform rectTransform, Vector2 screenPoint)
    {
        // Convertir la posición del mouse a coordenadas locales del RectTransform
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPoint,
            null, // Camera puede ser null para UI
            out localPoint);

        // Verificar si el punto está dentro del rectángulo
        return rectTransform.rect.Contains(localPoint);
    }
}