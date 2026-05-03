namespace Dune.Domain;

public class Visitante
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NivelAdquisitivo Nivel { get; set; }
    public double ProbabilidadDonacion { get; set; }

    public Visitante(NivelAdquisitivo nivel)
    {
        Nivel = nivel;
        ProbabilidadDonacion = nivel switch
        {
            NivelAdquisitivo.ALTO => 0.8,
            NivelAdquisitivo.MEDIO => 0.5,
            _ => 0.2
        };
    }
}
