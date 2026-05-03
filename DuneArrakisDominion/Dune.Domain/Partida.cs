using System.Collections.Generic;

namespace Dune.Domain;

public class Partida
{
    public string NombreJugador { get; set; } = "Vic";
    public int MesActual { get; set; } = 1;
    public double Solaris { get; set; } = 100000;
    public List<Enclave> Enclaves { get; set; } = new();
    public List<string> RegistroEventos { get; set; } = new();
}
