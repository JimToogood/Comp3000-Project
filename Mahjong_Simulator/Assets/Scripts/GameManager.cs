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

    private bool isPaused = true;
    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;
    

    public void TogglePause(bool paused) {
        isPaused = paused;

        if (!isPaused) {
            Time.timeScale = 1.0f;
        } else {
            Time.timeScale = 0.0f;
        }
    }

    public void StartGame() {
        // Create tiles and shuffle
        List<MahjongTile> tiles = TileSpawner.CreateFullTileSet();
        Shuffle(tiles);

        // Create players
        players = new List<Player>();
        for (int i = 0; i < 4; i++) {
            players.Add(new Player(i));
        }

        if (debugMode) {
            Player testPlayer = players[0];

            // CURRENT DEBUG SCENARIO: Player 0 one tile away from winning Knitting
            for (int i = 1; i < 5; i++) {
                AddTileToHand(tiles, testPlayer.hand, TileSuit.Characters, i);
                AddTileToHand(tiles, testPlayer.hand, TileSuit.Bamboo, i);
                AddTileToHand(tiles, testPlayer.hand, TileSuit.Dots, i);
            }
            AddTileToHand(tiles, testPlayer.hand, TileSuit.Characters, 5);

            if (testPlayer.hand.Count != 13) {
                Debug.LogError("Incorrect debug player hand count.");
                MenuManager.Instance.QuitButton();
            }
        }

        // Deal hands to players (13 tiles each)
        for (int j = 0; j < 13; j++) {
            foreach (Player player in players) {
                // Skip player 0 if in debug mode
                if (debugMode && player.seat == 0) { continue; }

                MahjongTile tile = tiles[0];
                player.hand.Add(tile);
                tiles.RemoveAt(0);
            }
        }

        // Add remaining tiles to the wall
        wall = new Queue<MahjongTile>(tiles);

        TableManager.Instance.SetupTable(players, wall);

        // Start first turn
        StartTurn(true);
    }

    private void StartTurn(bool drawTile) {
        TableManager.Instance.MoveCamera(currentPlayerIndex);
        MenuManager.Instance.SetPlayerText(currentPlayerIndex + 1);

        Debug.Log($"Player {currentPlayerIndex} turn begins");

        Player currentPlayer = players[currentPlayerIndex];

        if (drawTile) { DrawTile(currentPlayer); }
        waitingForDiscard = true;
    }

    public void EndTurn() {
        Player currentPlayer = players[currentPlayerIndex];

        TableManager.Instance.RefreshPlayerVisuals(currentPlayer);

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn(true);
    }

    public void OnTileClicked(TileView tileView) {
        if (isPaused) { return; }
        if (!waitingForDiscard) { return; }

        MahjongTile tile = tileView.tileData;
        Player currentPlayer = players[currentPlayerIndex];

        if (!currentPlayer.hand.Contains(tile)) { Debug.Log($"That tile is not in the current player's hand!"); return; }

        DiscardTile(currentPlayer, tile);
    }

    private void DrawTile(Player player) {
        if (wall.Count <= 0) {
            Debug.Log("Wall is empty");
            EndGame(null);
            return;
        }

        // Draw next tile from wall and add to player's hand
        currentDrawnTile = wall.Dequeue();
        player.hand.Add(currentDrawnTile);

        Debug.Log($"Player {player.seat} draws tile {currentDrawnTile.id}");

        // Move drawn tile to above player's hand
        TableManager.Instance.AnimateDraw(player, currentDrawnTile);

        // Check if new tile allows player to win
        if (HandEvaluator.IsWinningHand(player)) {
            EndGame(player);
            return;
        }

        if (CheckKanUpgrade(player, currentDrawnTile)) { return; }
        if (CheckConcealedKan(player)) { return; }
    }

    private void DiscardTile(Player player, MahjongTile tile) {
        Debug.Log($"Player {player.seat} discards tile {tile.id}");

        player.hand.Remove(tile);
        player.discards.Add(tile);
        lastDiscardedTile = tile;

        TableManager.Instance.AnimateDiscard(tile, currentDrawnTile);
        TableManager.Instance.RefreshPlayerVisuals(player);

        waitingForDiscard = false;

        CheckRon();
    }

    public bool TryPonKan(int playerIndex) {
        Player player = players[playerIndex];

        int matchingTiles = player.hand.Count(t => t.IsSameTile(lastDiscardedTile));

        if (matchingTiles >= 2) {
            if (matchingTiles == 3) {
                player.pendingCall = CallType.Kan;
                Debug.Log($"Player {playerIndex} can Kan tile {lastDiscardedTile.id}");
            } else {
                player.pendingCall = CallType.Pon;
                Debug.Log($"Player {playerIndex} can Pon tile {lastDiscardedTile.id}");
            }

            player.callTile = lastDiscardedTile;

            ResolveCall(player, null);
            return true;
        }

        return false;
    }

    public bool TryChi(int playerIndex) {
        Player player = players[playerIndex];
        int nextPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        if (playerIndex == nextPlayerIndex) {
            List<MahjongTile> chiTiles = GetChiTiles(player, lastDiscardedTile);

            if (chiTiles != null) {
                player.pendingCall = CallType.Chi;
                Debug.Log($"Player {playerIndex} can Chi tile {lastDiscardedTile.id}");

                player.callTile = lastDiscardedTile;

                ResolveCall(player, chiTiles);
                return true;
            }
        }

        return false;
    }

    private void ResolveCall(Player player, List<MahjongTile> chiTiles) {
        currentPlayerIndex = player.seat;
        currentDrawnTile = null;
        
        bool isKan = player.pendingCall == CallType.Kan;
        bool isChi = chiTiles != null;

        // Add called tile to player's melds
        Meld newMeld = new Meld(new List<MahjongTile>());
        newMeld.tiles.Add(player.callTile);

        // Calculate how many tiles need to be moved from players hand to players melds
        int tilesNeeded = 0;
        if (isKan) {
            Debug.Log($"Player {player.seat} Kans tile {player.callTile.id}");
            tilesNeeded = 3;
            newMeld.type = CallType.Kan;
        } else if (isChi) {
            Debug.Log($"Player {player.seat} Chis tile {player.callTile.id}");
            newMeld.type = CallType.Chi;
        } else {
            Debug.Log($"Player {player.seat} Pons tile {player.callTile.id}");
            tilesNeeded = 2;
            newMeld.type = CallType.Pon;
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

        if (HandEvaluator.IsWinningHand(player)) {
            EndGame(player);
            return;
        }

        StartTurn(isKan);   // If call is a Kan, draw a Kan tile, else don't draw a tile
    }

    private void CheckRon() {
        for (int i = 1; i < players.Count; i++) {
            int playerIndex = (currentPlayerIndex + i) % players.Count;
            Player currentPlayer = players[playerIndex];

            currentPlayer.hand.Add(lastDiscardedTile);

            if (HandEvaluator.IsWinningHand(currentPlayer)) {
                TableManager.Instance.AnimateDraw(currentPlayer, lastDiscardedTile);

                EndGame(currentPlayer);
                return;
            } else {
                currentPlayer.hand.Remove(lastDiscardedTile);
            }
        }

        // If no Ron found, proceed to call menu
        Debug.Log("No Ron found.");
        TableManager.Instance.TopViewCamera();
        MenuManager.Instance.OpenCallMenu(
            $"Player {currentPlayerIndex + 1} discarded {lastDiscardedTile.GetDisplayName()}"
        );
    }

    private bool CheckKanUpgrade(Player player, MahjongTile tile) {
        // Check for an existing Pon to upgrade into a Kan
        foreach (Meld meld in player.melds) {
            if (meld.tiles.Count == 3 && meld.tiles.All(t => t.IsSameTile(tile))) {
                // Upgrade Pon to Kan
                player.hand.Remove(tile);
                meld.tiles.Add(tile);
                meld.type = CallType.Kan;

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

                player.melds.Add(new Meld(new List<MahjongTile>(matchingTiles), CallType.Kan, true));

                Debug.Log($"Player {player.seat} declares a Concealed Kan.");
                TableManager.Instance.RefreshPlayerVisuals(player);

                DrawTile(player);
                waitingForDiscard = true;
                return true;
            }
        }

        return false;
    } 

    private void AddTileToHand(
        List<MahjongTile> tiles, List<MahjongTile> hand, TileSuit suit,
        int number = 0, WindType wind = WindType.None, DragonType dragon = DragonType.None, 
        int count = 1
    ) {
        for (int i = 0; i < count; i++) {
            int index;
            if (wind == WindType.None && dragon == DragonType.None) {
                index = tiles.FindIndex(t => t.suit == suit && t.number == number);
            }
            else if (wind == WindType.None) {
                index = tiles.FindIndex(t => t.suit == suit && t.dragon == dragon);
            }
            else {
                index = tiles.FindIndex(t => t.suit == suit && t.wind == wind);
            }

            if (index != -1) {
                hand.Add(tiles[index]);
                tiles.RemoveAt(index);
            } else {
                Debug.LogError($"Unable to find: {number} {suit} {wind} {dragon}.");
                MenuManager.Instance.QuitButton();
            }
        }
    }

    private static List<MahjongTile> GetChiTiles(Player player, MahjongTile tile) {
        // If tile is a wind or a dragon, it cannot Chi
        if (!tile.IsNumbered()) { return null; }

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

    private void EndGame(Player winner) {
        if (winner != null) {
            Debug.Log($"Player {winner.seat} wins!");
            
            TableManager.Instance.MoveCamera(winner.seat);
            MenuManager.Instance.ShowWinScreen(winner.seat);
        } else {
            Debug.Log("Its a draw!");

            TableManager.Instance.TopViewCamera();
            MenuManager.Instance.ShowWinScreen(-1);
        }
    }
}
