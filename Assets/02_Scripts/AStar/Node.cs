using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public bool Walkable;
    public Vector3 worldPos;

    public int gridX; //그리드 내 x인덱스
    public int gridY; // 그리그내 y인덱스

    public int gCost; // 시작점에서 현재 노드까지의 실제 비율
    public int hCost; //목표까지의 휴리스틱 비율
    public Node parent; //경로 재구성

    public int fCost { get { return gCost + hCost; } } // public int fCost => gCost + hCost;
    public Node(bool tmpwalkable,Vector3 tmpWorldPos,int x,int y)
    {
        Walkable = tmpwalkable;
        worldPos = tmpWorldPos;
        this.gridX = x;
        this.gridY = y;
    }
}
