namespace Dune.Domain;

public class Partida
{
    public string NombreJugador { get; set; } = "Vic";
    public int MesActual { get; set; } = 1;
    public double Solaris { get; set; } = 100000;
    public double StockEspecia { get; set; } = 500;
    public double StockAgua { get; set; } = 1000;
    public List<Enclave> Enclaves { get; set; } = new();
    public List<string> RegistroEventos { get; set; } = new();
}

