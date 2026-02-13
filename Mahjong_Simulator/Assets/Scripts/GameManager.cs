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
    private float cameraBaseTilt;
    private MahjongTile currentDrawnTile;

    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;


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
            SpawnHand(players[i].hand, players[i].seat);
        }
        SpawnWall(wall);

        // Start first turn
        StartTurn();
    }

    private void StartTurn() {
        StartCoroutine(MoveCamera(currentPlayerIndex));
        Debug.Log($"Player {currentPlayerIndex} turn begins");

        Player currentPlayer = players[currentPlayerIndex];

        DrawTile(currentPlayer);

        waitingForDiscard = true;
    }

    private void EndTurn() {
        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count) { currentPlayerIndex = 0; }
        StartTurn();
    }

    public void OnTileClicked(TileView tileView) {
        Debug.Log($"Clicked tile {tileView.tileData.id}");
        if (!waitingForDiscard) { return; }

        MahjongTile tile = tileView.tileData;
        Player currentPlayer = players[currentPlayerIndex];

        if (!currentPlayer.hand.Contains(tile)) { Debug.Log($"That tile does not belong to the current player!"); return; }

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

        // Move drawn tile to where discarded tile was
        if (tile != currentDrawnTile) {
            Debug.Log($"Tile {currentDrawnTile.id} moves to where tile {tile.id} was");
            StartCoroutine(MoveTile(
                tileViews[currentDrawnTile], tileViews[tile].transform.localPosition, tileViews[tile].transform.localRotation
            ));
        }

        // TODO: Move to discard pile instead of destroying
        Destroy(tileView.gameObject);
        waitingForDiscard = false;

        EndTurn();
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

    // Fisher–Yates shuffle
    private static void Shuffle(List<MahjongTile> tiles) {
        for (int i = tiles.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);

            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }
    }
}
