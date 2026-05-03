namespace Dune.Domain;

public class Enclave
{
    public string Nombre { get; set; } = string.Empty;
    public List<Instalacion> Instalaciones { get; set; } = new();
    public int PoblacionVisitantes { get; set; }
    public NivelAdquisitivo Nivel { get; set; }
}

