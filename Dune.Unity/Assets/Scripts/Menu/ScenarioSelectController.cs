using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ScenarioSelectController : MonoBehaviour
{
    [Header("Input")]
    public TMP_InputField inputNombreJugador;

    [Header("Botones de escenario")]
    public Button btnArrakeen;
    public Button btnGiediPrime;
    public Button btnCaladan;

    [Header("Info del escenario seleccionado")]
    public TextMeshProUGUI textoNombreEscenario;
    public TextMeshProUGUI textoDescripcion;
    public TextMeshProUGUI textoSolaris;
    public TextMeshProUGUI textoNivelAdquisitivo;

    [Header("Acciones")]
    public Button btnIniciar;
    public TextMeshProUGUI textoError;

    // Colores para feedback de selección
    private readonly Color colorSeleccionado = new Color(0.29f, 0.53f, 1f);   // #4A87FF
    private readonly Color colorNormal = new Color(0.24f, 0.24f, 0.36f); // #3D3D5C

    private string escenarioSeleccionado = "";

    // Datos de los escenarios (fuente: practica_dune.md y backend-api-reference.md)
    private struct DatosEscenario
    {
        public string nombreApi;      // el que espera el backend
        public string nombreDisplay;
        public string descripcion;
        public string solaris;
        public string nivelAdquisitivo;
    }

    private readonly DatosEscenario[] escenarios = new[]
    {
        new DatosEscenario
        {
            nombreApi        = "Arrakeen",
            nombreDisplay    = "ARRAKEEN: DOMINIO DE LA ESPECIA",
            descripcion      = "Arrakis, planeta desertico.\nOperacion prestigiosa y altamente rentable.",
            solaris          = "100.000 ?",
            nivelAdquisitivo = "ALTO"
        },
        new DatosEscenario
        {
            nombreApi        = "GiediPrime",
            nombreDisplay    = "GIEDI PRIME: GALERIA INDUSTRIAL",
            descripcion      = "Alta afluencia y baja exclusividad.\nEstetica industrial Harkonnen.",
            solaris          = "50.000 ?",
            nivelAdquisitivo = "BAJO"
        },
        new DatosEscenario
        {
            nombreApi        = "Caladan",
            nombreDisplay    = "CALADAN: RESERVA DUCAL",
            descripcion      = "Mundo oceanico de Casa Atreides.\nMejores condiciones logisticas.",
            solaris          = "150.000 ?",
            nivelAdquisitivo = "MEDIO"
        }
    };

    void Start()
    {
        if (textoError != null) textoError.text = "";
        if (btnIniciar != null) btnIniciar.interactable = false;

        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += OnPartidaIniciada;

        // Asignar listeners de los botones de escenario
        btnArrakeen?.onClick.AddListener(() => SeleccionarEscenario(0));
        btnGiediPrime?.onClick.AddListener(() => SeleccionarEscenario(1));
        btnCaladan?.onClick.AddListener(() => SeleccionarEscenario(2));

        // Seleccionar Arrakeen por defecto
        SeleccionarEscenario(0);
    }

    void OnDestroy()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= OnPartidaIniciada;
    }

    private void SeleccionarEscenario(int indice)
    {
        escenarioSeleccionado = escenarios[indice].nombreApi;

        // Actualizar info
        if (textoNombreEscenario != null) textoNombreEscenario.text = escenarios[indice].nombreDisplay;
        if (textoDescripcion != null) textoDescripcion.text = escenarios[indice].descripcion;
        if (textoSolaris != null) textoSolaris.text = $"Solaris iniciales: {escenarios[indice].solaris}";
        if (textoNivelAdquisitivo != null) textoNivelAdquisitivo.text = $"Nivel adquisitivo: {escenarios[indice].nivelAdquisitivo}";

        // Feedback visual en los botones
        SetColorBoton(btnArrakeen, indice == 0 ? colorSeleccionado : colorNormal);
        SetColorBoton(btnGiediPrime, indice == 1 ? colorSeleccionado : colorNormal);
        SetColorBoton(btnCaladan, indice == 2 ? colorSeleccionado : colorNormal);

        ValidarFormulario();
    }

    private void SetColorBoton(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    // Llamado por el TMP_InputField ? OnValueChanged en el Inspector
    public void OnNombreJugadorChanged(string valor)
    {
        ValidarFormulario();
    }

    private void ValidarFormulario()
    {
        bool valido = inputNombreJugador != null &&
                      inputNombreJugador.text.Trim().Length >= 2 &&
                      escenarioSeleccionado != "";
        if (btnIniciar != null) btnIniciar.interactable = valido;
    }

    public void OnIniciarClick()
    {
        string nombre = inputNombreJugador.text.Trim();
        if (nombre.Length < 2) { MostrarError("El nombre debe tener al menos 2 caracteres."); return; }
        if (textoError != null) textoError.text = "Iniciando partida...";
        btnIniciar.interactable = false;
        GameManager.Instance.IniciarPartida(nombre, escenarioSeleccionado);
    }

    public void OnVolverClick()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void OnPartidaIniciada(PartidaData partida)
    {
        SceneManager.LoadScene("GameView");
    }

    private void MostrarError(string msg)
    {
        if (textoError != null) textoError.text = msg;
        if (btnIniciar != null) btnIniciar.interactable = true;
    }
}