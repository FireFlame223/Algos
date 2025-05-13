using UnityEngine;

public class GraphTester : MonoBehaviour
{
    void Start()
    {
        Graph<string> graph = new Graph<string>();

        graph.AddNode("A");
        graph.AddNode("B");
        graph.AddNode("C");
        graph.AddNode("D");
        graph.AddNode("E");
        graph.AddNode("F");

        graph.AddEdge("A", "E");
        graph.AddEdge("A", "C");
        graph.AddEdge("A", "F");
        graph.AddEdge("B", "D");
        graph.AddEdge("B", "F");
        graph.AddEdge("B", "E");
        graph.AddEdge("C", "E");
        graph.AddEdge("D", "F");
        graph.AddEdge("Q", "E");

        Debug.Log("Graph Structure:");
        graph.PrintGraph();
    }
}