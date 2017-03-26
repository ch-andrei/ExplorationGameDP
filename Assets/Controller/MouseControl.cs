using UnityEngine;
using System;
using System.Collections.Generic;

using TileAttributes;
using Tiles;

using Pathfinding;
using System.Collections;

[AddComponentMenu("Mouse-Control")]
public class MouseControl : MonoBehaviour {

    int menuWidth = 200;
    int menuHeight = 300;

    static bool selectionOrder = true;
    static bool moveMode = false;

    static Tile mouseOverTile, selectedTile, firstClickedTile, secondClickedTile;
    static int[] currentSelectedIndex;
    static Vector3 ySelectionOffset = new Vector3(0, 5, 0);

    static GUIStyle guiStyle;

    // selection game object
    GameObject mouseOverIndicator, selectionTileIndicator;

    // path related game objects
    GameObject pathIndicator;
    GameObject pathExploredIndicator;

    PathFinder DijsktraPF, AstarPF;

    PathResult pathResult;

    string attemptedMoveMessage;

    void Start() {

        // setup all vars

        mouseOverIndicator = GameObject.FindGameObjectWithTag("MouseOverIndicator");
        selectionTileIndicator = GameObject.FindGameObjectWithTag("SelectionTileIndicator");
        pathIndicator = GameObject.FindGameObjectWithTag("PathIndicator");
        pathExploredIndicator = GameObject.FindGameObjectWithTag("PathExploredIndicator");
        
        // create GUI style
        guiStyle = new GUIStyle();
        guiStyle.alignment = TextAnchor.LowerLeft;
        guiStyle.normal.textColor = Utilities.hexToColor("#153870");

        AstarPF = new AstarPathFinder(maxDepth: 50, maxCost: 50, maxIncrementalCost: GameControl.gameSession.humanPlayer.getMaxActionPoints());
        DijsktraPF = new DijkstraPathFinder(maxDepth: GameControl.gameSession.humanPlayer.getMaxActionPoints(), 
                                            maxCost: GameControl.gameSession.humanPlayer.getActionPoints(),
                                            maxIncrementalCost: GameControl.gameSession.humanPlayer.getMaxActionPoints()
                                            );
    }

    void Awake() {
        
    }

    void Update() {

        /// *** RAYCASTING FOR SELECTING TILES PART *** ///

        // update selection tile
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo)) {
            GameObject hitObject = hitInfo.collider.transform.gameObject;
            if (hitObject == null) {
                // nothing to do
            } else {
                if ((mouseOverTile = GameControl.gameSession.mapGenerator.getRegion().getTileAt(hitInfo.point, out currentSelectedIndex)) != null) {
                    mouseOverIndicator.transform.position = mouseOverTile.getPos() + ySelectionOffset; // move mouseOverIndicator
                } else {
                    // do nothing
                }
            }
        }

        // left click
        bool leftMouseClick = Input.GetMouseButtonDown(0);
        if (leftMouseClick && mouseOverTile != null) {
            selectionTileIndicator.transform.position = mouseOverTile.getPos() + ySelectionOffset; // move selectionTileIndicator
            selectedTile = mouseOverTile;
        }

        /// *** PATHFINDING PART *** ///
        if (moveMode) {
            DijsktraPF.maxDepth = GameControl.gameSession.humanPlayer.getMaxActionPoints();
            DijsktraPF.maxCost = GameControl.gameSession.humanPlayer.getActionPoints();

            // draw move range only 
            // TODO optimize this to not recalculate path on every frame
            StartCoroutine(
                displayPath(
                    DijsktraPF.pathFromTo(
                        GameControl.gameSession.humanPlayer.getPosTile(),
                        new HexTile(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector2(float.MaxValue, float.MaxValue)),
                        playersCanBlockPath: true
                        ),
                    writeToGlobalPathResult: false,
                    displayTimeInSeconds: 0.1f,
                    drawExplored: true));

            // draw path to the selected tile 
            // TODO optimize this to not recalculate path on every frame
            if (mouseOverTile != null)
                StartCoroutine(
                    displayPath(
                         AstarPF.pathFromTo(
                             GameControl.gameSession.humanPlayer.getPosTile(),
                             mouseOverTile,
                             playersCanBlockPath: true
                             ),
                         writeToGlobalPathResult: false,
                         displayTimeInSeconds: 0.1f,
                         drawExplored: false,
                         drawCost: true
                         ));

            // *** MOUSE CLICKS CONTROL PART *** //

            // right click
            bool rightMouseClick = Input.GetMouseButtonDown(1);
            if (rightMouseClick && currentSelectedIndex != null) {
                if (selectionOrder) {
                    firstClickedTile = mouseOverTile;
                    // draw path using A* pathfinder (not Dijkstra) for faster performance
                } else {
                    secondClickedTile = mouseOverTile;

                    // check if right clicked same tile twice
                    if (firstClickedTile.Equals(secondClickedTile)) {
                        pathResult = GameControl.gameSession.playerAttemptMove(firstClickedTile, out attemptedMoveMessage, movePlayer: true);
                        StartCoroutine(displayPath(pathResult));
                        changeMoveMode();
                    } else {
                        // clicked another tile: overwrite first selection
                        firstClickedTile = mouseOverTile;

                        // flip selection order an extra time
                        selectionOrder = !selectionOrder;
                    }
                }
                // flip selection order
                selectionOrder = !selectionOrder;
            }

            // draw path to selected tile
            // TODO optimize this to not recalculate path every frame
            if (firstClickedTile != null) {
                // draw move path
                StartCoroutine(
                    displayPath(
                        DijsktraPF.pathFromTo(
                            GameControl.gameSession.humanPlayer.getPosTile(),
                            firstClickedTile,
                            playersCanBlockPath: true
                            ),
                        writeToGlobalPathResult: false,
                        displayTimeInSeconds: 0.1f,
                        drawExplored: false));
            }
        }
    }

    public static void changeMoveMode() {
        moveMode = !moveMode;
    }

    public static void resetMouseControlView() {
        moveMode = false;
    }

    // FOR DEBUGGING PURPOSES
    public IEnumerator computePath(PathFinder pathFinder, Tile start, Tile goal, float displayTimeInSeconds = 2f, bool writeToGlobalPathResult = true) {
        if (start != null && goal != null) {
            // compute path
            PathResult pr = pathFinder.pathFromTo(start, goal);
            yield return displayPath(pr, displayTimeInSeconds, writeToGlobalPathResult, drawExplored : true);
        }
    }

    public IEnumerator displayPath(PathResult pr, float displayTimeInSeconds = 2f, bool writeToGlobalPathResult = true, bool drawExplored = false, bool drawCost = false) {
        List<GameObject> _pathIndicators = null;
        List<GameObject> _exploredIndicators = null;
        GameObject costIndicator = null;

        if (pr != null) {

            if (writeToGlobalPathResult)
                pathResult = pr;

            // reset indicator lists
            _pathIndicators = new List<GameObject>();
            _exploredIndicators = new List<GameObject>();

            // draw path info
            foreach (Tile tile in pr.getTilesOnPathStartFirst()) {
                GameObject _pathIndicator = Instantiate(pathIndicator);
                _pathIndicator.transform.parent = this.transform;
                _pathIndicator.transform.position = tile.getPos() + ySelectionOffset;
                _pathIndicators.Add(_pathIndicator);
            }

            if (drawExplored) {
                // draw explored info
                foreach (Tile tile in pr.getExploredTiles()) {
                    GameObject _exploredIndicator = Instantiate(pathExploredIndicator);
                    _exploredIndicator.transform.parent = this.transform;
                    _exploredIndicator.transform.position = tile.getPos() + ySelectionOffset;
                    _exploredIndicators.Add(_exploredIndicator);
                }
            }

            if (drawCost) {
                if (pr.reachedGoal) {
                    costIndicator = Instantiate(Resources.Load("Prefabs/Text/TileCostObject"), this.transform) as GameObject;

                    // set position
                    Tile tile = pr.getTilesOnPath()[0];
                    costIndicator.transform.position = tile.getPos() + ySelectionOffset * 2;

                    // set text
                    string pathCost = Mathf.CeilToInt(pr.pathCost) + "AP";
                    costIndicator.transform.GetChild(0).GetComponent<TextMesh>().text = pathCost;

                    costIndicator.transform.LookAt(Camera.main.transform);
                    costIndicator.transform.forward = -costIndicator.transform.forward;
                }
            }
        }

        // wait for some time
        yield return new WaitForSeconds(displayTimeInSeconds);

        // destroy indicators
        if (_pathIndicators != null)
            foreach (GameObject go in _pathIndicators) {
                Destroy(go);
            }
        if (_exploredIndicators != null)
            foreach (GameObject go in _exploredIndicators) {
                Destroy(go);
            }
        if (costIndicator != null)
            Destroy(costIndicator);
    }

    void OnGUI() {
        if (selectedTile != null) {
            string currentSelection = selectedTile.ToString();
            GUI.Box(new Rect(Screen.width - menuWidth, Screen.height - menuHeight, menuWidth, menuHeight), currentSelection);
        }
        if (firstClickedTile != null) {
            string leftSelection = "First tile\n" + firstClickedTile;
            GUI.Label(new Rect(0, Screen.height - menuHeight, menuWidth, menuHeight), leftSelection, guiStyle);
        }
        if (secondClickedTile != null) {
            string rightSelection = "Second tile\n" + secondClickedTile;
            GUI.Label(new Rect(0, Screen.height - 2 * menuHeight, menuWidth, menuHeight), rightSelection, guiStyle);
        }
        if (pathResult != null) {
            string pathInfo = "Path cost:" + pathResult.pathCost;
            foreach (Tile tile in pathResult.getTilesOnPathStartFirst()) {
                pathInfo += "\n" + tile.index;
            }
            GUI.Label(new Rect(menuWidth, Screen.height - menuHeight, menuWidth, menuHeight), pathInfo, guiStyle);
        }
    }
}