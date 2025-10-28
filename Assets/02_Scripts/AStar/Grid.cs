using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [Header("그리드설정")]
    public GameObject character;
    public LayerMask UnWalkableLayer;
    public Vector2 gridWorldSize;
    public float nodeRadius;

    public Node[,] grid;
    public int gridXCnt;
    public int gridYCnt;
    public float nodeDiameter;

    public Vector3 worldBottomLeft;
    private void Awake()
    {
        Init();
    }   
    private void Init()
    {
        nodeDiameter = nodeRadius * 2;
        gridXCnt = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridYCnt = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreatGrid();
    }
    private void CreatGrid()
    {
        grid = new Node[gridXCnt, gridYCnt];

        //현재 포지션에서 x축으로 gridWorldSize의 x값/2 만큼 빼고 y축으로 gridWorldSize.y/2만큼 빼야함.
        worldBottomLeft = transform.position 
                          - Vector3.right * gridWorldSize.x / 2
                          - Vector3.up * gridWorldSize.y / 2;

        for (int i = 0; i < gridXCnt; i++)
        {
            for(int j = 0; j < gridYCnt;j++)
            {
                Vector3 worldPoint = worldBottomLeft + (i * nodeDiameter + nodeRadius) * Vector3.right + (j * nodeDiameter + nodeRadius) * Vector3.up;
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius, UnWalkableLayer);
                grid[i, j] = new Node(walkable, worldPoint,i,j);
            }
        }
    }
    public Node GetNodeFromWorldPoint(Vector3 worldPos)
    {
        float percentX = (worldPos.x - worldBottomLeft.x) / gridWorldSize.x;
        float percentY = (worldPos.y - worldBottomLeft.y) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridXCnt - 1) * percentX);
        int y = Mathf.RoundToInt((gridYCnt - 1) * percentY);
        return grid[x, y];
    }
    public List<Node> FindPath(Vector3 startPos,Vector3 targetPos)
    {
        Node startNode = GetNodeFromWorldPoint(startPos);
        Node targetNode = GetNodeFromWorldPoint(targetPos);

        List<Node> path = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        path.Add(startNode);
        while (path.Count > 0)
        {
            Node presentNode = path[0];
            for (int i = 1; i < path.Count; i++) //왜 1부터일까
            {
                if (path[i].fCost < presentNode.fCost|| (path[i].fCost == presentNode.fCost && path[i].hCost < presentNode.hCost))
                {
                    presentNode = path[i];
                }
            }
            path.Remove(presentNode);
            closedSet.Add(presentNode);

            if(presentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }
            foreach (Node neighbor in GetNeighbor(presentNode) )
            {
                if (!neighbor.Walkable || closedSet.Contains(neighbor))
                    continue;

                int newCostNeighbor = presentNode.gCost + GetDistance(presentNode, neighbor);
                if(newCostNeighbor < neighbor.gCost || !path.Contains(neighbor))
                {
                    neighbor.gCost = newCostNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = presentNode;

                    if(!path.Contains(neighbor))
                    {
                        path.Add(neighbor);
                    }
                }
            }
        }    
        return null;
    }
    List<Node> GetNeighbor(Node node)
    {
        List<Node> result = new List<Node>();

        for(int i = -1; i <= 1;i++)
        {
            for(int j = -1; j<=1;j++)
            {
                if (Mathf.Abs(i) == Mathf.Abs(j)) continue;

                int checkX = node.gridX + i;
                int checkY = node.gridY + j;

                if(checkX >= 0 && checkX<gridXCnt && checkY >= 0 && checkY < gridYCnt)
                {
                    result.Add(grid[checkX, checkY]);
                }    
            }
        }
        return result;
    }
    int GetDistance(Node a,Node b)
    {
        int distX = Mathf.Abs(a.gridX - b.gridX);
        int distY = Mathf.Abs(a.gridY - b.gridY);
        return distX + distY;
    }
    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node presentNode = endNode;
        while (presentNode != startNode) 
        {
            path.Add(presentNode);
            presentNode = presentNode.parent;
        }
        path.Reverse();
        return path;
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x,gridWorldSize.y,1));

        if(grid != null)
        {
            Node characterNode = null;
            if (character != null)
            { 
                characterNode = GetNodeFromWorldPoint(character.transform.position); 
            }
            foreach(Node n in grid)
            {
                Gizmos.color = n.Walkable ? Color.green : Color.red;
                if (characterNode == n) Gizmos.color = Color.cyan;
                Gizmos.DrawCube(n.worldPos, Vector3.one * (nodeDiameter - 0.05f));
            }
        }
    }
}

