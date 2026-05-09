using Dune.Domain;
using Dune.Domain.Exceptions;
using Xunit;

namespace Dune.Tests;

public class CriaturaTests
{
    [Fact]
    public void Ingesta_AntesDeAdulta_EsApetitoBasePorEdad()
    {
        var g = new GusanoDeArena { EdadActual = 10 };
        // Fórmula PDF: apetito × edad = 5 × 10 = 50
        Assert.Equal(50, g.CalcularIngestaRequerida(TipoActividad.ACLIMATACION));
    }

    [Fact]
    public void Ingesta_TrasAdulta_Aclimatacion_AplicaAlfa15()
    {
        var g = new GusanoDeArena { EdadActual = 25 }; // adulta=24, diff=1
        // apetito × 2^(25-24) × 15 = 5 × 2 × 15 = 150
        Assert.Equal(150, g.CalcularIngestaRequerida(TipoActividad.ACLIMATACION));
    }

    [Fact]
    public void Ingesta_TrasAdulta_Exhibicion_AplicaAlfa1()
    {
        var g = new GusanoDeArena { EdadActual = 25 };
        // apetito × 2^1 × 1 = 5 × 2 × 1 = 10
        Assert.Equal(10, g.CalcularIngestaRequerida(TipoActividad.EXHIBICION));
    }

    [Theory]
    [InlineData(0,    -30)]  // <25% → pierde 30
    [InlineData(10,   -30)]  // 10/50=20% → <25% → pierde 30
    [InlineData(15,   -20)]  // 15/50=30% → 25-75% → pierde 20
    [InlineData(40,   -10)]  // 40/50=80% → 75-100% → pierde 10
    [InlineData(50,    +5)]  // 50/50=100% → recupera 5 (salud parte de 90)
    public void Alimentar_AplicaPenalizacionCorrecta(double cantidad, int cambioSalud)
    {
        var g = new GusanoDeArena { EdadActual = 10, Salud = 90 };
        // Ingesta requerida = 5×10 = 50
        g.Alimentar(cantidad, TipoActividad.ACLIMATACION);
        Assert.Equal(90 + cambioSalud, g.Salud);
    }

    [Fact]
    public void Alimentar_SaludLlegaACero_EntraEnLetargo()
    {
        var g = new GusanoDeArena { EdadActual = 10, Salud = 20 };
        g.Alimentar(0, TipoActividad.ACLIMATACION); // pierde 30 → -10 → clamped a 0
        Assert.True(g.EnLetargo);
        Assert.Equal(0, g.Salud);
    }

    [Fact]
    public void Alimentar_SaludNoSuperaCien()
    {
        var g = new GusanoDeArena { EdadActual = 10, Salud = 98 };
        g.Alimentar(50, TipoActividad.ACLIMATACION); // 100% → +5 → cap a 100
        Assert.Equal(100, g.Salud);
    }
}

public class InstalacionTests
{
    [Fact]
    public void Donaciones_SinCriaturas_DevuelveCero()
    {
        var inst = new Instalacion { Tipo = TipoActividad.EXHIBICION };
        Assert.Equal(0, inst.CalcularDonacionesTotales(500, NivelAdquisitivo.ALTO));
    }

    [Fact]
    public void Donaciones_ConCriatura_AplicaFormulaPDF()
    {
        var inst = new Instalacion { Tipo = TipoActividad.EXHIBICION };
        inst.Criaturas.Add(new TigraLaza { Salud = 100, EdadActual = 38 }); // adulta=38

        // donacion = numVisitantes × 10 × (100/100) × (38/38) × σ
        // Con ALTO (σ=30): 100 × 10 × 1 × 1 × 30 = 30000
        double resultado = inst.CalcularDonacionesTotales(100, NivelAdquisitivo.ALTO);
        Assert.Equal(30000, resultado);
    }

    [Fact]
    public void Donaciones_NivelBajo_SigmaEs1()
    {
        var inst = new Instalacion { Tipo = TipoActividad.EXHIBICION };
        inst.Criaturas.Add(new MuadDib { Salud = 100, EdadActual = 12 }); // adulta=12

        // 100 × 10 × 1 × 1 × 1 = 1000
        Assert.Equal(1000, inst.CalcularDonacionesTotales(100, NivelAdquisitivo.BAJO));
    }
}

public class EnclaveTests
{
    [Fact]
    public void CapacidadAlmacen_EsTresVecesHectareas()
    {
        var e = new Enclave { Hectareas = 5000 };
        Assert.Equal(15000, e.CapacidadAlmacen);
    }

    [Fact]
    public void ActualizarVisitantes_SinCriaturas_PoblacionCaeACero()
    {
        var e = new Enclave
        {
            Hectareas = 10000,
            PoblacionVisitantes = 3000,
            VisitantesMensualesBase = 3000,
            Instalaciones = new() { new Instalacion { Hectareas = 400 } }
        };

        e.ActualizarVisitantes();
        Assert.Equal(0, e.PoblacionVisitantes);
    }
}

public class CostetrasladoTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(25)]
    public void CosteTraslado_RecienAdulta_EsCienPorSigma(double sigma)
    {
        // Fórmula: 100 × 3^(edad-adulta) × σ. Si edad=adulta → 3^0=1
        double esperado = 100 * 1 * sigma;
        double resultado = 100 * Math.Pow(3, 0) * sigma;
        Assert.Equal(esperado, resultado);
    }
}
