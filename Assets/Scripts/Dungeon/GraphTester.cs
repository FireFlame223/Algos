using UnityEngine;

/// <summary>
/// Wires the GraphVisualizer to the DungeonGenerator in the scene.
/// Attach this if the two components are on different GameObjects.
/// </summary>
public class GraphTester : MonoBehaviour
{
    public DungeonGenerator dungeonGenerator;
    public GraphVisualizer graphVisualizer;

    void Start()
    {
        if (dungeonGenerator == null)
        {
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
        }

        if (graphVisualizer == null)
        {
            graphVisualizer = FindObjectOfType<GraphVisualizer>();
        }

        if (dungeonGenerator != null && graphVisualizer != null)
        {
            dungeonGenerator.graphVisualizer = graphVisualizer;
        }
    }
}
