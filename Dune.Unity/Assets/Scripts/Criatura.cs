using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

namespace Dune.Domain
{
    [System.Serializable]
    public class Criatura
    {
        public string id;
        public string nombre;
        public double salud;
        public int edadActual;
        public int edadAdulta;
        public double apetitoBase;
        public string dieta;      // "RECOLECTOR" o "DEPREDADOR"
        public string habitat;    // "DESIERTO", "AEREO", "SUBTERRANEO"
        public bool enLetargo;
        public int vecesFavorita;
    }
}
