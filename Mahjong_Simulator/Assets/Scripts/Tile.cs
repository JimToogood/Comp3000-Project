using UnityEngine;

public class Tile : MonoBehaviour {
    [SerializeField] private MeshRenderer faceRenderer;

    public void SetFaceTexture(Texture2D faceTexture) {
        faceRenderer.material.mainTexture = faceTexture;
    }
}
