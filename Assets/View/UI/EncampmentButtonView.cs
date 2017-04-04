using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EncampmentButtonView : MonoBehaviour {

    bool button = false, dirty = true;

    public GameObject buildCampButton, leaveCampButton;

	// Update is called once per frame
	void Update () {
        if (GameControl.gameSession.humanPlayer.getCampStatus()) {
            if (!button)
                dirty = true;
            button = true;
        } else {
            if (button)
                dirty = true;
            button = false;
        }

        if (dirty) {
            redraw();
            dirty = false;
        }
	}

    void redraw() {
        if (button) {
            buildCampButton.SetActive(false);
            leaveCampButton.SetActive(true);
        } else {
            buildCampButton.SetActive(true);
            leaveCampButton.SetActive(false);
        }
    }
}
