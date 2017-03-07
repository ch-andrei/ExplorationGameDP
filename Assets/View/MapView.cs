using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using TileViews;
using Tiles;

public class MapView : MonoBehaviour {

    // states for redrawing the views
    public enum States { notupdated, destroyQueued, destroying, destroyed, setupQueued, settingup, setupDone, updated};

    int state = (int)States.notupdated;
    int destroyState = (int)States.destroyed;
    int setupState = (int)States.setupDone;

    // imported prefab dimensions
    public const float prefabElevation = 200f;
    public const float prefabXZ = 50f;

    // draw tiles under water? -> set to false for better fps
    public bool drawUnderWater = false;

    // strings holdings names of assets
    public static string[] treesBroadleaf;
    public static string[] treesConifers;
    public static string[] treesConifersSnowy;
    public static string[] treesRandompoly;

    public static string[] tileAssets; // TODO

    // must set this in editor -> this will be instantiated to create each hex tile in the game world
    public TileView tile;

    // location from which the view distance will be calculated
    public static Vector3 viewCenterPoint;

    // up until how far from viewCenterPoint tiles will still be drawn
    public static float drawDistance = 300f;

    // customize in editor
    // tree parameters
    public TileViewInitParams tileViewInitParams = new TileViewInitParams();

    // set this to true to allow updates to be called
    private bool updateAllowed = false;

    // list of all currently drawn tiles
    private List<TileView> drawnTiles;

    // redraw world every 10 frames
    static int secondsBetweenUpdates = 1; // TODO read from xml
    DateTime lastRedrawCallComplete;

    private void Awake() {
    }

    private void Start() {

        drawnTiles = new List<TileView>();

        lastRedrawCallComplete = DateTime.UtcNow;

        // initialize asset names
        treesBroadleaf = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "broadleaf")).Split(',');
        treesConifers = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "conifers")).Split(',');
        treesConifersSnowy = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "conifersSnowy")).Split(',');
        treesRandompoly = (Utilities.statsXMLreader.getParameterFromXML("Assets/trees/", "randompoly")).Split(',');
    }

    public void redraw() {
        state = (int)States.notupdated;
    }

    public void startUpdating() {
        updateAllowed = true;
    }

    public void Update() {
        // update camera center
        viewCenterPoint = CameraControl.viewCenterPoint;

        if (updateAllowed) {

            if (state == (int)States.updated) {
                redraw();
            }
            if (state == (int)States.notupdated) {
                // update state
                state = (int)States.destroyQueued;
            }
            if (state == (int)States.destroyQueued) {
                state = (int)States.destroying;
                StartCoroutine(destroyViews());
            }
            if (state == (int)States.destroyed) {
                state = (int)States.settingup;
                StartCoroutine(setupViews());
            }
            if (state == (int)States.setupDone) {
                state = (int)States.notupdated;
            }
        }
    }

    private IEnumerator destroyViews() {

        TileView drawnTile;

        Vector3 childPos, distanceToChild;
        // loop over children TileViews
        for (int i = 0; i < drawnTiles.Count; i++) {

            drawnTile = drawnTiles[i];

            // get child position and set 0 to y component
            childPos = drawnTile.transform.position;
            // ignore elevation component
            childPos.y = 0;

            // vector distance to child
            distanceToChild = childPos - viewCenterPoint;

            // destroy only if outside of drawn range or if tileview's tile is 'dirty'
            if ((distanceToChild).magnitude > drawDistance || drawnTile.tile.dirty) {

                // destroy game object
                GameObject.Destroy(drawnTile.gameObject);

                // remove from list
                drawnTiles.RemoveAt(i);

                // readjust i to account for removing an item from the list
                i--;
            }
            
            yield return null;
        }

        // update state
        this.state = (int)States.destroyed;
    }

    private IEnumerator setupViews() {

        List<Tile> tiles = GameControl.gameSession.mapGenerator.getRegion().getViewableTiles();

        foreach (Tile tile in tiles) {

            // get tiles position
            Vector3 tilePos = tile.getPos();
            tilePos.y = 0;

            Vector3 distanceToTile = (tilePos - viewCenterPoint);

            if (distanceToTile.magnitude <= drawDistance) {

                if (drawUnderWater || tile.elevationToWater >= 0) {

                    // TODO use dictionary (hashmap) instead of a list for existing tiles

                    // very innefficient O(n) on every check -> O(n^2) overall when having to check all of the existing tiles
                    bool tileAlreadyDrawn = false;
                    // check if already initialized
                    foreach (TileView view in drawnTiles) {
                        if ((view.transform.position - tilePos).magnitude < 1f) { // built in Vector3 comparison
                            tileAlreadyDrawn = true;
                            break;
                        }
                    }

                    // do not instantiate this tile since it already exists in the drawn list
                    if (tileAlreadyDrawn)
                        continue;

                    // tile is no longer dirty since it will now be updated
                    tile.dirty = false;

                    TileView tileView = Instantiate(this.tile);
                    tileView.transform.parent = this.transform;

                    tileView.transform.position = tilePos;

                    // instantiating a prefab with demensions [50, sqrt 3 / 2 * 50, 200], must rescale it!
                    // rescale from dimensions s x h x elevation = 50 x sqrt 3 / 2 * 50 x 200 to as needed by region dimensions
                    float scaleFactorY, scaleFactorXZ;
                    scaleFactorY = tile.getY() / prefabElevation;
                    scaleFactorXZ = HexTile.size / prefabXZ;

                    tileView.transform.localScale = new Vector3(scaleFactorXZ, scaleFactorY, scaleFactorXZ);
                    tileView.InitializeTileViewObject(tile, tileViewInitParams);

                    drawnTiles.Add(tileView);

                    // wait a little
                    yield return null;
                }
            }
        }

        // update state
        this.state = (int)States.setupDone;
    }
}
