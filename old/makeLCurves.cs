// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(DataTree<Point3d> difusores, DataTree<Point3d> Rs,DataTree<Curve> conductos, ref object a)
    {
        DataTree<Polyline> outPutPoly = new DataTree<Polyline>();
        for(var p = 0; p<difusores.BranchCount;p++)
        {
            GH_Path currentPath = difusores.Paths[p];
            if(!conductos.PathExists(currentPath)) continue;


            List<Point3d> difusoresBranch= difusores.Branch(currentPath);
            
            List<Curve> conductosBranch = conductos.Branch(currentPath);
            List<Point3d> poolRs = new List<Point3d>();
            if(Rs.PathExists(currentPath))
            {
                poolRs = new List<Point3d>(Rs.Branch(currentPath));
            }
            
            for(int d=0; d< difusoresBranch.Count;d++)
            {
                Point3d ptDif = difusoresBranch[d];
                List<Point3d> listpts = new List<Point3d>();

                if(poolRs.Count > 0)
                {  
                    double mindist = double.MaxValue;
                    int nearIndex= 0;
                    for(int r = 0; r < poolRs.Count; r++)
                    {
                        double distance = ptDif.DistanceTo(poolRs[r]);
                        if(distance < mindist)
                        {
                            mindist = distance;
                            nearIndex = r;
                        }
                            
                    }
                    Point3d ptRWithDif = poolRs[nearIndex];
                    poolRs.RemoveAt(nearIndex);
                    Point3d finalPt = Point3d.Unset;
                    double minDistCond = double.MaxValue;

                    for(int c =0;c<conductosBranch.Count;c++)
                    {
                        Curve condPosible = conductosBranch[c];
                        double t;
                        if(condPosible.ClosestPoint(ptRWithDif, out t))
                        {
                            Point3d closestPointCond = condPosible.PointAt(t);
                            double distanceTo = ptRWithDif.DistanceTo(closestPointCond);
                            if (distanceTo < minDistCond)
                            {
                                minDistCond  = distanceTo;
                                finalPt = closestPointCond;
                            }
                            
                        }
                        
                    }

                }
               
              
              
            }



            
        }
        // Write your logic here
        a = null;
    }
}
public class PointsClosest
{
    public static Point3d FindPoint (List<Point3d> p1,List<Point3d> p2)
    {
        double dist = double.MaxValue;
        int indexNear = 0,

        

         
        
    }
}
