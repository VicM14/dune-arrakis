using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class EventLogUI : MonoBehaviour
{
    [Header("Referencias")]
    public Transform contenedor;        // El Content del ScrollView
    public GameObject prefabEntrada;    // Prefab con un TextMeshPro
    public ScrollRect scrollRect;

    private readonly List<GameObject> entradasActuales = new();

    public void Actualizar(PartidaData partida)
    {
        if (partida.RegistroEventos == null) return;
        LimpiarLog();
        foreach (var evento in partida.RegistroEventos)
            CrearEntrada(evento);
        ScrollAlFinal();
    }

    private void CrearEntrada(string texto)
    {
        if (prefabEntrada == null || contenedor == null) return;
        var go = Instantiate(prefabEntrada, contenedor);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = texto;
        entradasActuales.Add(go);
    }

    public void AgregarEntrada(string texto)
    {
        CrearEntrada(texto);
        ScrollAlFinal();
    }


    private void LimpiarLog()
    {
        foreach (var go in entradasActuales)
            Destroy(go);
        entradasActuales.Clear();
    }

    private void ScrollAlFinal()
    {
        if (scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
