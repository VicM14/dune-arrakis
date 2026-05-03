using System;

namespace Dune.Domain;

public abstract class Criatura
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public double Salud { get; set; } = 100;
    public int EdadActual { get; set; } = 0;
    public int EdadAdulta { get; set; }
    public double ApetitoBase { get; set; }
    public Alimentacion Dieta { get; set; }
    public Medio Habitat { get; set; }

    public abstract double CalcularIngestaRequerida(TipoActividad actividad);

    public void Alimentar(double cantidad, TipoActividad actividad)
    {
        double requerida = CalcularIngestaRequerida(actividad);
        double ratio = (requerida > 0) ? cantidad / requerida : 1;

        if (ratio < 0.25) Salud -= 30;
        else if (ratio < 0.75) Salud -= 20;
        else if (ratio < 1.0) Salud -= 10;
        else Salud = Math.Min(100, Salud + 5);

        if (Salud < 0) Salud = 0;
    }
}

// Ejemplo de clase específica: Gusano de Arena
public class GusanoDeArena : Criatura
{
    public GusanoDeArena()
    {
        EdadAdulta = 50;
        ApetitoBase = 100;
        Dieta = Alimentacion.CARNIVORO;
        Habitat = Medio.TERRESTRE;
    }

    public override double CalcularIngestaRequerida(TipoActividad actividad)
    {
        // Fórmula exponencial del PDF: ApetitoBase * 2^(Edad-EdadAdulta)
        if (EdadActual < EdadAdulta) return ApetitoBase * EdadActual;
        int alfa = (actividad == TipoActividad.EXHIBICION) ? 1 : 15;
        return ApetitoBase * Math.Pow(2, (EdadActual - EdadAdulta)) * alfa;
    }
}
