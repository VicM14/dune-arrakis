using Dune.SimulationService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// SERVICIOS (Dependency Injection)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Estado de la simulación: Singleton (una sola partida activa por proceso).
builder.Services.AddSingleton<SimulationState>();

// Cliente del servicio de persistencia: registrado como HttpClient tipado.
builder.Services.AddHttpClient<IPersistenceClient, PersistenceClient>();

// CORS para Unity y cualquier frontend.
builder.Services.AddCors(options =>
    options.AddPolicy("AllowUnity",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// MIDDLEWARE
// ─────────────────────────────────────────────────────────────────────────────
app.UseCors("AllowUnity");
app.MapControllers();

app.Run();
