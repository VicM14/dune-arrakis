using Dune.Domain;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
// -----------------------

var app = builder.Build();

// --- Y ESTO AQUÍ (Antes de los MapGet) ---
app.UseCors("AllowUnity");
// -----------------------------------------

app.MapGet("/estado-inicial", () =>
{
    return new
    {
        Escenario = "Arrakeen: Dominio de la Especia",
        Solaris = 100000,
        Mensaje = "Conexión exitosa con el Imperio"
    };
});
// Necesitarás añadir HttpClient al builder al principio del archivo:
// builder.Services.AddHttpClient();

app.MapPost("/simulacion/guardar-actual", async (Partida partida, IHttpClientFactory clientFactory) => {
    var client = clientFactory.CreateClient();
    // Cambia el puerto (5001) por el que use tu PersistenceService
    var response = await client.PostAsJsonAsync("http://localhost:5032/persistir/guardar", partida);

    if (response.IsSuccessStatusCode) return Results.Ok("Simulación guardada correctamente.");
    return Results.Problem("Error al conectar con el servicio de persistencia.");
});


app.Run();


