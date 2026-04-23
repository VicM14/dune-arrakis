using Dune.Domain;

var builder = WebApplication.CreateBuilder(args);

// --- AÑADE ESTO AQUÍ ---
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

app.Run();


