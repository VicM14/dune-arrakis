using System;

namespace Dune.Domain;

public class Criatura
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nombre { get; set; } = string.Empty;
    public TipoCriatura Tipo { get; set; }
    public double Salud { get; set; } = 100;
    public int EdadActual { get; set; } = 0;
    public int EdadAdulta { get; set; }
    public double ApetitoBase { get; set; }
    public int ContadorVecesFavorita { get; set; } = 0;

    // Constructor vacío para serialización JSON
    public Criatura() { }

    // Fórmula de Ingesta Requerida (Sección 3.5)
    public double CalcularIngestaRequerida(TipoActividad actividad)
    {
        if (EdadActual < EdadAdulta)
        {
            return ApetitoBase * EdadActual;
        }
        else
        {
            int alfa = (actividad == TipoActividad.EXHIBICION) ? 1 : 15;
            return ApetitoBase * Math.Pow(2, (EdadActual - EdadAdulta)) * alfa;
        }
    }

    // Lógica de actualización de salud (Sección 3.5)
    public void Alimentar(double cantidadIngerida, TipoActividad actividad)
    {
        double requerida = CalcularIngestaRequerida(actividad);
        double porcentaje = (requerida > 0) ? (cantidadIngerida / requerida) : 1;

        if (porcentaje < 0.25) Salud -= 30;
        else if (porcentaje < 0.75) Salud -= 20;
        else if (porcentaje < 1.00) Salud -= 10;
        else
        {
            Salud = Math.Min(100, Salud + 5);
        }

        if (Salud < 0) Salud = 0;
    }
}
