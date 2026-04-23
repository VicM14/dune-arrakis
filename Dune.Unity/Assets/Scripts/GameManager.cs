using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    private string apiUrl = "http://localhost:5000/estado-inicial";


    void Start()
    {
        StartCoroutine(CargarDatosDesdeAPI());
    }

    IEnumerator CargarDatosDesdeAPI()
    {
        Debug.Log("Conectando con el servidor de Arrakis...");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("¡Datos recibidos!: " + webRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error de conexión: " + webRequest.error);
            }
        }
    }
}
