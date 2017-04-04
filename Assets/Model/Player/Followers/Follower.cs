using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Followers {
    public class Follower {

        public int recruitCost { get; }

        [Range(0f, 1f)]
        private float morale;

        private float willpower;

        private string followerType;

        private int strength;

        private int healthPoints, maxHeathPoints;

        //List<Upgrade> upgrades;

        private int foodDemand;

        public Follower(string followerType) {
            this.followerType = followerType;
            foodDemand = (int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "foodDemand")));
            morale = (float.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "morale")));
            strength = (int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "strength")));
            healthPoints = maxHeathPoints = (int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "health")));
            willpower = (float.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "willpower")));
            recruitCost = (int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "recruitCost")));
        }

        public void changeHealthPoints(int hpChange) {
            this.healthPoints += hpChange;
            if (this.healthPoints < 0) {
                this.healthPoints = 0;
            } else if (this.healthPoints > maxHeathPoints) {
                this.healthPoints = maxHeathPoints;
            }
        }
        public int getFoodDemand() {
            return this.foodDemand;
        }
        public float getMorale() {
            return this.morale;
        }
        public int getStrengthBeforeMorale() {
            return this.strength;
        }
        public int getStrength() {
            return (int)(strength * this.morale);
        }
        public int getHealthPoints() {
            return this.healthPoints;
        }
        public int getMaxHealthPoints() {
            return this.maxHeathPoints;
        }
        public float getWillpower() {
            return this.willpower;
        }
        public void setFoodDemand(int foodDemand) {
            this.foodDemand = foodDemand;
        }
        public void setMorale(float morale) {
            this.morale = morale;
        }
        public void setStrength(int strength) {
            this.strength = strength;
        }
        public void setWillpower(float mentalStrength) {
            this.willpower = mentalStrength;
        }
        override
        public string ToString() {
            return "" + getFollowerType() + ": " + statsAsString();
        }
        public string statsAsString(bool strength = true, bool health = true, bool morale = true) {
            string stats = "";
            if(strength)
                stats += "S " + getStrength() + "/" + getStrengthBeforeMorale() + " ";
            if (health)
                stats += "HP " + getHealthPoints() + "/" + getMaxHealthPoints() + " ";
            if (morale)
                stats += "M " + (this.morale).ToString("0.0") + " ";
            return stats;
        }
        public string getFollowerType() {
            return this.followerType;
        }
    }

    // children of the Follower class will have their stats loaded from the stats.xml file

    public class Wizard : Follower {
        public Wizard() : base("Wizard") {
        }
    }

    public class Swordsman : Follower {
        public Swordsman() : base("Swordsman") {
        }
    }

    public class Peasant : Follower {
        public Peasant() : base("Peasant") {
        }
    }

    public class Archer : Follower {
        public Archer() : base("Archer") {
        }
    }

    public class PeasantArcher : Follower {
        public PeasantArcher() : base("PeasantArcher") {
        }
    }
}