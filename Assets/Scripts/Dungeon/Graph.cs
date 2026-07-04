using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A simple graph: nodes (points) connected by edges (links).
/// Used to represent rooms and doors in the dungeon.
///
/// Graph shape:  RoomA --- Door --- RoomB  (door sits between two rooms)
/// </summary>
public class Graph
{
    /// <summary>
    /// One point in the graph. Stores a world position and a list of connected node IDs.
    /// </summary>
    private class Node
    {
        public Vector2 Position;
        public List<int> Neighbors = new List<int>();
    }

    // Lookup table: node ID -> node data.
    private Dictionary<int, Node> nodes = new Dictionary<int, Node>();

    /// <summary>Adds a node if that ID is not used yet.</summary>
    public void AddNode(int nodeId, Vector2 position)
    {
        if (!nodes.ContainsKey(nodeId))
        {
            nodes[nodeId] = new Node { Position = position };
        }
    }

    /// <summary>Connects two existing nodes (works both ways).</summary>
    public void AddEdge(int node1, int node2)
    {
        if (!nodes.ContainsKey(node1) || !nodes.ContainsKey(node2))
        {
            return;
        }

        if (!nodes[node1].Neighbors.Contains(node2))
        {
            nodes[node1].Neighbors.Add(node2);
        }

        if (!nodes[node2].Neighbors.Contains(node1))
        {
            nodes[node2].Neighbors.Add(node1);
        }
    }

    /// <summary>Returns the position of a node, or (0,0) if the ID does not exist.</summary>
    public Vector2 GetNodePosition(int nodeId)
    {
        if (nodes.ContainsKey(nodeId))
        {
            return nodes[nodeId].Position;
        }

        return Vector2.zero;
    }

    /// <summary>Returns a copy of all neighbors for a node.</summary>
    public List<int> GetNeighbors(int nodeId)
    {
        if (nodes.ContainsKey(nodeId))
        {
            return new List<int>(nodes[nodeId].Neighbors);
        }

        return new List<int>();
    }

    /// <summary>How many nodes are in the graph.</summary>
    public int GetNodeCount()
    {
        return nodes.Count;
    }

    /// <summary>All node IDs in the graph.</summary>
    public IEnumerable<int> GetAllNodes()
    {
        return nodes.Keys;
    }

    /// <summary>
    /// Breadth-First Search (BFS)
    /// Visits nodes in "rings": start node first, then its neighbors, then their neighbors and so on.
    /// Returns every node ID that can be reached from the start.
    /// Time complexity: O(n) where n = number of graph nodes (rooms and doors), because each node is visited at most once.
    /// </summary>
    public HashSet<int> BFS(int startNodeId)
    {
        HashSet<int> visited = new HashSet<int>();

        // If the start node isn't in the graph, return empty instead of crashing.
        if (!nodes.ContainsKey(startNodeId))
        {
            return visited;
        }

        Queue<int> toVisit = new Queue<int>();
        toVisit.Enqueue(startNodeId);
        visited.Add(startNodeId);

        while (toVisit.Count > 0)
        {
            int current = toVisit.Dequeue();

            // Visit every neighbor of the current node. HashSet gives O(1) "already visited?" checks.
            foreach (int neighbor in nodes[current].Neighbors)
            {
                if (visited.Add(neighbor))
                {
                    toVisit.Enqueue(neighbor);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Depth-First Search (DFS)
    /// Goes deep down one path first, then backtracks when there is nowhere left to go.
    /// Returns every node ID that can be reached from the start.
    /// Time complexity: O(n) where n = number of graph nodes (rooms and doors), because each node is visited at most once.
    /// </summary>
    public HashSet<int> DFS(int startNodeId)
    {
        HashSet<int> visited = new HashSet<int>();

        // If the start node isn't in the graph, return empty instead of crashing.
        if (!nodes.ContainsKey(startNodeId))
        {
            return visited;
        }

        Stack<int> toVisit = new Stack<int>();
        toVisit.Push(startNodeId);

        while (toVisit.Count > 0)
        {
            int current = toVisit.Pop();

            if (!visited.Add(current))
            {
                continue; // already handled this node
            }

            // Push unvisited neighbors - the last pushed will be explored first (go deep before backtracking).
            foreach (int neighbor in nodes[current].Neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    toVisit.Push(neighbor);
                }
            }
        }

        return visited;
    }
}
