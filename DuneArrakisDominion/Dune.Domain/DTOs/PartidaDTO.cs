namespace Dune.Domain.DTOs;

public class PartidaDTO
{
    public string IdPartida { get; set; } = string.Empty;
    public string NombreJugador { get; set; } = string.Empty;
    public int MesActual { get; set; }
    public double Solaris { get; set; }
    public string EscenarioNombre { get; set; } = string.Empty;
    public List<EnclaveDTO> Enclaves { get; set; } = new();


    public static PartidaDTO DesdeDominio(Partida p) => new()
    {
        IdPartida = p.IdPartida,
        NombreJugador = p.NombreJugador,
        MesActual = p.MesActual,
        Solaris = p.Solaris,
        EscenarioNombre = p.Escenario?.Nombre ?? "-",
        Enclaves = p.Enclaves.Select(EnclaveDTO.DesdeDominio).ToList()
    };
}

public class EnclaveDTO
{
    public string Nombre { get; set; } = string.Empty;
    public string TipoEnclave { get; set; } = string.Empty;
    public int VisitantesActuales { get; set; }
    public string NivelAdquisitivo { get; set; } = string.Empty;
    public int NumeroInstalaciones { get; set; }
    public int NumeroCriaturas { get; set; }

    public static EnclaveDTO DesdeDominio(Enclave e) => new()
    {
        Nombre = e.Nombre,
        TipoEnclave = e.TipoEnclave.ToString(),
        VisitantesActuales = e.PoblacionVisitantes,
        NivelAdquisitivo = e.NivelAdquisitivo.ToString(),
        NumeroInstalaciones = e.Instalaciones.Count,
        NumeroCriaturas = e.Instalaciones.Sum(i => i.Criaturas.Count)
    };
}

public class PartidaResumenDTO
{
    public string IdPartida { get; set; } = string.Empty;
    public string NombreJugador { get; set; } = string.Empty;
    public int MesActual { get; set; }
    public double Solaris { get; set; }
    public string EscenarioNombre { get; set; } = string.Empty;
    public DateTime FechaModificacion { get; set; }

    public static PartidaResumenDTO DesdeDominio(Partida p) => new()
    {
        IdPartida = p.IdPartida,
        NombreJugador = p.NombreJugador,
        MesActual = p.MesActual,
        Solaris = p.Solaris,
        EscenarioNombre = p.Escenario?.Nombre ?? "-",
        FechaModificacion = p.FechaModificacion
    };
}