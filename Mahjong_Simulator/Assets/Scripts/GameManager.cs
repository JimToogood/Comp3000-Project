using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    public GameObject tilePrefab;

    private List<Player> players;
    private Queue<MahjongTile> wall;

    void Start() {
        // Create tiles and shuffle
        List<MahjongTile> tiles = TileSpawner.CreateFullTileSet();
        Shuffle(tiles);

        // Create players
        players = new List<Player>();
        for (int i = 0; i < 4; i++) {
            players.Add(new Player(i));
        }

        // Deal hands to players (13 tiles each)
        int tileIndex = 0;
        for (int j = 0; j < 13; j++) {
            foreach (Player player in players) {
                player.hand.Add(tiles[tileIndex]);
                tileIndex++;
            }
        }

        // Add remaining tiles to the wall
        wall = new Queue<MahjongTile>();
        for (int i = tileIndex; i < tiles.Count; i++) {
            wall.Enqueue(tiles[i]);
        }

        // Spawn player tiles
        for (int i = 0; i < players.Count; i++) {
            SpawnHand(players[i].hand, i);
        }

        // Spawn wall
        SpawnWall(wall, 4);
    }

    private void SpawnHand(List<MahjongTile> hand, int z) {
        int i = 0;
        foreach (MahjongTile tile in hand) {
            Vector3 pos = new Vector3(2.1f * i, 0.5f, 6 * z);
            SpawnTile(tile, pos);
            i++;
        }
    }

    private void SpawnWall(Queue<MahjongTile> wall, int z) {
        int i = 0;
        foreach (MahjongTile tile in wall) {
            Vector3 pos = new Vector3(2.1f * (i / 7), 0.5f + (1.0f * (i % 7)), 6 * z);
            SpawnTile(tile, pos);
            i++;
        }
    }

    private void SpawnTile(MahjongTile tile, Vector3 pos) {
        var tileObject = Instantiate(tilePrefab, pos, Quaternion.identity);
        var tileView = tileObject.GetComponent<TileView>();
        tileView.SetTile(tile);
    }

    // Fisher–Yates shuffle
    private static void Shuffle(List<MahjongTile> tiles) {
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
