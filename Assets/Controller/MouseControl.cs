using UnityEngine;
using System;
using System.Collections.Generic;

using TileAttributes;
using Tiles;

using Pathfinding;

[AddComponentMenu("Mouse-Control")]
public class MouseControl : MonoBehaviour {

    int menuWidth = 200;
    int menuHeight = 250;

    static bool selectionOrder = true;

    static Tile currentSelectedTile, firstClickedTile, secondClickedTile;
    static int[] currentSelectedIndex, firstClickedIndex, secondClickedIndex;

    static GUIStyle guiStyle;

    // selection game object
    GameObject selectionIndicator;

    // path related game objects
    GameObject pathIndicator;
    GameObject pathExploredIndicator;

    PathFinder AstarPF, DijsktraPF;

    List<Tile> path;
    List<GameObject> pathIndicators;

    void Start() {

        // setup all vars

        firstClickedIndex = new int[2];
        secondClickedIndex = new int[2];
        pathIndicators = new List<GameObject>();

        selectionIndicator = GameObject.FindGameObjectWithTag("SelectionIndicator");
        pathIndicator = GameObject.FindGameObjectWithTag("PathIndicator");
        pathExploredIndicator = GameObject.FindGameObjectWithTag("PathExploredIndicator");

        AstarPF = new AstarPathFinder();
        DijsktraPF = new DijkstraPathFinder();
        
        // create GUI style
        guiStyle = new GUIStyle();
        guiStyle.alignment = TextAnchor.LowerLeft;
        guiStyle.normal.textColor = Utilities.hexToColor("#153870");
    }

    void Awake() { }

    void Update() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo)) {
            GameObject hitObject = hitInfo.collider.transform.gameObject;
            if (hitObject == null) {
                // nothing to do
            } else {
                if ((currentSelectedTile = GameControl.gameSession.mapGenerator.getRegion().getTileAt(hitInfo.point, out currentSelectedIndex)) != null)
                    selectionIndicator.transform.position = currentSelectedTile.getPos(); // move selection indicator
                else {
                    // do nothing
                }
            }
        }

        bool leftMouseClick = Input.GetMouseButtonDown(0);
        if (leftMouseClick) {
            // TODO some player controls
        }

        bool rightMouseClick = Input.GetMouseButtonDown(1);
        if (rightMouseClick && currentSelectedIndex != null) {
            if (selectionOrder) {
                firstClickedTile = currentSelectedTile;
                currentSelectedIndex.CopyTo(firstClickedIndex, 0);
            } else {
                secondClickedTile = currentSelectedTile;
                currentSelectedIndex.CopyTo(secondClickedIndex, 0);
            }
            // flip selection order
            selectionOrder = !selectionOrder;
        }

    }

    public void computePath() {
        if (firstClickedTile != null && secondClickedTile != null) {
            // destroy previous indicators
            foreach (GameObject go in pathIndicators) {
                Destroy(go);
            }
            // reset indicator list
            pathIndicators = new List<GameObject>();

            // compute path
            path = AstarPF.pathFromTo(GameControl.gameSession.mapGenerator.getRegion() as HexRegion, firstClickedTile, secondClickedTile).getTiles();

            // draw path info
            foreach (Tile tile in path) {
                GameObject _pathIndicator = Instantiate(pathIndicator);
                _pathIndicator.transform.parent = this.transform;
                _pathIndicator.transform.position = tile.getPos();
                pathIndicators.Add(_pathIndicator);
            }
        }
    }

    void OnGUI() {

        GUILayout.BeginArea(new Rect(Screen.width - menuWidth, Screen.height - menuHeight, menuWidth, menuHeight));
        {
            if (GUILayout.Button("Compute path")) {
                computePath();
            }
        }
        GUILayout.EndArea();

        if (firstClickedTile != null) {
            string leftSelection = "First tile\nPosition: " + firstClickedTile.getPos() +
                "\nCoordinate: [" + (firstClickedIndex[0]) + ", " + (firstClickedIndex[1]) +
                "]\n" + firstClickedTile;
            GUI.Label(new Rect(0, Screen.height - menuHeight, menuWidth, menuHeight), leftSelection, guiStyle);
        }
        if (secondClickedTile != null) {
            string rightSelection = "Second tile\nPosition: " + secondClickedTile.getPos() +
                "\nCoordinate: [" + (secondClickedIndex[0]) + ", " + (secondClickedIndex[1]) +
                "]\n" + secondClickedTile;
            GUI.Label(new Rect(0, Screen.height - 2 * menuHeight, menuWidth, menuHeight), rightSelection, guiStyle);
        }
        if (path != null) {
            string pathInfo = "";
            foreach (Tile tile in path) {
                pathInfo += "\n" + tile.index;
            }
            GUI.Label(new Rect(menuWidth, Screen.height - menuHeight, menuWidth, menuHeight), pathInfo, guiStyle);
        }
    }
}