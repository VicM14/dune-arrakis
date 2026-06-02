using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain;

public class GusanoDeArena : Criatura
{
    public GusanoDeArena()
    {
        EdadAdulta = 24;
        ApetitoBase = 5;
        Dieta = Alimentacion.DEPREDADOR;
        Habitat = Medio.SUBTERRANEO;
    }

    public override double CalcularIngestaRequerida(TipoActividad actividad)
    {
        if (EdadActual < EdadAdulta) return ApetitoBase * EdadActual;
        int alfa = (actividad == TipoActividad.EXHIBICION) ? 1 : 15;
        return ApetitoBase * Math.Pow(2, (EdadActual - EdadAdulta)) * alfa;
    }
}
