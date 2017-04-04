using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Followers;
using Playable;

public class ActionMenuView : MonoBehaviour {

    public Button followersButton;
    public Button recruitmentButton;
    public Button upgradesButton;
    public GameObject MenuSelectionIndicator;

    public GameObject CardDeck;
    public GameObject ActionsMenuTitle;

    public Text playerTotalStrengthText;
    public Text playerAverageMoraleText;

    private enum ViewStates { followers, recruitment, upgrades }

    private int viewState = (int)ViewStates.followers;

    bool dirty = true;
    int turnCount;

    // Use this for initialization
    void Start () {
        Button btn;

        // init followers button
        btn = followersButton.GetComponent<Button>();
        btn.onClick.AddListener(followersButtonOnClick);

        // init recruitment button
        btn = recruitmentButton.GetComponent<Button>();
        btn.onClick.AddListener(recruitmentButtonOnClick);

        // init upgrades button
        btn = upgradesButton.GetComponent<Button>();
        btn.onClick.AddListener(upgradesButtonOnClick);

        followersButtonOnClick();
    }
	
	// Update is called once per frame
	void Update () {
        if (GameControl.gameSession.turnCount != this.turnCount) {
            this.turnCount = GameControl.gameSession.turnCount;
            dirty = true;
        }

        if (dirty) {
            redraw();
            dirty = false;
        }
        playerTotalStrengthText.text = "Total Strength: " +
            GameControl.gameSession.humanPlayer.computeStrength();
        playerAverageMoraleText.text = "Average Morale: " + 
            (100f * GameControl.gameSession.humanPlayer.computeMorale()).ToString("0") + "%";
	}

    void followersButtonOnClick() {
        viewState = (int)ViewStates.followers;
        ActionsMenuTitle.GetComponent<Text>().text = "Followers Menu";
        MenuSelectionIndicator.transform.position = followersButton.transform.position;
        dirty = true;
    }

    void recruitmentButtonOnClick() {
        viewState = (int)ViewStates.recruitment;
        ActionsMenuTitle.GetComponent<Text>().text = "Recruitment Menu";
        MenuSelectionIndicator.transform.position = recruitmentButton.transform.position;
        dirty = true;
    }

    void upgradesButtonOnClick() {
        viewState = (int)ViewStates.upgrades;
        ActionsMenuTitle.GetComponent<Text>().text = "Upgrades Menu";
        MenuSelectionIndicator.transform.position = upgradesButton.transform.position;
        dirty = true;
    }

    void redraw() {
        // destroy existing game objects
        foreach (Transform t in CardDeck.transform) {
            Destroy(t.gameObject);
        }

        List<Follower> followers = GameControl.gameSession.humanPlayer.getFollowers();

        if (viewState == (int)ViewStates.followers) {
            // create card game objects
            foreach (Follower f in GameControl.gameSession.humanPlayer.getFollowers()) {
                string cardPrefabName = f.getFollowerType() + "Card";
                GameObject card = Instantiate(Resources.Load("Prefabs/UI Prefabs/Cards/" + cardPrefabName), CardDeck.transform) as GameObject;
                card.transform.position = new Vector3(0, 0, 0);
                getCardViewStatsTextGameObject(card).text = f.statsAsString(morale: false);
            }
        } else if (viewState == (int)ViewStates.recruitment) {
            // TODO

            Button btn;

            // init center view on player button

            List<Follower> recruitable = GameControl.gameSession.humanPlayer.getRecruitableFollowers();

            foreach (Follower f in recruitable) {
                InitRecruitmentButton(f);
            }

        } else if (viewState == (int)ViewStates.upgrades) {
            // TODO
        }
    }

    void InitRecruitmentButton(Follower f) {
        string cardPrefabName = f.getFollowerType() + "Card";
        GameObject card = Instantiate(Resources.Load("Prefabs/UI Prefabs/Cards/" + cardPrefabName), CardDeck.transform) as GameObject;
        card.transform.position = new Vector3(0, 0, 0);
        getCardViewStatsTextGameObject(card).text = f.statsAsString(morale: false);
        getCardViewNameTextGameObject(card).text += ": " + f.recruitCost;

        Button btn;
        btn = card.GetComponent<Button>();
        btn.onClick.AddListener(() => { eventRecruitmentButtonOnClick(f); });
    }

    void eventRecruitmentButtonOnClick(Follower f) {
        GameControl.gameSession.playerAttemptRecruit(GameControl.gameSession.humanPlayer, f);        
    }

    Text getCardViewNameTextGameObject(GameObject card) {
        foreach (Transform child in card.transform) {
            if (child.name == "NameTextField") {
                return child.GetComponent<Text>();
            }
        }
        return null;
    }

    Text getCardViewStatsTextGameObject(GameObject card) {
        foreach (Transform child in card.transform) {
            if (child.name == "StatsTextField") {
                return child.GetComponent<Text>();
            }
        }
        return null;
    }

}
