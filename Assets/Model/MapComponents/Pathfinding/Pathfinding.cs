using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Tiles;
using TileAttributes;

using Playable;

namespace Pathfinding {

    public class PathResult {
        protected List<PathTile> pathTiles { get; } // goal tile is first in this list
        protected List<PathTile> exploredPathTiles { get; }
        public bool reachedGoal { get; set; }
        public float pathCost { get; set; }
        public PathResult() {
            pathTiles = new List<PathTile>();
            exploredPathTiles = new List<PathTile>();
            reachedGoal = false;
        }
        public List<Tile> getTilesOnPath() {
            List<Tile> tilesOnPath = new List<Tile>();
            foreach (PathTile pt in pathTiles) {
                tilesOnPath.Add(pt.tile);
            }
            return tilesOnPath;
        }
        public List<Tile> getTilesOnPathStartFirst() {
            List<Tile> tilesOnPath = getTilesOnPath();
            // reverse the order to be start tile first
            tilesOnPath.Reverse();
            return tilesOnPath;
        }
        public List<Tile> getExploredTiles() {
            List<Tile> exploredTiles = new List<Tile>();
            foreach (PathTile pt in exploredPathTiles) {
                exploredTiles.Add(pt.tile);
            }
            return exploredTiles;
        }
        public void addPathtile(PathTile pt) {
            this.pathTiles.Add(pt);
        }
        public void addExploredPathtile(PathTile pt) {
            this.exploredPathTiles.Add(pt);
        }
    }

    // to be used as key in a dictionary
    public interface IKeyable<T1> {
        T1 getKey();
    }

    public class PathTile : IKeyable<Vector2> {
        public Tile tile { get; }
        public int depth { get; set; }
        public PathTile(Tile tile) {
            this.tile = tile;
        }
        public bool CompareTo(PathTile pt) {
            if (this.tile.index == pt.tile.index)
                return true;
            else
                return false;
        }
        public Vector2 getKey() {
            return this.tile.index;
        }
    }

    public abstract class PathFinder {

        public static float upElevatonPerPoint = 6f;
        public static float downElevatonPerPoint = 7f;

        public int maxDepth { get; set; }
        public float maxCost { get; set; }
        public float maxIncrementalCost { get; set; }

        public PathFinder(int maxDepth, float maxCost, float maxIncrementalCost) {
            this.maxDepth = maxDepth;
            this.maxCost = maxCost;
            this.maxIncrementalCost = maxIncrementalCost;
        }

        // assumes the tiles are adjacent to each other
        public virtual float costBetween(PathTile t1, PathTile t2) {
            float cost = 1f; // base cost between tiles

            // cost due to elevation
            float elevationDelta = (t2.tile.getY() - t1.tile.getY());
            if (elevationDelta < 0)
                cost -= elevationDelta / downElevatonPerPoint;
            else
                cost += elevationDelta / upElevatonPerPoint;

            // cost due to tile attributes
            cost += t2.tile.moveCostPenalty;

            if (cost > this.maxIncrementalCost)
                return float.PositiveInfinity;
            return cost;
        }

        public abstract PathResult pathFromTo(Tile start, Tile goal, bool playersCanBlockPath = false);

        public abstract PathResult pathFromTo(HexRegion _region, Tile start, Tile goal, bool playersCanBlockPath = false);
    }

    public class LongDistancePathFinder : PathFinder {

        private static int _maxDepth = 25;
        private static float _maxCost = 500;

        DijkstraPathFinder DijsktraPF;
        AstarPathFinder AstarPF;

        public LongDistancePathFinder(int maxDepth, float maxCost, float maxIncrementalCost) : base(maxDepth, maxCost, maxIncrementalCost){
            DijsktraPF = new DijkstraPathFinder(maxDepth, maxCost, maxIncrementalCost);
            AstarPF = new AstarPathFinder(_maxDepth, _maxCost, maxIncrementalCost);
        }

        override
        public PathResult pathFromTo(HexRegion _region, Tile start, Tile goal, bool playersCanBlockPath = false) {
            // attempt normal Dijsktra pathfinder first
            PathResult pr = DijsktraPF.pathFromTo(
                            _region,
                            start,
                            goal,
                            playersCanBlockPath
                            );

            if (pr.reachedGoal) {
                return pr;
            }

            // get full path to tile even if its out of range
            PathResult prA = AstarPF.pathFromTo(
                            _region,
                            start,
                            GameControl.gameSession.humanPlayer.getTilePos(),
                            playersCanBlockPath
                            );

            // get move range
            PathResult prD = DijsktraPF.pathFromTo(
                            _region,
                            start,
                            new HexTile(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector2(float.MaxValue, float.MaxValue)),
                            playersCanBlockPath
                            );

            // get the last tile given by astar pathfinder to goal that is still within move range
            Tile _goal = null;
            if (prA.reachedGoal) {
                foreach (Tile t in prA.getTilesOnPathStartFirst()) {
                    bool outOfRange = true;
                    foreach (Tile explored in prD.getExploredTiles()) {
                        if (t.getPos() == explored.getPos()) {
                            _goal = t;
                            outOfRange = false;
                            break;
                        }
                    }
                    if (outOfRange)
                        break;
                }
            }

            if (_goal != null) {
                return DijsktraPF.pathFromTo(
                            _region,
                            start,
                            _goal,
                            playersCanBlockPath
                            );
            } else {
                return prD;
            }
        }

        override
        public PathResult pathFromTo(Tile start, Tile goal, bool playersCanBlockPath = false) {
            return pathFromTo(GameControl.gameSession.mapGenerator.getRegion() as HexRegion, start, goal, playersCanBlockPath);
        }

    }

    public abstract class HeuristicPathFinder : PathFinder{

        public static float heuristicDepthInfluence = 1e-3f; // nudges priorities for tie breaking

        public HeuristicPathFinder(int maxDepth, float maxCost, float maxIncrementalCost) : base(maxDepth, maxCost, maxIncrementalCost) {
        }

        public virtual PathResult pathFromTo(HexRegion region, Tile start, Tile goal, HeuristicPathFinder heuristic, bool playersCanBlockPath = false) {

            PathResult pathResult = new PathResult();

            PathTile goalPt = new PathTile(goal);

            // set up lists 
            PriorityQueue<PathTile> frontier = new PriorityQueue<PathTile>();
            Dictionary<Vector2, PathTile> explored = new Dictionary<Vector2, PathTile>();
            Dictionary<Vector2, PathTile> previous = new Dictionary<Vector2, PathTile>();
            Dictionary<Vector2, float> costs = new Dictionary<Vector2, float>();

            PathTile crt;

            crt = new PathTile(start);
            crt.depth = 0;

            frontier.Enqueue(crt, 0);
            previous[crt.tile.index] = null;
            costs[crt.tile.index] = 0;

            // start pathfinding
            while (!frontier.IsEmpty()) {

                // get current 
                crt = frontier.Dequeue();

                // record that the tile was explored
                explored[crt.tile.index] = crt;

                if (crt.CompareTo(goalPt)) {
                    // reached goal; search complete
                    pathResult.reachedGoal = true;
                    pathResult.pathCost = costs[crt.tile.index];
                    break;
                }

                // get neighbor tiles
                List<PathTile> neighbors = new List<PathTile>();
                foreach (Tile neighborTile in region.getTileNeighbors(crt.tile.index)) {
                    PathTile neighbor = new PathTile(neighborTile);
                    //neighborPt.cost = crt.cost + costBetween(crt, neighborPt);
                    neighbor.depth = crt.depth + 1;
                    neighbors.Add(neighbor);
                }

                // add neighbor tiles to search
                float cost, priority;
                foreach (PathTile neighbor in neighbors) {

                    // check if exceeding max depth
                    if (neighbor.depth > maxDepth) {
                        break;
                    }

                    // compute cost
                    float _cost = costBetween(crt, neighbor);

                    // check if path is blocked by another player
                    if (playersCanBlockPath && GameControl.gameSession.checkForPlayersAt(neighbor.tile) != null) {
                        if (!neighbor.CompareTo(goalPt))  // ensures that you can move to a tile with an enemy
                            _cost = float.PositiveInfinity; // set highest cost to signify that the tile is unreachable
                    }

                    cost = costs[crt.tile.index] + _cost;

                    if (cost <= maxCost) {
                        if (!costs.ContainsKey(neighbor.tile.index) || cost < costs[neighbor.tile.index]) {
                            costs[neighbor.tile.index] = cost;

                            // compute heuristic priority
                            priority = cost + heuristic.heuristic(neighbor, goalPt);
                            priority -= neighbor.depth * heuristicDepthInfluence; // makes so that tiles closest to goal are more eagerly explored

                            frontier.Enqueue(neighbor, priority);

                            previous[neighbor.tile.index] = crt;
                        }
                    }
                }
            }

            // build list of tiles on path if goal was reached
            if (pathResult.reachedGoal) {
                pathResult.addPathtile(goalPt);

                crt = previous[goal.index];

                while (crt != null) {
                    pathResult.addPathtile(crt);
                    crt = previous[crt.tile.index];
                }
            }

            foreach (PathTile pt in explored.Values) {
                pathResult.addExploredPathtile(pt);
            }

            return pathResult;
        }

        override
        public PathResult pathFromTo(Tile start, Tile goal, bool playersCanBlockPath = false) {
            return pathFromTo(start, goal, this, playersCanBlockPath);
        }

        // inspired by http://www.redblobgames.com/pathfinding/a-star/introduction.html
        public virtual PathResult pathFromTo(Tile start, Tile goal, HeuristicPathFinder heuristic, bool playersCanBlockPath = false) {
            HexRegion region = GameControl.gameSession.mapGenerator.getRegion() as HexRegion;
            return pathFromTo(region, start, goal, heuristic, playersCanBlockPath);
        }

        override
        public PathResult pathFromTo(HexRegion region, Tile start, Tile goal, bool playersCanBlockPath = false) {
            return pathFromTo(region, start, goal, this, playersCanBlockPath);
        }

        // *** HEURISTIC COMPUTATIONS *** ///

        public abstract float heuristic(PathTile start, PathTile goal);
    }

    public class AstarPathFinder : HeuristicPathFinder {

        public AstarPathFinder(int maxDepth, float maxCost, float maxIncrementalCost) : base(maxDepth, maxCost, maxIncrementalCost){
        }

        override
        public float heuristic(PathTile start, PathTile goal) {
            float cost = HexTile.distanceBetweenHexCoords(start.tile.index, goal.tile.index);
            float elevationDelta = (start.tile.getY() - goal.tile.getY());
            if (elevationDelta < 0)
                cost += -elevationDelta / downElevatonPerPoint;
            else
                cost += elevationDelta / upElevatonPerPoint;
            return cost;
        }
    }

    public class DijkstraPathFinder : HeuristicPathFinder {

        public DijkstraPathFinder(int maxDepth, float maxCost, float maxIncrementalCost) : base(maxDepth, maxCost, maxIncrementalCost) {
        }

        override
        public float heuristic(PathTile start, PathTile goal) {
            return 0;
        }

    }

    public class DijkstraUniformCostPathFinder : DijkstraPathFinder {

        float uniformCost;

        public DijkstraUniformCostPathFinder(float uniformCost, int maxDepth, float maxCost, float maxIncrementalCost=0) : base(maxDepth, maxCost, maxIncrementalCost) {
            this.uniformCost = uniformCost;
        }

        override
        public float costBetween(PathTile t1, PathTile t2) {
            return uniformCost;
        }
    }

    public class PriorityQueue<T> {

        public struct PriorityQueueElement<T> {
            public float priority { get; set; }
            public T item { get; set; }
            public PriorityQueueElement(T item, float priority) {
                this.priority = priority;
                this.item = item;
            }
            // for priority queue
            public int ComparePriority(PriorityQueueElement<T> pt) {
                if (this.priority < pt.priority)
                    return -1;
                else if (this.priority == pt.priority)
                    return 0;
                else
                    return 1;
            }
        }

        private List<PriorityQueueElement<T>> data;

        public PriorityQueue() {
            this.data = new List<PriorityQueueElement<T>>();
        }

        // adds highest priority in the end
        // code from: https://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c/listing3.aspx
        public virtual void Enqueue(T item, float priority) {
            PriorityQueueElement<T> pqe = new PriorityQueueElement<T>(item, priority);
            // add item
            data.Add(pqe);
            int ci = data.Count - 1;
            // swap items to maintain priority
            while (ci > 0) {
                int pi = (ci - 1) / 2;
                if (data[ci].ComparePriority(data[pi]) >= 0)
                    break;
                PriorityQueueElement<T> tmp = data[ci];
                data[ci] = data[pi];
                data[pi] = tmp;
                ci = pi;
            }
        }

        // this dequeue method results in queue 'travelling' in memory as it grows and items are popped
        public T DequeueBad() {
            if (IsEmpty())
                throw new IndexOutOfRangeException("Priority Queue: attempting to pop from an empty queue.");
            PriorityQueueElement<T> item = data[0];
            data.RemoveAt(0);
            return item.item;
        }

        // code from https://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c/listing4.aspx
        public virtual T Dequeue() {
            // assumes pq is not empty; up to calling code
            int li = data.Count - 1; // last index (before removal)
            PriorityQueueElement<T> frontItem = data[0];   // fetch the front
            data[0] = data[li];
            data.RemoveAt(li);

            --li; // last index (after removal)
            int pi = 0; // parent index. start at front of pq
            while (true) {
                int ci = pi * 2 + 1; // left child index of parent
                if (ci > li) break;  // no children so done
                int rc = ci + 1;     // right child
                if (rc <= li && data[rc].ComparePriority(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                    ci = rc;
                if (data[pi].ComparePriority(data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                PriorityQueueElement<T> tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp; // swap parent and child
                pi = ci;
            }
            return frontItem.item;
        }

        public bool IsEmpty() {
            if (this.data.Count == 0)
                return true;
            else
                return false;
        }

        public int Count() {
            return data.Count;
        }
    }
}