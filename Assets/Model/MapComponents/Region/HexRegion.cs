using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TileAttributes;
using Tiles;

public static class RegionParams {

    // temperature parameters
    public static float worldAmbientTemperature;

    public static float worldScale;
    public static float worldCoreTemperature;
    public static float midTempElevation;

    public static float tanFalloffTemp;
    public static float tanOffsetTemp;

    public static float linFalloffTemp;

    public static float latitudeFactorTemp;

    // other
    public static float waterLevelElevation;

    public static void UpdateWorldGenerationParams(ViewableRegion region) {
        midTempElevation = region.getAverageElevation();
        tanOffsetTemp = midTempElevation * tanFalloffTemp;
        waterLevelElevation = region.getWaterLevelElevation();
    }

    public static void InitializeWorldGenerationParams(ViewableRegion region) {

        // TODO read from xml file

        worldAmbientTemperature = 45f;

        worldScale = 2500;
        worldCoreTemperature = 75f;

        tanFalloffTemp = 0.02f;
        linFalloffTemp = 0.05f;

        latitudeFactorTemp = 0.02f;
    }
}

public class HexRegion : ViewableRegion {

    private int n, seed, gridRadius, region_size;
    private float center;

    private float elevation;

    private int maxElevation, minElevation, averageElevation, waterLevelElevation;

    private Tile[,] tiles;

    private Noise noise;

    private float hexSize, hexHeight;

    public HexRegion(int n, int seed, int region_size, int elevation, float water, int rivers, Noise noise) {
        this.n = n;
        this.seed = seed;
        this.region_size = region_size;
        this.center = region_size / 2f;
        this.elevation = elevation;
        this.noise = noise;

        RegionParams.InitializeWorldGenerationParams(this);

        CreateHexPositionVectors(this.n);

        computeElevationParameters();

        RegionParams.UpdateWorldGenerationParams(this);

        computeTemperatures();

        generateWater(water);

        RegionParams.UpdateWorldGenerationParams(this);

        Debug.Log("Generated Region with " + this.getViewableTiles().Count + "/" + this.n + " viewable tiles; hexsize " + this.hexSize + ", hexheight " + this.hexHeight);
    }

    private void CreateHexPositionVectors(int n) {
        if (n < 1) return;
        this.gridRadius = getGridSizeForHexagonalGridWithNHexes(n);

        int array_size = 2 * this.gridRadius + 1;
        tiles = new HexTile[array_size, array_size];

        int counter = 0;
        this.hexSize = this.center / (array_size);
        this.hexHeight = (float)(Math.Sqrt(3f) / 2 * this.hexSize);

        HexTile.height = hexHeight;
        HexTile.size = hexSize;

        Tile tile;
        // hex cube coordinatess
        for (int X = -this.gridRadius; X <= this.gridRadius; X++) {
            for (int Y = -this.gridRadius; Y <= this.gridRadius; Y++) {
                int i, j;
                i = X + this.gridRadius;
                j = Y + this.gridRadius;
                if (Math.Abs(X + Y) > this.gridRadius ) {
                    tiles[i, j] = new HexTile(new Vector3(-1, -1, -1));
                    continue;
                }
                int Z = -X - Y;
                //Debug.Log("Making new tile at array index: " + i + ", " + j + "; hex coords: " + X + ", " + Y + ", " + (Y-Z));
                // unity axis coordinates
                float x = this.hexSize * X * 3 / 2;
                float z = this.hexHeight * (Y - Z);
                float y = this.elevation * noise.getNoiseValueAt((int)(x + this.center), (int)(z + this.center), this.region_size); // get elevation from Noise 
                // create vector3 for x y z
                tile = new HexTile(new Vector3(x, y, z), new LandTileType(true)); // height, size
                tiles[i, j] = tile;
                counter++;
            }
        }
    }

    private void computeTemperatures() {
        foreach (Tile tile in this.getViewableTiles()) {
            tile.computeTemperature(tile.getPos());
        }
    }

    public Tile getTileAt(Vector3 pos, out int[] index) {
        Vector3 v = worldCoordToIndex(pos.x, pos.z);
        int i, j;
        i = (int)v.x + this.gridRadius;
        j = (int)v.y + this.gridRadius;
        //Debug.Log(i + ", " + j);
        if (i < 0 || j < 0 || i >= tiles.GetLength(0) || j >= tiles.GetLength(0)) {
            index = null;
            return null;
        }
        index = new int[] { i, j };
        return this.tiles[i, j];
    }

    private Vector2 worldCoordToIndex(float x, float y) {
        float q = (x) * 2f / 3f / this.hexSize;
        float r = (float)(-(x) / 3f + Math.Sqrt(3f) / 3f * (y)) / this.hexSize;
        return roundCubeCoord(q, r);
    }

    private Vector3 roundCubeCoord(float X, float Y) {
        return roundCubeCoord(new Vector3(X, Y, -X - Y));
    }

    // code refactored from http://www.redblobgames.com/grids/hexagons/
    private Vector3 roundCubeCoord(Vector3 cubeCoord) {
        float rx = (float)Math.Round(cubeCoord.x);
        float ry = (float)Math.Round(cubeCoord.y);
        float rz = (float)Math.Round(cubeCoord.z);
        float x_diff = (float)Math.Abs(rx - cubeCoord.x);
        float y_diff = (float)Math.Abs(ry - cubeCoord.y);
        float z_diff = (float)Math.Abs(rz - cubeCoord.z);
        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;
        return new Vector3(rx, ry, rz);
    }

    private void generateWater(float waterLevelUnitFactor) {
        if (waterLevelUnitFactor > 1) waterLevelUnitFactor = 1;
        if (waterLevelUnitFactor < 0) waterLevelUnitFactor = 0;
        this.waterLevelElevation = (int)(this.minElevation + ((this.maxElevation - this.minElevation) +
                (this.averageElevation - this.minElevation)) / 2 * waterLevelUnitFactor);
        foreach (Tile tile in this.getViewableTiles()) {
            tile.elevationToWater = tile.getY() - waterLevelElevation;
            if (tile.elevationToWater <= 0) {
                tile.setTileType(new WaterTileType(true, waterLevelElevation));
            }
        }
        this.computeElevationParameters();
    }

    private int getGridSizeForHexagonalGridWithNHexes(int n) {
        int numberOfHexes = 1;
        int size = 1;
        while (numberOfHexes <= n) {
            numberOfHexes += (size++) * 6;
        }
        return size - 2;
    }

    private int numberOfHexesForGridSize(int gridSize) {
        if (gridSize <= 0) return 1;
        else {
            return 6 * gridSize + numberOfHexesForGridSize(gridSize - 1);
        }
    }

    private void computeElevationParameters() {
        this.minElevation = this.computeMinimumElevation();
        this.maxElevation = this.computeMaximumElevation();
        this.averageElevation = this.computeAverageElevation();
    }

    public int computeAverageElevation() {
        long sum = 0;
        List<Tile> tiles = getViewableTiles();
        foreach (Tile tile in tiles) {
            sum += (int)tile.getY();
        }
        return (int)(sum / (tiles.Count));
    }

    public int computeMaximumElevation() {
        int max = 0;
        List<Tile> tiles = getViewableTiles();
        foreach (Tile tile in tiles) {
            if (max < tile.getY()) {
                max = (int)tile.getY();
            }
        }
        return max;
    }

    public int computeMinimumElevation() {
        int min = int.MaxValue;
        List<Tile> tiles = getViewableTiles();
        foreach (Tile tile in tiles) {
            if (min > tile.getY()) {
                min = (int)tile.getY();
            }
        }
        if (min == int.MaxValue) min = -1;
        return min;
    }

    // getters //
    public List<Tile> getViewableTiles() {
        List<Tile> tiles = new List<Tile>();
        foreach (Tile tile in this.tiles)
            if (tile.getTileType() != null)
                tiles.Add(tile);
        return tiles;
    }
    public int getMinimumElevation() {
        return this.minElevation;
    }
    public int getMaximumElevation() {
        return this.maxElevation;
    }
    public int getAverageElevation() {
        return this.averageElevation;
    }
    public int getWaterLevelElevation() {
        return this.waterLevelElevation;
    }
    public int getViewableSize() {
        return this.region_size;
    }
    public long getViewableSeed() {
        return this.seed;
    }
}
