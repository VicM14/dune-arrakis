using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain;

public class Escenario
{
    public string Nombre { get; set; } = string.Empty;
    public double SolarisIniciales { get; set; }
    public string EnclaveExhibicionNombre { get; set; } = string.Empty;

    public static Escenario Arrakeen() => new()
    {
        Nombre = "Arrakeen: Dominio de la Especia",
        SolarisIniciales = 100000,
        EnclaveExhibicionNombre = "Arrakeen"
    };

    public static Escenario GiediPrime() => new()
    {
        Nombre = "Giedi Prime: Galería Industrial",
        SolarisIniciales = 50000,
        EnclaveExhibicionNombre = "Giedi Prime"
    };

    public static Escenario Caladan() => new()
    {
        Nombre = "Caladan: Reserva Ducal",
        SolarisIniciales = 150000,
        EnclaveExhibicionNombre = "Caladan"
    };
}
