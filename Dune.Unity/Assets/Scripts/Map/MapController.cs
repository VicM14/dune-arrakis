using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Image = UnityEngine.UI.Image;

public class MapController : MonoBehaviour
{
    [Header("Contenedores")]
    public RectTransform gridContainer;           // Grid Layout Group con los tiles
    public RectTransform installationsContainer;  // Capa encima para las instalaciones

    [Header("Prefabs")]
    public GameObject tilePrefab;           // Image 32x32
    public GameObject installationPrefab;   // InstallationSprite prefab

    [Header("Sprites de tiles (opcional)")]
    public Sprite spriteSandBase;
    public Sprite spriteSandDark;
    public Sprite spriteRock;

    [Header("Sprites de instalaciones (opcional)")]
    public Sprite spriteRocaSellada;
    public Sprite spriteEscudoEstatico;
    public Sprite spriteCuplaBlindada;
    public Sprite spritePozoReforzado;

    [Header("Dimensiones del grid")]
    public int columnas = 9;
    public int filas = 7;
    public int tileSize = 32;

    private readonly List<GameObject> tiles = new();
    private readonly List<GameObject> instalObjetos = new();

    void Start()
    {
        GameManager.OnEstadoActualizado += Actualizar;
        if (GameManager.Instance?.PartidaActual != null)
            Actualizar(GameManager.Instance.PartidaActual);
    }

    void OnDestroy() => GameManager.OnEstadoActualizado -= Actualizar;

    public void Actualizar(PartidaData partida)
    {
        GenerarTiles();
        ColocarInstalaciones(partida);
    }

    // ─── Tiles de fondo ──────────────────────────────────────────────────

    private void GenerarTiles()
    {
        foreach (var t in tiles) Destroy(t);
        tiles.Clear();

        if (gridContainer == null || tilePrefab == null) return;

        for (int f = 0; f < filas; f++)
        {
            for (int c = 0; c < columnas; c++)
            {
                var go = Instantiate(tilePrefab, gridContainer);
                var img = go.GetComponent<Image>();
                if (img == null) continue;

                // Variación aleatoria determinista para dar textura
                bool oscuro = (f + c) % 7 == 0 || (f * c) % 11 == 0;

                if (oscuro && spriteSandDark != null)
                    img.sprite = spriteSandDark;
                else if (!oscuro && spriteSandBase != null)
                    img.sprite = spriteSandBase;
                else
                    img.color = oscuro
                        ? new Color(0.55f, 0.37f, 0.24f)  // #8B5E3C
                        : new Color(0.77f, 0.58f, 0.35f); // #C4945A

                tiles.Add(go);
            }
        }
    }

    // ─── Instalaciones ───────────────────────────────────────────────────

    private void ColocarInstalaciones(PartidaData partida)
    {
        foreach (var go in instalObjetos) Destroy(go);
        instalObjetos.Clear();

        if (installationsContainer == null || installationPrefab == null) return;
        if (partida.Enclaves == null) return;

        int slot = 0;
        foreach (var enclave in partida.Enclaves)
        {
            if (enclave.Instalaciones == null) continue;
            foreach (var inst in enclave.Instalaciones)
            {
                var go = Instantiate(installationPrefab, installationsContainer);

                var sprite = go.GetComponent<InstallationSprite>();
                Color color = inst.Tipo == 0
                    ? new Color(0.32f, 0.53f, 1f)    // azul — aclimatación
                    : new Color(1f, 0.80f, 0.47f);   // dorado — exhibición

                sprite?.Inicializar(inst, ObtenerSprite(inst.TipoRecinto), color);

                // Posición en el grid: columnas de izquierda a derecha
                int col = slot ;
                int fila = slot ;
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 1);   // top-left
                    rt.anchorMax = new Vector2(0, 1);   // top-left
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(
                        col * tileSize + tileSize * 0.5f,
                        -(fila * tileSize + tileSize * 0.5f)
                    );
                }
                instalObjetos.Add(go);
                slot++;
            }
        }
    }

    private Sprite ObtenerSprite(int tipoRecinto) => tipoRecinto switch
    {
        0 => spriteRocaSellada,
        1 => spriteEscudoEstatico,
        2 => spriteCuplaBlindada,
        3 => spritePozoReforzado,
        _ => null
    };
}
