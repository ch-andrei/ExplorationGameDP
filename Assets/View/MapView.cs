using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using TileViews;
using Tiles;

public class MapView : MonoBehaviour {

    public const float prefabElevation = 200f;
    public const float prefabXZ = 50f;

    public bool drawUnderWater = false;

    public static string[] treesBroadleaf;
    public static string[] treesConifers;
    public static string[] treesConifersSnowy;
    public static string[] treesRandompoly;

    public static string[] tileAssets; // TODO

    public bool updated = false;

    // must set this in editor
    public TileView tile;

    public static Vector3 viewCenterPoint;
    public static float drawDistance = 500f;

    // customize in editor
    public TileViewInitParams tileViewInitParams = new TileViewInitParams();

    private bool updateAllowed = false;

    private List<TileView> drawnTiles;

    // redraw world every 10 frames
    static int secondsBetweenUpdates = 1; // TODO read from xml
    DateTime lastUpdate;

    private void Awake() {
    }

    private void Start() {

        drawnTiles = new List<TileView>();

        lastUpdate = DateTime.UtcNow;

        // initialize asset names
        treesBroadleaf = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "broadleaf")).Split(',');
        treesConifers = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "conifers")).Split(',');
        treesConifersSnowy = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "conifersSnowy")).Split(',');
        treesRandompoly = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "randompoly")).Split(',');
    }

    public void Update() {

        // TODO 
        // relocate tiles and redraw tile graphics based on camera location
        // updateTiles(); 
        // TODO

        viewCenterPoint = CameraControl.viewCenterPoint;

        if (updateAllowed) {
            //if (!updated) {
            //    SetUpTiles();
            //    updated = true;
            //}
            if (DateTime.UtcNow - lastUpdate > TimeSpan.FromSeconds(secondsBetweenUpdates) || !updated) {
                //Debug.Log("Updating using center point: " + viewCenterPoint);
                UpdateView();
                updated = true;
                lastUpdate = DateTime.UtcNow;
            }
        }
    }

    public void startUpdating() {
        updateAllowed = true;
    }

    private void UpdateView() {

        TileView childView;

        // loop over children TileViews
        for (int i = 0; i < drawnTiles.Count; i++) {

            childView = drawnTiles[i];
            
            // get child position and set 0 to y component
            Vector3 childPos = childView.transform.position;
            childPos.y = 0;

            Vector3 distanceToChild = childPos - viewCenterPoint;

            // destroy only if outside of range
            if ((distanceToChild).magnitude > drawDistance) {

                // destroy game object
                GameObject.Destroy(childView.gameObject);

                // remove from list
                drawnTiles.RemoveAt(i);

                //Debug.Log("Destroyed child transform pos " + childView.transform.position);
            }
            //Debug.Log("Child transform pos " + child.transform.position);
        }

        // run set up tiles
        SetUpTiles();
    }

    private void SetUpTiles() {

        List<Tile> tiles = GameControl.gameSession.mapGenerator.getRegion().getViewableTiles();

        foreach (Tile tile in tiles) {

            // get tiles position
            Vector3 tilePos = tile.getPos();
            tilePos.y = 0;

            Vector3 distanceToTile = (tilePos - viewCenterPoint);

            if (distanceToTile.magnitude <= drawDistance) {

                if (drawUnderWater || tile.elevationToWater >= 0 ) {

                    //Debug.Log("Checking pos " + tilePos);

                    bool tileAlreadyDrawn = false; 
                    // check if already initialized
                    foreach (TileView view in drawnTiles) {
                        if ((view.transform.position - tilePos).magnitude < 1f) { // built in Vector3 comparison
                            tileAlreadyDrawn = true;
                            break;
                        }
                        // do instantiate this tile since it already exists in the drawn list
                    }

                    if (tileAlreadyDrawn)
                        continue;

                    TileView tileView = Instantiate(this.tile);
                    tileView.transform.parent = this.transform;

                    tileView.transform.position = tilePos;

                    // rescale from dimensions s x h x elevation = 50 x sqrt 3 / 2 * 50 x 200 as needed by region dimensions
                    float scaleFactorY, scaleFactorXZ;
                    scaleFactorY = tile.getY() / prefabElevation;
                    scaleFactorXZ = HexTile.size / prefabXZ;

                    tileView.transform.localScale = new Vector3(scaleFactorXZ, scaleFactorY, scaleFactorXZ);
                    tileView.InitializeTileViewObject(tile, tileViewInitParams);

                    drawnTiles.Add(tileView);
                    //Debug.Log("Added new tile at " + tileView.transform.position);
                }
            }
        }
    }
}
