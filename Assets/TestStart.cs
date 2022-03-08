using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OptionsFile;

public class TestStart : MonoBehaviour
{
    GameObject basePlane;
    GameObject terrain;
    Vector3 mBoundsMin;
    Vector3 mBoundsMax;

    int currentDistCheck = 10;
    int regionCount = 0;
    List<GameObject> regionList;
    public GameObject cell;
    Vector3 cellScale;
    public GameObject line;
    GameObject hfParent;
    List<BoxCollider> colliderList;
    List<GameObject> lineList;

    //Options Values
    Options ops;
    float boundsHeight;
    float mXZCellSize;
    float mYCellSize;
    int maxCheckDistValue;
    int initialRegionSize;
    int minRegionDistFromBorder;

    // Start is called before the first frame update
    void Start()
    {
        ops = new Options();
        boundsHeight = ops.BoundsHeight;
        mXZCellSize = ops.XZCellSize;
        mYCellSize = ops.YCellSize;
        maxCheckDistValue = ops.MaxCheckDistValue;
        initialRegionSize = ops.InitialRegionSize;
        minRegionDistFromBorder = ops.MinRegionDistFromBorder;

        cellScale = new Vector3(mXZCellSize, mYCellSize, mXZCellSize);
        basePlane = GameObject.Find("Plane");
        Bounds planeBounds = basePlane.GetComponent<BoxCollider>().bounds;
        mBoundsMin = planeBounds.min;
        mBoundsMax = planeBounds.max;
        mBoundsMax.y += boundsHeight;

        colliderList = new List<BoxCollider>();
        colliderList.Add(basePlane.GetComponent<BoxCollider>());
        terrain = GameObject.Find("Terrain");
        BoxCollider[] childColliders = terrain.GetComponentsInChildren<BoxCollider>();
        foreach (BoxCollider cC in childColliders)
        {

            colliderList.Add(cC);
        }

    }

    public void Stage(int i)
    {
        switch (i)
        {
            case 1:
                Voxelise(colliderList);
                break;
            case 2:
                NeighboursGen();
                break;
            case 3:
                SmoothingPass();
                break;
            case 4:
                RegionGen();
                RegionClean();
                break;
            case 5:
                EdgeFind();
                ContourGen();
                break;
            case 6:
                RefineContour();
                break;
            case 7:
                PolygonGeneration();
                GivePolygonsLines();
                break;
        }
    }

    public void Voxelise(List<BoxCollider> meshList) //85 seconds on full scale
    {
        hfParent = new GameObject("hfParent");

        List<GameObject> columnList = new List<GameObject>();
        GameObject firstInColumn = null;

        for (float x = mBoundsMin.x; x <= mBoundsMax.x; x += mXZCellSize)
        {
            for (float z = mBoundsMin.z; z <= mBoundsMax.z; z += mXZCellSize)
            {
                for (float y = mBoundsMin.y; y <= mBoundsMax.y; y += mYCellSize)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    GameObject newCell = GameObject.Instantiate(cell, pos, Quaternion.identity);
                    bool intersect = false;
                    Collider[] hitList = Physics.OverlapBox(newCell.transform.position, newCell.transform.localScale / 2, Quaternion.identity);

                    foreach (Collider hitCollider in hitList)
                    {
                        foreach (BoxCollider levelCollider in meshList)
                        {
                            if (levelCollider == hitCollider)
                            {
                                intersect = true;
                                break;
                            }
                        }
                        if (intersect) { break; }
                    }
                    if (intersect == false)
                    {
                        GameObject.Destroy(newCell);
                        if (columnList.Count > 1)
                        {
                            ColumnCreate(columnList, firstInColumn, x, z);
                            columnList = new List<GameObject>();
                            firstInColumn = null;
                        }
                    }
                    else
                    {
                        newCell.transform.parent = hfParent.transform;
                        if (columnList.Count == 0)
                        { firstInColumn = newCell; }
                        columnList.Add(newCell);

                    }
                }
                columnList = new List<GameObject>();
            }
        }
    }

    public void ColumnCreate(List<GameObject> columnCells, GameObject columnFirst, float x, float z)
    {
        int numOfCellsInColumn = columnCells.Count;
        GameObject columnLast = columnCells[numOfCellsInColumn - 1];
        float yPoint = (columnLast.transform.position.y + columnFirst.transform.position.y) / 2;
        foreach (GameObject oldCell in columnCells) { GameObject.Destroy(oldCell); }
        Vector3 pos = new Vector3(x, yPoint, z);
        Vector3 newScale = new Vector3(1, numOfCellsInColumn, 1);
        GameObject newColumn = GameObject.Instantiate(cell, pos, Quaternion.identity);
        newColumn.transform.localScale = Vector3.Scale(cellScale, newScale);
        newColumn.transform.parent = hfParent.transform;
    }

    public void NeighboursGen()
    {
        basePlane.SetActive(false); terrain.SetActive(false);
        Cell[] cellList = hfParent.GetComponentsInChildren<Cell>();
        foreach (Cell c in cellList)
        {
            c.Stage2Start();
        }
        foreach (Cell c in cellList)
        {
            c.NeighbourFind();
        }
        foreach (Cell c in cellList)
        {
            c.TotalNeighbourCount();
        }
        DistanceMap();

        basePlane.SetActive(true); terrain.SetActive(true);
    }

    public void DistanceMap()   //25 seconds on full scale
    {
        Cell[] cellList = hfParent.GetComponentsInChildren<Cell>();

        for (int i = 0; i < cellList.Length; i++)
        {
            Queue<Cell> cellQueue = new Queue<Cell>();
            List<Cell> checkedCells = new List<Cell>();
            Cell currentCell = cellList[i];
            int dist = 1;
            if (!currentCell.BorderCell)
            {
                checkedCells.Add(currentCell);
                cellQueue.Enqueue(currentCell);
                while (cellQueue.Count > 0)
                {
                    if (dist > maxCheckDistValue) { cellList[i].DistFromBorder = maxCheckDistValue; break; } //THIS SPED THINGS UP A TON, AS DISTANCE GRANULARITY NOT NEEDED IN BIG OPEN SPACES
                    currentCell = cellQueue.Dequeue();
                    List<Cell> currentNeighbours = currentCell.Neighbours;
                    if (cellQueue.Count % 8 == 7) { dist++; }
                    foreach (Cell c in currentNeighbours)
                    {
                        if (c.BorderCell)
                        {
                            cellList[i].DistFromBorder = dist;
                            currentCell.DistFromBorder = 1;
                            cellQueue.Clear();
                            break;
                        }
                        else if (!checkedCells.Contains(c))
                        {
                            checkedCells.Add(c);
                            cellQueue.Enqueue(c);
                        }
                    }
                }
            }
        }
    }

    public void SmoothingPass() //<1 second on full scale
    {
        Cell[] cellList = hfParent.GetComponentsInChildren<Cell>();
        for (int i = 0; i < cellList.Length; i++)
        {
            Cell currentCell = cellList[i];
            List<Cell> currentNeighbours = currentCell.Neighbours;
            int check = maxCheckDistValue;
            foreach (Cell c in currentNeighbours)
            {
                if (c.DistFromBorder < check) { check = c.DistFromBorder; }
                if (currentCell.DistFromBorder - check > 1) { currentCell.DistFromBorder = check + 1; }
            }
        }
    }

    public void RegionGen()
    {
        regionList = new List<GameObject>();
        List<Cell> cellList = new List<Cell>(hfParent.GetComponentsInChildren<Cell>());
        regionCount = 0;
        for (int i = 0; i < cellList.Count; i++) { if (cellList[i].DistFromBorder <= 3 || cellList[i].BorderCell) { cellList.Remove(cellList[i]); } }
        bool firstPassDone = false;
        while (cellList.Count > 0)
        {
            currentDistCheck--;
            if (currentDistCheck <= minRegionDistFromBorder) { break; }

            if (!firstPassDone)
            {
                for (int i = 0; i < cellList.Count; i++)
                {
                    if (cellList[i].DistFromBorder > currentDistCheck && !cellList[i].InRegion)
                    {
                        regionCount++;
                        Cell currentCell = cellList[i];
                        cellList.Remove(cellList[i]);

                        GameObject regionObject = new GameObject("Region" + regionCount.ToString());
                        regionList.Add(regionObject);
                        currentCell.transform.parent = regionObject.transform;
                        int regionSize = initialRegionSize - 1;

                        Queue<Cell> cellQueue = new Queue<Cell>();
                        cellQueue.Enqueue(currentCell);

                        while (cellQueue.Count > 0 && regionSize > 0)
                        {
                            currentCell = cellQueue.Dequeue();
                            List<Cell> currentNeighbours = currentCell.Neighbours;
                            foreach (Cell c in currentNeighbours)
                            {
                                if (c.DistFromBorder > currentDistCheck && !c.InRegion)
                                {
                                    c.Region = regionCount;
                                    c.transform.parent = regionObject.transform;
                                    regionSize--;
                                    cellQueue.Enqueue(c);
                                    cellList.Remove(c);
                                }
                            }
                        }
                        cellQueue.Clear();
                    }
                }
                firstPassDone = true;
            }
            for (int i = 0; i < cellList.Count; i++)
            {
                if (cellList[i].DistFromBorder > currentDistCheck && !cellList[i].InRegion)
                {
                    Cell currentCell = cellList[i];
                    List<Cell> currentNeighbours = currentCell.Neighbours;
                    List<int> localRegionList = new List<int>();
                    foreach (Cell c in currentNeighbours)
                    {
                        if (c.DistFromBorder > currentDistCheck && c.InRegion)
                        {
                            localRegionList.Add(c.Region);
                        }
                    }
                    if (localRegionList.Count > 1)
                    {
                        int mostRegion = GetMode(localRegionList);
                        currentCell.Region = mostRegion;
                        currentCell.transform.parent = GameObject.Find("Region" + mostRegion.ToString()).transform;
                        cellList.Remove(currentCell);
                    }
                    else if (localRegionList.Count == 1)
                    {
                        currentCell.Region = localRegionList[0];
                        currentCell.transform.parent = GameObject.Find("Region" + localRegionList[0].ToString()).transform;
                        cellList.Remove(currentCell);
                    }
                }
            }
        }
        cellList = new List<Cell>(hfParent.GetComponentsInChildren<Cell>()); //Extra pass to pick up any cells left behind
        for (int i = 0; i < cellList.Count; i++)
        {
            if(cellList[i].DistFromBorder > minRegionDistFromBorder && !cellList[i].InRegion)
            {
                Cell currentCell = cellList[i];
                List<Cell> currentNeighbours = currentCell.Neighbours;
                foreach (Cell c in currentNeighbours)
                {
                    if(c.InRegion)
                    {
                        currentCell.Region = c.Region;
                        currentCell.transform.parent = c.transform.parent;
                    }
                }
            }
        }
    }

    public void RegionClean() //Cleans up voxels that stick out of their region, and should be in another
    {
        foreach(GameObject r in regionList)
        {
            List<Cell> cellList = new List<Cell>(r.GetComponentsInChildren<Cell>());
            for(int i = 0; i < cellList.Count; i++)
            {
                Cell currentCell = cellList[i];
                List<Cell> currentNeighbours = currentCell.Neighbours;
                int otherRegionCells = 0;
                int ownRegionCells = 0;
                Cell RegionCellPick = currentCell;
                foreach(Cell c in currentNeighbours)
                {
                    if(c.Region != currentCell.Region && c.InRegion)
                    {
                        if (cellList.Count == 1)
                        {
                            currentCell.Region = c.Region;
                        }
                        else
                        {
                            otherRegionCells++;
                            RegionCellPick = c;
                        }
                    }
                    if(c.Region == currentCell.Region)
                    {
                        ownRegionCells++;
                    }
                }
                if(otherRegionCells > ownRegionCells)
                {
                    currentCell.Region = RegionCellPick.Region;
                    currentCell.transform.parent = RegionCellPick.transform.parent;
                }
            }


        }

        List<Cell> borderCellList = new List<Cell>(hfParent.GetComponentsInChildren<Cell>());
        foreach(Cell c in borderCellList)
        {
            c.Region = 0;
            c.BorderCell = true;
        }
    }

    public void EdgeFind() //Finds which of a cell's edges are border edges and which are internal edges
    {
        foreach (GameObject r in regionList)
        {
            List<Cell> cellList = new List<Cell>(r.GetComponentsInChildren<Cell>());
            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].EdgeBorderFind();
            }
        }
    }

    public void ContourGen()
    {

        foreach (GameObject r in regionList)
        {
            if (r.transform.childCount == 0)
            {
                continue;
            }

            List<Vector3> contourVertices = new List<Vector3>();

            Cell firstInContour = null;
            List<Cell> cellList = new List<Cell>(r.GetComponentsInChildren<Cell>());

            for (int i = 0; i < cellList.Count; i++) //All this loop to find a cell on the region's border
            {
                if (cellList[i].NorthEdgeBorder == true)
                {
                    firstInContour = cellList[i];
                    break;
                }
            }
            
            bool contourComplete = false;
            Cell currentCell = firstInContour;
            bool turnAntiClockwise = false;
            int j = 1;

            while(!contourComplete)
            {   
                Vector3 vertex1 = new Vector3(0, 0, 0);
                Vector3 vertex2 = new Vector3(0, 0, 0);

                if (currentCell.EdgeBorderCheck(j, out vertex1, out vertex2))
                {

                    if (!contourVertices.Contains(vertex1)) { contourVertices.Add(vertex1); }
                    if (!contourVertices.Contains(vertex2)) { contourVertices.Add(vertex2); }
                    if(contourVertices.Count > 2)
                    {                       
                        if (contourVertices[0] == vertex1 || contourVertices[0] == vertex2)
                        {
                            contourComplete = true;
                        }
                    }
                }
                else
                {
                    currentCell = currentCell.ClockwiseNeighbourCheck(j);
                    turnAntiClockwise = true; //Tells next search to turn 'anticlockwise' by one step when moved into new cell
                }

                if (turnAntiClockwise) //Turns search 'anticlockwise' by one step after moving into new cell
                {
                    turnAntiClockwise = false;
                    if (j == 1) { j = 5; }
                    j--;
                }
                else //Turns search 'clockwise' by one step when staying in same cell
                {
                    j++;
                    if (j == 5) { j = 1; }
                }
            }

            GameObject regionLineObject = GameObject.Instantiate(line, new Vector3(0, 0, 0), Quaternion.identity);
            regionLineObject.transform.parent = r.transform;

            LineScript regionLineScript = regionLineObject.GetComponent<LineScript>();
            regionLineScript.FillContour(contourVertices);
            regionLineScript.Region = firstInContour.Region;
            
            Vector3 v = regionLineObject.transform.position;
            v.y += 0.1f;
            regionLineObject.transform.position = v;
        }
    }

    public void RefineContour()
    {
        foreach (GameObject r in regionList)
        {
            LineScript line = r.GetComponentInChildren<LineScript>();
            if(line == null) { continue; }
            bool enoughMands = line.CheckMandsAndNullBorders();
            if(enoughMands)
            {
                line.RefineContour2();
            }
            else
            {
                line.RefineContourNoMands();
            }
        }
    }

    public void PolygonGeneration()
    {
        lineList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Line"));
        foreach (GameObject line in lineList)
        {
            LineScript lineScript = line.GetComponent<LineScript>();
            List<Vertex> vertices = lineScript.ContourVertices;

            bool allTrianglesMade = false;

            while (!allTrianglesMade)
            {
                float shortestPotentialEdge = 1000;
                Vertex cornerOfPotentailEdgeTri = null;

                for (int i = 0; i < vertices.Count; i++)  //Gives Vertices their before's and after's
                {
                    if(i == 0)
                    {
                        vertices[i].Before = vertices[vertices.Count - 1];
                        vertices[i].After = vertices[i + 1];
                    }
                    else if(i == vertices.Count-1)
                    {
                        vertices[i].Before = vertices[i - 1];
                        vertices[i].After = vertices[0];
                    }
                    else
                    {
                        vertices[i].Before = vertices[i - 1];
                        vertices[i].After = vertices[i + 1];
                    }
                }

                for (int i = 0; i < vertices.Count; i++)
                {
                    if(vertices.Count == 3) //If just 3 vertices left, make triangle out of them
                    {
                        allTrianglesMade = true;
                        cornerOfPotentailEdgeTri = vertices[1];
                        break;
                    }
                    else
                    {
                        Vertex current = vertices[i];
                        Vertex before = current.Before;
                        Vertex after = current.After;

                        Vector2 towardBefore = before.XZVector - current.XZVector;
                        Vector2 towardAfter = after.XZVector - current.XZVector;
                        Vector2 towardPotential = after.After.XZVector - current.XZVector;

                        float betweenOrNot = VectorDegreesNormalized(towardBefore, towardAfter, towardPotential); //Find if potential partition is between before and after and if not, use it

                        if (betweenOrNot < 1 && betweenOrNot > 0)
                        {
                            float dist = Vector3.Distance(current.Position, after.After.Position);
                            if (dist < shortestPotentialEdge)
                            {
                                shortestPotentialEdge = dist;
                                cornerOfPotentailEdgeTri = after;

                            }
                        }
                    }
                }
                if(cornerOfPotentailEdgeTri != null)
                {
                    GameObject triangle = GameObject.Instantiate(Resources.Load<GameObject>("Triangle"), new Vector3(0, 0, 0), Quaternion.identity);
                    Mesh triMesh = triangle.GetComponent<MeshFilter>().mesh;

                    Vector3[] triVerts = new[] { cornerOfPotentailEdgeTri.Before.Position, cornerOfPotentailEdgeTri.Position, cornerOfPotentailEdgeTri.After.Position };
                    triMesh.vertices = triVerts;
                    int[] triangles = new int[] { 0, 1, 2 };
                    triMesh.triangles = triangles;
                    vertices.Remove(cornerOfPotentailEdgeTri);

                    Vector3 v = triangle.transform.position;
                    v.y += 0.1f;
                    triangle.transform.position = v;
                }
                else
                {
                    allTrianglesMade = true;
                }
            }
        }
    }

    public void GivePolygonsLines()
    {
        foreach(GameObject line in lineList)
        {
            Destroy(line);
        }

        List<GameObject> pgList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Polygon"));
        foreach(GameObject polygon in pgList)
        {
            List<Vector3> vertList = new List<Vector3>(polygon.GetComponent<MeshFilter>().mesh.vertices);
            
            GameObject regionLineObject = GameObject.Instantiate(Resources.Load<GameObject>("Line"), new Vector3(0, 0, 0), Quaternion.identity);
            LineScript regionLineScript = regionLineObject.GetComponent<LineScript>();

            lineList.Add(regionLineObject);

            regionLineScript.FillContour(vertList);

            Vector3 v = regionLineObject.transform.position; //Raises contour to make easily viewable
            v.y += 0.2f;
            regionLineObject.transform.position = v;
            regionLineObject.GetComponent<LineRenderer>().startWidth = 0.1f;
            regionLineObject.GetComponent<LineRenderer>().endWidth = 0.1f;
        }
    }

    public int GetMode(List<int> list)
    {
        // Initialize the return value
        int mode = default(int);
        // Test for a null reference and an empty list
        if (list != null && list.Count > 0)
        {
            // Store the number of occurences for each element
            Dictionary<int, int> counts = new Dictionary<int, int>();
            // Add one to the count for the occurence of a character
            foreach (int element in list)
            {
                if (counts.ContainsKey(element))
                    counts[element]++;
                else
                    counts.Add(element, 1);
            }
            // Loop through the counts of each element and find the 
            // element that occurred most often
            int max = 0;
            foreach (KeyValuePair<int, int> count in counts)
            {
                if (count.Value > max)
                {
                    // Update the mode
                    mode = count.Key;
                    max = count.Value;
                }
            }
        }
        return mode;
    }

    public float VectorDegreesNormalized(Vector2 left, Vector2 right, Vector2 toCheck) //If vector exceeds right bound, it returns greater than one, and less than zero if vector exceeds left bound.
    {
        float funnelAngle = Vector2.Angle(left, right);
        float checkAngle = Vector2.SignedAngle(left, toCheck);
        return checkAngle / funnelAngle;
    }
}
