namespace Dune.Domain;

public class Partida
{
    public string NombreJugador { get; set; } = string.Empty;
    public int MesActual { get; set; } = 1;
    public double Solaris { get; set; }
    public List<Enclave> Enclaves { get; set; } = new();
}
