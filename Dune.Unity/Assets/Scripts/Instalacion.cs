using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain
{
    [System.Serializable]
    public class Instalacion
    {
        public string id;
        public string nombre;
        public string tipo;           // "CRIANZA" o "EXHIBICION"
        public int capacidadMaxima;
        public int hectareas;
        public int costeConstruccion;
        public List<Criatura> criaturas;
        public List<Visitante> visitantesActuales;
    }
}
