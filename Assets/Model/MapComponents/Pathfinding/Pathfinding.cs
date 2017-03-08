using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Tiles;
using TileAttributes;

namespace Pathfinding {

    public class PathResult {
        public List<PathTile> pathTiles { get; }
        public List<PathTile> exploredPathTiles { get; }
        public bool reachedGoal { get; set; }
        public float pathCost { get; set; }
        public PathResult() {
            pathTiles = new List<PathTile>();
            exploredPathTiles = new List<PathTile>();
            reachedGoal = false;
        }
        public List<Tile> getTiles() {
            List<Tile> tilesOnPath = new List<Tile>();
            foreach (PathTile pt in pathTiles) {
                tilesOnPath.Add(pt.tile);
            }
            return tilesOnPath;
        }
        public List<Tile> getExploredTiles() {
            List<Tile> exploredTiles = new List<Tile>();
            foreach (PathTile pt in exploredPathTiles) {
                exploredTiles.Add(pt.tile);
            }
            return exploredTiles;
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

        public static float upElevatonPerPoint = 3.5f;
        public static float downElevatonPerPoint = 5f;

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

            return cost;
        }

        public abstract PathResult pathFromTo(HexRegion region, Tile start, Tile goal);
    }

    public abstract class HeuristicPathFinder : PathFinder{

        public static float heuristicDepthInfluence = 1e-3f; // nudges priorities for tie breaking
        public int maxDepth = 50;
        public float maxCost = 10f;

        // inspired by http://www.redblobgames.com/pathfinding/a-star/introduction.html
        public virtual PathResult pathFromTo(HexRegion region, Tile start, Tile goal, HeuristicPathFinder heuristic) {

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
                    // reached goal
                    // search complete
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
                    cost = costs[crt.tile.index] + costBetween(crt, neighbor);

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
                crt = previous[goal.index];

                while (crt != null) {
                    pathResult.pathTiles.Add(crt);
                    crt = previous[crt.tile.index];
                }
            }

            foreach (PathTile pt in explored.Values) {
                pathResult.exploredPathTiles.Add(pt);
            }

            return pathResult;
        }

        // *** HEURISTIC COMPUTATIONS *** ///

        public abstract float heuristic(PathTile start, PathTile goal);
    }

    public class AstarPathFinder : HeuristicPathFinder {

        public AstarPathFinder(int maxDepth = 50, float maxCost = 10f) {
            base.maxDepth = maxDepth;
            base.maxCost = maxCost;
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

        override
        public PathResult pathFromTo(HexRegion region, Tile start, Tile goal) {
            return base.pathFromTo(region, start, goal, this);
        }
    }

    public class DijkstraPathFinder : HeuristicPathFinder {

        public DijkstraPathFinder(int maxDepth = 50, float maxCost = 10f) {
            base.maxDepth = maxDepth;
            base.maxCost = maxCost;
        }

        override
        public float heuristic(PathTile start, PathTile goal) {
            return 0;
        }

        override
        public PathResult pathFromTo(HexRegion region, Tile start, Tile goal) {
            return base.pathFromTo(region, start, goal, this);
        }
    }

    public class DijkstraUniformCostPathFinder : DijkstraPathFinder {

        float uniformCost;

        public DijkstraUniformCostPathFinder(int maxDepth = 50, float maxCost = 10f, float uniformCost = 1f) : base(maxDepth, maxCost) {
            this.uniformCost = uniformCost;
        }

        override
        public float costBetween(PathTile t1, PathTile t2) {
            return uniformCost;
        }
    }

    public class PriorityQueue<T> {

        public class PriorityQueueElement<T> {
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