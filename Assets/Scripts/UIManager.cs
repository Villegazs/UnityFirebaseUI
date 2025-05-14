using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private float fadeTime = 1.0f;
    [SerializeField] private AnimationList[] uiPanels; // Todos los paneles que quieres controlar
    [SerializeField] private string mainMenuPannel;

    private Dictionary<string, AnimationList> panelDictionary = new Dictionary<string, AnimationList>();

    void Awake()
    {
        // Inicializar el diccionario de paneles
        foreach (var panel in uiPanels)
        {
            panelDictionary.Add(panel.gameObject.name, panel);
            //panel.gameObject.SetActive(false); // Asegurarse que todos están desactivados al inicio
        }
        //ShowPanel(mainMenuPannel);

        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Función pública para mostrar un panel con fade in
    public void ShowPanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out AnimationList panel))
        {
            panel.PanelFadeIn(fadeTime);
        }
        else
        {
            Debug.LogWarning($"Panel '{panelName}' no encontrado en UIManager");
        }
    }

    // Función pública para ocultar un panel con fade out
    public void HidePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out AnimationList panel))
        {
            if(panel.isActiveAndEnabled)
                panel.PanelFadeOut(fadeTime);
        }
        else
        {
            Debug.LogWarning($"Panel '{panelName}' no encontrado en UIManager");
        }
    }

    // Función para alternar un panel (si está visible lo oculta, si está oculto lo muestra)
    public void TogglePanel(string panelName)
    {
        if (panelDictionary.TryGetValue(panelName, out AnimationList panel))
        {
            if (panel.gameObject.activeSelf)
            {
                HidePanel(panelName);
            }
            else
            {
                ShowPanel(panelName);
            }
        }
    }

    // Formato: "panelToHide|panelToShow"
    public void SwitchPanels(string panels)
    {
        string[] parts = panels.Split('/');
        if (parts.Length == 2)
        {
            StartCoroutine(SwitchPanelsRoutine(parts[0], parts[1]));
        }
    }


    private IEnumerator SwitchPanelsRoutine(string panelToHide, string panelToShow)
    {
        if (panelDictionary.TryGetValue(panelToHide, out AnimationList hidePanel))
        {
            // Esperar un poco antes de mostrar el nuevo panel
            yield return null;

            if (hidePanel.isActiveAndEnabled)
                StartCoroutine(hidePanel.PanelFadeOutRoutine(fadeTime));
        }



        if (panelDictionary.TryGetValue(panelToShow, out AnimationList showPanel))
        {
            showPanel.PanelFadeIn(fadeTime);
        }
    }
}
