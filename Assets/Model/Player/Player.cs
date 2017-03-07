using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Followers;
using TileAttributes;

public class Player : ViewablePlayer {

    private Vector3 pos;
    private int actionPoints, actionPointsLeft;
    private int supplies;
    private List<Resource> resources;
    private List<Follower> followers;
    private float moraleUp, moraleDown;

    private static float followerDeathThreshold = 1e-3f;

    public Player() {
        this.pos = new Vector3();
        this.resources = new List<Resource>();
        this.followers = new List<Follower>();
        initPlayer();
    }

    public void initPlayer() {
        this.supplies = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "supplies"));
        this.actionPoints = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPoints"));
        this.actionPointsLeft = this.actionPoints;
        this.moraleUp = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraleup"));
        this.moraleDown = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraledown"));
        addFollower(new Swordsman());
        addFollower(new Swordsman());
        addFollower(new Wizard());
    }

    public void newTurn() {
        this.actionPointsLeft = this.actionPoints;
        this.supplies -= getFoodConsumption();
        if (this.supplies < 0) this.supplies = 0;
        updateMorale();
        updateFollowers();
    }

    public void updateMorale() {
        float adjustement;
        foreach (Follower f in followers) {
            adjustement = this.moraleUp;
            if (this.supplies == 0) {
                adjustement = this.moraleDown + (1f - this.moraleDown) * f.getWillpower();
            }
            float morale_adjusted =  adjustement * f.getMorale();
            if (morale_adjusted > 1f) morale_adjusted = 1f;
            f.setMorale(morale_adjusted);
        }
    }

    private void updateFollowers() {
        Follower f;
        for (int i = 0; i < followers.Count; i++) {
            f = followers[i];
            if ( f.getStrength() < followerDeathThreshold ) {
                followers.RemoveAt(i);
            }
        }
    }

    public int getFoodConsumption() {
        int sum = 0;
        foreach (Follower f in followers) {
            sum += f.getFoodDemand();
        }
        return sum;
    }

    public int computeStrength() {
        int sum = 0;
        foreach (Follower f in followers) {
            sum += f.getStrength();
        }
        return sum;
    }

    public float computeMorale() {
        float sum = 0;
        foreach (Follower f in followers) {
            sum += f.getMorale();
        }
        return sum / followers.Count;
    }

    public void addFollower(Follower f) {
        this.followers.Add(f);
    }
    public void setPos(Vector3 pos) {
        this.pos = pos;
    }


    // getters
    public Vector3 getPos() {
        resources = new List<Resource>();
        return this.pos;
    }
    public int getActionPoints() {
        return this.actionPoints;
    }
    public List<Resource> getResources() {
        return this.resources;
    }
    public int getSupplies() {
        return this.supplies;
    }
    public string getFollowersAsString() {
        string s = "";
        foreach (Follower f in followers) {
            s += f + "\n";
        }
        return s;
    }

    public bool getCampStatus(out string message) {
        message = "";
        return false;
    }
    public bool getCurrentUpgrades(out string message) {
        message = "";
        return false;
    }
    public bool getPossibleUpgrades(out string message) {
        message = "";
        return false;
    }

    // actions
    public bool attemptMove(Vector3 moveToPos, out string message) {
        message = "";
        return false;
    }
    public bool changeCampStatus(out string message) {
        message = "";
        return false;
    }
    public bool doQuest(out string message) {
        message = "";
        return false;
    }
    public bool doUpgrade(out string message) {
        message = "";
        return false;
    }
    public bool gatherResource(out string message) {
        message = "";
        return false;
    }

}
