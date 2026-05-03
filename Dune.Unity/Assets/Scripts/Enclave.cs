using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dune.Domain;

[System.Serializable]
public class Enclave
{
    public string id;
    public string nombre;
    public int hectareas;
    public int poblacionVisitantes;
    public int visitantesMensualesBase;
    public string nivelAdquisitivo;
    public string tipoEnclave;
    public List<Instalacion> instalaciones;
}