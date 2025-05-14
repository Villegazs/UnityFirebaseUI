using DG.Tweening.Core.Easing;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

public class AnimationList : MonoBehaviour
{
    [SerializeField] public float fadeTime = 1.0f;
    public GameObject backgroundObject;
    public Canvas myCanvas;
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;
    private float canvasWidth;
    public List<GameObject> items = new List<GameObject>();


    void Awake()
    {

    }
    public void Reset()
    {
        myCanvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        Debug.Log("Ancho del Canvas: " + canvasWidth);
    }
    public void Start()
    {

        // Obtener el componente RectTransform del Canvas
        RectTransform canvasRect = myCanvas.GetComponent<RectTransform>();


        // Obtener el ancho del Canvas
        canvasWidth = canvasRect.rect.width;

        Debug.Log("Ancho del Canvas: " + canvasWidth);
    }


    public void PanelFadeIn(float fadeTime)
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        //rectTransform.transform.localPosition = new Vector3(-canvasWidth, 0f, 0f);
        //rectTransform.DOAnchorPos(new Vector2(0f, 0f), fadeTime, false).SetEase(Ease.OutElastic).SetUpdate(true);
        canvasGroup.DOFade(1, fadeTime).SetUpdate(true);

        StartCoroutine("ItemsAnimation");
    }

    public void PanelFadeOut(float fadeTime)
    {
        StartCoroutine(PanelFadeOutRoutine(fadeTime));
    }
    public IEnumerator PanelFadeOutRoutine(float fadeTime)
    {
        yield return StartCoroutine(ItemsDissapear());
        canvasGroup.alpha = 1f;
        //rectTransform.transform.localPosition = new Vector3(0f, 0f, 0f);
        //rectTransform.DOAnchorPos(new Vector2(canvasWidth, 0f), fadeTime, false).SetEase(Ease.InOutQuint);
        canvasGroup.DOFade(0, fadeTime);
        yield return new WaitForSeconds(fadeTime);
        gameObject.SetActive(false);

    }

    public void PanelZoomIn()
    {
        RectTransform rectTransformBG = backgroundObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroupBG = backgroundObject.GetComponent<CanvasGroup>();
        canvasGroupBG.DOFade(1, fadeTime);


        Debug.Log("Abrir Menu");
        // canvasGroup.alpha = 0f;
        // rectTransform.transform.localPosition = new Vector3(-canvasWidth, 0f, 0f);

        rectTransformBG.DOScale(1.75f, fadeTime);
        // rectTransform.DOAnchorPos(new Vector2(0f, 0f), fadeTime, false).SetEase(Ease.OutElastic).SetUpdate(true);
        //.DOFade(1, fadeTime).SetUpdate(true);

        StartCoroutine("ItemsAnimation");
    }
    public void PanelZoomOut()
    {
        RectTransform rectTransformBG = backgroundObject.GetComponent<RectTransform>();
        Debug.Log("Cerrar Menu");

        StartCoroutine("ItemsDissapear");
        rectTransformBG.DOScale(1, fadeTime);

    }

    IEnumerator ItemsAnimation()
    {
        foreach (var item in items)
        {
            item.transform.localScale = Vector3.zero;
        }
        foreach (var item in items)
        {
            item.transform.DOScale(1f, fadeTime).SetEase(Ease.OutBounce);
            yield return new WaitForSeconds(0.25f);
        }

    }
    IEnumerator ItemsDissapear()
    {
        foreach (var item in items)
        {
            item.transform.DOScale(0f, fadeTime / 2);
            yield return new WaitForSeconds(fadeTime / 8);
        }
    }
}
