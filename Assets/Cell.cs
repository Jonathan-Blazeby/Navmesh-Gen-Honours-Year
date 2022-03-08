using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OptionsFile;

public class Cell : MonoBehaviour
{
    List<Cell> neighbours;
    Collider[] neighbourCols;
    public Collider col;
    public MeshRenderer meshR;
    Options ops;
    
    float stepHeight;
    float cellHeight;
    float cellSize;
    int distFromBorder;
    int region = 0;
    bool borderCell = false;
    bool distChecked = false;
    bool inRegion = false;
    bool swappedRegion = false;

    public bool northEdgeBorder = false;
    public bool southEdgeBorder = false;
    public bool westEdgeBorder = false;
    public bool eastEdgeBorder = false;

    public Cell northNeighbour;
    public Cell southNeighbour;
    public Cell westNeighbour;
    public Cell eastNeighbour;

    Vector3 northWestVertex;
    Vector3 northEastVertex;
    Vector3 southWestVertex;
    Vector3 southEastVertex;


    #region Getters/Setters
    public List<Cell> Neighbours
    {
        get { return neighbours; }
        set { neighbours = value; }
    }

    public float CellHeight
    {
        get { return cellHeight; }
        set { cellHeight = value; }
    }

    public int DistFromBorder
    {
        get { return distFromBorder; }
        set 
        { 
            distFromBorder = value;
            DistChecked = true;
            DistanceMapColourChange();
        }
    }

    public int Region
    {
        get { return region; }
        set
        {
            region = value;
            inRegion = true;
            RegionColourChange();
        }
    }

    public bool BorderCell
    {
        get { return borderCell; }
        set { borderCell = value; }
    }

    public bool DistChecked
    {
        get { return distChecked; }
        set { distChecked = value; }
    }

    public bool InRegion
    {
        get { return inRegion; }
        set { inRegion = value; }
    }

    public bool SwappedRegion
    {
        get { return swappedRegion; }
        set { swappedRegion = value; }
    }

    public bool NorthEdgeBorder
    {
        get { return northEdgeBorder; }
        set { northEdgeBorder = value; }
    }
    public bool SouthEdgeBorder
    {
        get { return southEdgeBorder; }
        set { southEdgeBorder = value; }
    }
    public bool WestEdgeBorder
    {
        get { return westEdgeBorder; }
        set { westEdgeBorder = value; }
    }
    public bool EastEdgeBorder
    {
        get { return eastEdgeBorder; }
        set { eastEdgeBorder = value; }
    }

    public Cell NorthNeighbour
    {
        get { return northNeighbour; }
        set { northNeighbour = value; }
    }
    public Cell SouthNeighbour
    {
        get { return southNeighbour; }
        set { southNeighbour = value; }
    }
    public Cell WestNeighbour
    {
        get { return westNeighbour; }
        set { westNeighbour = value; }
    }
    public Cell EastNeighbour
    {
        get { return eastNeighbour; }
        set { eastNeighbour = value; }
    }

    public Vector3 NorthWestVertex
    {
        get { return northWestVertex; }
        set { northWestVertex = value; }
    }
    public Vector3 NorthEastVertex
    {
        get { return northEastVertex; }
        set { northEastVertex = value; }
    }
    public Vector3 SouthWestVertex
    {
        get { return southWestVertex; }
        set { southWestVertex = value; }
    }
    public Vector3 SouthEastVertex
    {
        get { return southEastVertex; }
        set { southEastVertex = value; }
    }
    #endregion

    public void Stage2Start()
    {
        ops = new Options();
        cellSize = ops.XZCellSize;
        stepHeight = ops.StepHeight;
        neighbours = new List<Cell>();
        cellHeight = transform.position.y + (transform.localScale.y / 2);
        VertexSet();
    }

    public void NeighbourFind()
    {       
        cellHeight = transform.position.y + (transform.localScale.y /2);
        Vector3 cellTop = transform.position;
        cellTop.y += (transform.localScale.y / 2);
        neighbourCols = Physics.OverlapSphere(cellTop, cellSize);
        foreach(Collider c in neighbourCols)
        {
            if (c != col && c.gameObject.CompareTag("Cell") && !neighbours.Contains(c.GetComponent<Cell>()))
            {
                Cell n = c.gameObject.GetComponent<Cell>();
                if (System.Math.Abs(cellHeight - n.CellHeight) <= stepHeight)
                {
                    neighbours.Add(n);
                    n.AddNeighbour(this);
                }
             
            }
        }
    }

    public void AddNeighbour(Cell c)
    {
        Vector3 cellPos = c.transform.position;
        if (cellPos.x == transform.position.x && cellPos.z - transform.position.z >= 0) //North Check
        {
            northNeighbour = c;
            c.SouthNeighbour = this;
        }
        if (cellPos.x == transform.position.x && cellPos.z - transform.position.z < 0) //South Check
        {
            southNeighbour = c;
            c.NorthNeighbour = this;
        }

        if (cellPos.x - transform.position.x < 0 && cellPos.z == transform.position.z) //West Check
        {
            westNeighbour = c;
            c.EastNeighbour = this;
        }

        if (cellPos.x - transform.position.x >= 0 && cellPos.z == transform.position.z) //East Check
        {
            eastNeighbour = c;
            c.WestNeighbour = this;
        }
        if (neighbours.Count < 8 && !neighbours.Contains(c))
        {
            neighbours.Add(c);           
        }
    }

    public void TotalNeighbourCount()
    {
        if (neighbours.Count < 8)
        {
            this.BorderCell = true;
            distFromBorder = 0;
            meshR.material = Resources.Load<Material>("0");
        }
    }

    public void DistanceMapColourChange()
    {
        if (distFromBorder < 11)
        {
            meshR.material = Resources.Load<Material>(distFromBorder.ToString());
        }
        else
        {
            meshR.material = Resources.Load<Material>("10");
        }
    }

    public void RegionColourChange()
    {
        if (region != 0)
        {
            int r = region % 5;
            meshR.material = Resources.Load<Material>("Region" + r.ToString());
        }
        else { meshR.material = Resources.Load<Material>("10"); }
    }

    public void VertexSet()
    {
        NorthWestVertex = new Vector3(transform.position.x - (cellSize / 2), cellHeight, transform.position.z + (cellSize / 2));
        NorthEastVertex = new Vector3(transform.position.x + (cellSize / 2), cellHeight, transform.position.z + (cellSize / 2));
        SouthWestVertex = new Vector3(transform.position.x - (cellSize / 2), cellHeight, transform.position.z - (cellSize / 2));
        SouthEastVertex = new Vector3(transform.position.x + (cellSize / 2), cellHeight, transform.position.z - (cellSize / 2));
    }

    public void EdgeBorderFind()
    {
        if (northNeighbour == null || northNeighbour.Region != region) { northEdgeBorder = true; } else { northEdgeBorder = false; }
        if (southNeighbour == null || southNeighbour.Region != region) { southEdgeBorder = true; } else { southEdgeBorder = false; }
        if (westNeighbour == null || westNeighbour.Region != region) { westEdgeBorder = true; } else { westEdgeBorder = false; }
        if (eastNeighbour == null || eastNeighbour.Region != region) { eastEdgeBorder = true; } else { eastEdgeBorder = false; }
    }

    public bool EdgeBorderCheck(int directionNESW, out Vector3 vertex1, out Vector3 vertex2)
    {
        switch (directionNESW)
        {
            case 1:
                if (northEdgeBorder) { vertex1 = northWestVertex; vertex2 = northEastVertex; return true; }
                break;
            case 2:
                if (eastEdgeBorder) { vertex1 = northEastVertex; vertex2 = southEastVertex; return true; }
                break;
            case 3:
                if (southEdgeBorder) { vertex1 = southEastVertex; vertex2 = southWestVertex; return true; }
                break;
            case 4:
                if (westEdgeBorder) { vertex1 = southWestVertex; vertex2 = northWestVertex; return true; }
                break;
        }
        vertex1 = new Vector3(0, 0, 0); vertex2 = new Vector3(0, 0, 0);
        return false;
    }

    public Cell ClockwiseNeighbourCheck(int directionNESW)
    {
        switch(directionNESW)
        {
            case 1:
                if (!northEdgeBorder) { return northNeighbour; } break;
            case 2:
                if (!eastEdgeBorder) { return eastNeighbour; } break;
            case 3:
                if (!southEdgeBorder) { return southNeighbour; } break;
            case 4:
                if (!westEdgeBorder) { return westNeighbour; } break;
        }
        return null;
    }


}
