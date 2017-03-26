using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TileAttributes;
using Tiles;

using Pathfinding;

public static class RegionParams {

    // the parameters below must be initialized during the setup of the region

    // temperature parameters
    public static float worldAmbientTemperature; // used by tileView to determine which trees to draw (cold vs temperate)
    public static float worldCoreTemperature; // world's maximum temperature at elevation = 0
    public static float worldScale; // higher world scale means slower decrease in temperature as latitude (z component) moves away from the origin (planet 'poles')
    public static float midTempElevation; // elevation at the average of the temperature 
    public static float tanFalloffTemp; // scales the influence of elevation on temperature (linear: higher means faster decrease) 
    public static float tanOffsetTemp; // linear offset for atan function to start at ~0 and end at around ~1
    public static float linFalloffTemp; // scales the influence of latitude (y) on temperature (linear: higher means faster decrease) 
    public static float latitudeFactorTemp; // scales the influence of latitude (z) on temperature (exponential: higher means faster decrease)

    // other
    public static float waterLevelElevation; // elevation of water level

    // erosion parameters
    public static float erosionWaterAmount; // amount of water to deposit during erosion simulation: higher means more erosion
    public static float erosionStrength; // scales the influence of erosion linearly (higher means more)
    public static float erosionWaterLoss; // amount of water to lose after each simulation iteration: water[time k + 1] = erosionWaterLoss * water[time k]
    public static float earthStability; // scales the influence of erosion on terrain movement (1 means terrain wont move, 0 means maximum terrain movement)
    public static float erosionVelocityElevationCap; // limits the influence of elevation difference on terrain movement; acts as maximum elevaton difference after which terrain movement wont be affect as much
    public static int erosionIterations;

    // feature parameters
    public static float forestryProbability; // defines how many forest subregions the region will attempt to spawn
    public static float forestryMaxSpread; // defines the maximum size of each forest subregion

    // need to update the parameters to reflect region changes or updates
    public static void UpdateWorldGenerationParams(ViewableRegion region) {
        tanOffsetTemp = midTempElevation * tanFalloffTemp;
        midTempElevation = region.getAverageElevation();
        waterLevelElevation = region.getWaterLevelElevation();
    }

    // initialize the region parameters
    public static void InitializeWorldGenerationParams(ViewableRegion region) {
        // TODO load from XML

        // world temperature parameters
        worldAmbientTemperature = 60f;
        worldScale = region.getViewableSize() / 50; 
        worldCoreTemperature = 175f;
        tanFalloffTemp = 0.01f;
        linFalloffTemp = 0.05f;
        latitudeFactorTemp = 0.025f;

        // erosion parameters
        erosionWaterAmount = 1f;
        erosionStrength = 1f;
        erosionWaterLoss = 0.98f;
        erosionIterations = 100;
        earthStability = 0.25f;
        erosionVelocityElevationCap = 25f;

        // region features
        forestryProbability = 0.1f;
        forestryMaxSpread = 25f;
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

    private float water;

    public HexRegion(int n, int seed, int region_size, int elevation, float water, int rivers, Noise noise) {
        this.n = n;
        this.seed = seed;
        this.region_size = region_size;
        this.center = region_size / 2f;
        this.elevation = elevation;
        this.noise = noise;
        this.water = water;

        RegionParams.InitializeWorldGenerationParams(this);

        CreateHexPositionVectors(this.n);

        computeElevationParameters();

        RegionParams.UpdateWorldGenerationParams(this);

        //computeErosion(RegionParams.erosionWaterAmount, RegionParams.erosionStrength, RegionParams.erosionWaterLoss,
        //                RegionParams.earthStability, RegionParams.erosionVelocityElevationCap, RegionParams.erosionIterations);

        computeElevationParameters();

        computeTemperatures();

        generateWaterTiles(this.water);

        Debug.Log("Generated Region with " + this.getViewableTiles().Count + "/" + this.n + " viewable tiles; hexsize " + this.hexSize + ", hexheight " + this.hexHeight +
            ", water level elevation " + this.waterLevelElevation + "; min/avg/max elevations = " + this.minElevation + "/" + this.averageElevation + "/" + this.maxElevation);
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
        int i, j;
        // loop over X and Y in hex cube coordinatess
        for (int X = -this.gridRadius; X <= this.gridRadius; X++) {
            for (int Y = -this.gridRadius; Y <= this.gridRadius; Y++) {

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
                tile = new HexTile(new Vector3(x, y, z), new Vector2(i, j)); // height, size
                tiles[i, j] = tile;
            }
        }
    }

    // *** TILE POSITION COMPUTATIONS AND GETTERS *** //

    public Tile getTileAt(Vector2 pos) {
        Vector2 index = worldCoordToIndex(pos);
        int i, j;
        i = (int)index.x + this.gridRadius;
        j = (int)index.y + this.gridRadius;
        //Debug.Log(i + ", " + j);
        if (i < 0 || j < 0 || i >= tiles.GetLength(0) || j >= tiles.GetLength(0)) {
            return null;
        }
        return this.tiles[i, j];
    }
    public Tile getTileAt(Vector3 pos) {
        return getTileAt(new Vector2(pos.x, pos.z));
    }
    // writes index of the tile to the 'out' parameter
    public Tile getTileAt(Vector3 pos, out int[] index) {
        Tile tile = getTileAt(pos);
        if (tile != null)
            index = new int[] { (int)tile.index.x, (int)tile.index.y };
        else
            index = null;
        return tile;
    }

    // unity units coordinates
    public List<Tile> getTileNeighbors(Vector3 tilePos) {
        return getTileNeighbors(worldCoordToIndex(tilePos));
    }
    // array index coordinates
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
    private Vector2 worldCoordToIndex(Vector3 pos) {
        return worldCoordToIndex(pos.x, pos.z);
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

    // *** GEOMETRY COMPUTATIONS *** //

    private void computeTileGeometry() {
        foreach (Tile tile in this.getViewableTiles()) {
            tile.computeGeometry();
        }
    }

    // *** TILE FEATURES COMPUTATIONS *** //

    private void plantForests() {

        DijkstraPathFinder dijkstra;

        foreach (Tile tile in this.getViewableTiles()) {
            
        }
    }

    private void computeTemperatures() {
        foreach (Tile tile in this.getViewableTiles()) {
            computeTemperature(tile);
        }
    }
    public static void computeTemperature(Tile tile) {
        tile.temperature = (computeTemperatureAtPos(tile.getPos()));
    }

    public static float computeTemperatureAtPos(Vector3 tilePos) {
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
        erosionDepositWaterRandom(waterVolumes, waterAmount, 0.1f, 2);
        while (iterations-- > 0) {
            computeErosionIteration(waterVolumes, strength, waterLoss, earthStability, velocityElevationCap);
            //Debug.Log("Iterations left " + iterations);
        }
        computeTileGeometry();
    }

    private void computeErosionIteration(float[,] waterVolumes, float strength, float waterLoss, float earthStability, float velocityElevationCap, float minWaterThreshold=1e-3f) {

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

                // do not do anything for small water amounts
                if (waterVolumes[i, j] < minWaterThreshold)
                    continue;

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
                            waterVelocity = 1f - Mathf.Exp(-Mathf.Abs(elevationGradientWithWater[k].y) / velocityElevationCap); // range [0,1]

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
                            float terrainMovement = waterLossAmount / RegionParams.erosionWaterAmount * (1f - earthStability) * elevationGradient[k].y;

                            //Debug.Log("terrainMovement " + terrainMovement + " from " + current.index + " to " + neighbor.index);

                            // adjust elevations
                            current.setY(current.getY() - terrainMovement);
                            neighbor.setY(neighbor.getY() + terrainMovement);

                            current.dirty = true;
                            neighbor.dirty = true;
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
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                waterVolumes[i, j] *= erosionWaterLoss;
            }
        }
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

    private void erosionDepositWaterRandom(float[,] waterVolumes, float waterAmount, float probability, int radius) {
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                waterVolumes[i, j] = 0;
            }
        }
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                if (UnityEngine.Random.Range(0f, 1f) < probability) {
                    for (int ii = i - radius; ii < i + radius; ii++) {
                        for (int jj = j - radius; jj < j + radius; jj++) {
                            try {
                                if (this.tiles[ii, jj] != null)
                                    waterVolumes[ii, jj] += UnityEngine.Random.Range(0f, waterAmount);
                            } catch (NullReferenceException e) {
                                // do nothing
                            } catch (IndexOutOfRangeException e) {
                                // do nothing
                            }
                        }
                    }
                }
            }
        }
        for (int i = 0; i < this.tiles.GetLength(0); i++) {
            for (int j = 0; j < this.tiles.GetLength(0); j++) {
                if (waterVolumes[i, j] > waterAmount) {
                    waterVolumes[i, j] = waterAmount;
                }
            }
        }
    }

    // *** WATER GENERATION CODE *** //

    private void generateWaterTiles(float waterLevelUnitFactor) {
        // clamp input value
        if (waterLevelUnitFactor > 1) waterLevelUnitFactor = 1;
        if (waterLevelUnitFactor < 0) waterLevelUnitFactor = 0;

        List<float> elevations = new List<float>();
        // compute water level
        foreach (Tile t in this.tiles) {
            if (t != null)
                elevations.Add(t.getPos().y);
        }
        elevations.Sort();
        this.waterLevelElevation = (int)elevations[(int)(elevations.Count * waterLevelUnitFactor)];

        RegionParams.waterLevelElevation = this.waterLevelElevation;

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
    public int getMaxTileIndex() {
        return this.tiles.GetLength(0);
    }
}
