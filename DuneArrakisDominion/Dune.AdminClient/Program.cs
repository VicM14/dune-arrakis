using System.Net.Http.Json;
using Dune.Domain;

Console.WriteLine("--- DUNE: ADMIN CLIENT ---");
using var client = new HttpClient();

// Simulamos la creación de una partida básica
var nuevaPartida = new Partida { NombreJugador = "Paul Atreides", Solaris = 50000 };

Console.WriteLine("Enviando orden de guardado...");
var response = await client.PostAsJsonAsync("http://localhost:5000/simulacion/guardar-actual", nuevaPartida);

if (response.IsSuccessStatusCode)
{
    Console.WriteLine("¡Servidor respondió: Partida Guardada!");
}
else
{
    Console.WriteLine($"Error: {response.StatusCode}");
    var errorContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Detalle: {errorContent}");
}
Console.ReadLine();

