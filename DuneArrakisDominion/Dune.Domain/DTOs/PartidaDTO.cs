namespace Dune.Domain.DTOs;

public class PartidaDTO
{
    public int MesActual { get; set; }
    public double Solaris { get; set; }
    public string EscenarioNombre { get; set; } = string.Empty;
    public List<EnclaveDTO> Enclaves { get; set; } = new();
}

public class EnclaveDTO
{
    public string Nombre { get; set; } = string.Empty;
    public int VisitantesActuales { get; set; }
    public string NivelAdquisitivo { get; set; } = string.Empty;
    public int NumeroCriaturas { get; set; }
}

