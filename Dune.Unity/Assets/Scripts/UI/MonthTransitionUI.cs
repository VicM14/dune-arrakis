using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MonthTransitionUI : MonoBehaviour
{
    [Header("Referencias")]
    public CanvasGroup overlayGroup;
    public TextMeshProUGUI textoMes;
    public TextMeshProUGUI textoSubtitulo;
    public RectTransform contenedorParticulas;
    public GameObject prefabParticula;

    [Header("Configuracion")]
    public int cantidadParticulas = 90;
    public float duracionMinima = 2.5f;

    private bool apiCompleta = false;
    private readonly List<GameObject> particulas = new();

    private readonly Color[] coloresArena =
    {
        new Color(0.949f, 0.831f, 0.596f),  // #F2D398
        new Color(0.769f, 0.580f, 0.353f),  // #C4945A
        new Color(1f,     0.549f, 0.259f),  // #FF8C42
        new Color(0.545f, 0.369f, 0.235f),  // #8B5E3C
    };

    void Start() => gameObject.SetActive(false);

    // ??? API pública ??????????????????????????????????????????????????????

    public void NotificarApiCompleta() => apiCompleta = true;

    public IEnumerator Ejecutar(int mesNuevo)
    {
        apiCompleta = false;
        gameObject.SetActive(true);

        // Fase 1: fade-in overlay (0.3s)
        yield return FadeOverlay(0f, 1f, 0.3f);

        // Fase 2: crear partículas y mostrar texto
        CrearParticulas();
        if (textoMes != null)
        {
            textoMes.text = $"MES {mesNuevo}";
            textoMes.alpha = 0f;
        }
        if (textoSubtitulo != null)
        {
            textoSubtitulo.text = "— Simulando ronda —";
            textoSubtitulo.alpha = 0f;
        }

        yield return FadeTexto(0f, 1f, 0.25f);

        // Fase 3: esperar duración mínima Y que la API responda
        float t = 0;
        while (t < duracionMinima || !apiCompleta)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Fase 4: fade-out (0.35s)
        if (textoSubtitulo != null) textoSubtitulo.text = "— Actualizando estado —";
        yield return new WaitForSeconds(0.2f);
        yield return FadeOverlay(1f, 0f, 0.35f);

        LimpiarParticulas();
        gameObject.SetActive(false);
    }

    // ??? Fades ???????????????????????????????????????????????????????????

    private IEnumerator FadeOverlay(float desde, float hasta, float duracion)
    {
        float t = 0;
        while (t < duracion)
        {
            t += Time.deltaTime;
            if (overlayGroup != null)
                overlayGroup.alpha = Mathf.Lerp(desde, hasta, t / duracion);
            yield return null;
        }
        if (overlayGroup != null) overlayGroup.alpha = hasta;
    }

    private IEnumerator FadeTexto(float desde, float hasta, float duracion)
    {
        float t = 0;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(desde, hasta, t / duracion);
            if (textoMes != null) textoMes.alpha = v;
            if (textoSubtitulo != null) textoSubtitulo.alpha = v;
            yield return null;
        }
    }

    // ??? Partículas ??????????????????????????????????????????????????????

    private void CrearParticulas()
    {
        LimpiarParticulas();
        if (prefabParticula == null || contenedorParticulas == null) return;

        float h = contenedorParticulas.rect.height;
        float w = contenedorParticulas.rect.width;

        for (int i = 0; i < cantidadParticulas; i++)
        {
            var go = Instantiate(prefabParticula, contenedorParticulas);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = coloresArena[i % coloresArena.Length];

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(
                    Random.Range(-100f, w),
                    Random.Range(0f, h)
                );
                float sz = Random.Range(4f, 10f);
                rt.sizeDelta = new Vector2(sz * Random.Range(1f, 3f), sz);
            }

            particulas.Add(go);
            StartCoroutine(AnimarParticula(go));
        }
    }

    private IEnumerator AnimarParticula(GameObject go)
    {
        if (go == null) yield break;
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float vel = Random.Range(300f, 800f);
        float ondaFq = Random.Range(1.5f, 4f);
        float ondaAm = Random.Range(5f, 25f);
        float offset = Random.Range(0f, Mathf.PI * 2f);
        float t = 0;
        float w = contenedorParticulas != null ? contenedorParticulas.rect.width : 800f;

        while (go != null && gameObject.activeSelf)
        {
            t += Time.deltaTime;
            if (rt != null)
            {
                var pos = rt.anchoredPosition;
                pos.x += vel * Time.deltaTime;
                pos.y += Mathf.Sin(t * ondaFq + offset) * ondaAm * Time.deltaTime;

                if (pos.x > w + 50f)
                    pos.x = -Random.Range(10f, 80f);

                rt.anchoredPosition = pos;
            }
            yield return null;
        }
    }

    private void LimpiarParticulas()
    {
        foreach (var p in particulas)
            if (p != null) Destroy(p);
        particulas.Clear();
    }
}