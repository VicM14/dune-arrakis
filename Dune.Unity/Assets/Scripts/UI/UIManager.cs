using UnityEngine;

/// <summary>
/// Coordina todos los paneles de la GameView.
/// Se actualiza cada vez que GameManager dispara OnEstadoActualizado.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles")]
    public TopBarUI topBar;
    public EnclavePanelUI enclavePanel;
    public DetailPanelUI detailPanel;
    public EventLogUI eventLog;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.OnEstadoActualizado += RefrescarUI;
        GameManager.OnError += MostrarError;
    }

    void OnDisable()
    {
        GameManager.OnEstadoActualizado -= RefrescarUI;
        GameManager.OnError -= MostrarError;
    }

    void Start()
    {
        // Si ya hay partida en memoria (venimos de ScenarioSelect), refrescar
        if (GameManager.Instance?.PartidaActual != null)
            RefrescarUI(GameManager.Instance.PartidaActual);
    }

    public void RefrescarUI(PartidaData partida)
    {
        topBar?.Actualizar(partida);
        enclavePanel?.Actualizar(partida);
        eventLog?.Actualizar(partida);
        detailPanel?.LimpiarDetalle();
    }

    private void MostrarError(string msg)
    {
        eventLog?.AgregarEntrada($"[ERROR] {msg}");
    }
}