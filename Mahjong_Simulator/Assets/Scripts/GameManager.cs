using UnityEngine;
using System.Collections.Generic;

public struct SeatLayout {
    public Vector3 basePos;
    public Vector3 tileDirection;
    public Quaternion rotation;
}


public class GameManager : MonoBehaviour {
    // Set instance so game manager can be called in other classes
    public static GameManager Instance { get; private set; }
    void Awake() { Instance = this; }

    private List<Player> players;
    private Queue<MahjongTile> wall;

    private MahjongTile currentDrawnTile;
    private MahjongTile lastDiscardedTile;

    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;


    private void Start() {
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

        TableManager.Instance.SetupTable(players, wall);

        // Start first turn
        StartTurn(true);
    }

    private void StartTurn(bool drawTile) {
        TableManager.Instance.MoveCamera(currentPlayerIndex);

        Debug.Log($"Player {currentPlayerIndex} turn begins");

        Player currentPlayer = players[currentPlayerIndex];

        if (drawTile) { DrawTile(currentPlayer); }
        waitingForDiscard = true;
    }

    private void EndTurn() {
        TableManager.Instance.LayoutHand(players[currentPlayerIndex]);
        TableManager.Instance.LayoutMelds(players[currentPlayerIndex]);

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn(true);
    }

    public void OnTileClicked(TileView tileView) {
        if (!waitingForDiscard) { return; }

        MahjongTile tile = tileView.tileData;
        Player currentPlayer = players[currentPlayerIndex];

        if (!currentPlayer.hand.Contains(tile)) { Debug.Log($"That tile is not in the current player's hand!"); return; }

        DiscardTile(currentPlayer, tile);
    }

    private void DrawTile(Player player) {
        if (wall.Count <= 0) {
            Debug.Log("Wall is empty");
            return;
        }

        // Draw next tile from wall and add to player's hand
        currentDrawnTile = wall.Dequeue();
        player.hand.Add(currentDrawnTile);

        Debug.Log($"Player {player.seat} draws tile {currentDrawnTile.id}");

        // Move drawn tile to above player's hand
        TableManager.Instance.AnimateDraw(player, currentDrawnTile);
    }

    private void DiscardTile(Player player, MahjongTile tile) {
        Debug.Log($"Player {player.seat} discards tile {tile.id}");

        player.hand.Remove(tile);
        player.discards.Add(tile);
        lastDiscardedTile = tile;

        TableManager.Instance.AnimateDiscard(tile, currentDrawnTile);

        waitingForDiscard = false;

        CheckForCalls();
    }
    
    private void CheckForCalls() {
        Debug.Log($"Checking for calls...");

        // Check every player except player who just discarded
        for (int i = 1; i < players.Count; i++) {
            int seatToCheck = (currentPlayerIndex + i) % players.Count;
            Player playerToCheck = players[seatToCheck];

            // Find how many tiles matching the discard that the player has in their hand
            int matchCount = 0;
            foreach (MahjongTile tile in playerToCheck.hand) {
                if (tile.IsSameTile(lastDiscardedTile)) {
                    matchCount++;
                }
            }

            // If player has 3 or more identical tiles, then they can Kan
            if (matchCount >= 3) {
                playerToCheck.pendingCall = CallType.Kan;
                playerToCheck.callTile = lastDiscardedTile;

                Debug.Log($"Player {seatToCheck} can Kan tile {playerToCheck.callTile.id}");
                // TODO: Change this automatic call to player choice
                ResolveCall(playerToCheck);
                return;
            }
            // If player has 2 or more identical tiles, then they can Pon
            else if (matchCount == 2) {
                playerToCheck.pendingCall = CallType.Pon;
                playerToCheck.callTile = lastDiscardedTile;

                Debug.Log($"Player {seatToCheck} can Pon tile {playerToCheck.callTile.id}");
                ResolveCall(playerToCheck);
                return;
            }
            // If player is next in turn order, check if they can Chi
            /*else if (i == 1 && CanChi(playerToCheck, lastDiscardedTile)) {
                playerToCheck.pendingCall = CallType.Chi;
                playerToCheck.callTile = lastDiscardedTile;

                Debug.Log($"Player {seatToCheck} can Chi tile {playerToCheck.callTile.id}");
                ResolveCall(playerToCheck);
                return;
            }*/
        }

        Debug.Log($"No calls found. Ending turn...");
        EndTurn();
    }

    private void ResolveCall(Player player) {
        // TODO: Add logic to resolve Chi
        currentPlayerIndex = player.seat;
        currentDrawnTile = null;
        bool isKan = player.pendingCall == CallType.Kan;

        // Add called tile to player's melds
        player.melds.Add(player.callTile);

        // Calculate how many tiles need to be moved from players hand to players melds
        int tilesNeeded;
        if (isKan) {
            Debug.Log($"Player {player.seat} Kans tile {player.callTile.id}");
            tilesNeeded = 3;
        } else {
            Debug.Log($"Player {player.seat} Pons tile {player.callTile.id}");
            tilesNeeded = 2;
        }

        // Find tiles from hand
        List<MahjongTile> tilesToMove = new();

        foreach (MahjongTile tile in player.hand) {
            if (tile.IsSameTile(player.callTile)) {
                tilesToMove.Add(tile);
                if (tilesToMove.Count >= tilesNeeded) { break; }
            }
        }

        // Remove tiles from hand in seperate pass to avoid InvalidOperationException
        foreach (MahjongTile tile in tilesToMove) {
            player.hand.Remove(tile);
            player.melds.Add(tile);
        }

        TableManager.Instance.LayoutHand(player);
        TableManager.Instance.LayoutMelds(player);

        player.callTile = null;
        player.pendingCall = null;

        StartTurn(isKan);   // If call is a Kan, draw a Kan tile, else don't draw a tile
    }

    private static bool CanChi(Player player, MahjongTile tile) {
        // If tile is a wind or a dragon, it cannot Chi
        if (tile.suit is not (TileSuit.Characters or TileSuit.Bamboo or TileSuit.Dots)) { return false; }

        int n = tile.number;

        bool hasMinus2 = false;
        bool hasMinus1 = false;
        bool hasPlus2 = false;
        bool hasPlus1 = false;

        foreach (MahjongTile t in player.hand) {
            // Only tiles of the same suit can Chi
            if (t.suit != tile.suit) { continue; }

            if (t.number == n - 2) { hasMinus2 = true; }
            if (t.number == n - 1) { hasMinus1 = true; }
            if (t.number == n + 2) { hasPlus2 = true; }
            if (t.number == n + 1) { hasPlus1 = true; }
        }

        if (hasMinus2 && hasMinus1) { return true; }
        else if (hasMinus1 && hasPlus1) { return true; }
        else if (hasPlus1 && hasPlus2) { return true; }
        else { return false; }
    }

    private static void Shuffle(List<MahjongTile> tiles) {
        // Fisher–Yates shuffle
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
