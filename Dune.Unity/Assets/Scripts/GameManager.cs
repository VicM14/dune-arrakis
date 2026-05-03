using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// ─── Modelos que reflejan el JSON del backend ───────────────────────────────

[System.Serializable]
public class PartidaData
{
    public string nombreJugador;
    public int mesActual;
    public double solaris;
    public double stockAgua;
    public double stockEspecia;
    public List<EnclaveData> enclaves;
    public List<string> registroEventos;
}

[System.Serializable]
public class EnclaveData
{
    public string id;
    public string nombre;
    public int hectareas;
    public int poblacionVisitantes;
    public int visitantesMensualesBase;
    public string nivelAdquisitivo;   // "BAJO", "MEDIO", "ALTO"
    public string tipoEnclave;        // "CRIANZA", "EXHIBICION"
    public List<InstalacionData> instalaciones;
}

[System.Serializable]
public class InstalacionData
{
    public string id;
    public string nombre;
    public string tipo;               // "CRIANZA", "EXHIBICION"
    public int capacidadMaxima;
    public int hectareas;
    public int costeConstruccion;
    public List<CriaturaData> criaturas;
}

[System.Serializable]
public class CriaturaData
{
    public string id;
    public string nombre;
    public double salud;
    public int edadActual;
    public int edadAdulta;
    public double apetitoBase;
    public string dieta;              // "RECOLECTOR", "DEPREDADOR"
    public string habitat;            // "DESIERTO", "AEREO", "SUBTERRANEO"
    public bool enLetargo;
    public int vecesFavorita;
}

// ─── GameManager ─────────────────────────────────────────────────────────────

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private const string SimUrl = "http://localhost:5000";

    public PartidaData partidaActual;

    void Start()
    {
        StartCoroutine(CargarEstado());
    }

    // ── Cargar estado completo ──────────────────────────────────────────────
    public void CargarEstadoPublico() => StartCoroutine(CargarEstado());

    IEnumerator CargarEstado()
    {
        using var req = UnityWebRequest.Get($"{SimUrl}/estado-inicial");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log($"Estado cargado — Mes {partidaActual.mesActual} | Solaris: {partidaActual.solaris}");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("Error al cargar estado: " + req.error);
        }
    }

    // ── Ejecutar ronda mensual ──────────────────────────────────────────────
    public void EjecutarRonda() => StartCoroutine(EjecutarRondaCoroutine());

    IEnumerator EjecutarRondaCoroutine()
    {
        using var req = new UnityWebRequest($"{SimUrl}/simulacion/ejecutar-ronda", "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log($"Ronda {partidaActual.mesActual} completada | Solaris: {partidaActual.solaris}");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("Error al ejecutar ronda: " + req.error);
        }
    }

    // ── Comprar recursos ───────────────────────────────────────────────────
    public void ComprarRecursos(double agua, double especia) =>
        StartCoroutine(ComprarRecursosCoroutine(agua, especia));

    IEnumerator ComprarRecursosCoroutine(double agua, double especia)
    {
        string url = $"{SimUrl}/simulacion/comprar-recursos?agua={agua}&especia={especia}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log($"Recursos comprados | Agua: {partidaActual.stockAgua} | Especia: {partidaActual.stockEspecia}");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("Error al comprar recursos: " + req.error);
        }
    }

    // ── Trasladar criatura ─────────────────────────────────────────────────
    public void TrasladarCriatura(string criaturaId, string origenId, string destinoId) =>
        StartCoroutine(TrasladarCriaturaCoroutine(criaturaId, origenId, destinoId));

    IEnumerator TrasladarCriaturaCoroutine(string criaturaId, string origenId, string destinoId)
    {
        string url = $"{SimUrl}/simulacion/trasladar-criatura?criaturaId={criaturaId}&instalacionOrigenId={origenId}&instalacionDestinoId={destinoId}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Traslado completado: " + req.downloadHandler.text);
            StartCoroutine(CargarEstado());
        }
        else
        {
            Debug.LogError("Error en traslado: " + req.error);
        }
    }
    public void CargarPartidaGuardada() => StartCoroutine(CargarPartidaGuardadaCoroutine());

    IEnumerator CargarPartidaGuardadaCoroutine()
    {
        using var req = UnityWebRequest.Get("http://localhost:5032/persistir/cargar");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log("Partida cargada desde disco.");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("No se encontró partida guardada: " + req.error);
        }
    }
    public void IniciarPartida(string nombreJugador, string escenario) =>
    StartCoroutine(IniciarPartidaCoroutine(nombreJugador, escenario));

    IEnumerator IniciarPartidaCoroutine(string nombreJugador, string escenario)
    {
        string url = $"{SimUrl}/simulacion/iniciar-partida?nombreJugador={nombreJugador}&nombreEscenario={escenario}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log($"Partida iniciada — Escenario: {escenario} | Jugador: {nombreJugador}");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("Error al iniciar partida: " + req.error);
        }
    }
    // ── Guardar partida manualmente ────────────────────────────────────────
    public void GuardarPartida() => StartCoroutine(GuardarPartidaCoroutine());

    IEnumerator GuardarPartidaCoroutine()
    {
        string json = JsonUtility.ToJson(partidaActual);
        using var req = new UnityWebRequest($"{SimUrl}/simulacion/guardar-actual", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("Partida guardada correctamente.");
        else
            Debug.LogError("Error al guardar: " + req.error);
    }
    public void ConstruirInstalacion(string codigoInstalacion, string enclaveId) =>
    StartCoroutine(ConstruirInstalacionCoroutine(codigoInstalacion, enclaveId));

    IEnumerator ConstruirInstalacionCoroutine(string codigoInstalacion, string enclaveId)
    {
        string url = $"{SimUrl}/simulacion/construir-instalacion?codigoInstalacion={codigoInstalacion}&enclaveId={enclaveId}";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(new byte[0]);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            partidaActual = JsonUtility.FromJson<PartidaData>(req.downloadHandler.text);
            Debug.Log($"Instalación {codigoInstalacion} construida.");
            OnEstadoCargado();
        }
        else
        {
            Debug.LogError("Error al construir instalación: " + req.error);
        }
    }
    // ── Callback para que los demás scripts de Unity reaccionen ───────────
    void OnEstadoCargado()
    {
        // Tu amiga puede llamar aquí a los métodos de UI:
        // UIManager.Instance.RefrescarPanel(partidaActual);
        // etc.
    }
}
