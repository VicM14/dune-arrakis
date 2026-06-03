using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI errorText;
    public Button btnCargar;

    private bool esperandoCarga = false;

    void Start()
    {
        if (errorText != null) errorText.text = "";

        if (GameManager.Instance == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        GameManager.OnError += MostrarError;
        GameManager.OnEstadoActualizado += OnEstadoRecibido;
    }

    void OnDestroy()
    {
        GameManager.OnError -= MostrarError;
        GameManager.OnEstadoActualizado -= OnEstadoRecibido;
    }

    public void OnNuevaPartidaClick()
    {
        SceneManager.LoadScene("ScenarioSelect");
    }

    public void OnCargarPartidaClick()
    {
        esperandoCarga = true;
        if (errorText != null) errorText.text = "Conectando...";
        if (btnCargar != null) btnCargar.interactable = false;
        GameManager.Instance.CargarPartidaGuardada();
    }

    private void OnEstadoRecibido(PartidaData partida)
    {
        // Solo navegar si el usuario hizo click en Cargar explícitamente
        if (!esperandoCarga) return;
        esperandoCarga = false;
        SceneManager.LoadScene("GameView");
    }

    private void MostrarError(string msg)
    {
        esperandoCarga = false;
        if (errorText != null) errorText.text = msg;
        if (btnCargar != null) btnCargar.interactable = true;
    }
}
