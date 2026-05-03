using System.Text.Json;
using Dune.Domain;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Ruta del archivo donde se guardará la partida
const string FilePath = "partida_arrakis.json";

// Endpoint para ALMACENAR el estado (POST)
app.MapPost("/persistir/guardar", async (Partida partida) => {
    var options = new JsonSerializerOptions { WriteIndented = true };
    string jsonString = JsonSerializer.Serialize(partida, options);
    await File.WriteAllTextAsync(FilePath, jsonString);
    return Results.Ok(new { mensaje = "Partida guardada en disco con éxito", fecha = DateTime.Now });
});

// Endpoint para RECUPERAR la información (GET)
app.MapGet("/persistir/cargar", async () => {
    if (!File.Exists(FilePath)) return Results.NotFound("No se encontró ninguna partida guardada.");
    string jsonString = await File.ReadAllTextAsync(FilePath);
    return Results.Content(jsonString, "application/json");
});

app.Run();
