using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Pathfinding;
using Viewable;
using Tiles;
using Followers;
using TileAttributes;

namespace Playable {

    public abstract class Player : ViewablePlayer {

        // Player constants

        protected static int maxActionPoints = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPoints"));
        protected static int startingSupplies = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "startingSupplies"));
        protected static int startingMaxSupplies = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "startingMaxSupplies"));

        protected static int actionPointsToLeave = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPointsLeave"));
        protected static int actionPointsToEncamp = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "actionPointsEncamp"));

        protected static float moraleUp = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraleup"));
        protected static float moraleDown = float.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "moraledown"));

        protected static int suppliesPerFertilityPoint = int.Parse(Utilities.statsXMLreader.getParameterFromXML("player", "fertilitySupplies"));

        // Player variables

        private bool _dead;
        public bool dead { get { return this._dead; } }

        public long id { get; set; }

        protected Tile tilePos;

        protected List<Resource> resources;
        protected List<Follower> followers;

        protected bool encampment;

        protected int supplies;
        protected int maxSupplies;

        protected int actionPoints;

        public Player() : base() {
            this.tilePos = new HexTile(new Vector3(), new Vector2());
            this.resources = new List<Resource>();
            this.followers = new List<Follower>();
            this.encampment = false;
            this.actionPoints = maxActionPoints;
            this.supplies = startingSupplies;
            this.maxSupplies = startingMaxSupplies;
            this._dead = false;
            this.id = DateTime.Now.Ticks;
            initPlayer();
        }

        public abstract void initPlayer();

        public virtual void newTurn() {
            // pre updates
            updateMorale();
            updateHealthPoints();
            checkFollowersHealthPoints();
            this.supplies -= getFoodConsumption();
            if (this.supplies < 0) this.supplies = 0;

            // post updates
            if (encampment) { // in camp
                int supplies = (int)((tilePos.fertility - tilePos.danger) * suppliesPerFertilityPoint);
                if (supplies > 0)
                    playerAddSupplies(supplies);
            } else { // not in camp
                     // do nothing
            }

            // final updates
            this.actionPoints = maxActionPoints;

            checkPlayerAlive();
        }

        public void playerAddSupplies(int suppliesToAdd) {
            this.supplies += suppliesToAdd;
            if (this.supplies > maxSupplies)
                this.supplies = maxSupplies;
        }

        public void kill() {
            followers.RemoveRange(0, followers.Count);
            this.supplies = 0;
            this._dead = true;
            notifyDestroy();
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

        public void distributeDamage(float damage) {
            float average = damage / followers.Count;
            foreach (Follower f in followers) {
                f.changeHealthPoints(-(int)average);
            }
        }

        // updates the morale of each follower based on supplies
        public void updateHealthPoints() {
            foreach (Follower f in followers) {
                if (this.supplies < this.getFoodConsumption()) {
                    f.changeHealthPoints(-1);
                } else {
                    f.changeHealthPoints(1);
                }
            }
        }

        public void checkFollowersHealthPoints() {
            Follower f;
            for (int i = 0; i < followers.Count; i++) {
                f = followers[i];
                if (f.getHealthPoints() <= 0) {
                    followers.RemoveAt(i);
                    i--; // adjust to acount for removing
                }
            }
        }

        public int getFollowersMaxHealth() {
            int hpSum = 0;
            foreach (Follower f in followers) {
                hpSum += f.getMaxHealthPoints();
            }
            return hpSum;
        } // meow - Jessy Yu

        public int getFollowersHealth() {
            int hpSum = 0;
            foreach (Follower f in followers) {
                hpSum += f.getHealthPoints();
            }
            return hpSum;
        }

        private void checkPlayerAlive() {
            if (followers.Count == 0) {
                this.kill();
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
        public void setTilePos(Tile pos) {
            this.tilePos = pos;
        }

        // getters
        public Tile getTilePos() {
            return this.tilePos;
        }
        public Vector3 getPos() {
            return this.tilePos.getPos();
        }
        public Vector2 getTilePosIndex() {
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
        public int getMaxSupplies() {
            return this.maxSupplies;
        }
        public string getFollowersAsString() {
            string s = "";
            foreach (Follower f in followers) {
                s += f + "\n";
            }
            return s;
        }
        public List<Follower> getFollowers() {
            return this.followers;
        }

        public bool getCampStatus() {
            return encampment;
        }
        public bool getCampStatus(out string message) {
            message = "";
            return encampment;
        }
        public bool getCurrentUpgrades(out string message) {
            message = "";
            return false;
        }
        public bool getPossibleUpgrades(out string message) {
            message = "";
            return false;
        }
        public List<Follower> getRecruitableFollowers() {
            // TODO do this without hardcoding
            List<Follower> recruitable = new List<Follower>();
            recruitable.Add(new Peasant());
            recruitable.Add(new PeasantArcher());
            recruitable.Add(new Swordsman());
            recruitable.Add(new Archer());
            recruitable.Add(new Wizard());
            return recruitable;
        }

        override
        public string ToString() {
            string str = "";
            str += "Player info:\nPosition: " + getPos() +
            "\nCoordinates: " + getTilePosIndex() +
            "\nEncampment: " + getCampStatus() +
            "\nAction points: " + getActionPoints() +
            "\nStrength: " + computeStrength() +
            "\nMorale: " + computeMorale() +
            "\nSupplies: " + getSupplies() +
            "\nSupplies Consumption per turn: " + getFoodConsumption() +
            "\nFollowers:\n" + getFollowersAsString();
            return str;
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
                    this.notifyView();
                    return true;
                }
                message = "Failed to leave camp.";
            } else { // if not in camp
                if (actionPoints - actionPointsToEncamp >= 0) {
                    encampment = true;
                    loseActionPoints(actionPointsToEncamp);
                    message += "Successfully built camp.";
                    this.notifyView();
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

    public class PlayablePlayer : Player {
        override
        public void initPlayer() {
            addFollower(new Swordsman());
            addFollower(new Swordsman());
            addFollower(new Wizard());
        }
    }

    public class NonPlayablePlayer : Player {

        public NonPlayablePlayer(int supplies, int strength) : base(){

            this.supplies = supplies;

            while (this.computeStrength() < strength) {
                Follower f;

                // randomize selection of f
                f = new Swordsman();

                if (this.computeStrength() + f.getStrength() <= strength)
                    this.addFollower(f);
                else
                    break;
            }
        }

        override
        public void initPlayer() {     
            // do nothing
        }

        override
        public void newTurn() {
            // final updates
            this.actionPoints = maxActionPoints;
        }
    }
}

