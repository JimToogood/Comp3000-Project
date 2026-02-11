using UnityEngine;
using System.Collections;
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

    [SerializeField] public GameObject tilePrefab;
    [SerializeField] public Transform tileParent;
    [SerializeField] public Camera mainCamera;

    private Dictionary<MahjongTile, TileView> tileViews;
    private List<Player> players;
    private Queue<MahjongTile> wall;
    private Vector3 cameraBasePos;
    private float cameraBaseTilt;

    private int currentPlayerIndex = 0;
    private bool waitingForDiscard = false;
    

    void Start() {
        // Get camera base position
        cameraBasePos = mainCamera.transform.position;
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
        MoveTile(tileViews[newTile], tileViews[tile].transform.position, tileViews[tile].transform.rotation);

        // TODO: Move to discard pile instead of destroying
        Destroy(tileView.gameObject);
        waitingForDiscard = false;

        EndTurn();
    }

    private void MoveTile(TileView tileView, Vector3 pos, Quaternion rot) {
        tileView.gameObject.transform.position = pos;
        tileView.gameObject.transform.rotation = rot;
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
        foreach (MahjongTile tile in wall) {
            int side = index / tilesPerSide;
            if (side >= 4) { return; }

            SeatLayout layout = GetSeatLayout(side, 12.7f, 0.0f, false);

            // TODO: Fix issue where each side ends with a single tile instead of a stack of two
            int i = index % tilesPerSide;
            int column = i / 2;
            int stackLevel = 1 - (i % 2);

            float tileOffset = (column - ((tilesPerSide / 2.0f) - 1) / 2.0f) * 2.1f;
            Vector3 pos = layout.basePos + layout.tileDirection * tileOffset;
            pos.y += stackLevel * 1.0f;

            SpawnTile(tile, pos, layout.rotation);
            index++;
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
        float duration = 0.5f;
        
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 endPos = Quaternion.Euler(0.0f, angle, 0.0f) * cameraBasePos;
        Quaternion endRot = Quaternion.Euler(cameraBaseTilt, angle + 180.0f, 0.0f);

        while (time < duration) {
            float t = Mathf.SmoothStep(0.0f, 1.0f, time / duration);

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        // Snap to pos after movement to avoid drift
        mainCamera.transform.position = endPos;
        mainCamera.transform.rotation = endRot;
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
