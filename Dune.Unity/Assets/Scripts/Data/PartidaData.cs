using System.Collections.Generic;
using UnityEngine;

// ??? Refleja exactamente la respuesta JSON del SimulationService ?????????????
// Campos en PascalCase porque el backend serializa con PascalCase por defecto.
// Usaremos Newtonsoft.Json para la deserialización (soporta $type y PascalCase).

[System.Serializable]
public class PartidaData
{
    public string NombreJugador;
    public int MesActual;
    public double Solaris;
    public List<EnclaveData> Enclaves;
    public List<string> RegistroEventos;
    public EscenarioData Escenario;
}

[System.Serializable]
public class EscenarioData
{
    public string Nombre;
    public double SolarisIniciales;
    public string EnclaveExhibicionNombre;
}

[System.Serializable]
public class EnclaveData
{
    public string Id;
    public string Nombre;
    public int Nivel;
    public int PoblacionVisitantes;
    public List<InstalacionData> Instalaciones;
    public int Hectareas;
    public int VisitantesMensualesBase;
    public int NivelAdquisitivo;   // enum int: 0=BAJO, 1=MEDIO, 2=ALTO
    public int Suministros;
    public int TipoEnclave;        // enum int: 0=ACLIMATACION, 1=EXHIBICION

    // Calculados en Unity (marcados [JsonIgnore] en el backend)
    public int CapacidadAlmacen => Hectareas * 3;
    public int EspacioLibreAlmacen => CapacidadAlmacen - Suministros;
}

[System.Serializable]
public class InstalacionData
{
    public string Id;
    public string Codigo;
    public string Nombre;
    public int Tipo;               // 0=ACLIMATACION, 1=EXHIBICION
    public int CapacidadMaxima;
    public List<CriaturaData> Criaturas;
    public int Hectareas;
    public int CosteConstruccion;
    public int Medio;              // 0=DESIERTO, 1=AEREO, 2=SUBTERRANEO
    public int Alimentacion;       // 0=RECOLECTOR, 1=DEPREDADOR
    public int TipoRecinto;        // 0=ROCA_SELLADA, 1=ESCUDO_ESTATICO, 2=CUPULA_BLINDADA, 3=POZO_REFORZADO
    public int Suministros;
    public int SuministrosIniciales;
}

[System.Serializable]
public class CriaturaData
{
    // $type indica la especie — deserializado con Newtonsoft
    [Newtonsoft.Json.JsonProperty("$type")]
    public string Tipo;            // "GusanoDeArena", "TigraLaza", "MuadDib", "HalconDelDesierto", "TruchaDeArena"

    public string Id;
    public string Nombre;
    public double Salud;
    public int EdadActual;
    public int EdadAdulta;
    public double ApetitoBase;
    public int Dieta;              // 0=RECOLECTOR, 1=DEPREDADOR
    public int Habitat;            // 0=DESIERTO, 1=AEREO, 2=SUBTERRANEO
    public bool EnLetargo;
    public int VecesFavorita;

    // Helpers de lectura para la UI
    public bool EsAdulta => EdadActual >= EdadAdulta;
    public bool PuedeTraslado => EsAdulta && Salud >= 75 && !EnLetargo;
}

// Respuesta de error genérica del backend
[System.Serializable]
public class ErrorResponse
{
    public string error;
}

// Respuestas específicas de endpoints
[System.Serializable]
public class ComprarSuministrosResponse
{
    public string mensaje;
    public double coste;
    public int almacen;
    public double solarisRestantes;
}

[System.Serializable]
public class MoverSuministrosResponse
{
    public string mensaje;
    public int almacen;
    public int stockInstalacion;
}

[System.Serializable]
public class TrasladarCriaturaResponse
{
    public string mensaje;
    public double costeTraslado;
    public double solarisRestantes;
}

[System.Serializable]
public class DescartarCriaturaResponse
{
    public string mensaje;
    public double solarisRestantes;
}