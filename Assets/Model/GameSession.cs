using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapGeneration;
using Tiles;
using TileAttributes;

using Pathfinding;

public class GameSession  {

    public static int playerPosLandmassTilesConstraint = 50;
    public static int playerPosLocalTilesConstraint = 5;

    // private variables
    public Player player { get; }
    public MapGenerator mapGenerator { get; }

    public GameSession(MapGeneratorInput mgi) {
        player = new Player();
        mapGenerator = new MapGenerator(mgi);
        InitializePlayerPosition();
    }

    public void newTurn() {
        player.newTurn();
    }

    public PathResult playerAttemptMove(Tile moveTo, out string message, bool movePlayer = false) {

        if (player.getCampStatus()) {
            message = "We must leave the encampment first!";
            return null;
        }

        PathResult pr = (new DijkstraPathFinder(maxDepth: player.getMaxActionPoints(), 
                                                maxCost: player.getActionPoints(),
                                                maxIncrementalCost: player.getMaxActionPoints()))
                             .pathFromTo(mapGenerator.getRegion() as HexRegion,
                             player.getPosTile(),
                             moveTo);

        // if successful move 
        if (movePlayer && pr.reachedGoal) {
            player.setPos(moveTo);
            player.loseActionPoints(Mathf.CeilToInt(pr.pathCost));
        }

        player.attemptedMove(moveTo.getPos(), out message);

        return pr;
    }

    //public bool playerAttemptChangeEncampmentStatus(out string message) {
        
    //}

    // TODO teset this method more thoroughly
    // attempt to place player in the center of the generated region
    private void InitializePlayerPosition() {

        // get a tile in the center of the region
        float regionSize = mapGenerator.getRegion().getViewableSize();
        Tile tile = mapGenerator.getRegion().getTileAt(new Vector3());

        PathResult pr, prLocal;

        int landmassSearchDepth = 5, maxDepth = 50;
        while (landmassSearchDepth <= maxDepth) {
            Debug.Log("Running iteration " + landmassSearchDepth);
            // run uniform cost search to locate surronding land tiles
            pr = (new DijkstraUniformCostPathFinder( uniformCost: 1f, maxDepth: landmassSearchDepth, maxCost: float.MaxValue))
                             .pathFromTo( mapGenerator.getRegion() as HexRegion, 
                             tile, 
                             new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
            Debug.Log("Done pathfind 1");
            // check if explored tiles are part of big enough landmass
            foreach (Tile _tile in pr.getExploredTiles()) {

                if (_tile.getTileType().GetType() == typeof(LandTileType)) {

                    // run Dijkstra pathfinder starting at current landtype tile and count how many landtype tiles are reachable from it
                    prLocal = (new DijkstraPathFinder( maxDepth: 20, maxCost: 500, maxIncrementalCost: player.getMaxActionPoints()))
                             .pathFromTo( mapGenerator.getRegion() as HexRegion, 
                             _tile, 
                             new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
                    Debug.Log("Done pathfind 2");
                    int localCount = 0;
                    foreach (Tile __tile in prLocal.getExploredTiles()) {
                        // count local landmass landtype tiles
                        if (__tile.getTileType().GetType() == typeof(LandTileType)) {
                            localCount++;
                        }
                    }

                    // check if tile's local landmass has enough landtype tiles
                    if (localCount >= playerPosLandmassTilesConstraint) {
                        // run pathfinder again but with small max depth to see if the player can actually move around
                        prLocal = (new DijkstraPathFinder(maxDepth: player.getMaxActionPoints(), 
                                                            maxCost: player.getMaxActionPoints(),
                                                            maxIncrementalCost: player.getMaxActionPoints()))
                                    .pathFromTo(mapGenerator.getRegion() as HexRegion,
                                    _tile,
                                    new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
                        Debug.Log("Done pathfind 3");
                        int _localCount = 0;
                        foreach (Tile __tile in prLocal.getExploredTiles()) {
                            // count local landmass landtype tiles
                            if (__tile.getTileType().GetType() == typeof(LandTileType)) {
                                _localCount++;
                            }
                        }

                        if (_localCount >= playerPosLocalTilesConstraint) {
                            // located acceptable starting position
                            // set player pos and return from this method
                            player.setPos(_tile);
                            return;
                        }    
                    }
                }
            }

            // if couldnt locate land tiles during last time, increase maxdepth and run again
            landmassSearchDepth += 5;
        }
    }
}

