using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapGeneration;
using TileAttributes;
using Tiles;

public class GameControl : MonoBehaviour {

    public MapGeneratorInput mgi = new MapGeneratorInput();

    [Range(1, 100)]
    public int gizmoSize;

    public bool useRandomSeed;

    public bool drawGizmos = false;

    public static GameSession gameSession;

    public MapView mapView;

    void Awake() {
        mgi.Initialize(useRandomSeed);
        gameSession = new GameSession(mgi);
        //Debug.Log("GameSession Region with tiles count " + gameSession.mapGenerator.getRegion().getViewableTiles().Count);
    }

    // Use this for initialization
    void Start () {
        GameObject go = GameObject.FindGameObjectWithTag("MapView");
        mapView = (MapView)go.GetComponent(typeof(MapView));
        mapView.startUpdating();
    }

    public void generateMap() {
        Awake();
    }

    int counter = 0;

    public void Update() {
        // placeholder turn counter
        // make new turn every 100 frames
        if (counter++ > 100) {
            counter = 0;
            gameSession.newTurn();
        }
    }

    public void setPreset(string preset) {
        mgi.preset = preset;
    }

    private void OnDrawGizmos() {
        if (!drawGizmos || gameSession == null || gameSession.mapGenerator == null) {
            return;
        }
        if (gameSession.mapGenerator.getRegion() != null) {
            //Debug.Log("Drawing gizmos.");
            // set color and draw gizmos
            int water_level = gameSession.mapGenerator.getRegion().getWaterLevelElevation();
            Color c;
            foreach (Tile tile in gameSession.mapGenerator.getRegion().getViewableTiles()) {
                if (tile.getTileType() != null) {
                    int elevation = (int)tile.getY() - water_level;
                    if (tile.getTileType().GetType() == typeof(WaterTileType)) {
                        //Debug.Log("water: elevation " + elevation);
                        if (elevation > -5) {
                            c = hexToColor("#C2D2E7");
                        } else if (elevation > -10) {
                            c = hexToColor("#54B3F0");
                        } else if (elevation > -25) {
                            c = hexToColor("#067DED");
                        } else if (elevation > -50) {
                            c = hexToColor("#005F95");
                        } else
                            c = hexToColor("#004176");
                    } else if (tile.getTileType().GetType() == typeof(LandTileType)) {
                        //Debug.Log("water: elevation " + elevation);
                        if (elevation < 0)
                            c = hexToColor("#696300");
                        else if (elevation < 5)
                            c = hexToColor("#00C103");
                        else if (elevation < 10)
                            c = hexToColor("#59FF00");
                        else if (elevation < 15)
                            c = hexToColor("#F2FF00");
                        else if (elevation < 20)
                            c = hexToColor("#FFBE00");
                        else if (elevation < 25)
                            c = hexToColor("#FF8C00");
                        else if (elevation < 30)
                            c = hexToColor("#FF6900");
                        else if (elevation < 40)
                            c = hexToColor("#E74900");
                        else if (elevation < 50)
                            c = hexToColor("#E10C00");
                        else if (elevation < 75)
                            c = hexToColor("#971C00");
                        else if (elevation < 100)
                            c = hexToColor("#C24340");
                        else if (elevation < 150)
                            c = hexToColor("#B9818A");
                        else if (elevation < 200)
                            c = hexToColor("#988E8B");
                        else if (elevation < 1000)
                            c = hexToColor("#AEB5BD");
                        else // default
                            c = new Color(0, 0, 0, 0);
                    } else
                        c = new Color(0, 0, 0, 0);
                    Gizmos.color = c;
                    Vector3 pos = tile.getPos(); ;
                    //if (elevation < 0) {
                    //    pos.y = water_level; // if it's water, draw elevation as equal to water_level
                    //}
                    Gizmos.DrawSphere(pos, gizmoSize);
                }
            }
        }
    }

    public Color hexToColor(string hex) {
        return Utilities.hexToColor(hex);
    }

    int playerInfoWidth = 400;
    int playerInfoHeight = 200;
    // shows region stats
    void OnGUI() {
        string[] names = QualitySettings.names;
        GUILayout.BeginVertical();
        int i = 0;
        while (i < names.Length) {
            if (GUILayout.Button(names[i]))
                QualitySettings.SetQualityLevel(i, true);

            i++;
        }
        GUILayout.EndVertical();
        GUI.Box(new Rect(0, Screen.height - playerInfoHeight, playerInfoWidth, playerInfoHeight), 
            "Player info:\nPosition: " + gameSession.player.getPos() + "\nAction points: " + gameSession.player.getActionPoints() + "\nStrength: " +
            gameSession.player.computeStrength() + "\nMorale: " + gameSession.player.computeMorale() + "\nSupplies: " + gameSession.player.getSupplies() + 
            "\nSupplies Consumption per turn: " + gameSession.player.getFoodConsumption() + "\nFollowers:\n" + gameSession.player.getFollowersAsString());
    }
}
