using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Tiles;
using Followers;
using TileAttributes;

// DO WE REALLY NEED THIS?
public interface ViewablePlayer {

    // getters
    Vector3 getPos();
    Tile getPosTile();
    Vector2 getPosIndex();

    int getActionPoints();
    int getMaxActionPoints();
    List<Resource> getResources();
    int getSupplies();
    bool getCampStatus(out string message);
    bool getCurrentUpgrades(out string message);
    bool getPossibleUpgrades(out string message);

    // setters
    void setPos(Tile pos);
    void loseActionPoints(int actionPointsToLose);

    // actions
    bool attemptedMove(Vector3 moveToPos, out string message);
    bool changeCampStatus(out string message);
    bool doQuest(out string message);
    bool doUpgrade(out string message);
    bool gatherResource(out string message);

    // updates
    void newTurn();
}

public class Player : ViewablePlayer {

    // Player constants

    private static int maxActionPoints = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPoints"));
    private static int startingSupplies = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "startingSupplies"));
    private static int startingMaxSupplies = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "startingMaxSupplies"));

    private static int actionPointsToLeave = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPointsLeave"));
    private static int actionPointsToEncamp = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPointsEncamp"));

    private static float moraleUp = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraleup"));
    private static float moraleDown = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraledown"));

    private static int suppliesPerFertilityPoint = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "fertilitySupplies"));

    private static float followerDeathThreshold = 1e-3f;

    // Player variables

    private Tile tilePos;

    private List<Resource> resources;
    private List<Follower> followers;

    private bool encampment;

    private int supplies;
    private int maxSupplies;

    private int actionPoints;

    public Player() {
        this.tilePos = new HexTile(new Vector3(), new Vector2());
        this.resources = new List<Resource>();
        this.followers = new List<Follower>();
        initPlayer();
    }

    public void initPlayer() {
        addFollower(new Swordsman());
        addFollower(new Swordsman());
        addFollower(new Wizard());
        this.encampment = false;
        this.actionPoints = maxActionPoints;
        this.supplies = startingSupplies;
        this.maxSupplies = startingMaxSupplies;
    }

    public void newTurn() {
        // pre updates
        updateMorale();
        updateFollowers();
        this.supplies -= getFoodConsumption();
        if (this.supplies < 0) this.supplies = 0;

        // post updates
        if (encampment) { // in camp
            playerAddSupplies((int)(tilePos.fertility * suppliesPerFertilityPoint));
        } else { // not in camp
            // do nothing
        }
        
        // final updates
        this.actionPoints = maxActionPoints;
    }

    public void playerAddSupplies(int suppliesToAdd) {
        this.supplies += suppliesToAdd;
        if (this.supplies > maxSupplies)
            this.supplies = maxSupplies;
    }

    // updates the morale of each follower based on supplies
    public void updateMorale() {

        float adjustement, morale_adjusted;

        foreach (Follower f in followers) {

            if (this.supplies < this.getFoodConsumption()) {
                adjustement = moraleDown * (1 - f.getWillpower());
                morale_adjusted = f.getMorale() - adjustement;
            } else {
                adjustement = moraleUp * (1 - f.getMorale()) * (1 + f.getWillpower());
                morale_adjusted = f.getMorale() + adjustement;
            }

            if (morale_adjusted > 1f)
                morale_adjusted = 1f;
            if (morale_adjusted < 0f)
                morale_adjusted = 0f;

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
        return (followers.Count == 0) ? 0 : sum / followers.Count;
    }

    public void addFollower(Follower f) {
        this.followers.Add(f);
    }
    public void setPos(Tile pos) {
        this.tilePos = pos;
    }

    // getters
    public Tile getPosTile() { 
        return this.tilePos;
    }
    public Vector3 getPos() {
        return this.tilePos.getPos();
    }
    public Vector2 getPosIndex() { 
        return this.tilePos.index;
    }
    public int getActionPoints() {
        return this.actionPoints;
    }
    public int getMaxActionPoints() {
        return maxActionPoints;
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

    public bool getCampStatus() {
        return encampment;
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

    public void loseActionPoints(int actionPointsToLose) {
        if (this.actionPoints >= actionPointsToLose) {
            this.actionPoints -= actionPointsToLose;
        }
    }
    public bool attemptedMove(Vector3 moveToPos, out string message) {
        message = "Player move failed.";
        if (getPos() == moveToPos) {
            message = "Player move successful.";
            return true;
        }
        return false;
    }
    public bool changeCampStatus(out string message) {
        message = "";
        
        if (encampment) { // if in camp
            if (actionPoints - actionPointsToLeave >= 0) {
                encampment = false;
                loseActionPoints(actionPointsToLeave);
                message += "Successfully left camp.";
                return true;
            }
            message = "Failed to leave camp.";
        } else { // if not in camp
            if (actionPoints - actionPointsToEncamp >= 0) {
                encampment = true;
                loseActionPoints(actionPointsToEncamp);
                message += "Successfully built camp.";
                return true;
            }
            message = "Failed to build camp.";
        }
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
