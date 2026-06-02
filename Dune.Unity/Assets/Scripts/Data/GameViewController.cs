using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameViewController : MonoBehaviour
{
    [Header("Panel superior")]
    [SerializeField] private TMP_Text txtJugador;
    [SerializeField] private TMP_Text txtMes;
    [SerializeField] private TMP_Text txtSolaris;

    [Header("Panel enclaves")]
    [SerializeField] private Transform contenedorEnclaves;
    [SerializeField] private GameObject prefabEnclaveCard;

    [Header("Registro de eventos")]
    [SerializeField] private TMP_Text txtEventos;
    [SerializeField] private ScrollRect scrollEventos;

    [Header("Botones")]
    [SerializeField] private Button btnEjecutarRonda;
    [SerializeField] private Button btnGuardar;
    [SerializeField] private TMP_Text txtFeedback;

    void OnEnable()
    {
        GameManager.OnEstadoActualizado += RefrescarUI;
        GameManager.OnError += e => txtFeedback.text = $"ERROR: {e}";
    }
    void OnDisable()
    {
        GameManager.OnEstadoActualizado -= RefrescarUI;
        GameManager.OnError -= e => txtFeedback.text = $"ERROR: {e}";
    }

    void Start()
    {
        // Cargar estado inicial al entrar en la escena
        GameManager.Instance.ObtenerEstado();
        btnEjecutarRonda.onClick.AddListener(OnEjecutarRonda);
        btnGuardar.onClick.AddListener(OnGuardar);
    }

    private void RefrescarUI(PartidaData p)
    {
        txtJugador.text = $"Casa: {p.NombreJugador}";
        txtMes.text = $"Mes {p.MesActual}";
        txtSolaris.text = $"{p.Solaris:N0} ₪";

        // Limpiar y regenerar cards de enclaves
        foreach (Transform t in contenedorEnclaves) Destroy(t.gameObject);
        foreach (var enclave in p.Enclaves)
        {
            var card = Instantiate(prefabEnclaveCard, contenedorEnclaves);
            card.GetComponent<EnclaveCard>().Inicializar(enclave);
        }

        // Eventos (los últimos 20, orden cronológico)
        var ultimos = p.RegistroEventos.Count > 20
            ? p.RegistroEventos.GetRange(p.RegistroEventos.Count - 20, 20)
            : p.RegistroEventos;
        txtEventos.text = string.Join("\n", ultimos);
        Canvas.ForceUpdateCanvases();
        scrollEventos.verticalNormalizedPosition = 0f; // scroll al final
    }

    private void OnEjecutarRonda()
    {
        btnEjecutarRonda.interactable = false;
        txtFeedback.text = "Ejecutando ronda...";
        GameManager.Instance.EjecutarRonda();
        // El botón se reactiva cuando OnEstadoActualizado dispara RefrescarUI
        btnEjecutarRonda.interactable = true;
    }

    private void OnGuardar()
    {
        GameManager.Instance.GuardarPartida();
        txtFeedback.text = "Partida guardada.";
    }
}
