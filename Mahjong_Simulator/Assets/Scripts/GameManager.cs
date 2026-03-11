using UnityEngine;
using System.Collections;
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

    [SerializeField] public GameObject tilePrefab;
    [SerializeField] public Transform tileParent;
    [SerializeField] public Camera mainCamera;

    private Dictionary<MahjongTile, TileView> tileViews;
    private List<Player> players;
    private Queue<MahjongTile> wall;
    private Vector3 cameraBasePos;
    private Vector3 discardBasePos;
    private float cameraBaseTilt;
    private MahjongTile currentDrawnTile;
    private MahjongTile lastDiscardedTile;

    private int discardIndex = 0;
    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;
    private bool waitingForCall = false;


    private void Start() {
        // Get camera base position
        cameraBasePos = mainCamera.transform.localPosition;
        cameraBaseTilt = mainCamera.transform.eulerAngles.x;

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
            SortHand(players[i].hand);
            SpawnHand(players[i].hand, players[i].seat);
        }
        SpawnWall(wall);

        // Find discard starting position from position of first wall tile
        discardBasePos = tileViews[wall.ElementAt(1)].transform.localPosition;
        discardBasePos += new Vector3(1.1f, 0.0f, -3.1f);

        // Start first turn
        StartTurn(true);
    }

    private void StartTurn(bool drawTile) {
        StartCoroutine(MoveCamera(currentPlayerIndex));
        Debug.Log($"Player {currentPlayerIndex} turn begins");

        Player currentPlayer = players[currentPlayerIndex];

        RelayoutHand(currentPlayer.hand, currentPlayer.seat);

        if (drawTile) { DrawTile(currentPlayer); }
        waitingForDiscard = true;
    }

    private void EndTurn() {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        StartTurn(true);
    }

    public void OnTileClicked(TileView tileView) {
        //Debug.Log($"Clicked tile {tileView.tileData.id}");
        if (!waitingForDiscard) { return; }

        MahjongTile tile = tileView.tileData;
        Player currentPlayer = players[currentPlayerIndex];

        if (!currentPlayer.hand.Contains(tile)) { Debug.Log($"That tile is not in the current player's hand!"); return; }

        DiscardTile(currentPlayer, tile, tileView);
    }

    private void DrawTile(Player player) {
        if (wall.Count <= 0) {
            Debug.Log("Wall is empty");
            return;
        }
   
        // Get the position for the drawn tile as above the position of the centre hand tile
        Vector3 drawnTilePos = GetHandCentrePos(player.hand);
        drawnTilePos.y += 4.25f;

        // Draw next tile from wall and add to player's hand
        currentDrawnTile = wall.Dequeue();
        player.hand.Add(currentDrawnTile);

        Debug.Log($"Player {player.seat} draws tile {currentDrawnTile.id}");

        // Move drawn tile to above player's hand
        StartCoroutine(MoveTile(
            tileViews[currentDrawnTile], drawnTilePos, tileViews[player.hand[0]].transform.localRotation
        ));
    }

    private void DiscardTile(Player player, MahjongTile tile, TileView tileView) {
        Debug.Log($"Player {player.seat} discards tile {tile.id}");

        player.hand.Remove(tile);
        player.discards.Add(tile);
        lastDiscardedTile = tile;

        // Move drawn tile to where discarded tile was
        if (tile != currentDrawnTile && currentDrawnTile != null) {
            //Debug.Log($"Tile {currentDrawnTile.id} moves to where tile {tile.id} was");
            StartCoroutine(MoveTile(
                tileViews[currentDrawnTile], tileViews[tile].transform.localPosition, tileViews[tile].transform.localRotation
            ));
        }

        // Move discarded tile to discard pile
        StartCoroutine(MoveTile(tileViews[tile], GetDiscardPos(), Quaternion.identity));

        waitingForDiscard = false;

        CheckForCalls();
    }
    
    private void CheckForCalls() {
        Debug.Log($"Checking for calls...");
        waitingForCall = false;

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

            // If player has 2 or more identical tiles, then they can Pon
            if (matchCount >= 2) {
                playerToCheck.pendingCall = CallType.Pon;
                playerToCheck.callTile = lastDiscardedTile;
                waitingForCall = true;

                Debug.Log($"Player {seatToCheck} can Pon tile {playerToCheck.callTile.id}");
                // TODO: Change this automatic call to player choice
                ResolveCall(playerToCheck);
            }

            // If player has 3 or more identical tiles, then they can Kan
            if (matchCount >= 3) {
                playerToCheck.pendingCall = CallType.Kan;
                playerToCheck.callTile = lastDiscardedTile;
                waitingForCall = true;

                Debug.Log($"Player {seatToCheck} can Kan tile {playerToCheck.callTile.id}");
            }

            // If player is next in turn order, check if they can Chi
            // TODO: Implement chi detection
        }

        if (!waitingForCall) {
            Debug.Log($"No calls found. Ending turn...");
            EndTurn();
        }
    }

    private void ResolveCall(Player player) {
        currentPlayerIndex = player.seat;
        currentDrawnTile = null;

        // Add called tile to player's melds
        player.melds.Add(player.callTile);
        StartCoroutine(MoveTile(
            // TODO: Change 0 Vector to proper melds position
            tileViews[player.callTile], new Vector3(0.0f, 0.0f, 0.0f), tileViews[player.callTile].transform.localRotation
        ));

        // Calculate how many tiles need to be moved from players hand to players melds
        int tilesNeeded;
        if (player.pendingCall == CallType.Kan) {
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

            StartCoroutine(MoveTile(
                tileViews[tile], new Vector3(0.0f, 0.0f, 0.0f), tileViews[tile].transform.localRotation
            ));
        }

        // Reset call variables and start turn
        player.pendingCall = null;
        player.callTile = null;
        StartTurn(false);
    }

    private void RelayoutHand(List<MahjongTile> hand, int seat) {
        SortHand(hand);

        SeatLayout layout = GetSeatLayout(seat, 18.0f, 1.0f, true);

        for (int i = 0; i < hand.Count; i++) {
            float tileOffset = (i - (hand.Count - 1) / 2.0f) * 2.1f;
            Vector3 pos = layout.basePos + layout.tileDirection * tileOffset;

            StartCoroutine(MoveTile(tileViews[hand[i]], pos, layout.rotation));
        }
    }

    private void SpawnHand(List<MahjongTile> hand, int seat) {
        SeatLayout layout = GetSeatLayout(seat, 18.0f, 1.0f, true);

        for (int i = 0; i < hand.Count; i++) {
            float tileOffset = (i - (hand.Count - 1) / 2.0f) * 2.1f;
            Vector3 pos = layout.basePos + layout.tileDirection * tileOffset;

            SpawnTile(hand[i], pos, layout.rotation);
        }
    }

    private void SpawnWall(Queue<MahjongTile> wall) {
        int tilesPerSide = wall.Count / 4;

        int index = 0;
        for (int side = 0; side < 4; side++) {
            // If tilesPerSide is odd, make one pair of opposite sides have an one extra column to avoid single tiles
            int columnsPerSide;
            if (side % 2 == 0) {
                columnsPerSide = Mathf.CeilToInt(tilesPerSide / 2.0f);
            } else {
                columnsPerSide = Mathf.FloorToInt(tilesPerSide / 2.0f);
            }

            for (int column = 0; column < columnsPerSide; column++) {
                // Flip loop to make top tiles be drawn first
                for (int stack = 1; stack >= 0; stack--) {
                    if (index >= wall.Count) { return; }

                    // Get tile without removing from queue
                    MahjongTile tile = wall.ElementAt(index);

                    SeatLayout layout = GetSeatLayout(side, 12.1f, 0.0f, false);

                    float tileOffset = (column - (columnsPerSide - 1) / 2.0f) * 2.1f;
                    Vector3 pos = layout.basePos + layout.tileDirection * tileOffset;
                    pos.y += stack;

                    SpawnTile(tile, pos, layout.rotation);
                    index++;
                }
            }
        }
    }

    private void SpawnTile(MahjongTile tile, Vector3 pos, Quaternion rot) {
        // Spawn tile as child of tileParent
        var tileObject = Instantiate(tilePrefab, tileParent);

        // Update local pos and rot after instantiation (because Instantiate uses world pos)
        tileObject.transform.localPosition = pos;
        tileObject.transform.localRotation = rot;

        var tileView = tileObject.GetComponent<TileView>();
        tileView.SetTile(tile);
        tileViews[tile] = tileView;
    }

    private IEnumerator MoveCamera(int seat) {
        float angle = seat * 90.0f;
        float time = 0.0f;
        float duration = 0.6f;
        
        Vector3 startPos = mainCamera.transform.localPosition;
        Quaternion startRot = mainCamera.transform.localRotation;

        Vector3 endPos = Quaternion.Euler(0.0f, angle, 0.0f) * cameraBasePos;
        Quaternion endRot = Quaternion.Euler(cameraBaseTilt, angle + 180.0f, 0.0f);

        // Animate smooth movement between start and end
        while (time < duration) {
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);

            mainCamera.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            mainCamera.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Snap to end pos after animation to avoid drift
        mainCamera.transform.localPosition = endPos;
        mainCamera.transform.localRotation = endRot;
    }

    private IEnumerator MoveTile(TileView tileView, Vector3 endPos, Quaternion endRot) {
        float time = 0.0f;
        float duration = 0.75f;

        Vector3 startPos = tileView.gameObject.transform.localPosition;
        Quaternion startRot = tileView.gameObject.transform.localRotation;

        // Animate smooth movement between start and end
        while (time < duration) {
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);

            tileView.gameObject.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            tileView.gameObject.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Snap to end pos after animation to avoid drift
        tileView.gameObject.transform.localPosition = endPos;
        tileView.gameObject.transform.localRotation = endRot;
    }

    private Vector3 GetHandCentrePos(List<MahjongTile> hand) {
        Vector3 sum = Vector3.zero;

        foreach (MahjongTile tile in hand) {
            sum += tileViews[tile].gameObject.transform.localPosition;
        }

        return sum / hand.Count;
    }

    private Vector3 GetDiscardPos() {
        int tilesPerRow = 10;

        int row = discardIndex / tilesPerRow;
        int column = discardIndex % tilesPerRow;

        Vector3 pos;
        // Change position of row 6 to avoid clipping
        if (row == 6) {
            pos = discardBasePos + new Vector3(column * 2.1f, 0.0f, 3.1f);
        } else {
            // Account for row 6 being in different position
            if (row > 6) { row--; }
            pos = discardBasePos + new Vector3(column * 2.1f, 0.0f, -row * 3.1f);
        }

        discardIndex++;
        return pos;
    }

    private static bool CanChi(Player player, MahjongTile tile) {
        // If tile is a wind or a dragon, it cannot Chi
        if (tile.suit is not (TileSuit.Characters or TileSuit.Bamboo or TileSuit.Dots)) { return false; }

        // TODO: Implement chi detecting logic
        return false;
    }

    private static SeatLayout GetSeatLayout(int seat, float tableRadius, float height, bool isUpright) {
        // Get position data needed to spawn hand based on player seat
        float angle = seat * 90.0f;

        Vector3 basePos = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.forward * tableRadius;
        basePos.y = height;

        Quaternion rotation;
        if (isUpright) {
            rotation = Quaternion.Euler(-90.0f, angle + 180.0f, 0.0f);  // player hand
        } else {
            rotation = Quaternion.Euler(180.0f, angle, 0.0f);           // wall
        }

        return new SeatLayout {
            basePos = basePos,
            tileDirection = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.right,
            rotation = rotation
        };
    }

    private static void SortHand(List<MahjongTile> hand) {
        hand.Sort((a, b) => {
            // Sort by suit
            int suitComparison = b.suit.CompareTo(a.suit);
            if (suitComparison != 0) { return suitComparison; }

            // Sort by number/wind type/dragon type
            return a.suit switch {
                TileSuit.Characters or TileSuit.Bamboo or TileSuit.Dots => b.number.CompareTo(a.number),
                TileSuit.Winds => b.wind.CompareTo(a.wind),
                TileSuit.Dragons => b.dragon.CompareTo(a.dragon),
                _ => 0
            };
        });
    }

    private static void Shuffle(List<MahjongTile> tiles) {
        // Fisher–Yates shuffle
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
