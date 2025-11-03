using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Node
{
    public float F => G + H;
    public float G;
    public float H;
    public Node Parent;
    public Vector2Int Position;
    public Node(Vector2Int position)
    {
        Position = position;
        G = float.MaxValue;
    }
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1),
        new Vector2Int(1,1),
        new Vector2Int(-1,1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,-1)
    };
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2.Distance(a, b);
    }
    private static bool IsValid(bool[,] map,Vector2Int pos)
    {
        if (map == null) return false;       

        int w = map.GetLength(0);
        int h = map.GetLength(1);

        if (pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h)
            return false;
        if (!map[pos.x, pos.y])        { 
           
            return false;
        }

        Vector2Int worldPos = new Vector2Int(pos.x - (GameManager.Instance.mapOffset.x-1), pos.y - (GameManager.Instance.mapOffset.y-1));

        if (!GameManager.Instance.IsInsideBattle(worldPos)) 
        {           
            return false; 
        }        
        return true;
    }
    public static bool[,] ConvertToMap(List<Vector3Int> wallPositions, int width , int height)
    {       
        bool[,] map = new bool[width, height];
        Vector2Int offSet = new Vector2Int(width / 2, height / 2);

        for(int x = 0; x < width;x++)
        {
            for(int y = 0; y < height; y++)
            {
                map[x, y] = true;          
            }
        }

        foreach(Vector3Int wall in wallPositions)
        {
            int gridX = wall.x + offSet.x;
            int gridY = wall.y + offSet.y;

            if(gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
            {
                map[gridX, gridY] = false;                
            }            
        }
        return map;
    }
    private static List<Vector2Int> BuildPath(Node endNode)
    {
        List<Vector2Int> pathList = new List<Vector2Int>();
        Node pathNode = endNode;

        while(pathNode != null)
        {
            pathList.Add(pathNode.Position);
            pathNode = pathNode.Parent;
        }
        pathList.Reverse();
        return pathList;
    }
    public static List<Vector2Int> FindPath(bool[,] map, Vector2Int start, Vector2Int end)
    {
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        
        if (!IsValid(map, start) || !IsValid(map, end)) return null;

        List<Node> openList = new List<Node>();
        List<Vector2Int> closedList = new List<Vector2Int>();
        Dictionary<Vector2Int, Node> allNodes = new Dictionary<Vector2Int, Node>();

        Node startNode = new Node(start) { G = 0, H = Heuristic(start, end) };
        openList.Add(startNode);
        allNodes[start] = startNode;

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => 
            {                
                int fComparison = a.F.CompareTo(b.F);
                
                if (fComparison == 0)
                {                                	
                    return a.H.CompareTo(b.H);
                }
                return fComparison;
            });

            Node curNode = openList[0];
            openList.RemoveAt(0);
            closedList.Add(curNode.Position);

            if(curNode.Position == end)
            {
                return BuildPath(curNode);
            }
            foreach(Vector2Int dir in Directions)
            {
                Vector2Int nextPos = curNode.Position + dir;

                if(!IsValid(map,nextPos)|| closedList.Contains(nextPos))
                {
                    continue;
                }

                if(Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y)==1)
                {
                    Vector2Int horizontal = new Vector2Int(curNode.Position.x + dir.x, curNode.Position.y);
                    Vector2Int vertical = new Vector2Int(curNode.Position.x, curNode.Position.y + dir.y);

                    if (horizontal.x < 0 || horizontal.x >= w || horizontal.y < 0 || horizontal.y >= h ||
                        vertical.x < 0 || vertical.x >= w || vertical.y < 0 || vertical.y >= h)
                        continue;

                    if (!map[horizontal.x, horizontal.y] && !map[vertical.x, vertical.y])
                    {
                        continue;
                    }
                }

                float deltaG = (dir.x == 0 || dir.y == 0) ? 1f : 1.4f;
                float newG = curNode.G + deltaG;
                float newH = Heuristic(nextPos, end);

                if(!allNodes.TryGetValue(nextPos, out Node nextNode))
                {
                    nextNode = new Node(nextPos) { H = newH };
                    allNodes[nextPos] = nextNode;
                }
                if(newG < nextNode.G)
                {
                    nextNode.G = newG;
                    nextNode.Parent = curNode;

                    if(!openList.Contains(nextNode))
                    {
                        openList.Add(nextNode);
                    }
                }
            }
        }
        return null;
    }
}
