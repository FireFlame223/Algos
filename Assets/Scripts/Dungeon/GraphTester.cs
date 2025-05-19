using UnityEngine;

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

        // Connect the visualizer to the dungeon generator
        if (dungeonGenerator != null && graphVisualizer != null)
        {
            dungeonGenerator.graphVisualizer = graphVisualizer;
        }
    }
}