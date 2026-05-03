using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain
{
    public class Escenario
    {
        public string Nombre { get; set; } = string.Empty;
        public double SolarisIniciales { get; set; }
        public string EnclaveExhibicionNombre { get; set; } = string.Empty;
    }
}
