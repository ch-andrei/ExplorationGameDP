using System.Collections.Generic;
using UnityEngine;

using TileAttributes;

namespace Tiles {
    public abstract class Tile {

        private Vector3 pos;

        private TileType tileType;

        // tile properties
        public float fertility { get; set; }
        public float humidity { get; set; }
        public float temperature { get; set; }
        public float pollution { get; set; }
        public float danger { get; set; }

        public float elevationToWater { get; set; } // relative to water: negative when under water, else positive

        // constructors
        public Tile(Vector3 pos) {
            this.pos = pos;
            this.setTileType(new LandTileType(true));
            this.fertility = 0;
            this.humidity = 0;
            this.temperature = 0;
            this.pollution = 0;
            this.danger = 0;
            }

        public Tile(Vector3 pos, TileType tileType) : this(pos) {
            this.setTileType(tileType);
        }

        override
        public string ToString() {
            string s = "";
            s += "Type:" + this.getTileType();
            s += "\nFertility: " + this.fertility;
            s += "\nHumidity: " + this.humidity;
            s += "\nTemperature: " + this.temperature;
            s += "\nPollution: " + this.pollution;
            s += "\nDanger: " + this.danger;
            string tileAttrs = "\nTile Attributes:";
            foreach (TileAttribute ta in this.getTileAttributes()) {
                tileAttrs += "\n" + ta;
            }
            s += tileAttrs;
            return s;
        }

        public void computeTemperature(Vector3 tilePos) {
            // elevation using atan
            this.temperature = RegionParams.worldCoreTemperature * (1f - 1f / Mathf.PI * (Mathf.PI / 2f + Mathf.Atan(tilePos.y * RegionParams.tanFalloffTemp - RegionParams.tanOffsetTemp)));
            // elevation linear
            this.temperature -= RegionParams.linFalloffTemp * (tilePos.y - RegionParams.midTempElevation);
            // latitude exponential
            this.temperature -= RegionParams.worldCoreTemperature * (Mathf.Exp(RegionParams.latitudeFactorTemp * (Mathf.Abs(tilePos.z) / RegionParams.worldScale)) - 1);
        }
        // getters //
        public Vector3 getPos() {
            return pos;
        }
        public float getX() {
            return pos.x;
        }
        public float getY() {
            return pos.y;
        }
        public float getZ() {
            return pos.z;
        }
        public TileAttribute getTileType() {
            return this.tileType;
        }
        public List<TileAttribute> getTileAttributes() {
            return this.tileType.getTileAttributes();
        }
        public abstract Vector3[] getGeometry();

        // setters //
        public void setPos(Vector3 newpos) {
            pos = newpos;
        }
        public void setX(float x) {
            pos.x = x;
        }
        public void setY(float y) {
            pos.y = y;
        }
        public void setZ(float z) {
            pos.z = z;
        }
        public void setTileType(TileType tileType) {
            this.tileType = tileType;
            this.tileType.applyEffect(this);
        }
        public void addTileAttribute(TileAttribute tileAttribute) {
            this.tileType.addTileAttribute(tileAttribute);
            tileAttribute.applyEffect(this);
        }
    }

    public class HexTile : Tile {

        // static size and height since this is shared for all tiles within the region
        public static float size { get; set; }
        public static float height { get; set; }

        private Hexagon hexagon;

        // constructors
        public HexTile(Vector3 pos) : base(pos) {
            hexagon = new Hexagon(pos, height, size);
        }
        public HexTile(Vector3 pos, TileType tileType) : base(pos, tileType) {
            hexagon = new Hexagon(pos, height, size);
        }

        override
        public Vector3[] getGeometry() {
            return this.hexagon.getVertices();
        }
    }
}

