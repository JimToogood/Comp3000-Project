using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public struct SeatLayout {
    public Vector3 basePos;
    public Vector3 tileDirection;
    public Quaternion rotation;
}


public class GameManager : MonoBehaviour {
    // Set instance so game manager can be called in other classes
    public static GameManager Instance { get; private set; }
    void Awake() { Instance = this; }

    [SerializeField] private bool debugMode = false;

    private List<Player> players;
    private Queue<MahjongTile> wall;

    private MahjongTile currentDrawnTile;
    private MahjongTile lastDiscardedTile;

    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;


    private void Start() {
        List<MahjongTile> tiles = new();
        int tileIndex = 0;

        while (true) {
            // Create tiles and shuffle
            tiles = TileSpawner.CreateFullTileSet();
            Shuffle(tiles);

            // Create players
            players = new List<Player>();
            for (int i = 0; i < 4; i++) {
                players.Add(new Player(i));
            }

            // Deal hands to players (13 tiles each)
            tileIndex = 0;
            for (int j = 0; j < 13; j++) {
                foreach (Player player in players) {
                    player.hand.Add(tiles[tileIndex]);
                    tileIndex++;
                }
            }

            if (!debugMode) { break; }

            // CURRENT DEBUG SCENARIO: Check if player 0 has a concealed Kan
            bool found = false;
            Player testPlayer = players[0];

            foreach (MahjongTile tile in testPlayer.hand) {
                int count = 0;
                foreach (MahjongTile t in testPlayer.hand) {
                    if (t.IsSameTile(tile)) { count++; }
                }

                if (count == 4) {
                    Debug.Log("!! DEBUG SCENARIO FOUND !!");
                    found = true;
                    break;
                }
            }

            if (found) { break; }
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
        Player currentPlayer = players[currentPlayerIndex];

        TableManager.Instance.RefreshPlayerVisuals(currentPlayer);

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

        if (CheckKanUpgrade(player, currentDrawnTile)) { return; }
        if (CheckConcealedKan(player)) { return; }
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

        Player discardingPlayer = players[currentPlayerIndex];

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

                TableManager.Instance.RefreshPlayerVisuals(discardingPlayer);

                Debug.Log($"Player {seatToCheck} can Kan tile {playerToCheck.callTile.id}");
                
                // TODO: Change this automatic call to player choice
                ResolveCall(playerToCheck, null);
                return;
            }
            // If player has 2 or more identical tiles, then they can Pon
            else if (matchCount == 2) {
                playerToCheck.pendingCall = CallType.Pon;
                playerToCheck.callTile = lastDiscardedTile;

                TableManager.Instance.RefreshPlayerVisuals(discardingPlayer);

                Debug.Log($"Player {seatToCheck} can Pon tile {playerToCheck.callTile.id}");

                ResolveCall(playerToCheck, null);
                return;
            }
            // If player is next in turn order, check if they can Chi
            else if (i == 1) {
                List<MahjongTile> chiTiles = GetChiTiles(playerToCheck, lastDiscardedTile);

                if (chiTiles != null) {
                    playerToCheck.pendingCall = CallType.Chi;
                    playerToCheck.callTile = lastDiscardedTile;

                    TableManager.Instance.RefreshPlayerVisuals(discardingPlayer);

                    Debug.Log($"Player {seatToCheck} can Chi tile {playerToCheck.callTile.id}");

                    ResolveCall(playerToCheck, chiTiles);
                    return;
                }
            }
        }

        Debug.Log($"No calls found. Ending turn...");
        EndTurn();
    }

    private void ResolveCall(Player player, List<MahjongTile> chiTiles) {
        // TODO: Add logic to resolve Chi
        currentPlayerIndex = player.seat;
        currentDrawnTile = null;
        bool isKan = player.pendingCall == CallType.Kan;
        bool isChi = chiTiles != null;

        // Add called tile to player's melds
        Meld newMeld = new Meld(new List<MahjongTile>());
        newMeld.tiles.Add(player.callTile);

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

        if (isChi) {
            tilesToMove = chiTiles;
        } else {
            foreach (MahjongTile tile in player.hand) {
                if (tile.IsSameTile(player.callTile)) {
                    tilesToMove.Add(tile);
                    if (tilesToMove.Count >= tilesNeeded) { break; }
                }
            }
        }

        // Remove tiles from hand in seperate pass to avoid InvalidOperationException
        foreach (MahjongTile tile in tilesToMove) {
            player.hand.Remove(tile);
            newMeld.tiles.Add(tile);
        }

        // If call is Chi, sort new meld
        if (isChi) { TableManager.Instance.SortHand(newMeld.tiles); }
        
        player.melds.Add(newMeld);

        TableManager.Instance.RefreshPlayerVisuals(player);

        player.callTile = null;
        player.pendingCall = null;

        StartTurn(isKan);   // If call is a Kan, draw a Kan tile, else don't draw a tile
    }

    private bool CheckKanUpgrade(Player player, MahjongTile tile) {
        // Check for an existing Pon to upgrade into a Kan
        foreach (Meld meld in player.melds) {
            if (meld.tiles.Count == 3 && meld.tiles.All(t => t.IsSameTile(tile))) {
                // Upgrade Pon to Kan
                player.hand.Remove(tile);
                meld.tiles.Add(tile);

                Debug.Log($"Player {player.seat} upgrades Pon to Kan with tile {tile.id}");
                TableManager.Instance.RefreshPlayerVisuals(player);

                DrawTile(player);
                waitingForDiscard = true;
                return true;
            }
        }

        return false;
    }

    private bool CheckConcealedKan(Player player) {
        // Check for an existing Pon to upgrade into a Kan
        foreach (MahjongTile tile in player.hand) {
            List<MahjongTile> matchingTiles = new();

            foreach (MahjongTile t in player.hand) {
                if (t.IsSameTile(tile)) {
                    matchingTiles.Add(t);
                }
            }

            // If matching tiles count is 4, then we have found a concealed kan
            if (matchingTiles.Count == 4) {
                foreach (MahjongTile t in matchingTiles) {
                    player.hand.Remove(t);
                }

                player.melds.Add(new Meld(new List<MahjongTile>(matchingTiles), true));

                Debug.Log($"Player {player.seat} declares a Concealed Kan.");
                TableManager.Instance.RefreshPlayerVisuals(player);

                DrawTile(player);
                waitingForDiscard = true;
                return true;
            }
        }

        return false;
    } 

    private static List<MahjongTile> GetChiTiles(Player player, MahjongTile tile) {
        // If tile is a wind or a dragon, it cannot Chi
        if (tile.suit is not (TileSuit.Characters or TileSuit.Bamboo or TileSuit.Dots)) { return null; }

        int n = tile.number;

        MahjongTile minus2 = null;
        MahjongTile minus1 = null;
        MahjongTile plus2 = null;
        MahjongTile plus1 = null;

        foreach (MahjongTile t in player.hand) {
            // Only tiles of the same suit can Chi
            if (t.suit != tile.suit) { continue; }

            if (t.number == n - 2 && minus2 == null) { minus2 = t; }
            if (t.number == n - 1 && minus1 == null) { minus1 = t; }
            if (t.number == n + 2 && plus2 == null) { plus2 = t; }
            if (t.number == n + 1 && plus1 == null) { plus1 = t; }
        }

        if (minus2 != null && minus1 != null) {
            return new List<MahjongTile> { minus2, minus1 };
        } else if (minus1 != null && plus1 != null) {
            return new List<MahjongTile> { minus1, plus1 };
        } else if (plus1 != null && plus2 != null) {
            return new List<MahjongTile> { plus1, plus2 };
        }
        
        return null;
    }

    private static void Shuffle(List<MahjongTile> tiles) {
        // Fisher–Yates shuffle
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
