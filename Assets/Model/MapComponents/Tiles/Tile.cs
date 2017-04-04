using System.Collections.Generic;
using UnityEngine;

using TileAttributes;

namespace Tiles {

    public abstract class Tile {

        public Vector2 index { get; set; }

        public bool dirty { get; set; }

        // tile properties
        public float fertility { get; set; }
        public float humidity { get; set; }
        public float temperature { get; set; }
        public float pollution { get; set; }
        public float danger { get; set; }
        public float moveCostPenalty { get; set; } 

        public float elevationToWater { get; set; } // relative to water: negative when under water, else positive

        private Vector3 pos;

        private TileType tileType;

        // constructors
        public Tile(Vector3 pos, Vector2 index) {
            this.index = index;
            this.pos = pos;
            this.setTileType(new LandTileType(true));
            this.fertility = 0;
            this.humidity = 0;
            this.temperature = 0;
            this.pollution = 0;
            this.danger = 0;
            this.moveCostPenalty = 0;
            this.dirty = true;
        }

        public Tile(Vector3 pos, Vector2 index, TileType tileType) : this(pos, index) {
            this.setTileType(tileType);
        }

        override
        public string ToString() {
            string s = "";
            s += "Position: " + getPos();
            s += "\nCoordinate: [" + (int)(index.x) + ", " + (int)(index.y) + "]";
            s += "\n" + tileStats();
            return s;
        }

        public string tileStats() {
            string s = "";
            s += "Type:" + this.getTileType();
            s += "\nFertility: " + this.fertility;
            s += "\nHumidity: " + this.humidity;
            s += "\nTemperature: " + this.temperature;
            s += "\nPollution: " + this.pollution;
            s += "\nDanger: " + this.danger;
            s += "\nMove cost: " + this.moveCostPenalty;
            string tileAttrs = "\nTile Attributes:";
            foreach (TileAttribute ta in this.getTileAttributes()) {
                tileAttrs += "\n" + ta;
            }
            s += tileAttrs;
            return s;
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
        public abstract void computeGeometry();

        public bool Equals(Tile tile) {
            if (this.index == tile.index)
                return true;
            else
                return false;
        }

        // setters //
        public void setPos(Vector3 newpos) {
            setDirty();
            pos = newpos;
        }
        public void setX(float x) {
            setDirty();
            pos.x = x;
        }
        public void setY(float y) {
            setDirty();
            pos.y = y;
        }
        public void setZ(float z) {
            setDirty();
            pos.z = z;
        }
        public void setTileType(TileType tileType) {
            setDirty();
            this.tileType = tileType;
            this.tileType.applyEffect(this);
        }
        public void addTileAttribute(TileAttribute tileAttribute) {
            setDirty();
            this.tileType.addTileAttribute(tileAttribute);
            tileAttribute.applyEffect(this);
        }
        public void setDirty() {
            this.dirty = true;
        }
    }

    public class HexTile : Tile {

        // indexes for the Neighbors array
        public enum Directions : byte {
            TopRight = 0,
            Right = 1,
            BottomRight = 2,
            BottomLeft = 3,
            Left = 4,
            TopLeft = 5
        }

        // right, 
        public static Vector2[] Neighbors = new Vector2[] 
        {
            // order: top right, right, bottom right, bottom left, left, top left
            new Vector2(+1, -1),
            new Vector2(+1, 0),
            new Vector2(0, +1),
            new Vector2(-1, +1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };

        // static size and height since this is shared for all tiles within the region
        public static float size { get; set; }
        public static float height { get; set; }

        private Hexagon hexagon;

        // constructors
        public HexTile(Vector3 pos, Vector2 index) : base(pos, index) {
        }
        public HexTile(Vector3 pos, Vector2 index, TileType tileType) : base(pos, index, tileType) {
        }
        override
        public void computeGeometry() {
            hexagon = new Hexagon(this.getPos(), height, size);
        }

        override
        public Vector3[] getGeometry() {
            return this.hexagon.getVertices();
        }

        public static Vector3 AxialToCubeCoord(Vector2 axial) {
            return new Vector3(axial.x, axial.y, -axial.x - axial.y);
        }

        public static float distanceBetweenHexCoords(Vector2 a, Vector2 b) {
            return distanceBetweenHexCoords(AxialToCubeCoord(a), AxialToCubeCoord(b));
        }
        public static float distanceBetweenHexCoords(Vector3 a, Vector3 b) {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
        }
    }
}

