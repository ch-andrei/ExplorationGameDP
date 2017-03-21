using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour {

    List<GameObject> spawnedObjects;

    // states for redrawing the views
    public enum States { playerModel, encampmentModel };

    private int playerViewState;

    private bool dirty;

    void Start () {
        spawnedObjects = new List<GameObject>();
        dirty = true;
        playerViewState = (int)States.playerModel;
    }

    void Update() {
        // update position
        this.transform.position = GameControl.gameSession.player.getPos();

        // update player view state
        if (GameControl.gameSession.player.getCampStatus() && playerViewState != (int)States.encampmentModel) {
            playerViewState = (int)States.encampmentModel;
            dirty = true;
        } else if (!GameControl.gameSession.player.getCampStatus() && playerViewState != (int)States.playerModel) {
            playerViewState = (int)States.playerModel;
            dirty = true;
        }

        // redraw
        if (dirty) {
            foreach (GameObject go in spawnedObjects) {
                Destroy(go);
            }

            if (playerViewState == (int)States.encampmentModel) {
                string prefabName = "PlayerEncampment";
                GameObject village = Instantiate(Resources.Load("Prefabs/Player/" + prefabName), this.transform) as GameObject;

                // backup tree transform to preserve pos and scale offsets
                Transform t = village.transform;

                if (village != null) {
                    // preserve prefab scale
                    village.transform.localScale = t.localScale;

                    float randRotation = Random.Range(0f, 360f);
                    village.transform.Rotate(0, randRotation, 0);
                }

                spawnedObjects.Add(village);
            } else if (playerViewState == (int)States.playerModel) {
                string prefabName = "PlayerCube";
                GameObject player = Instantiate(Resources.Load("Prefabs/Player/" + prefabName), this.transform) as GameObject;
                spawnedObjects.Add(player);
            }

            dirty = false;
        }
	}
}
