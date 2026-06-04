using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton central. Almacena el estado de la partida y expone todos los
/// métodos de llamada al SimulationService (localhost:5000).
/// NUNCA llama directamente al PersistenceService (5032).
/// </summary>
public class GameManager : MonoBehaviour 
{
    public static GameManager Instance { get; private set; }

    private const string SimUrl = "http://localhost:5000"; 

    public PartidaData PartidaActual { get; private set; }

    // Eventos para que la UI reaccione sin polling
    public static event System.Action<PartidaData> OnEstadoActualizado;
    public static event System.Action<string> OnError;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ────────────────────────────────────────────────────────────────────────
    // HELPERS PRIVADOS
    // ────────────────────────────────────────────────────────────────────────

    private UnityWebRequest BuildPost(string url)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    private PartidaData ParsePartida(string json)
    {
        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            MetadataPropertyHandling = Newtonsoft.Json.MetadataPropertyHandling.Ignore
        };
        return Newtonsoft.Json.JsonConvert.DeserializeObject<PartidaData>(json, settings);
    }
    private void HandleSuccess(string json)
    {
        PartidaActual = ParsePartida(json);
        Debug.Log($"[GameManager] Estado recibido — Jugador: {PartidaActual?.NombreJugador} | Mes: {PartidaActual?.MesActual} | Solaris: {PartidaActual?.Solaris}");
        OnEstadoActualizado?.Invoke(PartidaActual);
    }
    private void HandleError(string context, string raw)
    {
        // Intentar parsear mensaje de error del backend
        try
        {
            var err = JsonConvert.DeserializeObject<ErrorResponse>(raw);
            OnError?.Invoke($"[{context}] {err?.error ?? raw}");
        }
        catch { OnError?.Invoke($"[{context}] {raw}"); }
    }

    // ────────────────────────────────────────────────────────────────────────
    // ENDPOINTS
    // ────────────────────────────────────────────────────────────────────────

    /// GET /estado-inicial
    public void ObtenerEstado() => StartCoroutine(ObtenerEstadoCoroutine());

    IEnumerator ObtenerEstadoCoroutine()
    {
        using var req = UnityWebRequest.Get($"{SimUrl}/estado-inicial");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            HandleSuccess(req.downloadHandler.text);
        else
            HandleError("ObtenerEstado", req.downloadHandler.text);
    }

    /// POST /simulacion/iniciar-partida
    public void IniciarPartida(string nombreJugador, string escenario) =>
        StartCoroutine(IniciarPartidaCoroutine(nombreJugador, escenario));

    IEnumerator IniciarPartidaCoroutine(string nombreJugador, string escenario)
    {
        string url = $"{SimUrl}/simulacion/iniciar-partida" +
                     $"?nombreJugador={UnityWebRequest.EscapeURL(nombreJugador)}" +
                     $"&nombreEscenario={escenario}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            HandleSuccess(req.downloadHandler.text);
        else
            HandleError("IniciarPartida", req.downloadHandler.text);
    }

    /// POST /simulacion/ejecutar-ronda
    public void EjecutarRonda() => StartCoroutine(EjecutarRondaCoroutine());

    IEnumerator EjecutarRondaCoroutine()
    {
        using var req = BuildPost($"{SimUrl}/simulacion/ejecutar-ronda");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            HandleSuccess(req.downloadHandler.text);
        else
            HandleError("EjecutarRonda", req.downloadHandler.text);
    }

    /// POST /simulacion/construir-instalacion
    public void ConstruirInstalacion(string codigo, string enclaveId) =>
        StartCoroutine(ConstruirInstalacionCoroutine(codigo, enclaveId));

    IEnumerator ConstruirInstalacionCoroutine(string codigo, string enclaveId)
    {
        string url = $"{SimUrl}/simulacion/construir-instalacion" +
                     $"?codigoInstalacion={codigo}&enclaveId={enclaveId}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            HandleSuccess(req.downloadHandler.text);
        else
            HandleError("ConstruirInstalacion", req.downloadHandler.text);
    }

    /// POST /simulacion/comprar-suministros
    public void ComprarSuministros(string enclaveId, int cantidad) =>
        StartCoroutine(ComprarSuministrosCoroutine(enclaveId, cantidad));

    IEnumerator ComprarSuministrosCoroutine(string enclaveId, int cantidad)
    {
        string url = $"{SimUrl}/simulacion/comprar-suministros" +
                     $"?enclaveId={enclaveId}&cantidad={cantidad}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            // Este endpoint NO devuelve Partida completa, recargamos estado
            StartCoroutine(ObtenerEstadoCoroutine());
        }
        else
            HandleError("ComprarSuministros", req.downloadHandler.text);
    }
    void OnApplicationQuit()
    {
        if (PartidaActual != null)
            StartCoroutine(GuardarPartidaCoroutine());
    }
    /// POST /simulacion/mover-suministros
    public void MoverSuministros(string enclaveId, string instalacionId, int cantidad) =>
        StartCoroutine(MoverSuministrosCoroutine(enclaveId, instalacionId, cantidad));

    IEnumerator MoverSuministrosCoroutine(string enclaveId, string instalacionId, int cantidad)
    {
        string url = $"{SimUrl}/simulacion/mover-suministros" +
                     $"?enclaveId={enclaveId}&instalacionId={instalacionId}&cantidad={cantidad}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            StartCoroutine(ObtenerEstadoCoroutine());
        else
            HandleError("MoverSuministros", req.downloadHandler.text);
    }

    /// POST /simulacion/trasladar-criatura
    public void TrasladarCriatura(string criaturaId, string origenId, string destinoId) =>
        StartCoroutine(TrasladarCriaturaCoroutine(criaturaId, origenId, destinoId));

    IEnumerator TrasladarCriaturaCoroutine(string criaturaId, string origenId, string destinoId)
    {
        string url = $"{SimUrl}/simulacion/trasladar-criatura" +
                     $"?criaturaId={criaturaId}" +
                     $"&instalacionOrigenId={origenId}" +
                     $"&instalacionDestinoId={destinoId}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            StartCoroutine(ObtenerEstadoCoroutine());
        else
            HandleError("TrasladarCriatura", req.downloadHandler.text);
    }

    /// POST /simulacion/descartar-criatura
    public void DescartarCriatura(string criaturaId) =>
        StartCoroutine(DescartarCriaturaCoroutine(criaturaId));

    IEnumerator DescartarCriaturaCoroutine(string criaturaId)
    {
        string url = $"{SimUrl}/simulacion/descartar-criatura?criaturaId={criaturaId}";
        using var req = BuildPost(url);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            StartCoroutine(ObtenerEstadoCoroutine());
        else
            HandleError("DescartarCriatura", req.downloadHandler.text);
    }

    /// POST /simulacion/cargar-partida  (NUNCA llamar al 5032 directamente)
    public void CargarPartidaGuardada() =>
        StartCoroutine(CargarPartidaGuardadaCoroutine());

    IEnumerator CargarPartidaGuardadaCoroutine()
    {
        using var req = BuildPost($"{SimUrl}/simulacion/cargar-partida");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            HandleSuccess(req.downloadHandler.text);
        else
            HandleError("CargarPartida", req.downloadHandler.text);
    }

    /// POST /simulacion/guardar-actual
    public void GuardarPartida() => StartCoroutine(GuardarPartidaCoroutine());

    IEnumerator GuardarPartidaCoroutine()
    {
        if (PartidaActual == null) { OnError?.Invoke("No hay partida activa para guardar."); yield break; }
        string json = JsonConvert.SerializeObject(PartidaActual);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        using var req = new UnityWebRequest($"{SimUrl}/simulacion/guardar-actual", "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("Partida guardada.");
        else
            HandleError("GuardarPartida", req.downloadHandler.text);
    }
    void Start()
    {
        Debug.Log("[GameManager] Arrancando — intentando conectar con el backend...");
        ObtenerEstado();
    }
}