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

        public TileAttribute(string tileAttributeName, bool applyEffect) {
            this.tileAttributeInfo = tileAttributeName;
            this._applyEffect = applyEffect;

            // TODO define these stats
            // temperatureEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "temperatureEffect"));
            // fertilityEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "fertilityEffect"));
            // humidityEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "humidityEffect"));
            // corruptionEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "corruptionEffect"));
            // dangerEffect = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/" + tileAttributeName, "dangerEffect"));
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
        public static float tribeProb= 0.02f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "tribeProb"));
        public static float forestProb = 0.75f; // = float.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/LocalTribe", "forestProb"));

        public LandTileType(bool applyEffect) : base("Land Tile", applyEffect) {
        }
        override
        public void applyEffect(Tile tile) {
            if (_applyEffect) {
                if (UnityEngine.Random.Range(0f,1f) < forestProb)
                    tile.addTileAttribute(new Forestry((float)Math.Pow(UnityEngine.Random.Range(0f, 1f), 2))); // random pow 5 to scale for less forests overall
                if (UnityEngine.Random.Range(0f,1f) < tribeProb)
                    tile.addTileAttribute(new LocalTribe(UnityEngine.Random.Range(0, LocalTribe.maxLevel)));
            }
        }
    }

    public class WaterTileType : TileType {
        public WaterTileType(bool applyEffect, float waterLevelElevation) : base("Water Tile", applyEffect) {
        }
        override
        public void applyEffect(Tile tile) {
            if (_applyEffect) {
                // update temperature: compute at water level elevation and not at tile's elevation
                tile.temperature = (HexRegion.computeTemperature(tile.getPos() - new UnityEngine.Vector3(0, tile.elevationToWater, 0)));
            }
        }
    }

    // ************************************** //
    // TILE ATTRIBUTES //

    public class Forestry : TileAttribute {
        public float forestryDensity { get; set; }

        public Forestry(float forestryDensity) : base("Forests", true) {
            this.forestryDensity = forestryDensity;
        }

        override
        public string ToString() {
            return this.tileAttributeInfo + " " + this.forestryDensity;
        }

        override
        public void applyEffect(Tile tile) {
            base.applyEffect(tile);

            int beasts = (int)(Beasts.maxLevel * this.forestryDensity);
            if (beasts > 0)
                tile.addTileAttribute(new Beasts(beasts));

            int animals = (int)(Animals.maxLevel * this.forestryDensity);
            if (animals > 0)
                tile.addTileAttribute(new Animals(animals));
        }
    }

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

    public class Animals : TileAttribute {
        // TODO load maxLevel from stats.xml
        public static int maxLevel = 10; // = int.Parse(Utilities.statsXMLreader.getParameterFromXML("TileAttributes/Animals", "maxLevel"));

        public int level { get; set; }

        public Animals(int level) : base("Animals", true) {
            this.level = (maxLevel < level) ? maxLevel : level;
            this.fertilityEffect = (level);
            this.pollutionEffect = (-level);
        }

        override
        public string ToString() {
            return this.tileAttributeInfo + " level " + level;
        }
    }

    public class LocalTribe : TileAttribute {
        // TODO load maxLevel from stats.xml
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