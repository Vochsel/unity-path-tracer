using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

struct ptBVHBoundingBox
{
    Vector3 position, bounds;
};

abstract class ptBVHNode
{
    public Vector3 bottom, top;
    public abstract bool IsLeaf();
};

class ptBVHInner: ptBVHNode
{
    public ptBVHNode left, right;
    public override bool IsLeaf() { return false; }
};

class ptBVHLeaf : ptBVHNode
{
    public List<ptTriangle> triangles;
    public override bool IsLeaf() { return true; }


    public ptBVHLeaf()
    {
        triangles = new List<ptTriangle>();
    }
};

[StructLayout(LayoutKind.Explicit)]
struct ptCacheFriendlyBVHNode
{
    [FieldOffset(0)]
    public Vector3 bottom;
    [FieldOffset(12)]
    public Vector3 top; //12 bytes

    //Inner node
    [FieldOffset(24)]
    public uint idxLeft;
    [FieldOffset(28)]
    public uint idxRight;

    //Leaf node
    [FieldOffset(24)]
    public uint count;
    [FieldOffset(28)]
    public uint startIndexInTriIndexList;
}

class BBoxTmp
{
    public Vector3 bottom, top, center;
    public ptTriangle pTri;
    public BBoxTmp()
    {
        bottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        top = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        pTri = new ptTriangle();
    }
}

class ptBVH
{
    

    public static ptBVHNode Recurse(List<BBoxTmp> work, int depth = 0)
    {
        if(work.Count < 4)
        {
            ptBVHLeaf leaf = new ptBVHLeaf();
            foreach(BBoxTmp tmp in work)
            {
                leaf.triangles.Add(tmp.pTri);
            }
            return leaf;
        }

        // Otherwise divide node into smaller nodes

        // Find current list bounding box 

        Vector3 bottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 top = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        // Loop over all boxes in current list, expanding  the working list
        for(int i = 0; i < work.Count; i++)
        {
            BBoxTmp v = work[i];
            bottom = Vector3.Min(bottom, v.bottom);
            top = Vector3.Max(top, v.top);
        }

        // Surface area heuristic, find sufrace area of bounding box by multiplyling the dimensions of the current working list bb
        float side1 = top.x - bottom.x;
        float side2 = top.y - bottom.y;
        float side3 = top.z - bottom.z;

        //current bbox has a cost of (num of tris) * sA of C = N * SA
        float minCost = work.Count * (side1 * side2 + side2 * side3 + side3 * side1);

        float bestSplit = float.MaxValue;

        int bestAxis = -1;

        //Try all three axis
        for(int j = 0; j < 3; j++)
        {
            int axis = j;

            float start = 0.0f, stop = 0.0f, step = 0.0f;

            //X axis
            if(axis == 0)
            {
                start = bottom.x;
                stop = top.x;
            }
            // Y axis
            else if (axis == 1)
            {
                start = bottom.y;
                stop = top.y;
            }
            //Z axis
            else if(axis == 2)
            {
                start = bottom.z;
                stop = top.z;
            }

            // In that axis do the bounding boxes in work queue span across
            // Or are they all baxed (close together)
            if(Mathf.Abs(stop - start) < 1e-4)
            {
                continue;
            }

             // Binning
            step = (stop - start) / (1024.0f / (depth + 1.0f));

            //For each bin
            for(float testSplit = start + step; testSplit < stop - step; testSplit += step)
            {
                Vector3 lbottom2 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 ltop2 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

                Vector3 rbottom2 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 rtop2 = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

                int countLeft = 0, countRight = 0;

                for(int i = 0; i < work.Count; i++)
                {
                    BBoxTmp v = work[i];

                    //Compute bbox center
                    float value;
                    if (axis == 0) value = v.center.x;
                    else if (axis == 1) value = v.center.y;
                    else value = v.center.z;

                    if(value < testSplit)
                    {
                        lbottom2 = Vector3.Min(lbottom2, v.bottom);
                        ltop2 = Vector3.Max(ltop2, v.top);
                        countLeft++;
                    } else
                    {
                        rbottom2 = Vector3.Min(rbottom2, v.bottom);
                        rtop2 = Vector3.Max(rtop2, v.top);
                        countRight++;
                    }
                }

                if (countLeft <= 1 || countRight <= 1) continue;


                float lside1 = ltop2.x - lbottom2.x;
                float lside2 = ltop2.y - lbottom2.y;
                float lside3 = ltop2.z - lbottom2.z;

                float rside1 = rtop2.x - rbottom2.x;
                float rside2 = rtop2.y - rbottom2.y;
                float rside3 = rtop2.z - rbottom2.z;

                float surfaceLeft = lside1 * lside2 + lside2 * lside3 + lside3 * lside1;
                float surfaceRight = rside1 * rside2 + rside2 * rside3 + rside3 * rside1;

                float totalCost = surfaceLeft * countLeft + surfaceRight * countRight;

                if(totalCost < minCost)
                {
                    minCost = totalCost;
                    bestSplit = testSplit;
                    bestAxis = axis;
                }
            }
        }

        // if found no split to improve, create leaf
        if(bestAxis == -1)
        {
            ptBVHLeaf leaf = new ptBVHLeaf();
            
            foreach (BBoxTmp tmp in work)
            {
                leaf.triangles.Add(tmp.pTri);
            }
            return leaf;
        }

        //Otherwise create bvh of inner node with l and r child nodes

        List<BBoxTmp> left = new List<BBoxTmp>();
        List<BBoxTmp> right = new List<BBoxTmp>();

        Vector3 lbottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 ltop = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        Vector3 rbottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 rtop = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        for(int i = 0; i < work.Count; i++)
        {
            BBoxTmp v = work[i];

            float value;
            if (bestAxis == 0) value = v.center.x;
            else if (bestAxis == 1) value = v.center.y;
            else value = v.center.z;

            if(value < bestSplit)
            {
                left.Add(v);
                lbottom = Vector3.Min(lbottom, v.bottom);
                ltop = Vector3.Max(ltop, v.top);
            } else
            {
                right.Add(v);
                rbottom = Vector3.Min(rbottom, v.bottom);
                rtop = Vector3.Max(rtop, v.top);
            }
        }

        ptBVHInner inner = new ptBVHInner();

        inner.left = Recurse(left, depth + 1);
        inner.left.bottom = lbottom;
        inner.left.top = ltop;

        inner.right = Recurse(right, depth + 1);
        inner.right.bottom = rbottom;
        inner.right.top = rtop;

        return inner;
    }

    public static ptBVHNode ConstructBVH(Mesh a_mesh)
    {
        //Create work bbox
        //Create bbox for each triangle and compute bounds
        // Expand bounds work bbox to fit all triangle boxes
        // Compute triangle bbxox center andadd tri to working list, build bvh tree with recurse
        // return root node
        // 
        List<BBoxTmp> work = new List<BBoxTmp>();

        Vector3 bottom = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 top = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

        Debug.Log("Gathering bounding box info from all triangles");

        for(int j = 0; j < a_mesh.triangles.Length; j+=3)
        {
            Vector3 v1 = a_mesh.vertices[a_mesh.triangles[j]];
            Vector3 v2 = a_mesh.vertices[a_mesh.triangles[j + 1]];
            Vector3 v3 = a_mesh.vertices[a_mesh.triangles[j + 2]];

            ptTriangle tri = new ptTriangle(v1, v2, v3);

            BBoxTmp b = new BBoxTmp();

            b.pTri = tri;

            b.bottom = Vector3.Min(b.bottom, v1);
            b.bottom = Vector3.Min(b.bottom, v2);
            b.bottom = Vector3.Min(b.bottom, v3);

            b.top = Vector3.Max(b.top, v1);
            b.top = Vector3.Max(b.top, v2);
            b.top = Vector3.Max(b.top, v3);

            bottom = Vector3.Min(bottom, b.bottom);
            top = Vector3.Max(top, b.top);

            b.center = (b.top + b.bottom) * 0.5f;

            work.Add(b);

        }

        Debug.Log("Creating BVH data...");
        ptBVHNode root = Recurse(work);

        root.bottom = bottom;
        root.top = top;

        return root;
    }

    public static uint CountBoxes(ptBVHNode root)
    {
        if (!root.IsLeaf())
        {
            ptBVHInner p = (ptBVHInner)root;
            return 1 + CountBoxes(p.left) + CountBoxes(p.right);
        }
        else
            return 1;
    }

    public static uint CountTriangles(ptBVHNode root)
    {
        if (!root.IsLeaf())
        {
            ptBVHInner p = (ptBVHInner)root;
            return CountTriangles(p.left) + CountTriangles(p.right);
        }
        else
        {
            ptBVHLeaf p = (ptBVHLeaf)root;
            return (uint)p.triangles.Count;
        }
    }

    public static void CountDepth(ptBVHNode root, int depth, ref int maxDepth)
    {
        if(maxDepth < depth)
        {
            maxDepth = depth;
        }
        if(!root.IsLeaf())
        {
            ptBVHInner p = (ptBVHInner)root;
            CountDepth(p.left, depth + 1, ref maxDepth);
            CountDepth(p.right, depth + 1, ref maxDepth);
        }
    }

    public List<ptTriangle> triangleList;
    public ptCacheFriendlyBVHNode[] cfbvhList;

    //public List<ptBVHNode> 

    public void PopulateCacheFriendlyBVH(ptBVHNode root, ref uint idxBoxes, ref uint idxTriList)
    {
        int currIdxBoxes = (int)idxBoxes;

        
        cfbvhList[currIdxBoxes] = new ptCacheFriendlyBVHNode();
        
        cfbvhList[currIdxBoxes].bottom = root.bottom;
        cfbvhList[currIdxBoxes].top = root.top;

        if (!root.IsLeaf())
        {
            // inner node

            ptBVHInner p = (ptBVHInner)root;
            uint idxLeft = ++idxBoxes;
            PopulateCacheFriendlyBVH(p.left, ref idxBoxes, ref idxTriList);

            uint idxRight = ++idxBoxes;
            PopulateCacheFriendlyBVH(p.right, ref idxBoxes, ref idxTriList);

            cfbvhList[currIdxBoxes].idxLeft = idxLeft;
            cfbvhList[currIdxBoxes].idxRight = idxRight;

        }
        else
        {
            //leaf
            ptBVHLeaf p = (ptBVHLeaf)root;
            uint count = (uint)p.triangles.Count;

            cfbvhList[currIdxBoxes].count = 0x80000000 | count;
            cfbvhList[currIdxBoxes].startIndexInTriIndexList = idxTriList;

            foreach(ptTriangle tri in p.triangles)
            {
                idxTriList++;
                triangleList.Add(tri);
            }
        }
    }

    public void CreateCacheFriendlyBVH(Mesh a_mesh)
    {

        ptBVHNode root = ptBVH.ConstructBVH(a_mesh);
        uint triCount = ptBVH.CountTriangles(root);
        uint boxCount = ptBVH.CountBoxes(root);

        triangleList = new List<ptTriangle>((int)triCount);
        cfbvhList = new ptCacheFriendlyBVHNode[((int)boxCount)];

        uint idxTriList = 0;
        uint idxBoxes = 0;

        PopulateCacheFriendlyBVH(root, ref idxBoxes, ref idxTriList);

        Debug.Log(cfbvhList.ToString());
        Debug.Log(cfbvhList.Length);
    }

    
};