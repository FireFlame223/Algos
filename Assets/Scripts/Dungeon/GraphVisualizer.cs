using UnityEngine;

/// <summary>
/// Draws the dungeon graph in the Scene view while it is being built.
/// Yellow spheres = nodes, cyan lines = edges.
/// </summary>
public class GraphVisualizer : MonoBehaviour
{
    [Tooltip("Color of room and door nodes in the Scene view.")]
    public Color nodeColor = Color.yellow;

    [Tooltip("Color of the lines between connected nodes.")]
    public Color edgeColor = Color.cyan;

    [Tooltip("Size of the node spheres.")]
    public float nodeRadius = 0.4f;

    private Graph graph;

    /// <summary>Called by DungeonGenerator whenever the graph changes.</summary>
    public void SetGraph(Graph newGraph)
    {
        graph = newGraph;
    }

    void OnDrawGizmos()
    {
        if (graph == null)
        {
            return;
        }

        // Draw each node as a small sphere above the floor.
        foreach (int nodeId in graph.GetAllNodes())
        {
            Vector2 position = graph.GetNodePosition(nodeId);
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(new Vector3(position.x, 1, position.y), nodeRadius);
        }

        // Draw each connection once (only when neighborId > nodeId to avoid duplicates).
        foreach (int nodeId in graph.GetAllNodes())
        {
            Vector2 startPos = graph.GetNodePosition(nodeId);

            foreach (int neighborId in graph.GetNeighbors(nodeId))
            {
                if (neighborId > nodeId)
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
