using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Image = UnityEngine.UI.Image;

public class GameViewController : MonoBehaviour
{
    [Header("Botones principales")]
    public Button btnSimularMes;
    public Button btnGuardar;
    public Button btnConstruir;
    public Button btnTrasladar;
    public Button btnComprar;
    public Button btnMover;
    public Button btnDescartar;

    [Header("Panel Construir")]
    public GameObject panelConstruir;
    public Button[] botonesCodigo;
    public TextMeshProUGUI textoCosteSeleccionado;
    public Button btnConfirmarConstruir;
    public Button btnCerrarConstruir;
    public Button btnEnclaveAclimConstruir;
    public Button btnEnclaveExhibConstruir;

    [Header("Panel Comprar Suministros")]
    public GameObject panelComprar;
    public TMP_InputField inputCantidadComprar;
    public TextMeshProUGUI textoCosteCompra;
    public Button btnConfirmarCompra;
    public Button btnCerrarCompra;
    public Button btnEnclaveAclimComprar;
    public Button btnEnclaveExhibComprar;

    [Header("Panel Mover Suministros")]
    public GameObject panelMover;
    public TMP_InputField inputCantidadMover;
    public TextMeshProUGUI textoDestinoMover;
    public Button btnConfirmarMover;
    public Button btnCerrarMover;

    [Header("Transicion de mes")]
    public MonthTransitionUI monthTransition;

    [Header("Feedback")]
    public TextMeshProUGUI textoFeedback;

    private readonly string[] codigos =
        { "ADR05", "ADP03", "AAV02", "ASU04", "EDR02", "EDP03", "EAV02", "ESU03" };
    private readonly int[] costes =
        { 1000, 2500, 5000, 3500, 21000, 12500, 15000, 25000 };

    private string codigoSeleccionado = "";
    private string enclaveIdSeleccionado = "";
    private string instalacionIdMover = "";
    private Coroutine feedbackCoroutine;

    private readonly Color colorActivo = new Color(0.32f, 0.53f, 1f);
    private readonly Color colorNormal = new Color(0.24f, 0.24f, 0.36f);

    // ─────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (panelConstruir != null) panelConstruir.SetActive(false);
        if (panelComprar != null) panelComprar.SetActive(false);
        if (panelMover != null) panelMover.SetActive(false);
        if (textoFeedback != null) textoFeedback.text = "";

        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += OnEstadoActualizado;

        for (int i = 0; i < botonesCodigo.Length && i < codigos.Length; i++)
        {
            int idx = i;
            botonesCodigo[i]?.onClick.AddListener(() => SeleccionarCodigo(idx));
        }

        inputCantidadComprar?.onValueChanged.AddListener(ActualizarCosteCompra);

        // Conectar botones de enclave del panel Comprar por código
        btnEnclaveAclimComprar?.onClick.AddListener(OnEnclaveAclimClick);
        btnEnclaveExhibComprar?.onClick.AddListener(OnEnclaveExhibClick);

        // Conectar botones de enclave del panel Construir por código
        btnEnclaveAclimConstruir?.onClick.AddListener(OnEnclaveAclimClick);
        btnEnclaveExhibConstruir?.onClick.AddListener(OnEnclaveExhibClick);
    }

    void OnDestroy()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= OnEstadoActualizado;
    }

    // ─────────────────────────────────────────────────────────────────────
    // BOTONES PRINCIPALES
    // ─────────────────────────────────────────────────────────────────────

    public void OnSimularMesClick()
    {
        SetBotonesInteractivos(false);

        int mesActual = GameManager.Instance?.PartidaActual?.MesActual ?? 0;

        if (monthTransition != null)
            StartCoroutine(SimularConTransicion(mesActual + 1));
        else
        {
            MostrarFeedback("Simulando mes...");
            GameManager.Instance.EjecutarRonda();
        }
    }

    private IEnumerator SimularConTransicion(int mesNuevo)
    {
        // Suscribirse temporalmente para notificar cuando la API responda
        void OnApiResponse(PartidaData _) => monthTransition?.NotificarApiCompleta();
        GameManager.OnEstadoActualizado += OnApiResponse;

        // Lanzar la API
        GameManager.Instance.EjecutarRonda();

        // Ejecutar la animación (espera internamente a que la API responda)
        yield return monthTransition.Ejecutar(mesNuevo);

        GameManager.OnEstadoActualizado -= OnApiResponse;
        SetBotonesInteractivos(true);
    }

    public void OnGuardarClick()
    {
        MostrarFeedback("Guardando partida...");
        GameManager.Instance.GuardarPartida();
        MostrarFeedback("Partida guardada.");
    }

    public void OnConstruirClick()
    {
        if (panelConstruir != null) panelConstruir.SetActive(true);
        codigoSeleccionado = "";
        if (textoCosteSeleccionado != null) textoCosteSeleccionado.text = "Selecciona un tipo";
        if (btnConfirmarConstruir != null) btnConfirmarConstruir.interactable = false;
        SeleccionarEnclave(true);
    }

    public void OnComprarClick()
    {
        if (panelComprar != null) panelComprar.SetActive(true);
        if (inputCantidadComprar != null) inputCantidadComprar.text = "";
        if (textoCosteCompra != null) textoCosteCompra.text = "Coste: 0 S";
        SeleccionarEnclave(true);
    }

    public void OnMoverClick()
    {
        var inst = UIManager.Instance?.detailPanel?.InstalacionSeleccionada;
        if (inst == null)
        {
            MostrarFeedback("Selecciona una instalación primero.");
            return;
        }
        instalacionIdMover = inst.Id;
        if (textoDestinoMover != null)
            textoDestinoMover.text = $"Destino: {inst.Codigo} (máx. {inst.CosteConstruccion} S)";
        if (inputCantidadMover != null) inputCantidadMover.text = "";
        if (panelMover != null) panelMover.SetActive(true);
        SeleccionarEnclave(true);
    }

    public void OnTrasladarClick()
    {
        var criatura = UIManager.Instance?.detailPanel?.CriaturaSeleccionada;
        if (criatura == null)
        {
            MostrarFeedback("Selecciona una criatura primero.");
            return;
        }
        if (!criatura.PuedeTraslado)
        {
            MostrarFeedback("Necesita salud >= 75 y ser adulta para trasladarse.");
            return;
        }
        MostrarFeedback("Haz click en la instalacion de EXHIBICION destino.");
        UIManager.Instance?.enclavePanel?.ActivarModoSeleccionDestino(criatura);
    }

    public void OnDescartarClick()
    {
        var criatura = UIManager.Instance?.detailPanel?.CriaturaSeleccionada;
        if (criatura == null)
        {
            MostrarFeedback("Selecciona una criatura primero.");
            return;
        }
        MostrarFeedback($"Descartando {criatura.Nombre}... (coste: 20.000 S)");
        GameManager.Instance.DescartarCriatura(criatura.Id);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SELECTOR DE ENCLAVE
    // ─────────────────────────────────────────────────────────────────────

    public void OnEnclaveAclimClick() => SeleccionarEnclave(true);
    public void OnEnclaveExhibClick() => SeleccionarEnclave(false);

    private void SeleccionarEnclave(bool aclimatacion)
    {
        var partida = GameManager.Instance?.PartidaActual;
        if (partida?.Enclaves == null) return;

        foreach (var e in partida.Enclaves)
        {
            bool esAclim = e.TipoEnclave == 0;
            if ((aclimatacion && esAclim) || (!aclimatacion && !esAclim))
            {
                enclaveIdSeleccionado = e.Id;
                break;
            }
        }

        // Actualizar color en ambos paneles
        SetColorBtn(btnEnclaveAclimConstruir, aclimatacion ? colorActivo : colorNormal);
        SetColorBtn(btnEnclaveExhibConstruir, !aclimatacion ? colorActivo : colorNormal);
        SetColorBtn(btnEnclaveAclimComprar, aclimatacion ? colorActivo : colorNormal);
        SetColorBtn(btnEnclaveExhibComprar, !aclimatacion ? colorActivo : colorNormal);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PANEL CONSTRUIR
    // ─────────────────────────────────────────────────────────────────────

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
        panelConstruir?.SetActive(false);
        GameManager.Instance.ConstruirInstalacion(codigoSeleccionado, enclaveIdSeleccionado);
    }

    public void OnCerrarConstruirClick() => panelConstruir?.SetActive(false);

    // ─────────────────────────────────────────────────────────────────────
    // PANEL COMPRAR
    // ─────────────────────────────────────────────────────────────────────

    private void ActualizarCosteCompra(string valor)
    {
        if (int.TryParse(valor, out int cantidad) && textoCosteCompra != null)
            textoCosteCompra.text = $"Coste: {cantidad * 5:N0} S";
    }

    public void OnConfirmarCompraClick()
    {
        if (!int.TryParse(inputCantidadComprar?.text, out int cantidad) || cantidad <= 0)
        {
            MostrarFeedback("Introduce una cantidad valida."); return;
        }
        panelComprar?.SetActive(false);
        MostrarFeedback($"Comprando {cantidad} suministros...");
        GameManager.Instance.ComprarSuministros(enclaveIdSeleccionado, cantidad);
    }

    public void OnCerrarCompraClick() => panelComprar?.SetActive(false);

    // ─────────────────────────────────────────────────────────────────────
    // PANEL MOVER
    // ─────────────────────────────────────────────────────────────────────

    public void OnConfirmarMoverClick()
    {
        if (!int.TryParse(inputCantidadMover?.text, out int cantidad) || cantidad <= 0)
        {
            MostrarFeedback("Introduce una cantidad valida."); return;
        }
        if (string.IsNullOrEmpty(instalacionIdMover))
        {
            MostrarFeedback("No hay instalacion destino seleccionada."); return;
        }
        panelMover?.SetActive(false);
        MostrarFeedback($"Moviendo {cantidad} suministros...");
        GameManager.Instance.MoverSuministros(enclaveIdSeleccionado, instalacionIdMover, cantidad);
    }

    public void OnCerrarMoverClick() => panelMover?.SetActive(false);

    // ─────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────

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
        if (btnMover != null) btnMover.interactable = activo;
    }

    private void MostrarFeedback(string msg)
    {
        if (textoFeedback == null) return;
        textoFeedback.text = msg;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        if (!string.IsNullOrEmpty(msg))
            feedbackCoroutine = StartCoroutine(LimpiarFeedbackTras(3f));
    }

    private IEnumerator LimpiarFeedbackTras(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (textoFeedback != null) textoFeedback.text = "";
    }

    private void MostrarError(string msg)
    {
        SetBotonesInteractivos(true);
        MostrarFeedback($"[ERROR] {msg}");
    }

    private void SetColorBtn(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
