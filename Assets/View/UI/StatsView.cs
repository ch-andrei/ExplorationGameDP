using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsView : MonoBehaviour {

    public GameObject actionPointsView;
    public GameObject suppliesView;

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update () {
        redrawActionPoints();
        redrawSupplies();
    }

    void redrawActionPoints() {
        actionPointsView.GetComponentInChildren<Text>().text = "" +
            GameControl.gameSession.humanPlayer.getActionPoints() + "/" +
            GameControl.gameSession.humanPlayer.getMaxActionPoints();
    }

    void redrawSupplies() {
        suppliesView.GetComponentInChildren<Text>().text = "" +
            GameControl.gameSession.humanPlayer.getSupplies() + "/" +
            GameControl.gameSession.humanPlayer.getMaxSupplies() + 
            "(-" + GameControl.gameSession.humanPlayer.getFoodConsumption() + ")";
    }
}
