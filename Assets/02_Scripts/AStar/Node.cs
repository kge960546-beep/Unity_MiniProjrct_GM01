using System.Collections;
using System.Collections.Generic;
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
        int w = map.GetLength(0);
        int h = map.GetLength(1);
        return (pos.x >= 0 && pos.x < w ) && (pos.y >= 0 && pos.y < h) && map[pos.x, pos.y];
    }
    public static bool[,] ConvertToMap(List<Vector3Int> wallPositions, int width , int height)
    {
        bool[,] map = new bool[width, height];
        for(int x = 0; x < width;x++)
        {
            for(int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x,y);
                map[x, y] = !wallPositions.Contains(new Vector3Int(x,y,0));
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
        Debug.Log($"[A*] FindPath 실행됨 → start={start}, end={end}");

        List<Node> openList = new List<Node>();
        List<Vector2Int> closedList = new List<Vector2Int>();
        Dictionary<Vector2Int, Node> allNodes = new Dictionary<Vector2Int, Node>();

        Node startNode = new Node(start) { G = 0, H = Heuristic(start, end) };
        openList.Add(startNode);
        allNodes[start] = startNode;

        while (openList.Count > 0)
        {
            // 3-1) openList 순서 정렬
            // PriorityQueue를 구현한 경우 해당 과정 필요x
            openList.Sort((a, b) => {
                // F값 기준 오름차순으로 정렬
                int fComparison = a.F.CompareTo(b.F);

                // 만약 F값이 같으면
                if (fComparison == 0)
                {
                    // H값 기준 오름차순으로 정렬 
                    // => 남은 경로(H)가 작아야 최단 경로일 가능성 ↑            	
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

                    if(!IsValid(map, horizontal) || !IsValid(map,vertical))
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
