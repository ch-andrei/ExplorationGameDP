using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Followers {
    public class Follower {

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
            return "" + this.followerType + ": S " + getStrength() + "/" + getStrengthBeforeMorale() + ", H " + getHealthPoints() + ", M " + this.morale;
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
}