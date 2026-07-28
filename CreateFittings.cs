
#region Usings
using System;
/*
#r "C:\Program Files\Autodesk\Revit 2025\RevitAPI.dll"
#r "C:\Program Files\Autodesk\Revit 2025\RevitAPIUI.dll"
#r "C:\Program Files\Rhino 8\Plug-ins\RhinoInside.Revit\RhinoInside.Revit.dll"
*/
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Revit = Autodesk.Revit.DB;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using RCurve = Rhino.Geometry.Curve;
using RvtCurve = Autodesk.Revit.DB.Curve;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Mechanical;
using RhinoInside.Revit.Convert.Geometry;
using Rhino.Render.DataSources;
using Rhino.UI.Controls.ThumbnailUI;
#endregion
public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object DuctA, object DuctB, ref object CreateTransition)
    {
        Document doc = RhinoInside.Revit.Revit.ActiveDBDocument;

        // 1. Conversión segura de entradas a objetos Duct de Revit
        Duct ductA = GetDuctElement(DuctA);
        Duct ductB = GetDuctElement(DuctB);

        if (ductA == null || ductB == null)
        {
            Print(" Error: Uno o ambos insumos no son Ductos válidos de Revit.");
            return;
        }

        // 2. Obtener los conectores libres que están enfrentados (más cercanos entre sí)
        var (connA, connB) = GetNearConnectors(ductA, ductB);


        if (connA == null || connB == null)
        {
            Print(" Error: No se encontraron conectores libres y enfrentados entre los ductos.");
            return;
        }
        bool isPerpendicular = IsPerpendicular(connA, connB);
        bool requiereTransition = !IsSameSize(connA, connB);

        using (Transaction t = new Transaction(doc, "Generar Transición"))
        {
            t.Start();
            FailureHandlingOptions failOptions = t.GetFailureHandlingOptions();
            failOptions.SetFailuresPreprocessor(new WarningSwallower());
            t.SetFailureHandlingOptions(failOptions);
            try
            {
                FamilyInstance familyInstanceFitting = null;
                if (requiereTransition)
                {
                    familyInstanceFitting = doc.Create.NewTransitionFitting(connA, connB);
                    Print("Transición creada con éxito.");
                }
                else if (isPerpendicular)
                {
                    familyInstanceFitting = doc.Create.NewElbowFitting(connA, connB);
                    Print("Codo creado con éxito.");
                }
                else
                {
                    familyInstanceFitting = doc.Create.NewUnionFitting(connA, connB);
                    Print("Unión (Copla) creada con éxito.");
                }
                CreateTransition = familyInstanceFitting;

                
            }
            catch (Exception ex)
            {
                Print($"Error de Revit al crear la transición: {ex.Message}");
            }

            t.Commit();
        }
    }

    // Helper para desempaquetar el objeto Duct enviado desde Grasshopper
    private Duct GetDuctElement(object input)
    {
        if (input == null) return null;

        if (input is Duct d) return d;


        if (input is GH_ObjectWrapper wrapper)
        {
            if (wrapper.Value is Duct ductWrapped) return ductWrapped;
        }


        var prop = input.GetType().GetProperty("Value");
        if (prop != null)
        {
            var val = prop.GetValue(input);
            if (val is Duct ductVal) return ductVal;
        }

        return null;
    }


    private (Connector, Connector) GetNearConnectors(Duct dA, Duct dB)

    {
        Connector mejorA = null;
        Connector mejorB = null;
        double menorDistancia = double.MaxValue;

        foreach (Connector cA in dA.ConnectorManager.Connectors)
        {
            if (cA.IsConnected) continue;

            foreach (Connector cB in dB.ConnectorManager.Connectors)
            {
                if (cB.IsConnected) continue;

                double dist = cA.Origin.DistanceTo(cB.Origin);
               
                if (dist < menorDistancia)
                {
                    menorDistancia = dist;
                    mejorA = cA;
                    mejorB = cB;
                }
            }
        }

        if (mejorA != null && mejorB != null)
            return (mejorA, mejorB);


    }
    private bool IsPerpendicular(Connector ca, Connector cb)
    {
        double dotProduct = ca.CoordinateSystem.BasisZ.DotProduct(cb.CoordinateSystem.BasisZ);
        return Math.Abs(dotProduct) < 0.15;

    }
    private bool IsSameSize(Connector ca, Connector cb)
    {
        double tolerancia = 0.1;
        if (ca.Shape != cb.Shape) return false;
        if (ca.Shape == ConnectorProfileType.Rectangular)
        {
            bool directSameSize = Math.Abs(ca.Width - cb.Width) < tolerancia && Math.Abs(ca.Height - cb.Height) < tolerancia;
            return directSameSize;

        }
        if (ca.Shape == ConnectorProfileType.Round)
        {
            return Math.Abs(ca.Radius - cb.Radius) < tolerancia;

        }
        return true;
    }

}
public class WarningSwallower : IFailuresPreprocessor
{
    public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
    {
        var failures = failuresAccessor.GetFailureMessages();
        foreach (var failure in failures)
        {
            if (failure.GetSeverity() == FailureSeverity.Warning)
            {
                failuresAccessor.DeleteWarning(failure);
            }
        }
        return FailureProcessingResult.Continue;
    }
}