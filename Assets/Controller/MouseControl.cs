using UnityEngine;
using System;

using TileAttributes;
using Tiles;

[AddComponentMenu("Mouse-Control")]
public class MouseControl : MonoBehaviour {

    private Tile currentSelectedTile, clickedTile;
    private int[] currentSelectedIndex, clickedIndex;
    private bool selectionGUIOn;

    void Start() {
        clickedIndex = new int[2];
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
                    transform.position = currentSelectedTile.getPos(); // move selection indicator
                else {
                    // do nothing
                }
            }
        }

        bool mouseClick = Input.GetMouseButtonDown(0);
        if (mouseClick && currentSelectedIndex != null) {
            clickedTile = currentSelectedTile;
            currentSelectedIndex.CopyTo(clickedIndex, 0);
        }

    }

    private int menuWidth = Screen.width / 5;
    private int menuHeight = 300;

    void OnGUI() {
        if (clickedTile != null) {
            GUI.Box(new Rect(Screen.width - menuWidth, 0, menuWidth, menuHeight), "Selected tile\nPosition: " + clickedTile.getPos() + 
                "\nCoordinate: [" + (clickedIndex[0]) + ", " + (clickedIndex[1]) +
                "]\n" + clickedTile);
        }
    }
}