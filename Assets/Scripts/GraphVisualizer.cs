using UnityEngine;

public class GraphVisualizer : MonoBehaviour
{
    public Color nodeColor = Color.yellow; // Color for all nodes
    public Color edgeColor = Color.cyan; // Color for connections between nodes

    public float nodeRadius = 0.4f; // Size for all nodes

    private Graph graph; // Reference to the graph being visualized

    // Updates the graph being visualized
    public void SetGraph(Graph newGraph)
    {
        graph = newGraph;
    }

    // Draws the graph visualization
    void OnDrawGizmos()
    {
        if (graph == null) return;

        // Draw nodes
        foreach (int nodeId in graph.GetAllNodes())
        {
            Vector2 position = graph.GetNodePosition(nodeId);
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(new Vector3(position.x, 1, position.y), nodeRadius);
        }

        // Draw all connections between nodes
        foreach (int nodeId in graph.GetAllNodes())
        {
            Vector2 startPos = graph.GetNodePosition(nodeId);
            foreach (int neighborId in graph.GetNeighbors(nodeId))
            {
                if (neighborId > nodeId) // Draw each edge only once
                {
                    Vector2 endPos = graph.GetNodePosition(neighborId);
                    Debug.DrawLine(
                        new Vector3(startPos.x, 1, startPos.y),
                        new Vector3(endPos.x, 1, endPos.y),
                        edgeColor
                    );
                }
            }
        }
    }
} 