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

    // erosion parameters
    public static float erosionWaterAmount;
    public static float erosionStrength;
    public static float erosionWaterLoss;
    public static float earthStability;
    public static float erosionVelocityElevationCap;
    public static int erosionIterations;

    public static void UpdateWorldGenerationParams(ViewableRegion region) {
        midTempElevation = region.getAverageElevation();
        tanOffsetTemp = midTempElevation * tanFalloffTemp;
        waterLevelElevation = region.getWaterLevelElevation();
    }

    public static void InitializeWorldGenerationParams(ViewableRegion region) {

        // TODO read from xml file

        worldAmbientTemperature = 45f;

        worldScale = 500;
        worldCoreTemperature = 125f;

        tanFalloffTemp = 0.005f;
        linFalloffTemp = 0.025f;

        latitudeFactorTemp = 0.025f;

        // erosion parameters
        erosionWaterAmount = 10f;
        erosionStrength = 0.75f;
        erosionWaterLoss = 0.99f;
        erosionIterations = 200;
        earthStability = 0.25f;
        erosionVelocityElevationCap = 50f;
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

        // compute elevation before erosion
        computeElevationParameters();

        // TODO 
        computeErosion(RegionParams.erosionWaterAmount, RegionParams.erosionStrength, RegionParams.erosionWaterLoss,
            RegionParams.earthStability, RegionParams.erosionVelocityElevationCap, RegionParams.erosionIterations);
        // RegionParams.UpdateWorldGenerationParams(this);

        // compute elevation after erosion
        computeElevationParameters();

        RegionParams.UpdateWorldGenerationParams(this);

        computeTemperatures();

        // TODO
        // computeHumidity()

        generateWaterTiles(water);

        RegionParams.UpdateWorldGenerationParams(this);

        computeTileGeometry(); // compute hexagon positions

        Debug.Log("Generated Region with " + this.getViewableTiles().Count + "/" + this.n + " viewable tiles; hexsize " + this.hexSize + ", hexheight " + this.hexHeight);
    }

    private void CreateHexPositionVectors(int n) {

        // return if n too small
        if (n < 1)
            return;

        // compute required array dimenssions
        this.gridRadius = getGridSizeForHexagonalGridWithNHexes(n);
        int array_size = 2 * this.gridRadius + 1;
        tiles = new HexTile[array_size, array_size];

        // compute tile hexagon dimensions
        this.hexSize = this.center / (array_size);
        this.hexHeight = (float)(Math.Sqrt(3f) / 2 * this.hexSize);
        HexTile.height = hexHeight;
        HexTile.size = hexSize;

        Tile tile;
        // loop over X and Y in hex cube coordinatess
        for (int X = -this.gridRadius; X <= this.gridRadius; X++) {
            for (int Y = -this.gridRadius; Y <= this.gridRadius; Y++) {

                int i, j;
                i = X + this.gridRadius;
                j = Y + this.gridRadius;

                // if outside of the hexagonal region
                if (Math.Abs(X + Y) > this.gridRadius) {
                    tiles[i, j] = null;
                    continue;
                }

                // compute hex Z coord
                int Z = -X - Y;

                // compute unity axis coordinates
                float x = this.hexSize * X * 3 / 2;
                float z = this.hexHeight * (Y - Z);
                float y = this.elevation * noise.getNoiseValueAt((int)(x + this.center), (int)(z + this.center), this.region_size); // get elevation from Noise 

                // initialize tile
                tile = new HexTile(new Vector3(x, y, z), new Vector2(i, j), new LandTileType(false)); // height, size
                tiles[i, j] = tile;
            }
        }
    }

    private void computeTileGeometry() {
        foreach (Tile tile in this.getViewableTiles()) {
            tile.computeGeometry();
        }
    }

    // *** TILE POSITION COMPUTATIONS *** //

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

    public List<Tile> getTileNeighbors(Vector3 tilePos) {
        return getTileNeighbors(worldCoordToIndex(tilePos));
    }
    public List<Tile> getTileNeighbors(Vector2 tileIndex) {
        List<Tile> neighbors = new List<Tile>();
        foreach (Vector2 dir in HexTile.Neighbors) {
            try {
                Vector2 index = tileIndex + dir;
                Tile neighbor = this.tiles[(int)index.x, (int)index.y];
                if (neighbor != null)
                    neighbors.Add(neighbor);
            } catch (IndexOutOfRangeException e) {
                // nothing to do
            }
        }
        return neighbors;
    }

    private Vector2 worldCoordToIndex(Vector2 pos) {
        return worldCoordToIndex(pos.x, pos.y);
    }
    private Vector2 worldCoordToIndex(float x, float y) {
        float q = (x) * 2f / 3f / this.hexSize;
        float r = (float)(-(x) / 3f + Math.Sqrt(3f) / 3f * (y)) / this.hexSize;
        return roundCubeCoord(q, r);
    }

    // code refactored from http://www.redblobgames.com/grids/hexagons/
    private Vector3 roundCubeCoord(float X, float Y) {
        return roundCubeCoord(new Vector3(X, Y, -X - Y));
    }
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

    // *** TEMPERATURE COMPUTATIONS *** //

    private void computeTemperatures() {
        foreach (Tile tile in this.getViewableTiles()) {
            computeTemperature(tile);
        }
    }
    public static void computeTemperature(Tile tile) {
        tile.temperature = (computeTemperature(tile.getPos()));
    }
    public static float computeTemperature(Vector3 tilePos) {
        // elevation using atan
        float temperature = RegionParams.worldCoreTemperature * (1f - 1f / Mathf.PI * (Mathf.PI / 2f + Mathf.Atan(tilePos.y * RegionParams.tanFalloffTemp - RegionParams.tanOffsetTemp)));
        // elevation linear
        temperature -= RegionParams.linFalloffTemp * (tilePos.y - RegionParams.midTempElevation);
        // latitude exponential
        temperature -= RegionParams.worldCoreTemperature * (Mathf.Exp(RegionParams.latitudeFactorTemp * (Mathf.Abs(tilePos.z) / RegionParams.worldScale)) - 1);
        return temperature;
    }

    // *** EROSSION COMPUTATIONS *** //

    private void computeErosion(float waterAmount, float strength, float waterLoss, float earthStability, float velocityElevationCap, int iterations) {
        float[,] waterVolumes = new float[this.tiles.GetLength(0), this.tiles.GetLength(0)];
        erosionDepositWater(waterVolumes, waterAmount);
        while (iterations-- > 0) {
            computeErosionIteration(waterVolumes, strength, waterLoss, earthStability, velocityElevationCap);
            computeTileGeometry();
            Debug.Log("Iterations left " + iterations);
        }
        Utilities.normalize(waterVolumes);
        foreach (Tile tile in this.getViewableTiles()) {
            tile.humidity = waterVolumes[(int)tile.index.x, (int)tile.index.y];
        }
    }

    private void computeErosionIteration(float[,] waterVolumes, float strength, float waterLoss, float earthStability, float velocityElevationCap) {

        float velocityElevationToProximityRatio = 0.95f;
        float velocityProximityInfluence = 0.25f;

        // deep copy of arrays
        float[,] waterUpdated = new float[waterVolumes.GetLength(0), waterVolumes.GetLength(0)];
        for (int i = 0; i < waterUpdated.GetLength(0); i++) {
            for (int j = 0; j < waterUpdated.GetLength(0); j++) {
                waterUpdated[i, j] = waterVolumes[i, j];
            }
        }

        Tile current;
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {

                current = this.tiles[i, j];

                if (current != null) {
                    // get tile neighbors
                    List<Tile> neighbors = getTileNeighbors(current.index);

                    Tile neighbor;
                    for (int k = 0; k < neighbors.Count; k++) {
                        neighbor = new HexTile(neighbors[k].getPos(), neighbors[k].index);
                    }

                    // sort in ascending order: lowest first
                    neighbors.Sort((x,y) => x.getPos().y.CompareTo(y.getPos().y));

                    // TEMP
                    // temporary addition: only use the lowest tile neighbor -> remove all but the lowest neighbor
                    neighbors.RemoveRange(1, neighbors.Count - 1);
                    // TEMP END

                    // compute elevation influenced 'velocity' vectors
                    List<Vector3> elevationGradientWithWater = new List<Vector3>();
                    List<Vector3> elevationGradient= new List<Vector3>();
                    foreach (Tile t in neighbors) {
                        elevationGradientWithWater.Add(((current.getPos() + new Vector3(0,  waterVolumes[(int)current.index.x, (int)current.index.y], 0)) - 
                            (t.getPos() + new Vector3(0, waterVolumes[(int)t.index.x, (int)t.index.y], 0))));
                        elevationGradient.Add(current.getPos() - t.getPos());
                    }

                    neighbor = null;
                    for (int k = 0; k < neighbors.Count; k++) {
                        if (elevationGradientWithWater[k].y > 0) {
                            // get neighbor
                            neighbor = neighbors[k];

                            // if no water left move on to next tile
                            if (waterUpdated[(int)current.index.x, (int)current.index.y] <= 0) {
                                waterUpdated[(int)current.index.x, (int)current.index.y] = 0;
                                break;
                            }

                            float waterVelocity;
                            // velocity from elevation
                            waterVelocity = 1f - Mathf.Exp(-elevationGradientWithWater[k].y / velocityElevationCap);
                            // velocity from proximity
                            // do weighted sum based on constants
                            waterVelocity = waterVelocity * velocityElevationToProximityRatio + 
                                (1 - velocityElevationToProximityRatio) * velocityProximityInfluence;

                            float waterLossAmount = strength * waterVelocity * waterUpdated[(int)current.index.x, (int)current.index.y];

                            //Debug.Log("velocity " + waterVelocity + "; waterLossAmount " + waterLossAmount + " from " + current.index + " to " + neighbor.index + " using elev diff " + elevationGradientWithWater[k].y);

                            // check if want to erode more water than currently available
                            if (waterLossAmount > waterUpdated[(int)current.index.x, (int)current.index.y])
                                waterLossAmount = waterUpdated[(int)current.index.x, (int)current.index.y];

                            // remove water from current
                            waterUpdated[(int)current.index.x, (int)current.index.y] -= waterLossAmount;

                            // add water to neighbor
                            waterUpdated[(int)neighbor.index.x, (int)neighbor.index.y] += waterLossAmount;

                            // compute terrain elevation adjustment
                            float terrainMovement = waterLossAmount / velocityElevationCap * (1f - earthStability) * elevationGradient[k].y;

                            //Debug.Log("terrainMovement " + terrainMovement + " from " + current.index + " to " + neighbor.index);

                            // adjust elevations
                            current.setY(current.getY() - terrainMovement);
                            neighbor.setY(neighbor.getY() + terrainMovement);
                        }
                    }
                }
            }
        }

        // write back updated water volumes
        for (int i = 0; i < waterUpdated.GetLength(0); i++) {
            for (int j = 0; j < waterUpdated.GetLength(0); j++) {
                waterVolumes[i, j] = waterUpdated[i, j];
            }
        }

        // simulate drying effect
        erosionRemoveWater(waterVolumes, waterLoss);
    }

    private void erosionRemoveWater(float[,] waterVolumes, float erosionWaterLoss) {
        //string str = "";
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            //str += "\n";
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                waterVolumes[i, j] *= erosionWaterLoss;
                //str += ", " + waterVolumes[i, j];
            }
        }
        //Debug.Log(str);
    }

    private void erosionDepositWater(float[,] waterVolumes, float waterAmount) {
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                if (this.tiles[i, j] != null)
                    waterVolumes[i, j] = waterAmount;
                else
                    waterVolumes[i, j] = 0;
            }
        }
    }

    // *** WATER GENERATION CODE *** //

    private void generateWaterTiles(float waterLevelUnitFactor) {
        // clamp input value
        if (waterLevelUnitFactor > 1) waterLevelUnitFactor = 1;
        if (waterLevelUnitFactor < 0) waterLevelUnitFactor = 0;

        // compute water level
        this.waterLevelElevation = (int)(this.minElevation + ((this.maxElevation - this.minElevation) +
                (this.averageElevation - this.minElevation)) / 2 * waterLevelUnitFactor);

        // setup tiles
        foreach (Tile tile in this.getViewableTiles()) {
            tile.elevationToWater = tile.getY() - waterLevelElevation;
            if (tile.elevationToWater <= 0) {
                tile.setTileType(new WaterTileType(true, waterLevelElevation));
            } else
                tile.setTileType(new LandTileType(true));
        }
        this.computeElevationParameters();
    }

    // *** REGION SIZE COMPUTATIONS *** //

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

    // *** ELEVATION PARAMETERS COMPUTATIONS *** //

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

    // *** GETTERS AND SETTERS *** //

    public List<Tile> getViewableTiles() {
        List<Tile> tiles = new List<Tile>();
        foreach (Tile tile in this.tiles)
            if (tile != null && tile.getTileType() != null)
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
