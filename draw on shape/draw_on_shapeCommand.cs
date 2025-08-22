using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Display;
using System;
using System.Linq;

namespace DrawOnShape
{
    public class DrawOnShapeCommand : Command
    {
        public override string EnglishName => "DrawOnShape";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // --- 1. SELECCIONAR LA GEOMETRÍA ---
            GetObject go = new GetObject();
            go.SetCommandPrompt("Select a SubD, Brep, or Mesh to draw on");
            go.GeometryFilter = ObjectType.SubD | ObjectType.Brep | ObjectType.Mesh;
            go.SubObjectSelect = false;
            go.Get();

            if (go.CommandResult() != Result.Success) return go.CommandResult();
            GeometryBase geo = go.Object(0).Geometry();
            if (geo == null) return Result.Failure;

            // --- 2. CONVERTIR A MALLA ---
            Mesh drawingMesh = null;
            if (geo is SubD subd) drawingMesh = Mesh.CreateFromSubD(subd, 3);
            else if (geo is Brep brep)
            {
                var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);
                if (meshes != null)
                {
                    drawingMesh = new Mesh();
                    foreach (var m in meshes) drawingMesh.Append(m);
                }
            }
            else if (geo is Mesh mesh) drawingMesh = mesh;

            if (drawingMesh == null) return Result.Failure;
            drawingMesh.Normals.ComputeNormals();

            // --- 3. RECOLECTAR PUNTOS ---
            var pointsOnMesh = new List<Point3d>();
            bool closeCurve = false;
            const double snapRadiusInPixels = 15.0;

            while (true)
            {
                var gp = new DynamicCurveOnMeshGetter(pointsOnMesh);
                gp.SetCommandPrompt("Click on surface to add point (click near start to close, Enter to finish)");
                gp.AcceptNothing(true);
                gp.Constrain(drawingMesh, false);

                if (pointsOnMesh.Count >= 2) // Se puede previsualizar cierre con 2 puntos
                {
                    gp.EnableSnapping(pointsOnMesh[0], snapRadiusInPixels);
                }

                GetResult getResult = gp.Get();

                if (getResult == GetResult.Point)
                {
                    Point3d currentPoint = gp.Point();

                    if (pointsOnMesh.Count >= 2)
                    {
                        double worldSnapRadius = gp.GetWorldSnapRadiusAtPoint(pointsOnMesh[0]);
                        if (currentPoint.DistanceTo(pointsOnMesh[0]) <= worldSnapRadius)
                        {
                            // ¡SNAP! Añadimos este último punto y marcamos para cerrar.
                            pointsOnMesh.Add(currentPoint);
                            closeCurve = true;
                            break;
                        }
                    }
                    pointsOnMesh.Add(currentPoint);
                    doc.Views.Redraw();
                }
                else if (getResult == GetResult.Nothing) break;
                else return Result.Cancel;
            }

            // --- 4. VALIDAR Y CREAR LA CURVA ---
            int minPoints = closeCurve ? 3 : 2;
            if (pointsOnMesh.Count < minPoints)
            {
                RhinoApp.WriteLine($"Not enough points. At least {minPoints} points are required.");
                return Result.Nothing;
            }

            Curve interpolatedCurve;

            if (closeCurve)
            {
                // SOLUCIÓN DEFINITIVA: Para una curva periódica, añadimos el primer punto
                // de nuevo al final de la lista y usamos un estilo de nudo periódico.
                pointsOnMesh.Add(pointsOnMesh[0]);
                // ChordPeriodic da excelentes resultados basados en la distancia entre puntos.
                interpolatedCurve = Curve.CreateInterpolatedCurve(pointsOnMesh, 3, CurveKnotStyle.ChordPeriodic);
            }
            else
            {
                interpolatedCurve = Curve.CreateInterpolatedCurve(pointsOnMesh, 3);
            }

            if (interpolatedCurve == null)
            {
                RhinoApp.WriteLine("Failed to create interpolated curve.");
                return Result.Failure;
            }

            // --- 5. PROYECTAR Y AÑADIR AL DOCUMENTO ---
            // La proyección es el último paso.
            Curve projectedCurve = interpolatedCurve.PullToMesh(drawingMesh, doc.ModelAbsoluteTolerance);
            if (projectedCurve != null)
            {
                doc.Objects.AddCurve(projectedCurve);
                doc.Views.Redraw();
                RhinoApp.WriteLine("Curve successfully created on the surface.");
                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine("Failed to project the curve onto the surface.");
                return Result.Failure;
            }
        }
    }

    public class DynamicCurveOnMeshGetter : GetPoint
    {
        private readonly List<Point3d> _points;
        private bool _snappingEnabled = false;
        private Point3d _snapPoint;
        private double _snapRadiusPixels;

        public DynamicCurveOnMeshGetter(List<Point3d> existingPoints)
        {
            _points = existingPoints ?? new List<Point3d>();
        }

        public void EnableSnapping(Point3d snapPoint, double radiusInPixels)
        {
            _snappingEnabled = true;
            _snapPoint = snapPoint;
            _snapRadiusPixels = radiusInPixels;
        }

        public double GetWorldSnapRadiusAtPoint(Point3d point)
        {
            var view = View();
            if (view?.ActiveViewport == null) return 0;
            double pixelsPerUnit;
            if (view.ActiveViewport.GetWorldToScreenScale(point, out pixelsPerUnit))
            {
                return pixelsPerUnit > 0 ? _snapRadiusPixels / pixelsPerUnit : 0;
            }
            return 0;
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            base.OnDynamicDraw(e);

            if (_points.Count > 0)
                e.Display.DrawPoints(_points, PointStyle.Circle, 4, System.Drawing.Color.CornflowerBlue);

            var previewPoints = new List<Point3d>(_points) { e.CurrentPoint };

            if (_snappingEnabled && e.CurrentPoint.DistanceTo(_snapPoint) <= GetWorldSnapRadiusAtPoint(_snapPoint))
            {
                // Si estamos en la zona de snap, la previsualización se cierra.
                previewPoints.Add(_snapPoint);
                e.Display.DrawCircle(new Circle(_snapPoint, GetWorldSnapRadiusAtPoint(_snapPoint)), System.Drawing.Color.FromArgb(100, System.Drawing.Color.DeepSkyBlue), 4);

                if (previewPoints.Count >= 3)
                {
                    try
                    {
                        Curve previewCurve = Curve.CreateInterpolatedCurve(previewPoints, 3, CurveKnotStyle.ChordPeriodic);
                        if (previewCurve != null)
                            e.Display.DrawCurve(previewCurve, System.Drawing.Color.LawnGreen, 2); // Color verde para indicar cierre
                    }
                    catch (Exception) { /* Ignorar */ }
                }
            }
            else
            {
                // Previsualización de curva abierta normal.
                if (previewPoints.Count >= 2)
                {
                    try
                    {
                        Curve previewCurve = Curve.CreateInterpolatedCurve(previewPoints, 3);
                        if (previewCurve != null)
                            e.Display.DrawCurve(previewCurve, System.Drawing.Color.DarkOrange, 2);
                    }
                    catch (Exception) { /* Ignorar */ }
                }
            }
        }
    }
}