using System;
using System.Net.Http.Json;
using Dune.Domain;

Console.WriteLine("--- DUNE: ADMIN CLIENT (MODO BUCLE) ---");

using var client = new HttpClient();
bool continuar = true;

while (continuar)
{
    Console.WriteLine("\nOpciones: [ENTER] Ejecutar Ronda | [S] Salir");
    var tecla = Console.ReadKey(true);

    if (tecla.Key == ConsoleKey.Enter)
    {
        Console.WriteLine("Ejecutando ronda...");
        var response = await client.PostAsJsonAsync("http://localhost:5000/simulacion/ejecutar-ronda", new { });

        if (response.IsSuccessStatusCode)
        {
            var partida = await response.Content.ReadFromJsonAsync<Partida>();
            Console.WriteLine($"¡Ronda completada! Mes actual: {partida?.MesActual}");
            Console.WriteLine($"Solaris: {partida?.Solaris:F2}");
            Console.WriteLine($"Eventos: {partida?.RegistroEventos.Count}");
        }
        else
        {
            Console.WriteLine("Error en la comunicación.");
        }
    }
    else if (tecla.Key == ConsoleKey.S)
    {
        continuar = false;
    }
}


