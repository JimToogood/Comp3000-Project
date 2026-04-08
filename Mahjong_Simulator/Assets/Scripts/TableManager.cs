using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class TableManager : MonoBehaviour {
    const float TILE_SPACING = 2.1f;
    const float TILE_ANIMATION_LENGTH = 0.75f;
    const float CAMERA_ANIMATION_LENGTH = 0.6f;

    // Set instance so table manager can be called in other classes
    public static TableManager Instance { get; private set; }

    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform tileParent;
    [SerializeField] private Camera mainCamera;

    private Dictionary<MahjongTile, TileView> tileViews = new();
    private Vector3 cameraBasePos;
    private Vector3 discardBasePos;
    private float cameraBaseTilt;
    private int discardIndex = 0;


    private void Awake() {
        Instance = this;

        // Get camera base position
        cameraBasePos = mainCamera.transform.localPosition;
        cameraBaseTilt = mainCamera.transform.eulerAngles.x;
    }

    public void SetupTable(List<Player> players, Queue<MahjongTile> wall) {
        // Spawn tiles
        for (int i = 0; i < players.Count; i++) {
            SortHand(players[i].hand);
            SpawnHand(players[i].hand, players[i].seat);
        }

        SpawnWall(wall);

        // Find discard starting position from position of first wall tile
        discardBasePos = tileViews[wall.ElementAt(1)].transform.localPosition;
        discardBasePos += new Vector3(1.1f, 0.0f, -4.4f);
    }

    public void AnimateDraw(Player player, MahjongTile tile) {
        // Get the position for the drawn tile as above the position of the centre hand tile
        SeatLayout layout = GetSeatLayout(player.seat, 18.0f, 1.0f, true);

        Vector3 drawnTilePos = layout.basePos + Vector3.up * 4.25f;

        MoveTile(tile, drawnTilePos, layout.rotation);
    }

    public void AnimateDiscard(MahjongTile tile, MahjongTile drawnTile) {
        // Move drawn tile to where discarded tile was
        if (drawnTile != null && tile != drawnTile) {
            MoveTile(drawnTile, tileViews[tile].transform.localPosition, tileViews[tile].transform.localRotation);
        }

        // Move discarded tile to discard pile
        MoveTile(tile, GetDiscardPos(), Quaternion.identity);
    }
    
    public void LayoutHand(Player player) {
        SeatLayout layout = GetSeatLayout(player.seat, 18.0f, 1.0f, true);

        float meldWidth = player.melds.Sum(m => m.tiles.Count) * TILE_SPACING;
        Vector3 meldOffset = layout.tileDirection * (meldWidth / 2.0f);

        for (int i = 0; i < player.hand.Count; i++) {
            float tileOffset = (i - (player.hand.Count - 1) / 2.0f) * TILE_SPACING;
            Vector3 pos = layout.basePos + layout.tileDirection * tileOffset + meldOffset;

            MoveTile(player.hand[i], pos, layout.rotation);
        }
    }

    public void LayoutMelds(Player player) {
        SeatLayout layout = GetSeatLayout(player.seat, 18.0f, 0.0f, false);
        layout.rotation *= Quaternion.Euler(180.0f, 180.0f, 0.0f);

        float handWidth = player.hand.Count * TILE_SPACING;
        Vector3 handOffset = -layout.tileDirection * (handWidth / 2.0f + 1.0f);

        int totalMeldTiles = player.melds.Sum(m => m.tiles.Count);
        int tileCounter = 0;

        foreach (Meld meld in player.melds) {
            for (int i = 0; i < meld.tiles.Count; i++) {
                float tileOffset = (tileCounter - (totalMeldTiles - 1) / 2.0f) * TILE_SPACING;
                Vector3 pos = layout.basePos + layout.tileDirection * tileOffset + handOffset;

                Quaternion rot = layout.rotation;

                if (meld.isConcealed) {
                    rot *= Quaternion.Euler(180.0f, 0.0f, 0.0f);
                }

                MoveTile(meld.tiles[i], pos, rot);
                tileCounter++;
            }
        }
    }

    public void SortHand(List<MahjongTile> hand) {
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

    private void SpawnHand(List<MahjongTile> hand, int seat) {
        SeatLayout layout = GetSeatLayout(seat, 18.0f, 1.0f, true);

        for (int i = 0; i < hand.Count; i++) {
            float tileOffset = (i - (hand.Count - 1) / 2.0f) * TILE_SPACING;
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

                    float tileOffset = (column - (columnsPerSide - 1) / 2.0f) * TILE_SPACING;
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

    public void RefreshPlayerVisuals(Player player) {
        SortHand(player.hand);
        LayoutHand(player);
        LayoutMelds(player);
    }

    public void ShowConcealedKans(Player player) {
        IEnumerator ShowConcealedKansRoutine(Player player) {
            // Wait for any tile animations to finish to avoid position drift
            yield return new WaitForSeconds(TILE_ANIMATION_LENGTH);

            foreach (Meld meld in player.melds) {
                if (meld.isConcealed) {
                    MahjongTile tile = meld.tiles[2];

                    // Rotate the tile to face the player
                    Quaternion rot = tileViews[tile].transform.localRotation * Quaternion.Euler(90.0f, 0.0f, 0.0f);
                    Vector3 pos = tileViews[tile].transform.localPosition;
                    pos.y += 1.0f;

                    MoveTile(tile, pos, rot);
                }
            }
        }

        StartCoroutine(ShowConcealedKansRoutine(player));
    }

    public void MoveCamera(int seat) {
        IEnumerator MoveCameraRoutine(int seat) {
            float angle = seat * 90.0f;
            float time = 0.0f;
            
            Vector3 startPos = mainCamera.transform.localPosition;
            Quaternion startRot = mainCamera.transform.localRotation;

            Vector3 endPos = Quaternion.Euler(0.0f, angle, 0.0f) * cameraBasePos;
            Quaternion endRot = Quaternion.Euler(cameraBaseTilt, angle + 180.0f, 0.0f);

            // Animate smooth movement between start and end
            while (time < CAMERA_ANIMATION_LENGTH) {
                float t = Mathf.SmoothStep(0.0f, 1.0f, time / CAMERA_ANIMATION_LENGTH);

                mainCamera.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

                time += Time.deltaTime;
                yield return null;
            }

            // Snap to end pos after animation to avoid drift
            mainCamera.transform.localPosition = endPos;
            mainCamera.transform.localRotation = endRot;
        }

        StartCoroutine(MoveCameraRoutine(seat));
    }

    public void TopViewCamera() {
        IEnumerator TopViewCameraRoutine() {
            float time = 0.0f;
            
            Vector3 startPos = mainCamera.transform.localPosition;
            Quaternion startRot = mainCamera.transform.localRotation;

            Vector3 endPos = new Vector3(0.0f, 5.3f, 0.0f);
            Quaternion endRot = Quaternion.Euler(90.0f, 0.0f, 0.0f);

            // Animate smooth movement between start and end
            while (time < CAMERA_ANIMATION_LENGTH) {
                float t = Mathf.SmoothStep(0.0f, 1.0f, time / CAMERA_ANIMATION_LENGTH);

                mainCamera.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

                time += Time.deltaTime;
                yield return null;
            }

            // Snap to end pos after animation to avoid drift
            mainCamera.transform.localPosition = endPos;
            mainCamera.transform.localRotation = endRot;
        }
    
        StartCoroutine(TopViewCameraRoutine());
    }

    public void MoveTile(MahjongTile tile, Vector3 pos, Quaternion rot) {
        IEnumerator MoveTileRoutine(TileView tileView, Vector3 endPos, Quaternion endRot) {
            float time = 0.0f;

            Vector3 startPos = tileView.gameObject.transform.localPosition;
            Quaternion startRot = tileView.gameObject.transform.localRotation;

            // Animate smooth movement between start and end
            while (time < TILE_ANIMATION_LENGTH) {
                float t = Mathf.SmoothStep(0.0f, 1.0f, time / TILE_ANIMATION_LENGTH);

                tileView.gameObject.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                tileView.gameObject.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

                time += Time.deltaTime;
                yield return null;
            }

            // Snap to end pos after animation to avoid drift
            tileView.gameObject.transform.localPosition = endPos;
            tileView.gameObject.transform.localRotation = endRot;
        }

        StartCoroutine(MoveTileRoutine(tileViews[tile], pos, rot));
    }

    private Vector3 GetDiscardPos() {
        int tilesPerRow = 10;

        int row = discardIndex / tilesPerRow;
        int column = discardIndex % tilesPerRow;

        Vector3 pos;
        // Change position of rows 6 and 7 to avoid clipping
        if (row == 6 || row == 7) {
            pos = discardBasePos + new Vector3(column * TILE_SPACING, 0.0f, (row - 5) * 3.1f);
        } else {
            // Account for rows 6 and 7 being in different position
            if (row > 7) { row -= 2; }
            pos = discardBasePos + new Vector3(column * TILE_SPACING, 0.0f, -row * 3.1f);
        }

        discardIndex++;
        return pos;
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
}
