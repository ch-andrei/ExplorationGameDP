﻿using UnityEngine;
using System;
using System.Collections.Generic;

using TileAttributes;
using Tiles;

using Pathfinding;
using System.Collections;

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

    PathFinder DijsktraPF;

    PathResult pathResult;

    string attemptedMoveMessage;

    void Start() {

        // setup all vars

        firstClickedIndex = new int[2];
        secondClickedIndex = new int[2];

        selectionIndicator = GameObject.FindGameObjectWithTag("SelectionIndicator");
        pathIndicator = GameObject.FindGameObjectWithTag("PathIndicator");
        pathExploredIndicator = GameObject.FindGameObjectWithTag("PathExploredIndicator");
        
        // create GUI style
        guiStyle = new GUIStyle();
        guiStyle.alignment = TextAnchor.LowerLeft;
        guiStyle.normal.textColor = Utilities.hexToColor("#153870");
    }

    void Awake() { }

    void Update() {

        DijsktraPF = new DijkstraPathFinder(
                        maxDepth: GameControl.gameSession.player.getMaxActionPoints(),
                        maxCost: GameControl.gameSession.player.getActionPoints()
                        );

        // update selection tile
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo)) {
            GameObject hitObject = hitInfo.collider.transform.gameObject;
            if (hitObject == null) {
                // nothing to do
            } else {
                if ((currentSelectedTile = GameControl.gameSession.mapGenerator.getRegion().getTileAt(hitInfo.point, out currentSelectedIndex)) != null) {
                    selectionIndicator.transform.position = currentSelectedTile.getPos(); // move selection indicator
                } else {
                    // do nothing
                }
            }
        }

        // draw move range
        StartCoroutine(
            displayPath(
                DijsktraPF.pathFromTo(
                    GameControl.gameSession.mapGenerator.getRegion() as HexRegion,
                    GameControl.gameSession.player.getPosTile(),
                    new HexTile(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector2(float.MaxValue, float.MaxValue))),
                writeToGlobalPathResult: false,
                displayTimeInSeconds: 0.1f,
                drawExplored: true));

        // *** mouse controls *** //

        // left click
        bool leftMouseClick = Input.GetMouseButtonDown(0);
        if (leftMouseClick) {
            // TODO some player controls
        }

        // right click
        bool rightMouseClick = Input.GetMouseButtonDown(1);
        if (rightMouseClick && currentSelectedIndex != null) {
            if (selectionOrder) {
                firstClickedTile = currentSelectedTile;
                currentSelectedIndex.CopyTo(firstClickedIndex, 0);
            } else {
                secondClickedTile = currentSelectedTile;
                currentSelectedIndex.CopyTo(secondClickedIndex, 0);

                // check if right clicked same tile twice
                if (firstClickedTile.Equals(secondClickedTile)) {
                    pathResult = GameControl.gameSession.playerAttemptMove(firstClickedTile, out attemptedMoveMessage, movePlayer: true);
                    StartCoroutine(displayPath(pathResult));
                } else {
                    // clicked another tile: overwrite first selection
                    firstClickedTile = currentSelectedTile;
                    currentSelectedIndex.CopyTo(firstClickedIndex, 0);

                    // flip selection order an extra time
                    selectionOrder = !selectionOrder;
                }
            }
            // flip selection order
            selectionOrder = !selectionOrder;
        }

        if (firstClickedTile != null) {
            // draw move path
            StartCoroutine(
                displayPath(
                    DijsktraPF.pathFromTo(
                        GameControl.gameSession.mapGenerator.getRegion() as HexRegion,
                        GameControl.gameSession.player.getPosTile(),
                        firstClickedTile),
                    writeToGlobalPathResult: false,
                    displayTimeInSeconds: 0.1f,
                    drawExplored: false));
        }
    }

    // FOR DEBUGGING PURPOSES
    public IEnumerator computePath(PathFinder pathFinder, Tile start, Tile goal, float displayTimeInSeconds = 2f, bool writeToGlobalPathResult = true) {
        if (start != null && goal != null) {
            // compute path
            PathResult pr = pathFinder.pathFromTo(GameControl.gameSession.mapGenerator.getRegion() as HexRegion, start, goal);
            yield return displayPath(pr, displayTimeInSeconds, writeToGlobalPathResult, drawExplored : true);
        }
    }

    public IEnumerator displayPath(PathResult pr, float displayTimeInSeconds = 2f, bool writeToGlobalPathResult = true, bool drawExplored = false) {
        List<GameObject> _pathIndicators = null;
        List<GameObject> _exploredIndicators = null;

        if (pr != null) {

            if (writeToGlobalPathResult)
                pathResult = pr;

            // reset indicator lists
            _pathIndicators = new List<GameObject>();
            _exploredIndicators = new List<GameObject>();

            // draw path info
            foreach (Tile tile in pr.getTiles()) {
                GameObject _pathIndicator = Instantiate(pathIndicator);
                _pathIndicator.transform.parent = this.transform;
                _pathIndicator.transform.position = tile.getPos();
                _pathIndicators.Add(_pathIndicator);
            }

            if (drawExplored) {
                // draw explored info
                foreach (Tile tile in pr.getExploredTiles()) {
                    GameObject _exploredIndicator = Instantiate(pathExploredIndicator);
                    _exploredIndicator.transform.parent = this.transform;
                    _exploredIndicator.transform.position = tile.getPos();
                    _exploredIndicators.Add(_exploredIndicator);
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
    }

    void OnGUI() {
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
        if (pathResult != null) {
            string pathInfo = "Path cost:" + pathResult.pathCost;
            foreach (Tile tile in pathResult.getTiles()) {
                pathInfo += "\n" + tile.index;
            }
            GUI.Label(new Rect(menuWidth, Screen.height - menuHeight, menuWidth, menuHeight), pathInfo, guiStyle);
        }
    }
}