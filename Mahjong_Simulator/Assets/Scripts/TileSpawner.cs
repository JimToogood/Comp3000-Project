using UnityEngine;

public class TileSpawner : MonoBehaviour {
    public GameObject tilePrefab;
    private Texture2D[] tileFaces;

    void Start() {
        // Load all tile face textures from folder
        tileFaces = Resources.LoadAll<Texture2D>("tile_faces");

        // Spawn all tiles
        int i = 0;
        for (int row = 0; row < (tileFaces.Length / 9.0f); row++) {
            for (int column = 0; column < 9; column++) {
                if (i > tileFaces.Length - 1) {
                    break;
                }

                SpawnTile(tileFaces[i], new Vector3(3 * column, 0.5f, 4 * row));
                i++;
            }
        }
    }

    void SpawnTile(Texture2D faceTexture, Vector3 pos) {
        var tileObject = Instantiate(tilePrefab, pos, Quaternion.identity);
        var tile = tileObject.GetComponent<Tile>();
        tile.SetFaceTexture(faceTexture);
    }
}
