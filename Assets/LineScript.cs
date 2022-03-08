using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OptionsFile;

public class LineScript : MonoBehaviour
{
    public LineRenderer lineRenderer;
    List<Vertex> contourVertices;
    int region;
    Options ops;

    public int Region
    {
        get { return region; }
        set { region = value; }
    }

    public List<Vertex> ContourVertices
    {
        get { return contourVertices; }
        set { contourVertices = value; }
    }

    public void FillContour(List<Vector3> cornerPositions)
    {
        ops = new Options();

        contourVertices = new List<Vertex>();
        lineRenderer.positionCount = cornerPositions.Count;
        for (int i = 0; i < cornerPositions.Count; i++)
        {
            Vertex vert = new Vertex(cornerPositions[i], region);
            lineRenderer.SetPosition(i, cornerPositions[i]);
            contourVertices.Add(vert);
        }
    }

    public bool CheckMandsAndNullBorders()
    {
        int numOfMands = 0;
        for (int i = 0; i < contourVertices.Count; i++)
        {
            Vertex vert = contourVertices[i];
            vert.SurroundingRegionCheck();
            if (vert.Mandatory)
            {
                numOfMands++;
            }
        }
        if (numOfMands >= 2)
        {
            return true;
        }
        else { return false; }
    }

    public void RefineContourNoMands()
    {
        bool nullVertSpacingBool = true;

        List<Vector3> newVerts = new List<Vector3>();
        for (int i = 0; i < contourVertices.Count; i++)
        {
            Vertex vert = contourVertices[i];
            if(vert.Mandatory)
            {
                newVerts.Add(vert.Position);
            }
            else if(vert.NullBorderingOnly & !nullVertSpacingBool)
            {
                newVerts.Add(vert.Position);
                nullVertSpacingBool = true;
            }
            else if(vert.NullBorderingOnly & nullVertSpacingBool)
            {
                nullVertSpacingBool = false;
            }
        }
        NewLine(newVerts);
    }

    public void RefineContour2()
    {
        List<Vertex> mandatories = new List<Vertex>();

        List<Vertex> finalVerts = new List<Vertex>();
        for (int i = 0; i < contourVertices.Count; i++)
        {
            if (contourVertices[i].Mandatory) //This section finds
            {
                mandatories.Add(contourVertices[i]);
                if (i == contourVertices.Count - 1 && contourVertices[0].NullBorderingOnly)
                {
                    contourVertices[i].MandFollowedByNull = true;
                }
                else if (i < contourVertices.Count - 1 && contourVertices[i + 1].NullBorderingOnly)
                {
                    contourVertices[i].MandFollowedByNull = true;
                }

            }
        }


        for (int j = 0; j < mandatories.Count; j++)
        {
            List<Vertex> nullRegionLine = new List<Vertex>();
            if (mandatories[j].MandFollowedByNull)
            {
                Vertex startMand;
                Vertex endMand;
                if (j == mandatories.Count - 1)
                {
                    startMand = mandatories[j];
                    endMand = mandatories[0];
                }
                else
                {
                    startMand = mandatories[j];
                    endMand = mandatories[j + 1];
                }

                int startMandContVertIndex = contourVertices.IndexOf(startMand);

                int endMandContVertIndex = contourVertices.IndexOf(endMand);

                List<Vertex> nullRegionVertices = new List<Vertex>();       //Start Mandatory - all the null-bordering vertices - End Mandatory

                if (startMandContVertIndex > endMandContVertIndex)   //For if it wraps around to the start of the list
                {
                    for (int x = endMandContVertIndex; x < contourVertices.Count; x++)
                    {
                        nullRegionVertices.Add(contourVertices[x]);
                    }
                    for (int x = 0; x < startMandContVertIndex + 1; x++)
                    {
                        nullRegionVertices.Add(contourVertices[x]);
                    }
                }
                else
                {
                    for (int x = startMandContVertIndex; x < endMandContVertIndex + 1; x++)
                    {
                        nullRegionVertices.Add(contourVertices[x]);
                    }
                }
                nullRegionLine.Add(startMand);
                nullRegionLine.Add(endMand);
                Vertex firstFound = MaxNullEdgeDistCheckFirst(startMand, endMand, nullRegionVertices);

                if (firstFound != null)
                {
                    nullRegionLine.Insert(1, firstFound);
                }

                for (int x = 1; x < nullRegionLine.Count; x++)
                {
                    Vertex v1 = nullRegionLine[(x - 1)];
                    Vertex v2 = nullRegionLine[x];
                    int v1Index = nullRegionVertices.IndexOf(v1);
                    int v2Index = nullRegionVertices.IndexOf(v2);
                    if (v1Index < 0 || v2Index < 0)
                    {
                        continue;
                    }
                    Vertex found = MaxNullEdgeDistCheck(v1, nullRegionVertices.IndexOf(v1), v2, nullRegionVertices.IndexOf(v2), nullRegionVertices);
                    if (found != null)
                    {
                        if(!nullRegionLine.Contains(found))
                        {
                            nullRegionLine.Insert(x, found);
                            if (nullRegionLine.Count > nullRegionVertices.Count) { break; }
                            x--;
                        }
                    }
                }
            }
            else
            {
                if (!finalVerts.Contains(mandatories[j]))
                {
                    finalVerts.Add(mandatories[j]);
                }
            }

            foreach (Vertex finalV in nullRegionLine)
            {
                if (!finalVerts.Contains(finalV))
                {
                    finalVerts.Add(finalV);
                }
            }
        }

   
        List<Vector3> finalVertPos = new List<Vector3>();

        finalVertPos.Add(finalVerts[0].Position);

        for(int q = 1; q < finalVerts.Count - 1; q++)
        {
            Vertex current = finalVerts[q];
            Vertex before = finalVerts[q-1];
            Vertex after = finalVerts[q+1];
            if (System.Math.Abs(Vector3.Distance(current.Position,before.Position)) > ops.XZCellSize || System.Math.Abs(Vector3.Distance(current.Position, after.Position)) > ops.XZCellSize)
            {
                finalVertPos.Add(current.Position);
            }
        }
        finalVertPos.Add(finalVerts[finalVerts.Count - 1].Position);

        NewLine(finalVertPos);
    }

    public Vertex MaxNullEdgeDistCheckFirst(Vertex StartMand, Vertex EndMand, List<Vertex> nullRegionContour) //First check must be done seperatly and slightly differently
    {
        Vector3 midPoint = ((StartMand.Position + EndMand.Position) / 2);
        Vertex furthestFound = null;
        float furthestDist = 0;
        for (int j = 1; j < nullRegionContour.Count-1; j++)
        {
            float dist = Vector3.Distance(midPoint, nullRegionContour[j].Position);
            if (dist > ops.MaxNullRegionContour && dist > furthestDist)
            {
                furthestFound = nullRegionContour[j];
                furthestDist = dist;
            }
        }
        if (furthestFound != null)
        {
            return furthestFound;
        }
        else { return null; }
    }

    public Vertex MaxNullEdgeDistCheck(Vertex v1, int v1Index, Vertex v2, int v2Index, List<Vertex> nullRegionContour)
    {
        Vector3 midPoint = ((v1.Position + v2.Position) / 2);
        Vertex furthestFound = null;
        float furthestDist = 0;
        if(v1Index > v2Index)
        {
            for (int j = v2Index; j < nullRegionContour.Count; j++)
            {
                float dist = Vector3.Distance(midPoint, nullRegionContour[0].Position);

                Vector3 difference = midPoint - nullRegionContour[0].Position;

                float xDiff = System.Math.Abs(difference.x);
                float zDiff = System.Math.Abs(difference.z);

                if (dist > ops.MaxNullRegionContour && dist > furthestDist)
                {
                    if(xDiff == 0 && zDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if(zDiff == 0 && xDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if(xDiff > ops.MaxNullRegionContour && zDiff > ops.MaxNullRegionContour )
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                }
            }
            for (int j = 0; j < v1Index +1; j++)
            {
                float dist = Vector3.Distance(midPoint, nullRegionContour[j + 1].Position);

                Vector3 difference = midPoint - nullRegionContour[0].Position;

                float xDiff = System.Math.Abs(difference.x);
                float zDiff = System.Math.Abs(difference.z);

                if (dist > ops.MaxNullRegionContour && dist > furthestDist)
                {
                    if (xDiff == 0 && zDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if (zDiff == 0 && xDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if (xDiff > ops.MaxNullRegionContour && zDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                }
            }
        }
        else
        {
            for (int j = v1Index; j < v2Index; j++)
            {
                float dist = Vector3.Distance(midPoint, nullRegionContour[j + 1].Position);

                Vector3 difference = midPoint - nullRegionContour[0].Position;

                float xDiff = System.Math.Abs(difference.x);
                float zDiff = System.Math.Abs(difference.z);

                if (dist > ops.MaxNullRegionContour && dist > furthestDist)
                {
                    if (xDiff == 0 && zDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if (zDiff == 0 && xDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }
                    else if (xDiff > ops.MaxNullRegionContour && zDiff > ops.MaxNullRegionContour)
                    {
                        furthestFound = nullRegionContour[j];
                        furthestDist = dist;
                    }

                }
            }
        }

        if(furthestFound != null)
        {
            return furthestFound;
        }
        else { return null; }
    }

    public void NewLine(List<Vector3> vertList)
    {
        GameObject regionLineObject = GameObject.Instantiate(Resources.Load<GameObject>("Line"), new Vector3(0, 0, 0), Quaternion.identity);
        LineScript regionLineScript = regionLineObject.GetComponent<LineScript>();

        regionLineScript.FillContour(vertList);

        Vector3 v = regionLineObject.transform.position; //Raises contour to make easily viewable
        v.y += 0.1f;
        regionLineObject.transform.position = v;

        Destroy(gameObject);
    }
}



public class Vertex
{
    Vector3 pos;
    Vector2 xzVector;
    int region;
    int numOfSurroundingRegions = 0;
    bool mandatory = false;
    bool mandFollowedByNull = false;
    bool nullBorderingOnly = false;
    List<int> regionList;
    Options ops;
    Vertex before;
    Vertex after;

    public Vertex(Vector3 p, int r)
    {
        pos = p;
        xzVector = new Vector2(p.x, p.z);
        region = r;
        regionList = new List<int>();
        ops = new Options();
    }

    #region Getters/Setters
    public Vector3 Position
    {
        get { return pos; }
        set { pos = value; }
    }

    public Vector2 XZVector
    {
        get { return xzVector; }
        set { xzVector = value; }
    }

    public bool Mandatory
    {
        get { return mandatory; }
        set { mandatory = value; }
    }

    public bool MandFollowedByNull
    {
        get { return mandFollowedByNull; }
        set { mandFollowedByNull = value; }
    }

    public bool NullBorderingOnly
    {
        get { return nullBorderingOnly; }
        set { nullBorderingOnly = value; }
    }

    public Vertex Before
    {
        get { return before; }
        set { before = value; }
    }

    public Vertex After
    {
        get { return after; }
        set { after = value; }
    }

    #endregion

    public void SurroundingRegionCheck()
    {
        List<Collider> cols = new List<Collider>(Physics.OverlapSphere(pos, ops.XZCellSize/2));
        foreach(Collider c in cols)
        {
            int rCheck;
            if (c.gameObject.CompareTag("Cell"))
            {
                if(c.gameObject.GetComponent<Cell>().BorderCell)
                {
                    rCheck = 0;
                }
                else
                {
                    rCheck = c.gameObject.GetComponent<Cell>().Region;
                }
            }
            else { continue; }

            if (!regionList.Contains(rCheck))
            {
                regionList.Add(rCheck);
                numOfSurroundingRegions++;
            }
        }
        if (numOfSurroundingRegions >= 3)
        {
            mandatory = true;
        }
        else if (numOfSurroundingRegions == 2 && regionList.Contains(0))
        {
            nullBorderingOnly = true;
        }
    }

}
