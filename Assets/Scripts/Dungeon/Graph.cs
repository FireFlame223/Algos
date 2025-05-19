using UnityEngine;
using System.Collections.Generic;

// Represents a graph data structure where nodes are connected by edges.
// Each node has a position in 2D space and can be connected to other nodes.
public class Graph
{
    // Stores the connections between nodes (adjacency list representation)
    // Key: node ID, Value: list of connected node IDs
    private Dictionary<int, List<int>> adjacencyList;
    
    // Stores the 2D position of each node
    // Key: node ID, Value: position in 2D space
    private Dictionary<int, Vector2> nodePositions;

    // Creates a new empty graph.
    public Graph()
    {
        adjacencyList = new Dictionary<int, List<int>>();
        nodePositions = new Dictionary<int, Vector2>();
    }

    // Adds a new node to the graph with the specified ID and position.
    // If a node with this ID already exists, it will not be modified.
    public void AddNode(int nodeId, Vector2 position)
    {
        if (!adjacencyList.ContainsKey(nodeId))
        {
            adjacencyList[nodeId] = new List<int>();
            nodePositions[nodeId] = position;
        }
    }

    // Creates a connection between two nodes.
    // If either node doesn't exist, it will be created.
    // If the connection already exists, nothing changes.
    public void AddEdge(int node1, int node2)
    {
        // Create nodes if they don't exist
        if (!adjacencyList.ContainsKey(node1))
            adjacencyList[node1] = new List<int>();
        if (!adjacencyList.ContainsKey(node2))
            adjacencyList[node2] = new List<int>();

        // Add connection if it doesn't exist
        if (!adjacencyList[node1].Contains(node2))
            adjacencyList[node1].Add(node2);
        if (!adjacencyList[node2].Contains(node1))
            adjacencyList[node2].Add(node1);
    }

    // Gets the 2D position of a node.
    public Vector2 GetNodePosition(int nodeId)
    {
        if (nodePositions.ContainsKey(nodeId))
        {
            return nodePositions[nodeId];
        }
        else
        {
            return Vector2.zero;
        }
    }

    // Gets all nodes that are connected to the specified node.
    public List<int> GetNeighbors(int nodeId)
    {
        if (adjacencyList.ContainsKey(nodeId))
        {
            return adjacencyList[nodeId];
        }
        else
        {
            return new List<int>();
        }
    }

    // Gets the total number of nodes in the graph.
    public int GetNodeCount()
    {
        return adjacencyList.Count;
    }

    // Gets all node IDs in the graph.
    public IEnumerable<int> GetAllNodes()
    {
        return adjacencyList.Keys;
    }
}