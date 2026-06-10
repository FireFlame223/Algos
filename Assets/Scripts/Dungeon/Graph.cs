using UnityEngine;
using System.Collections.Generic;

// Represents a graph data structure where nodes are connected by edges.
// Each node has a position in 2D space and can be connected to other nodes.
public class Graph
{
    private class Node
    {
        public Vector2 Position;
        public List<int> Neighbors = new List<int>();
    }

    // One dictionary holds both position and neighbors for each node.
    private Dictionary<int, Node> nodes;

    // Creates an empty graph.
    public Graph()
    {
        nodes = new Dictionary<int, Node>();
    }

    // Adds a new node to the graph with the specified ID and position.
    // If a node with this ID already exists, it will not be modified.
    public void AddNode(int nodeId, Vector2 position)
    {
        if (!nodes.ContainsKey(nodeId))
        {
            nodes[nodeId] = new Node { Position = position };
        }
    }

    // Creates a connection between two nodes that already exist.
    // If the connection already exists, nothing changes.
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

    // Returns the position of a node, or Vector2.zero if the ID does not exist.
    public Vector2 GetNodePosition(int nodeId)
    {
        if (nodes.ContainsKey(nodeId))
        {
            return nodes[nodeId].Position;
        }

        return Vector2.zero;
    }

    // Returns a copy so outside code cannot change the graph's internal lists.
    public List<int> GetNeighbors(int nodeId)
    {
        if (nodes.ContainsKey(nodeId))
        {
            return new List<int>(nodes[nodeId].Neighbors);
        }

        return new List<int>();
    }

    // Returns how many nodes are in the graph.
    public int GetNodeCount()
    {
        return nodes.Count;
    }

    // Returns every node ID in the graph.
    public IEnumerable<int> GetAllNodes()
    {
        return nodes.Keys;
    }
}
