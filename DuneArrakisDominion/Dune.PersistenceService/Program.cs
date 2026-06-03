using System.Text.Json;
using Dune.Domain;
using Dune.Domain.DTOs;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


const string DataDir = "partidas";
Directory.CreateDirectory(DataDir);

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

static string RutaDe(string id) =>
    Path.Combine("partidas", $"partida_{id}.json");

// GUARDAR — escribe (o sobrescribe) la partida en su fichero por id.
app.MapPost("/persistir/guardar", async (Partida partida) =>
{
    if (string.IsNullOrWhiteSpace(partida.IdPartida))
        return Results.BadRequest(new { error = "La partida no tiene IdPartida." });

    partida.FechaModificacion = DateTime.UtcNow;
    string json = JsonSerializer.Serialize(partida, jsonOptions);
    await File.WriteAllTextAsync(RutaDe(partida.IdPartida), json);
    return Results.Ok(new { mensaje = "Partida guardada en disco.", id = partida.IdPartida, fecha = partida.FechaModificacion });
});

// CARGAR POR ID — recupera una partida concreta. Maneja fichero corrupto.
app.MapGet("/persistir/cargar/{id}", async (string id) =>
{
    string ruta = RutaDe(id);
    if (!File.Exists(ruta))
        return Results.NotFound($"No existe la partida {id}.");
    try
    {
        string json = await File.ReadAllTextAsync(ruta);
        var partida = JsonSerializer.Deserialize<Partida>(json);
        if (partida == null)
            return Results.Problem($"El fichero de la partida {id} está vacío o no es válido.");
        return Results.Content(json, "application/json");
    }
    catch (JsonException ex)
    {
        // Estado corrupto de carga (Sección 3.9 del PDF).
        return Results.Problem($"La partida {id} está corrupta y no puede leerse: {ex.Message}");
    }
});

// CARGAR ÚLTIMA — la partida modificada más recientemente (bootstrap y "continuar").
app.MapGet("/persistir/cargar-ultima", async () =>
{
    var partidas = await LeerTodasAsync();
    var ultima = partidas.OrderByDescending(p => p.FechaModificacion).FirstOrDefault();
    if (ultima == null)
        return Results.NotFound("No hay ninguna partida guardada.");
    string json = JsonSerializer.Serialize(ultima, jsonOptions);
    return Results.Content(json, "application/json");
});

// LISTAR — resúmenes de todas las partidas guardadas, de más reciente a más antigua.
app.MapGet("/persistir/listar", async () =>
{
    var partidas = await LeerTodasAsync();
    var resumenes = partidas
        .OrderByDescending(p => p.FechaModificacion)
        .Select(PartidaResumenDTO.DesdeDominio)
        .ToList();
    return Results.Ok(resumenes);
});

app.Run();

// Lee y deserializa todas las partidas válidas del directorio, ignorando las
// que estén corruptas para que un fichero dañado no rompa el listado completo.
async Task<List<Partida>> LeerTodasAsync()
{
    var resultado = new List<Partida>();
    if (!Directory.Exists("partidas")) return resultado;

    foreach (string ruta in Directory.EnumerateFiles("partidas", "partida_*.json"))
    {
        try
        {
            string json = await File.ReadAllTextAsync(ruta);
            var partida = JsonSerializer.Deserialize<Partida>(json);
            if (partida != null) resultado.Add(partida);
        }
        catch (JsonException)
        {
            // Fichero corrupto: se omite del listado (se podría registrar/auditar).
        }
    }
    return resultado;
}