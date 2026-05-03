using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dune.Domain;

public class TigraLaza : Criatura
{
    public TigraLaza()
    {
        EdadAdulta = 20;
        ApetitoBase = 30;
        Dieta = Alimentacion.CARNIVORO;
        Habitat = Medio.TERRESTRE;
    }

    public override double CalcularIngestaRequerida(TipoActividad actividad)
    {
        if (EdadActual < EdadAdulta) return ApetitoBase * EdadActual;
        int alfa = (actividad == TipoActividad.EXHIBICION) ? 1 : 15;
        return ApetitoBase * Math.Pow(2, (EdadActual - EdadAdulta)) * alfa;
    }
}
