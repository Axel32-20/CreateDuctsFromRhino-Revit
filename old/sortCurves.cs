// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Runtime.CompilerServices;
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

    private void RunScript(List<object> crvs, ref object a)
		 
    {
        List<Rhino.Geometry.Curve> listCurves = crvs.Select( obj =>
        {
            
            if(obj is Rhino.Geometry.Curve c ) return c;
            if(obj is GH_Curve ghC) return ghC.Value;
            if (obj is GH_ObjectWrapper w && w.Value is Curve cw) return cw;
            return null;
        })
        .Where (c => c !=null && c.IsValid)
        .ToList();
            
        // obtener curva principal, maxima longitud orden decreciente

        Rhino.Geometry.Curve principalCrv = listCurves
        .OrderByDescending(c => c.GetLength())
        .First();
        //ramales
        List<Rhino.Geometry.Curve> listbranchCurves = listCurves.Where(c => c != principalCrv).ToList();
        // order
        List<Rhino.Geometry.Curve> cleanCurvesBranches = OrderCurves.OrderAndOrientBranchesCurves(principalCrv,listbranchCurves );
        a = cleanCurvesBranches;
    }
 
}
public static class OrderCurves
{
    public static List<Rhino.Geometry.Curve> OrderAndOrientBranchesCurves ( Rhino.Geometry.Curve princ, List<Rhino.Geometry.Curve> listCrvs)
    {
        return listCrvs
        .Select( crv =>
        { 
            Rhino.Geometry.Curve copyCrv = crv.DuplicateCurve();
         princ.ClosestPoint(copyCrv.PointAtStart, out double tStart);
         princ.ClosestPoint(copyCrv.PointAtEnd, out double tEnd);

        Rhino.Geometry.Point3d sPoint = copyCrv.PointAtStart;
        Rhino.Geometry.Point3d ePoint = copyCrv.PointAtEnd;
         double distStart = sPoint.DistanceTo(princ.PointAt(tStart));
         double distEnd = ePoint.DistanceTo(princ.PointAt(tEnd));

         if(distEnd < distStart)
            {
                copyCrv.Reverse();

            }
            return copyCrv;  
        })
        .OrderBy( curve =>
        {
            princ.ClosestPoint(curve.PointAtStart,out double t);
            return t;
        })
        .ToList();

    }

}