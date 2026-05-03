using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain;

public class TruchaDeArena : Criatura
{
    public TruchaDeArena()
    {
        EdadAdulta = 42;
        ApetitoBase = 10;
        Dieta = Alimentacion.RECOLECTOR;
        Habitat = Medio.SUBTERRANEO;
    }

    public override double CalcularIngestaRequerida(TipoActividad actividad)
    {
        if (EdadActual < EdadAdulta) return ApetitoBase * EdadActual;
        int alfa = (actividad == TipoActividad.EXHIBICION) ? 1 : 15;
        return ApetitoBase * Math.Pow(2, (EdadActual - EdadAdulta)) * alfa;
    }
}