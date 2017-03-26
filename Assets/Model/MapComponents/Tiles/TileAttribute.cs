using System.Collections.Generic;
using System;
using UnityEngine;

using Tiles;

namespace TileAttributes {

    public abstract class TileAttribute {
        public string tileAttributeInfo;

        public bool _applyEffect { get; set; }

        public float temperatureEffect { get; set; }
        public float fertilityEffect { get; set; }
        public float humidityEffect { get; set; }
        public float pollutionEffect { get; set; }
        public float dangerEffect { get; set; }
        public float moveCostEffect { get; set; }

        public TileAttribute(string tileAttributeName, bool applyEffect) {
            this.tileAttributeInfo = tileAttributeName;
            this._applyEffect = applyEffect;
            temperatureEffect = 0;
            fertilityEffect = 0;
            humidityEffect = 0;
            pollutionEffect = 0;
            dangerEffect = 0;
            moveCostEffect = 0;

            // TODO define these stats
            // temperatureEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "temperatureEffect"));
            // fertilityEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "fertilityEffect"));
            // humidityEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "humidityEffect"));
            // corruptionEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "corruptionEffect"));
            // dangerEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "dangerEffect"));
            // moveCostEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "moveCostEffect"));
        }

        override
        public string ToString() {
            return this.tileAttributeInfo;
        }

        public virtual void applyEffect(Tile tile) {
            tile.fertility += fertilityEffect;
            tile.humidity += humidityEffect;
            tile.temperature += temperatureEffect;
            tile.pollution += pollutionEffect;
            tile.danger += dangerEffect;
            tile.moveCostPenalty += moveCostEffect;
        }
    }

    // ************************************** //
    // TILE TYPES //
    public abstract class TileType : TileAttribute {

        private List<TileAttribute> tileAttributes { get; }

        public TileType(string s, bool applyEffect) : base(s, applyEffect) {
            tileAttributes = new List<TileAttribute>();
        }

        public List<TileAttribute> getTileAttributes() {
            return tileAttributes;
        }

        public void addTileAttribute(TileAttribute tileAttribute) {
            tileAttributes.Add(tileAttribute);
        }
    }

    public class LandTileType : TileType {
        // TODO load from stats.xml
        public static float tribeProb= 0.05f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "tribeProb"));
        public static float forestProb = 0.75f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "forestProb"));
        public static float marshProb = 0.05f;// = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "marshProb"));

        public LandTileType(bool applyEffect) : base("Land Tile", applyEffect) {
        }
        override
        public void applyEffect(Tile tile) {
            if (_applyEffect) {
                if (UnityEngine.Random.Range(0f, 1f) < forestProb) {
                    float val = (float)Math.Pow(UnityEngine.Random.Range(0f, 1f), 2); // random pow 5 to scale for less forests overall
                    tile.addTileAttribute(new Forestry(val)); 
                    tile.addTileAttribute(new Grassland(1-val));
                } else {
                    tile.addTileAttribute(new Grassland(UnityEngine.Random.Range(0f, 1f)));
                }
                if (UnityEngine.Random.Range(0f, 1f) < tribeProb)
                    tile.addTileAttribute(new Marshland(UnityEngine.Random.Range(0f, 1f)));
                if (UnityEngine.Random.Range(0f,1f) < tribeProb)
                    tile.addTileAttribute(new LocalTribe(UnityEngine.Random.Range(0, LocalTribe.maxLevel)));
                base.applyEffect(tile);
            }
        }
    }

    public class WaterTileType : TileType {
        public WaterTileType(bool applyEffect, float waterLevelElevation) : base("Water Tile", applyEffect) {
            this.moveCostEffect = float.MaxValue;
        }
        override
        public void applyEffect(Tile tile) {
            if (_applyEffect) {
                // update temperature: compute at water level elevation and not at tile's elevation
                tile.temperature = (HexRegion.computeTemperatureAtPos(tile.getPos() - new UnityEngine.Vector3(0, tile.elevationToWater, 0)));
                
                base.applyEffect(tile);
            }
        }
    }

    // ************************************** //
    // TILE TERRAIN ATTRIBUTES //

    public class Forestry : TileAttribute {

        public static float moveCostPenaltyFactor = 2f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Forestry", "moveCostPenaltyFactor"));
        public static float woodResourceFactor = 10f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Forestry", "woodResourceFactor"));

        public float forestryDensity { get; set; }

        public Forestry(float forestryDensity) : base("Forests", true) {
            this.forestryDensity = forestryDensity;
            this.moveCostEffect = forestryDensity * moveCostPenaltyFactor;
        }

        override
        public string ToString() {
            return tileAttributeInfo + " " + forestryDensity;
        }

        override
        public void applyEffect(Tile tile) {
            base.applyEffect(tile);

            int beasts = (int)(Beasts.maxLevel * forestryDensity);
            if (beasts > 0)
                tile.addTileAttribute(new Beasts(beasts));

            int animals = (int)(Animals.maxLevel * forestryDensity);
            if (animals > 0)
                tile.addTileAttribute(new ForestAnimals(animals));

            tile.addTileAttribute(new WoodResource((int)(woodResourceFactor * forestryDensity)));
        }
    }

    public class Grassland : TileAttribute {

        public static float moveCostPenaltyFactor = 0.01f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Grassland", "moveCostPenaltyFactor"));
        public static float humidityFactor = 0.1f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Grassland", "humidityFactor"));
        public static float fertilityFactor = 2f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Grassland", "fertilityFactor"));
        public static float fertilityBaseFactor = 2f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Grassland", "fertilityFactor"));

        public float grasslandDensity { get; set; }

        public Grassland(float grasslandDensity) : base("Grassland", true) {
            this.grasslandDensity = grasslandDensity;
            this.moveCostEffect = grasslandDensity * moveCostPenaltyFactor;
            this.humidityEffect = grasslandDensity * humidityFactor;
            this.fertilityEffect += fertilityBaseFactor + fertilityFactor * grasslandDensity;
        }

        override
        public string ToString() {
            return tileAttributeInfo + " " + grasslandDensity;
        }

        override
        public void applyEffect(Tile tile) {
            base.applyEffect(tile);

            int animals = (int)(Animals.maxLevel * grasslandDensity);
            if (animals > 0)
                tile.addTileAttribute(new GrasslandAnimals(animals));
        }
    }

    public class Marshland : TileAttribute {

        public static float moveCostPenaltyFactor = 0.75f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Marshland", "moveCostPenaltyFactor"));
        public static float humidityFactor = 0.75f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Marshland", "humidityFactor"));
        public static float fertilityFactor = -0.75f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Marshland", "fertilityFactor"));
        public static float fertilityBaseFactor = -1f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Marshland", "fertilityFactor"));

        public float marshDensity { get; set; }

        public Marshland(float marshDensity) : base("Marshland", true) {
            this.marshDensity = marshDensity;
            this.moveCostEffect = marshDensity * moveCostPenaltyFactor;
            this.humidityEffect = marshDensity * humidityFactor;
            this.fertilityEffect += fertilityBaseFactor + fertilityFactor * marshDensity;
        }

        override
        public string ToString() {
            return tileAttributeInfo + " " + marshDensity;
        }

        override
        public void applyEffect(Tile tile) {
            base.applyEffect(tile);

        }
    }

    /// ***  *** ///

    public class Beasts : TileAttribute {
        // TODO load maxLevel from stats.xml
        public static int maxLevel = 5; // = int.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Wild Beasts", "maxLevel"));

        public int level { get; set; }

        public Beasts(int level) : base("Beasts", true) {
            this.level = (maxLevel < level) ? maxLevel : level;
            this.dangerEffect = level;
            this.pollutionEffect = level;
        }

        override
        public string ToString() {
            return this.tileAttributeInfo + " level " + level;
        }
    }

    public abstract class Animals : TileAttribute {
        // TODO load maxLevel from stats.xml
        public static int maxLevel = 10; // = int.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Animals", "maxLevel"));
        public float fertilityFactor { get; set; }
        public float pollutionFactor { get; set; }

        public int level { get; set; }

        public Animals(int level, float fertilityFactor, float pollutionFactor, string tileAttributeName="Animals") : base(tileAttributeName, true) {
            this.level = (maxLevel < level) ? maxLevel : level;
            this.fertilityFactor = fertilityFactor;
            this.pollutionFactor = pollutionFactor;
            this.fertilityEffect = (level * this.fertilityFactor);
            this.pollutionEffect = (level * this.pollutionFactor);
        }

        override
        public string ToString() {
            return this.tileAttributeInfo + " level " + level;
        }
    }

    public class ForestAnimals : Animals {
        new public static float fertilityFactor = 0.25f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/ForestAnimals", "fertilityFactor"));
        new public static float pollutionFactor = -0.5f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/ForestAnimals", "pollutionFactor"));

        public ForestAnimals(int level) : base(level, fertilityFactor, pollutionFactor, "Forest Animals") {
            this.level = (maxLevel < level) ? maxLevel : level;
        }
    }

    public class GrasslandAnimals : Animals {
        new public static float fertilityFactor = 0.5f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/GrasslandAnimals", "fertilityFactor"));
        new public static float pollutionFactor = -0.25f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/GrasslandAnimals", "pollutionFactor"));

        public GrasslandAnimals(int level) : base(level, fertilityFactor, pollutionFactor, "Grassland Animals") {
            this.level = (maxLevel < level) ? maxLevel : level;
        }
    }

    public class LocalTribe : TileAttribute {
        public static int maxLevel = 10; // = int.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "maxLevel"));
        public static float minHostility = 0f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "minHostility"));
        public static float maxHostility = 1f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "maxHostility"));

        public float hostility { get; set; }
        public int level { get; set; }

        public LocalTribe(int level) : base("Local Tribe", true) {
            this.level = (maxLevel < level) ? maxLevel : level;
            this.hostility = UnityEngine.Random.Range(minHostility, maxHostility);
            this.dangerEffect = (level * hostility);
            this.pollutionEffect = (-level);
        }

        override
        public string ToString() {
            return this.tileAttributeInfo + " level " + level;
        }
    }

    // ************************************** //
    // RESOURCES //

    public abstract class Resource : TileAttribute {
        string resourceName;
        private int quantity;

        public Resource(string name, int quantity) : base("Resource/" + name, true) {
            this.resourceName = name;
            this.quantity = quantity;
        }
        public bool isOfType(string type) {
            if (type == this.resourceName)
                return true;
            else
                return false;
        }

        override
        public string ToString() {
            return this.quantity + " " + this.resourceName;
        }

        override
        public void applyEffect(Tile tile) {
            base.applyEffect(tile);
            // TODO add to some tile properties
        }
    }

    public class MetalResource : Resource {
        public MetalResource(int quantity) : base("Metal", quantity) {
        }
    }

    public class PreciousMetalResource : Resource {
        public PreciousMetalResource(int quantity) : base("Precious Metal", quantity) {
        }
    }

    public class WoodResource : Resource {
        public WoodResource(int quantity) : base("Wood", quantity) {
        }
    }

    public class ClayResource : Resource {
        public ClayResource(int quantity) : base("Clay", quantity) {
        }
    }
}