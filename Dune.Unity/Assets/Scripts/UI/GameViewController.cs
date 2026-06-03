using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameViewController : MonoBehaviour
{
    [Header("Botones principales")]
    public Button btnSimularMes;
    public Button btnGuardar;
    public Button btnConstruir;
    public Button btnTrasladar;
    public Button btnComprar;
    public Button btnDescartar;

    [Header("Panel Construir")]
    public GameObject panelConstruir;
    public Button[] botonesCodigo;       // uno por cada instalación (8 botones)
    public TextMeshProUGUI textoCosteSeleccionado;
    public Button btnConfirmarConstruir;
    public Button btnCerrarConstruir;

    [Header("Panel Comprar Suministros")]
    public GameObject panelComprar;
    public TMP_InputField inputCantidadComprar;
    public TextMeshProUGUI textoCosteCompra;
    public Button btnConfirmarCompra;
    public Button btnCerrarCompra;

    [Header("Feedback")]
    public TextMeshProUGUI textoFeedback;

    // Códigos de instalación disponibles (del backend-api-reference.md)
    private readonly string[] codigos =
        { "ADR05", "ADP03", "AAV02", "ASU04", "EDR02", "EDP03", "EAV02", "ESU03" };

    private readonly int[] costes =
        { 1000, 2500, 5000, 3500, 21000, 12500, 15000, 25000 };

    private string codigoSeleccionado = "";
    private string enclaveIdSeleccionado = "";

    void Start()
    {
        if (panelConstruir != null) panelConstruir.SetActive(false);
        if (panelComprar != null) panelComprar.SetActive(false);
        if (textoFeedback != null) textoFeedback.text = "";

        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += OnEstadoActualizado;

        // Conectar botones de código de instalación
        for (int i = 0; i < botonesCodigo.Length && i < codigos.Length; i++)
        {
            int idx = i;
            botonesCodigo[i]?.onClick.AddListener(() => SeleccionarCodigo(idx));
        }

        // Input de compra → actualizar coste en tiempo real
        inputCantidadComprar?.onValueChanged.AddListener(ActualizarCosteCompra);
    }

    void OnDestroy()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= OnEstadoActualizado;
    }

    // ── Botones principales ───────────────────────────────────────────────

    public void OnSimularMesClick()
    {
        SetBotonesInteractivos(false);
        MostrarFeedback("Simulando mes...");
        GameManager.Instance.EjecutarRonda();
    }

    public void OnGuardarClick()
    {
        MostrarFeedback("Guardando...");
        GameManager.Instance.GuardarPartida();
        MostrarFeedback("Partida guardada.");
    }

    public void OnConstruirClick()
    {
        if (panelConstruir != null) panelConstruir.SetActive(true);
        codigoSeleccionado = "";
        if (textoCosteSeleccionado != null) textoCosteSeleccionado.text = "Selecciona un tipo";
        if (btnConfirmarConstruir != null) btnConfirmarConstruir.interactable = false;

        // Por defecto usar el enclave de aclimatación
        var partida = GameManager.Instance.PartidaActual;
        if (partida?.Enclaves != null)
        {
            foreach (var e in partida.Enclaves)
            {
                if (e.TipoEnclave == 0) { enclaveIdSeleccionado = e.Id; break; }
            }
        }
    }

    public void OnComprarClick()
    {
        if (panelComprar != null) panelComprar.SetActive(true);
        if (inputCantidadComprar != null) inputCantidadComprar.text = "";
        if (textoCosteCompra != null) textoCosteCompra.text = "Coste: 0 S";

        // Por defecto enclave de aclimatación
        var partida = GameManager.Instance.PartidaActual;
        if (partida?.Enclaves != null)
        {
            foreach (var e in partida.Enclaves)
            {
                if (e.TipoEnclave == 0) { enclaveIdSeleccionado = e.Id; break; }
            }
        }
    }

    public void OnDescartarClick()
    {
        // Descartar la criatura seleccionada en DetailPanel
        var criatura = UIManager.Instance?.detailPanel?.CriaturaSeleccionada;
        if (criatura == null) { MostrarError("Selecciona una criatura primero."); return; }

        MostrarFeedback($"Descartando {criatura.Nombre}... (coste: 20.000 S)");
        GameManager.Instance.DescartarCriatura(criatura.Id);
    }

    public void OnTrasladarClick()
    {
        var criatura = UIManager.Instance?.detailPanel?.CriaturaSeleccionada;
        if (criatura == null) { MostrarError("Selecciona una criatura primero."); return; }
        if (!criatura.PuedeTraslado)
        {
            MostrarError("La criatura necesita salud ≥ 75 y ser adulta para trasladarse.");
            return;
        }
        MostrarFeedback("Selecciona la instalacion de exhibicion destino en el panel izquierdo.");
        UIManager.Instance?.enclavePanel?.ActivarModoSeleccionDestino(criatura);
    }

    // ── Panel Construir ───────────────────────────────────────────────────

    private void SeleccionarCodigo(int idx)
    {
        codigoSeleccionado = codigos[idx];
        if (textoCosteSeleccionado != null)
            textoCosteSeleccionado.text = $"{codigoSeleccionado} — Coste: {costes[idx]:N0} S";
        if (btnConfirmarConstruir != null)
            btnConfirmarConstruir.interactable = true;
    }

    public void OnConfirmarConstruirClick()
    {
        if (string.IsNullOrEmpty(codigoSeleccionado) ||
            string.IsNullOrEmpty(enclaveIdSeleccionado)) return;

        MostrarFeedback($"Construyendo {codigoSeleccionado}...");
        panelConstruir.SetActive(false);
        GameManager.Instance.ConstruirInstalacion(codigoSeleccionado, enclaveIdSeleccionado);
    }

    public void OnCerrarConstruirClick() =>
        panelConstruir?.SetActive(false);

    // ── Panel Comprar ─────────────────────────────────────────────────────

    private void ActualizarCosteCompra(string valor)
    {
        if (int.TryParse(valor, out int cantidad) && textoCosteCompra != null)
            textoCosteCompra.text = $"Coste: {cantidad * 5:N0} S";
    }

    public void OnConfirmarCompraClick()
    {
        if (!int.TryParse(inputCantidadComprar?.text, out int cantidad) || cantidad <= 0)
        {
            MostrarError("Introduce una cantidad valida."); return;
        }
        panelComprar?.SetActive(false);
        MostrarFeedback($"Comprando {cantidad} suministros...");
        GameManager.Instance.ComprarSuministros(enclaveIdSeleccionado, cantidad);
    }

    public void OnCerrarCompraClick() =>
        panelComprar?.SetActive(false);

    // ── Callbacks ─────────────────────────────────────────────────────────

    private void OnEstadoActualizado(PartidaData partida)
    {
        SetBotonesInteractivos(true);
        MostrarFeedback("");
    }

    private void SetBotonesInteractivos(bool activo)
    {
        if (btnSimularMes != null) btnSimularMes.interactable = activo;
        if (btnGuardar != null) btnGuardar.interactable = activo;
        if (btnConstruir != null) btnConstruir.interactable = activo;
        if (btnComprar != null) btnComprar.interactable = activo;
    }

    private void MostrarFeedback(string msg)
    {
        if (textoFeedback != null) textoFeedback.text = msg;
    }

    private void MostrarError(string msg)
    {
        SetBotonesInteractivos(true);
        if (textoFeedback != null) textoFeedback.text = $"[ERROR] {msg}";
    }
}
