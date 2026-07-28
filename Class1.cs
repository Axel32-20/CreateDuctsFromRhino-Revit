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

    private void RunScript(
		List<object> texts,
		ref object a,
		ref object b,
		ref object R)
    {
        if(texts == null || texts.Count == 0) return;

        List<InformacionConducto> informacionConductos = new List<InformacionConducto>();
        List<string> textSalida = new List<string>();
        List<Rhino.Geometry.Point3d> posSalida = new List<Rhino.Geometry.Point3d>();
        List<Rhino.Geometry.Point3d> listRPoints = new List<Rhino.Geometry.Point3d>();
         
         
        foreach(object obj in texts)
        {
            if(obj == null) continue;
            string textRaw = string.Empty;
            Rhino.Geometry.Point3d posText = Rhino.Geometry.Point3d.Origin;
            if(obj is TextEntity textEntity)
            {
                textRaw = textEntity.PlainText;
                posText = textEntity.Plane.Origin;

            }
            else if ( obj is GH_ObjectWrapper wrapper && wrapper.Value is TextEntity te)
            {
                 textRaw = te.PlainText;
                posText= te.Plane.Origin;
                
            }
            else
            {
                textRaw = obj.ToString();
            }
            if(DWGMatchText.IsCatchRegulator(textRaw))
            {
                listRPoints.Add(posText);
                continue;
            }
            InformacionConducto informacionConducto = DWGMatchText.CatchDimensionFromText(textRaw);
            if(informacionConducto != null)
            {
                informacionConducto.Position = posText;
                informacionConducto.PosR = Rhino.Geometry.Point3d.Unset;
                informacionConductos.Add(informacionConducto);
                posSalida.Add(posText);
                string d = informacionConducto.IsCircular ?
                informacionConducto.Diameter.ToString()
                : informacionConducto.DimRect.ToString();
                textSalida.Add(d);
            }
        }
        //Puntos cercanso de R con temrinal 
        double MaxDistance = 1.0; //mmm
        foreach(Rhino.Geometry.Point3d ptR in listRPoints )
        {
            InformacionConducto NearConduct = null;
           

            double minDis = double.MaxValue;
               foreach(var conducto in informacionConductos)
                {
                    Rhino.Geometry.Point3d ptR2D = new Rhino.Geometry.Point3d(ptR.X,ptR.Y,0);
                    Rhino.Geometry.Point3d con2D = new Rhino.Geometry.Point3d(conducto.Position.X,conducto.Position.Y,0);
                        double distanceR = ptR2D.DistanceTo(con2D);
                         
                        if(distanceR < minDis && distanceR < MaxDistance)
                            {
                                minDis = distanceR;
                                NearConduct = conducto;
                            
                            }
                }
                if(NearConduct != null)
            {
                NearConduct.PosR = ptR;
                Print("Asociado con éxito a una distancia de: " + Math.Round(minDis, 2) + " mm");
            }
        }
     
        // Write your logic here
        
        a = textSalida;
        b = posSalida;
        R = listRPoints;
    }
}
public class test
{
    public void Foo()
    {
        Point3d point = new Point3d();
        
    }
}
public class InformacionConducto
{
    public bool IsCircular {get;set;}
    public Rhino.Geometry.Point3d Position {get;set;}
    public double Diameter{get;set;}
    public double Width {get; set;}
    public double Height {get;set;}
    public (double width,double height) DimRect =>( Width,Height );
    public Rhino.Geometry.Point3d PosR {get;set;}

}
public static class DWGMatchText
{
    private static Regex RegIsCircular = new Regex(@"(?i)(Ø|%%c)\s*(?<Diameter>\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static Regex RegIsRectangular = new Regex(@"(?i)(?<Width>\d+(?:\.\d+)?)\s*x\s*(?<Height>\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static Regex RegIsRegulator = new Regex(@"^\s*(R)\s*$", RegexOptions.Compiled);

    public static InformacionConducto CatchDimensionFromText(string textDWG)
    { 
        if(string.IsNullOrWhiteSpace(textDWG)) return null;

        Match matchCircular = RegIsCircular.Match(textDWG);
        if(matchCircular.Success)
        {
            return new InformacionConducto
            {
                IsCircular = true,
                Diameter = Convert.ToDouble(matchCircular.Groups["Diameter"].Value)


            };
                
        }
        Match matchRectangular = RegIsRectangular.Match(textDWG);
        if(matchRectangular.Success)
        {
            return new InformacionConducto
            {
                IsCircular = false,
                Width = Convert.ToDouble(matchRectangular.Groups["Width"].Value),
                Height = Convert.ToDouble(matchRectangular.Groups["Height"].Value),
                
            };
        }
        

        return null;
    }
     public static bool IsCatchRegulator(string textDWG)
    {
        if(string.IsNullOrWhiteSpace(textDWG)) return false;
        string textoLimpio = textDWG.Trim().Replace("\r", "").Replace("\n", "");
        return textoLimpio.Equals("R", StringComparison.OrdinalIgnoreCase);

    }
   
  


}