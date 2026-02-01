using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }
    void Awake() { Instance = this; }

    public GameObject tilePrefab;

    private Dictionary<MahjongTile, TileView> tileViews;
    private List<Player> players;
    private Queue<MahjongTile> wall;

    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;

    void Start() {
        // Create tiles and shuffle
        List<MahjongTile> tiles = TileSpawner.CreateFullTileSet();
        Shuffle(tiles);
        
        tileViews = new Dictionary<MahjongTile, TileView>();

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

        // Spawn tiles
        for (int i = 0; i < players.Count; i++) {
            SpawnHand(players[i].hand, i);
        }
        SpawnWall(wall, players.Count);

        // Start first turn
        StartTurn();
    }

    private void StartTurn() {
        Debug.Log($"Player {currentPlayerIndex}'s turn begins.");
        waitingForDiscard = true;
    }

    private void EndTurn() {
        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count) { currentPlayerIndex = 0; }
        StartTurn();
    }

    public void OnTileClicked(TileView tileView) {
        Debug.Log($"Clicked Tile ID {tileView.tileData.id}");
        if (!waitingForDiscard) { return; }

        MahjongTile tile = tileView.tileData;
        Player currentPlayer = players[currentPlayerIndex];

        if (!currentPlayer.hand.Contains(tile)) { Debug.Log($"That tile does not belong to the current player!"); return; }

        DiscardTile(currentPlayer, tile, tileView);
    }

    private void DiscardTile(Player player, MahjongTile tile, TileView tileView) {
        if (wall.Count <= 0) {
            Debug.Log("Wall is empty");
            return;
        }

        Debug.Log($"Player {player.seat} discards tile {tile.id}");

        player.hand.Remove(tile);
        player.discards.Add(tile);

        // Draw next tile from wall and add to player's hand
        MahjongTile newTile = wall.Dequeue();
        player.hand.Add(newTile);
        
        // Move new tile to where discarded tile was
        MoveTile(tileViews[newTile], tileViews[tile].transform.position);

        // TODO: Move to discard pile instead of destroying
        Destroy(tileView.gameObject);
        waitingForDiscard = false;

        EndTurn();
    }

    private void MoveTile(TileView tileView, Vector3 pos) {
        tileView.gameObject.transform.position = pos;
    }

    private void SpawnHand(List<MahjongTile> hand, int z) {
        int i = 0;
        foreach (MahjongTile tile in hand) {
            Vector3 pos = new Vector3(2.1f * i, 0.5f, 6 * z);
            SpawnTile(tile, pos, Quaternion.identity);
            i++;
        }
    }

    private void SpawnWall(Queue<MahjongTile> wall, int z) {
        int i = 0;
        foreach (MahjongTile tile in wall) {
            Vector3 pos = new Vector3(2.1f * (i / 7), 0.5f + (1.0f * (i % 7)), 6 * z);
            SpawnTile(tile, pos, Quaternion.identity);
            i++;
        }
    }

    private void SpawnTile(MahjongTile tile, Vector3 pos, Quaternion rot) {
        var tileObject = Instantiate(tilePrefab, pos, rot);
        var tileView = tileObject.GetComponent<TileView>();

        tileView.SetTile(tile);
        tileViews[tile] = tileView;
    }

    // Fisher–Yates shuffle
    private static void Shuffle(List<MahjongTile> tiles) {
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
