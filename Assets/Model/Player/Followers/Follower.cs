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

        //List<Upgrade> upgrades;

        private int foodDemand;

        public Follower(string followerType) {
            this.followerType = followerType;
            this.setFoodDemand(int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "foodDemand")));
            this.setMorale(float.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "morale")));
            this.setStrength(int.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "strength")));
            this.setWillpower(float.Parse(Utilities.statsXMLreader.getParameterFromXML("followers/" + this.followerType, "willpower")));
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
            return "" + this.followerType + ": S " + getStrength() + "/" + getStrengthBeforeMorale() + ", M " + this.morale;
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
}