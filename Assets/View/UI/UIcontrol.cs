using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIcontrol : MonoBehaviour {

    public Button centerOnPlayerButton;
    public Button newTurnButton;
    public Button changeMoveModeButton;
    public Button buildCampButton, leaveCampButton;

    //public Button followerRecruitmentButton;
    //public Button followerDisplayButton;

    void Start() {
        Button btn;

        // init center view on player button
        btn = centerOnPlayerButton.GetComponent<Button>();
        btn.onClick.AddListener(centerOnPlayerOnClick);

        // new turn
        btn = newTurnButton.GetComponent<Button>();
        btn.onClick.AddListener(newTurnOnClick);

        // change move mode
        btn = changeMoveModeButton.GetComponent<Button>();
        btn.onClick.AddListener(changeMoveMoveOnClick);

        // encampment control
        btn = buildCampButton.GetComponent<Button>();
        btn.onClick.AddListener(changeEncampmentButton);
        btn = leaveCampButton.GetComponent<Button>();
        btn.onClick.AddListener(changeEncampmentButton);
    }

    void centerOnPlayerOnClick() {
        CameraControl.toggleCenterOnPlayer();
    }

    void newTurnOnClick() {
        GameControl.gameSession.newTurn();
        StartCoroutine(TurnOffInteractableForSeconds(newTurnButton));
    }

    void changeMoveMoveOnClick() {
        MouseControl.changeMoveMode();
    }

    void changeEncampmentButton() {
        string camp_status;
        GameControl.gameSession.humanPlayer.changeCampStatus(out camp_status);
    }

    IEnumerator TurnOffInteractableForSeconds(Button button, float seconds = 3) {
        button.interactable = false;
        yield return new WaitForSeconds(seconds);
        button.interactable = true;
    }
}
