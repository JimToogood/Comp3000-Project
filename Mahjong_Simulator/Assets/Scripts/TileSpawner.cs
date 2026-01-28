using UnityEngine;

public class TileSpawner : MonoBehaviour {
    public GameObject tilePrefab;
    private int nextTileID = 0;

    void Start() {
        SpawnSuit(TileSuit.Bamboo, 0);
        SpawnSuit(TileSuit.Characters, 4);
        SpawnSuit(TileSuit.Dots, 8);

        SpawnDragons(12);
        SpawnWinds(16);
    }

    private void SpawnSuit(TileSuit suit, int rowOffset) {
        // Spawn tiles 1-9 in given suit (x4)
        for (int i = 0; i < 9; i++) {
            for (int j = 0; j < 4; j++) {
                // Generate tile class
                MahjongTile tile = new MahjongTile{
                    id = nextTileID,
                    suit = suit,
                    number = i + 1,
                    wind = WindType.None,
                    dragon = DragonType.None
                };
                nextTileID++;

                // Calculate tile position
                Vector3 pos = new Vector3(
                    3 * i,
                    0.5f + j,
                    rowOffset
                );

                // Spawn tile
                SpawnTile(tile, pos);
            }
        }
    }

    private void SpawnDragons(int rowOffset) {
        // Spawn all 3 dragons (x4)
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 4; j++) {
                // Generate tile class
                MahjongTile tile = new MahjongTile{
                    id = nextTileID,
                    suit = TileSuit.Dragons,
                    number = 0,
                    wind = WindType.None,
                    dragon = (DragonType)i + 1
                };
                nextTileID++;

                // Calculate tile position
                Vector3 pos = new Vector3(
                    3 * i,
                    0.5f + j,
                    rowOffset
                );

                // Spawn tile
                SpawnTile(tile, pos);
            }
        }
    }
    
    private void SpawnWinds(int rowOffset) {
        // Spawn all 4 winds (x4)
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 4; j++) {
                // Generate tile class
                MahjongTile tile = new MahjongTile{
                    id = nextTileID,
                    suit = TileSuit.Winds,
                    number = 0,
                    wind = (WindType)i + 1,
                    dragon = DragonType.None
                };
                nextTileID++;

                // Calculate tile position
                Vector3 pos = new Vector3(
                    3 * i,
                    0.5f + j,
                    rowOffset
                );

                // Spawn tile
                SpawnTile(tile, pos);
            }
        }
    }

    private void SpawnTile(MahjongTile tileData, Vector3 pos) {
        var tileObject = Instantiate(tilePrefab, pos, Quaternion.identity);
        var tileView = tileObject.GetComponent<TileView>();
        tileView.SetTile(tileData);
    }
}
