using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventNotificationsView : MonoBehaviour {

    public Button eventNotificationToggleButton;

    public GameObject eventNotificationMenu;
    public Text eventNotificationMenuText;

    bool eventNoficationToggle = false;
    bool dirty = true;

    int logCount;

    void Start() {
        // init logs
        logCount = 0;
        eventNotificationMenuText.text = "";

        // disable menu
        eventNotificationMenu.SetActive(eventNoficationToggle);

        // init buttons
        Button btn;
        // init center view on player button
        btn = eventNotificationToggleButton.GetComponent<Button>();
        btn.onClick.AddListener(eventNotificationToggleButtonOnClick);
    }

    void eventNotificationToggleButtonOnClick() {
        eventNoficationToggle = !eventNoficationToggle;
        eventNotificationMenu.SetActive(eventNoficationToggle);
    }

    // Update is called once per frame
    void Update () {
        if (GameControl.gameSession.gamelog.Count != logCount) {
            logCount = GameControl.gameSession.gamelog.Count;
            redraw();
        }
	}

    void redraw() {
        string logs = "";
        foreach (string str in GameControl.gameSession.gamelog){
            logs += str + "\n";
        }
        eventNotificationMenuText.text = logs;
    }
}
