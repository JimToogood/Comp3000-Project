using UnityEngine;

public class TileView : MonoBehaviour {
    [SerializeField] private MeshRenderer faceRenderer;
    public MahjongTile tileData;

    public void SetTile(MahjongTile tile) {
        tileData = tile;
        UpdateTexture();
    }

    private void UpdateTexture() {
        string textureName = GetTextureName(tileData);
        Texture2D tex = Resources.Load<Texture2D>($"tile_faces/{textureName}");

        if (!tex) {
            Debug.LogError($"Failed to load texture: {textureName}");
            return;
        }

        SetFaceTexture(tex);
    }

    private void SetFaceTexture(Texture2D faceTexture) {
        faceRenderer.material.mainTexture = faceTexture;
    }

    private static string GetTextureName(MahjongTile tile) {
        return tile.suit switch {
            TileSuit.Characters => $"character{tile.number}",
            TileSuit.Bamboo => $"bamboo{tile.number}",
            TileSuit.Dots => $"dot{tile.number}",
            TileSuit.Winds => $"wind_{tile.wind.ToString().ToLower()}",
            TileSuit.Dragons => $"{tile.dragon.ToString().ToLower()}_dragon",
            _ => null
        };
    }
}
