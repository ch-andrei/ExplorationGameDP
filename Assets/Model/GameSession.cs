using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MapGeneration;
using Tiles;
using TileAttributes;

using Pathfinding;

using Playable;
using Followers;

using Viewable;

public class GameSession {

    public static float battleSuppliesWonPenalty = 0.25f;

    public static float battleDefenderBonus = 0.1f;
    public static float battleDefenderEncampedBonus = 0.2f;

    public static float battleDefenderForestryBonus = 0.1f;
    public static float battleAttackerForestryPenalty = 0.1f;

    public static float battleDefenderMarshlandPenalty = 0.05f;
    public static float battleAttackerMarshlandPenalty = 0.1f;

    public static int playerPosLandmassTilesConstraint = 50;
    public static int playerPosLocalTilesConstraint = 5;

    public static float enemyDespawnRadius = 400f;
    public static int enemySpawnPointSearchMaxDepth = 10;
    public static int enemyStrengthPerTribeLevel = 2;
    public static int maxEnemiesSpawned = 10;

    public List<string> gamelog { get; }

    // private variables
    public PlayablePlayer humanPlayer { get; }

    public int turnCount { get; set; }

    private List<ArtificialIntelligence> AIs;
    public List<NonPlayablePlayer> AIPlayers {
        get {
            List<NonPlayablePlayer> ps = new List<NonPlayablePlayer>();
            foreach (ArtificialIntelligence AI in AIs) {
                ps.Add(AI.getPlayer());
            }
            return ps;
        }
    }

    public MapGenerator mapGenerator { get; }

    public GameSession(MapGeneratorInput mgi) {
        mapGenerator = new MapGenerator(mgi);

        humanPlayer = new PlayablePlayer();
        InitializeHumanPlayerPosition();

        AIs = new List<ArtificialIntelligence>();
        spawnNonPlayablePlayers();

        gamelog = new List<string>();
    }

    public void newTurn() {
        turnCount++;

        // remove dead players
        foreach (NonPlayablePlayer p in AIPlayers) {
            if (p.dead) {
                removePlayerWithID(p.id);
            }
        }

        Log("A new day dawns!"); // TODO load from xml

        humanPlayer.newTurn();

        if (humanPlayer.dead) {
            Log("And so your journey ends here..."); // TODO load from xml
            endGame();
        }

        // despawn enemies out of range
        foreach (ArtificialIntelligence AI in AIs) {
            AI.processNewTurn();
            if ((AI.getPlayer().getPos() - humanPlayer.getPos()).magnitude > enemyDespawnRadius) {
                killPlayer(AI.getPlayer());
            }
        }
        spawnNonPlayablePlayers();
    }

    void Log(string str) {
        gamelog.Add("Turn " + turnCount + "\n" + str);
    }

    public Player checkForPlayersAt(Tile tile, long id=-1) {
        // check for human player
        if (this.humanPlayer.getTilePosIndex() == tile.index && humanPlayer.id != id)
            return humanPlayer;
        // check for AI players
        foreach (Player p in this.AIPlayers) {
            if (p.getTilePosIndex() == tile.index && p.id != id) {
                return p;
            }
        }
        return null;
    }

    public void playerAttemptRecruit(Player p, Follower f) {
        if (p.getSupplies() >= f.recruitCost) {
            p.playerAddSupplies(-f.recruitCost);
            p.addFollower(f);
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
                                p.getTilePos(), 
                                moveTo, 
                                playersCanBlockPath: false
                             );

        // if successful move 
        if (movePlayer && pr.reachedGoal) {

            message = "Player moved successfully";

            p.setTilePos(moveTo);
            p.notifyViewMovement(pr);
            p.loseActionPoints(Mathf.CeilToInt(pr.pathCost));

            Tile tile = p.getTilePos();
            Player playerAtDestination;
            if ((playerAtDestination = checkForPlayersAt(tile, p.id)) != null) {
                playerBattle(p, playerAtDestination);
            }
        }

        message = "Could not move player";

        return pr;
    }

    private void endGame() {
        // TODO
        // add some code to handle game exit here
    }

    private void removePlayerWithID(long id) {
        for (int i = 0; i < AIPlayers.Count; i++) {
            NonPlayablePlayer p = AIPlayers[i];
            if (p.id == id) {
                if (!p.dead)
                    killPlayer(p);
                AIs.RemoveAt(i);
                break;
            }
        }
    }

    private void playerBattle(Player attacker, Player defender) {
        float defenderBonus = 1f;
        float attackerBonus = 1f;

        // default defender bonus
        defenderBonus += battleDefenderBonus;
        if (defender.getCampStatus())
            defenderBonus += battleDefenderEncampedBonus;

        string log = "";
        log += ("Fighting a battle between players!\n");
        log += ("Attacker: ");
        log += (attacker);
        log += ("\nDefender: ");
        log += (defender);

        // compute bonuses/penalties
        foreach (TileAttribute ta in defender.getTilePos().getTileAttributes()) {
            if (ta.GetType() == typeof(Forestry)){
                defenderBonus += battleDefenderForestryBonus * (ta as Forestry).forestryDensity;
                attackerBonus += battleAttackerForestryPenalty * (ta as Forestry).forestryDensity;
            } else if (ta.GetType() == typeof(Marshland)) {
                defenderBonus += battleDefenderMarshlandPenalty * (ta as Marshland).marshDensity;
                attackerBonus += battleAttackerMarshlandPenalty * (ta as Marshland).marshDensity;
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

        if (strengthDifference >= 0) {
            // defender is the winner

            defender.distributeDamage(attackerStrength);
            defender.playerAddSupplies((int)(attacker.getSupplies() * (1 - battleSuppliesWonPenalty)));

            log += ("Defender won! Damage taken: " + attackerStrength);

            killPlayer(attacker);
            defender.checkFollowersHealthPoints();
        } else {
            // attacker is the winner

            attacker.distributeDamage(defenderStrength);
            attacker.playerAddSupplies((int)(defender.getSupplies() * (1 - battleSuppliesWonPenalty)));

            log += ("Attacker won! Damage taken: " + defenderStrength);

            killPlayer(defender);
            attacker.checkFollowersHealthPoints();
        }

        Log(log);
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
                            humanPlayer.setTilePos(_tile);
                            return;
                        }    
                    }
                }
            }

            // if couldnt locate land tiles during last time, increase maxdepth and run again
            landmassSearchDepth += increments;
        }
    }

    private void spawnNonPlayablePlayers() {

        HexRegion region = mapGenerator.getRegion() as HexRegion;

        // TODO implement more functionality here

        // get tiles within vicinity
        Tile tile = region.getTileAt(this.humanPlayer.getTilePos().index);
        PathResult pr = (new DijkstraUniformCostPathFinder(uniformCost: 1f, maxDepth: enemySpawnPointSearchMaxDepth, maxCost: float.MaxValue))
                 .pathFromTo(region, tile, new HexTile(new Vector3(), new Vector2(float.MaxValue, float.MaxValue)));

        // spawn enemies at tiles with tribes
        foreach (Tile t in pr.getExploredTiles()) {
            if ((t.getPos() - humanPlayer.getPos()).magnitude <= enemyDespawnRadius 
                        && checkForPlayersAt(t) == null
                        && AIs.Count < maxEnemiesSpawned) {
                if (t.getTileType().GetType() == typeof(LandTileType)) {
                    foreach (TileAttribute ta in t.getTileAttributes()) {
                        if (ta.GetType() == typeof(LocalTribe)) {
                            int strength = (ta as LocalTribe).level * enemyStrengthPerTribeLevel;
                            NonPlayablePlayer p = new NonPlayablePlayer(50, strength);
                            p.setTilePos(t);
                            if (p.computeStrength() > 0)
                                AIs.Add(new ArtificialIntelligence(p));
                            break;
                        }
                    }
                }
            }
        }
    }

    public class ArtificialIntelligence {

        public static float difficulty = 1f;

        public static float distanceToPlayerToAttack = 100f;

        public enum Behaviour { idle, wandering, attacking, healing };
        public int behaviourState;

        PathFinder DijsktraPF, AstarPF, longDistancePathFinder;

        NonPlayablePlayer player;

        public ArtificialIntelligence(NonPlayablePlayer p) {
            behaviourState = (int)Behaviour.idle;
            this.DijsktraPF = new DijkstraPathFinder(maxDepth: p.getMaxActionPoints(),
                                                maxCost: p.getActionPoints(),
                                                maxIncrementalCost: p.getMaxActionPoints());
            this.AstarPF = new AstarPathFinder(maxDepth: 25,
                                                maxCost: 500,
                                                maxIncrementalCost: p.getMaxActionPoints());
            longDistancePathFinder = new LongDistancePathFinder(maxDepth: p.getMaxActionPoints(),
                                                maxCost: p.getActionPoints(),
                                                maxIncrementalCost: p.getMaxActionPoints());
            this.player = p;
        }

        public NonPlayablePlayer getPlayer() {
            return this.player;
        }

        public void processNewTurn() {
            if (behaviourState == (int)Behaviour.idle) {
                behaviourState = (int)Behaviour.wandering;
            } else if (behaviourState == (int)Behaviour.wandering) {
                if (this.player.getFollowersHealth() < this.player.getMaxActionPoints())
                    behaviourState = (int)Behaviour.healing;
                else if ((this.player.getPos() - GameControl.gameSession.humanPlayer.getPos()).magnitude < distanceToPlayerToAttack)
                    behaviourState = (int)Behaviour.attacking;
                else
                    wander();
            } else if (behaviourState == (int)Behaviour.attacking) {
                attackHumanPlayer();
            } else if (behaviourState == (int)Behaviour.healing) {
                // TODO
            } else {
                // some weirdness
                behaviourState = (int)Behaviour.idle;
            }
            player.newTurn();
        }

        private void wander() {
            // get move range
            PathResult pr = DijsktraPF.pathFromTo(
                            this.player.getTilePos(),
                            new HexTile(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector2(float.MaxValue, float.MaxValue)),
                            playersCanBlockPath: true
                            );

            int rand = UnityEngine.Random.Range(0, pr.getExploredTiles().Count);

            string message;
            GameControl.gameSession.playerAttemptMove(this.player, pr.getExploredTiles()[rand], out message, movePlayer: true);
        }

        private void attackHumanPlayer() {
            // get move range
            PathResult pr = longDistancePathFinder.pathFromTo(
                            this.player.getTilePos(), 
                            GameControl.gameSession.humanPlayer.getTilePos(),
                            playersCanBlockPath: true
                            );

            int rand = UnityEngine.Random.Range(0, pr.getTilesOnPathStartFirst().Count);
            string message;
            GameControl.gameSession.playerAttemptMove(this.player, pr.getTilesOnPathStartFirst()[rand], out message, movePlayer: true);
            
        }
    }
}

