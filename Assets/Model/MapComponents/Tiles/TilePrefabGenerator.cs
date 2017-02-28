using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using TileViews;
using TileAttributes;
using Tiles;

public class TilePrefabGenerator : MonoBehaviour {

    public TileView _tileView;

    public TileViewInitParams tvip = new TileViewInitParams();

    public float hexSize;
    private float hexHeight;

    private Tile tile;

    void Awake() {
    }

    // Use this for initialization
    void Start() {
    }

    public bool CreateTilePrefab = false;

    // Update is called once per frame
    void Update() {
        if (CreateTilePrefab) {
            hexHeight = Mathf.Sqrt(3) / 2 * hexSize;
            HexTile.height = hexHeight;
            HexTile.size = hexSize;
            tile = new HexTile(new Vector3(0, 200, 0), new LandTileType(false));
            CreateTilePrefab = false;
            DoCreateTilePrefab();
        }
    }

    public void DoCreateTilePrefab() {

        MeshFilter mF = _tileView.GetComponent<MeshFilter>();
        MeshCollider mC = _tileView.GetComponent<MeshCollider>();

        _tileView.InitializeTileMesh(tile, mF, mC);

        //AssetDatabase.CreateAsset( mF.sharedMesh, "Assets/Resources/Prefabs/Meshes/HexMesh.obj");
        //AssetDatabase.SaveAssets();

        Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/Resources/Prefabs/" + "DefaultTile" + ".prefab");
        PrefabUtility.ReplacePrefab(_tileView.transform.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
    }

    private void OnDrawGizmos() {
        Update();
    }
}
