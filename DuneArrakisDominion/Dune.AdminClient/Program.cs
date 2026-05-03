using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using Dune.Domain;

Console.WriteLine("--- DUNE: IMPERIAL COMMAND CONSOLE ---");

using var client = new HttpClient();
const string SimUrl = "http://localhost:5000";
bool salir = false;

while (!salir)
{
    Console.WriteLine("\n========================================");
    Console.WriteLine("[1] Sembrar Datos (Arrakeen + Gusano)");
    Console.WriteLine("[2] EJECUTAR RONDA MENSUAL");
    Console.WriteLine("[3] Comprar Recursos (Agua/Especia)");
    Console.WriteLine("[4] Ver Estado Detallado");
    Console.WriteLine("[S] Salir");
    Console.WriteLine("========================================");
    Console.Write("Selecciona una opción: ");

    var opcion = Console.ReadLine()?.ToUpper();

    switch (opcion)
    {
        case "1": await SembrarDatos(client); break;
        case "2": await EjecutarRonda(client); break;
        case "3": await ComprarRecursos(client); break;
        case "4": await VerEstado(client); break;
        case "S": salir = true; break;
    }
}

async Task SembrarDatos(HttpClient client)
{
    var nuevaPartida = new Partida
    {
        NombreJugador = "Paul Atreides",
        Solaris = 100000,
        StockAgua = 1000,
        StockEspecia = 500,
        Enclaves = new List<Enclave>
        {
            new Enclave
            {
                Nombre = "Arrakeen",
                Nivel = 3,
                PoblacionVisitantes = 200,
                Instalaciones = new List<Instalacion>
                {
                    new Instalacion
                    {
                        Nombre = "Santuario de Shai-Hulud",
                        Tipo = TipoActividad.EXHIBICION,
                        Criaturas = new List<Criatura> { new GusanoDeArena { Nombre = "Hacedor Joven", EdadActual = 10 } }
                    }
                }
            }
        }
    };

    var response = await client.PostAsJsonAsync($"{SimUrl}/simulacion/guardar-actual", nuevaPartida);
    if (response.IsSuccessStatusCode) Console.WriteLine(">> Arrakis ha sido colonizada con éxito.");
}

async Task EjecutarRonda(HttpClient client)
{
    Console.WriteLine(">> Procesando ciclo planetario...");
    var response = await client.PostAsJsonAsync($"{SimUrl}/simulacion/ejecutar-ronda", new { });

    if (response.IsSuccessStatusCode)
    {
        var p = await response.Content.ReadFromJsonAsync<Partida>();
        Console.WriteLine($"\n--- INFORME MES {p?.MesActual} ---");
        Console.WriteLine($"Solaris: {p?.Solaris:F2} | Agua: {p?.StockAgua:F1} | Especia: {p?.StockEspecia:F1}");
        // Mostrar los últimos 2 eventos (Balance y posibles alertas)
        if (p?.RegistroEventos.Count >= 2)
        {
            Console.WriteLine($">> {p.RegistroEventos[^2]}");
            Console.WriteLine($">> {p.RegistroEventos[^1]}");
        }
    }
}

async Task ComprarRecursos(HttpClient client)
{
    Console.Write("Cantidad de AGUA a comprar (Coste: 2 Solaris/ud): ");
    double agua = double.Parse(Console.ReadLine() ?? "0");
    Console.Write("Cantidad de ESPECIA a comprar (Coste: 10 Solaris/ud): ");
    double especia = double.Parse(Console.ReadLine() ?? "0");

    var response = await client.PostAsJsonAsync($"{SimUrl}/simulacion/comprar-recursos?agua={agua}&especia={especia}", new { });

    if (response.IsSuccessStatusCode)
        Console.WriteLine(">> Suministros adquiridos y enviados a los almacenes.");
    else
        Console.WriteLine(">> Error: Fondos insuficientes en el Imperio.");
}

async Task VerEstado(HttpClient client)
{
    var p = await client.GetFromJsonAsync<Partida>($"{SimUrl}/estado-inicial");
    Console.WriteLine($"\n--- ESTADO DEL DOMINIO ---");
    Console.WriteLine($"Solaris: {p?.Solaris:F2} | Agua: {p?.StockAgua:F1} | Especia: {p?.StockEspecia:F1}");
    foreach (var e in p?.Enclaves ?? new())
    {
        Console.WriteLine($"\nEnclave: {e.Nombre} (Nivel {e.Nivel})");
        Console.WriteLine($"  Visitantes: {e.PoblacionVisitantes}");
        foreach (var i in e.Instalaciones)
        {
            Console.WriteLine($"  - Instalación: {i.Nombre} ({i.Tipo})");
            foreach (var c in i.Criaturas)
                Console.WriteLine($"    * Criatura: {c.Nombre} | Salud: {c.Salud}% | Edad: {c.EdadActual}");
        }
    }
}
