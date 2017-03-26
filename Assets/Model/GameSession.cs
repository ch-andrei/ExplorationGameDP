using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapGeneration;
using Tiles;
using TileAttributes;

using Pathfinding;

using Playable;

using Viewable;

public class GameSession  {

    public static float battleSuppliesWonPenalty = 0.25f;

    public static float battleDefenderBonus = 0.1f;
    public static float battleDefenderEncampedBonus = 0.2f;

    public static float battleDefenderForestryBonus = 0.1f;
    public static float battleAttackerForestryPenalty = 0.1f;

    public static float battleDefenderMarlandPenalty = 0.05f;
    public static float battleAttackerMarlandPenalty = 0.1f;

    public static int playerPosLandmassTilesConstraint = 50;
    public static int playerPosLocalTilesConstraint = 5;

    public static float enemyDespawnRadius = 400f;
    public static int enemySpawnPointSearchMaxDepth = 10;
    public static int enemyStrengthPerTribeLevel = 2;

    // private variables
    public PlayablePlayer humanPlayer { get; }
    public List<NonPlayablePlayer> AIPlayers { get; }
    public MapGenerator mapGenerator { get; }

    public GameSession(MapGeneratorInput mgi) {
        mapGenerator = new MapGenerator(mgi);

        humanPlayer = new PlayablePlayer();
        InitializeHumanPlayerPosition();

        AIPlayers = new List<NonPlayablePlayer>();
        spawnNonPlayablePlayers(1);
    }

    public void newTurn() {
        // remove dead players
        foreach (NonPlayablePlayer p in AIPlayers) {
            if (p.dead) {
                removePlayerWithID(p.id);
            }
        }

        Debug.Log("A new day dawns!"); // TODO load from xml

        humanPlayer.newTurn();

        foreach (NonPlayablePlayer p in AIPlayers) {
            p.newTurn();
            if ((p.getPos() - humanPlayer.getPos()).magnitude > enemyDespawnRadius) {
                killPlayer(p);
            }
        }

        if (humanPlayer.dead) {
            Debug.Log("And so your journey ends here..."); // TODO load from xml
            endGame();
        }
    }

    private void endGame() {
    }

    private void removePlayerWithID(long id) {
        for (int i = 0; i < AIPlayers.Count; i++) {
            NonPlayablePlayer p = AIPlayers[i];
            if (p.id == id) {
                AIPlayers.RemoveAt(i);
                break;
            }
        }
    }

    public PathResult playerAttemptMove(Tile moveTo, out string message, bool movePlayer = false) {
        return playerAttemptMove(humanPlayer, moveTo, out message, movePlayer);
    }

    public PathResult playerAttemptMove(Player p, Tile moveTo, out string message, bool movePlayer = false) {
        if (p.getCampStatus()) {
            message = "Player must leave the encampment first!";
            return null;
        }

        PathResult pr = (new DijkstraPathFinder(maxDepth: p.getMaxActionPoints(),
                                                maxCost: p.getActionPoints(),
                                                maxIncrementalCost: p.getMaxActionPoints()))
                             .pathFromTo(
                                p.getPosTile(), 
                                moveTo, 
                                playersCanBlockPath: false
                             );

        // if successful move 
        if (movePlayer && pr.reachedGoal) {

            message = "Player moved successfully";

            p.notifyViewMovement(pr);
            p.setPos(moveTo);
            p.loseActionPoints(Mathf.CeilToInt(pr.pathCost));

            Tile tile = pr.getTilesOnPath()[0];
            Player playerAtDestination;
            if ((playerAtDestination = checkForPlayersAt(tile)) != null) {
                if (p.id != playerAtDestination.id)
                    playerBattle(p, playerAtDestination);
            }
        }

        message = "Could not move player";

        return pr;
    }

    private void playerBattle(Player attacker, Player defender) {
        float defenderBonus = 1f;
        float attackerBonus = 1f;

        // default defender bonus
        defenderBonus += battleDefenderBonus;
        if (defender.getCampStatus())
            defenderBonus += battleDefenderEncampedBonus;

        Debug.Log("Fighting a battle between players!");
        Debug.Log("Attacker");
        Debug.Log(attacker);
        Debug.Log("Defender");
        Debug.Log(defender);

        // compute bonuses/penalties
        foreach (TileAttribute ta in defender.getPosTile().getTileAttributes()) {
            if (ta.GetType() == typeof(Forestry)){
                defenderBonus += battleDefenderForestryBonus * (ta as Forestry).forestryDensity;
                attackerBonus += battleAttackerForestryPenalty * (ta as Forestry).forestryDensity;
            } else if (ta.GetType() == typeof(Marshland)) {
                defenderBonus += battleDefenderMarlandPenalty * (ta as Marshland).marshDensity;
                attackerBonus += battleAttackerMarlandPenalty * (ta as Marshland).marshDensity;
            }
        }

        if (defenderBonus < 0) {
            defenderBonus = 0;
        }
        if (attackerBonus < 0) {
            attackerBonus = 0;
        }

        float defenderStrength = defenderBonus * defender.computeStrength();
        float attackerStrength = attackerBonus * attacker.computeStrength();

        float strengthDifference = defenderStrength - attackerStrength;

        Debug.Log("att/def: " + attackerStrength + "/" + defenderStrength);

        if (strengthDifference >= 0) {
            // defender is the winner

            defender.distributeDamage(attackerStrength);
            defender.playerAddSupplies((int)(attacker.getSupplies() * (1 - battleSuppliesWonPenalty)));

            Debug.Log("Defender won! Damage taken: " + attackerStrength);

            killPlayer(attacker);
        } else {
            // attacker is the winner

            attacker.distributeDamage(defenderStrength);
            attacker.playerAddSupplies((int)(defender.getSupplies() * (1 - battleSuppliesWonPenalty)));

            Debug.Log("Attacker won! Damage taken: " + defenderStrength);

            killPlayer(defender);
        }

        attacker.checkFollowersHealthPoints();
        defender.checkFollowersHealthPoints();
    }

    private void killPlayer(Player p) {
        p.kill();
        removePlayerWithID(p.id);
    }

    // TODO test this method more thoroughly
    // attempt to place player in the center of the generated region
    private void InitializeHumanPlayerPosition() {

        // get a tile in the center of the region
        float regionSize = mapGenerator.getRegion().getViewableSize();
        Tile tile = mapGenerator.getRegion().getTileAt(new Vector3());

        HexRegion region = mapGenerator.getRegion() as HexRegion;

        PathResult pr, prLocal;

        int landmassSearchDepth = 10, maxDepth = 50, increments = 10 ;
        while (landmassSearchDepth <= maxDepth) {
            // run uniform cost search to locate surronding land tiles
            pr = (new DijkstraUniformCostPathFinder( uniformCost: 1f, maxDepth: landmassSearchDepth, maxCost: float.MaxValue))
                             .pathFromTo(region, tile, new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
            // check if explored tiles are part of big enough landmass
            foreach (Tile _tile in pr.getExploredTiles()) {

                if (_tile.getTileType().GetType() == typeof(LandTileType)) {

                    // run Dijkstra pathfinder starting at current landtype tile and count how many landtype tiles are reachable from it
                    prLocal = (new DijkstraPathFinder( maxDepth: 20, maxCost: 500, maxIncrementalCost: humanPlayer.getMaxActionPoints()))
                             .pathFromTo(region, _tile, new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
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
                        prLocal = (new DijkstraPathFinder(maxDepth: humanPlayer.getMaxActionPoints(), 
                                                            maxCost: humanPlayer.getMaxActionPoints(),
                                                            maxIncrementalCost: humanPlayer.getMaxActionPoints()))
                                    .pathFromTo(region, _tile, new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));
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
                            humanPlayer.setPos(_tile);
                            return;
                        }    
                    }
                }
            }

            // if couldnt locate land tiles during last time, increase maxdepth and run again
            landmassSearchDepth += increments;
        }
    }

    public Player checkForPlayersAt(Tile tile) {
        // check for AI players
        foreach (Player p in this.AIPlayers) {
            if (p.getPosIndex() == tile.index) {
                return p;
            }
        }
        // check for human player
        if (this.humanPlayer.getPosIndex() == tile.index)
            return humanPlayer;
        return null;
    }

    private void spawnNonPlayablePlayers(int numberOfPlayersToSpawn) {

        HexRegion region = mapGenerator.getRegion() as HexRegion;

        // TODO implement more functionality here

        Tile tile = mapGenerator.getRegion().getTileAt(this.humanPlayer.getPosTile().index);
        PathResult pr = (new DijkstraUniformCostPathFinder(uniformCost: 1f, maxDepth: enemySpawnPointSearchMaxDepth, maxCost: float.MaxValue))
                 .pathFromTo(region, tile, new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));

        foreach (Tile t in pr.getExploredTiles()) {
            foreach (TileAttribute ta in t.getTileAttributes()) {
                if (ta.GetType() == typeof(LocalTribe)) {
                    int strength = (ta as LocalTribe).level * enemyStrengthPerTribeLevel;
                    NonPlayablePlayer p = new NonPlayablePlayer(50, strength);
                    p.setPos(t);
                    if (p.computeStrength() > 0)
                        AIPlayers.Add(p);
                    break;
                }
            }
        }
    }
}

