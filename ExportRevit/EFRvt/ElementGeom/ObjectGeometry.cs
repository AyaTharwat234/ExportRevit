using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
namespace EFRvt
{
    using GeoInstance = GeometryInstance;

    public class ObjectGeometry
    {
        public bool isValid = true;
        protected Solid m_solid;
        public List<FaceInfo> m_vFaces;
        public List<FaceInfo> m_hFaces;
        protected Line m_drivingLine;

        protected XYZ m_drivingVector;


        protected List<Line> m_edges = new List<Line>();
        protected List<LineAndRefrense> m_lineAndRefrenseList = new List<LineAndRefrense>();



        protected List<XYZ> m_points = new List<XYZ>();

        protected Transform m_transform;

        public XYZ drivingVector
        {
            get { return m_drivingVector; }
        }

        public List<XYZ> points
        {
            get { return m_points; }
        }

        //---
        public ObjectGeometry(FamilyInstance element, Options geoOptions, Line line)
        {
            // get the geometry element of the selected element
            GeometryElement geoElement = element.get_Geometry(geoOptions);
            IEnumerator<GeometryObject> Objects = geoElement.GetEnumerator();
            if (/*null == geoElement || */!Objects.MoveNext())
            {
                isValid = false;
                return;
                //  throw new Exception("Can't get the geometry of selected element.");
            }

            /* AnalyticalModel aModel = element.GetAnalyticalModel();
             if (aModel == null)
             {
                 isValid = false;
                 return;
                // throw new Exception("The selected FamilyInstance don't have AnalyticalModel.");
             }*/

            // AnalyticalModelSweptProfile swProfile = aModel.GetSweptProfile();
            /*SweptProfile swProfile = element.GetSweptProfile();
            if (swProfile == null || !(swProfile.GetDrivingCurve() is Line))
            {
                isValid = false;
                return;
                //  throw new Exception("The selected element driving curve is not a line.");
            }*/

            // get the driving path and vector of the beam or column
            // Line line = swProfile.GetDrivingCurve() as Line;
            if (null != line)
            {
                m_drivingLine = line;   // driving path
                m_drivingVector = GeomUtil.SubXYZ(line.GetEndPoint(1), line.GetEndPoint(0));
            }

            //get the geometry object
            Objects.Reset();
            //foreach (GeometryObject geoObject in geoElement.Objects)
            while (Objects.MoveNext())
            {
                GeometryObject geoObject = Objects.Current;

                //get the geometry instance which contain the geometry information
                GeoInstance instance = geoObject as GeoInstance;

                if (null != instance)
                {
                    //foreach (GeometryObject o in instance.SymbolGeometry.Objects)
                    IEnumerator<GeometryObject> Objects1 = instance.SymbolGeometry.GetEnumerator();
                    Objects1.Reset();
                    while (Objects1.MoveNext())
                    {
                        GeometryObject o = Objects1.Current;

                        // get the solid of beam of column
                        Solid solid = o as Solid;

                        // do some checks.
                        if (null == solid)
                        {
                            continue;
                        }
                        if (0 == solid.Faces.Size || 0 == solid.Edges.Size)
                        {
                            continue;
                        }

                        m_solid = solid;
                        //get the transform value of instance
                        m_transform = instance.Transform;

                        // Get the swept profile curves information
                        if (!GetSweptProfile(solid))
                        {
                            throw new Exception("Can't get the swept profile curves.");
                        }
                        break;
                    }
                    Objects1.Dispose();
                }
                /* else
                 {
                     if(geoObject is Solid)
                     {
                         #region
                         Solid solid = geoObject as Solid;
                         // do some checks.
                         if (null == solid)
                         {
                             continue;
                         }
                         if (0 == solid.Faces.Size || 0 == solid.Edges.Size)
                         {
                             continue;
                         }

                         m_solid = solid;
                         //get the transform value of instance
                       //  m_transform = geoObject.Transform;

                         // Get the swept profile curves information
                         if (!GetSweptProfile(solid))
                         {
                             throw new Exception("Can't get the swept profile curves.");
                         }
                         break;
                         #endregion
                     }
                 }*/


            }

            // do some checks about profile curves information
            if (null == m_edges)
            {
                isValid = false;
                return;
                // throw new Exception("Can't get the geometry edge information.");
            }
            if (4 != m_points.Count)
            {
                isValid = false;
                return;
                //  throw new Exception("The sample only work for rectangular beams or columns.");
            }

            /*foreach (LineAndRefrense li in m_lineAndRefrenseList)
            {
                AnalyticalModelSelector amSelector = new AnalyticalModelSelector(li.m_line);
                amSelector.CurveSelector = AnalyticalCurveSelector.StartPoint;
                li.m_edgeRefrense = aModel.GetReference(amSelector);
            }*/
            #region
            /* IList<Curve> activeCurveList = aModel.GetCurves(AnalyticalCurveType.ActiveCurves);
             foreach (Curve aCurve in activeCurveList)
             {
                 LineAndRefrense li = getLineAndRef(aCurve, m_lineAndRefrenseList);
                 if (li != null)
                 {
                     AnalyticalModelSelector amSelector = new AnalyticalModelSelector(aCurve);
                     amSelector.CurveSelector = AnalyticalCurveSelector.StartPoint;
                     Reference lineRef = aModel.GetReference(amSelector);
                     li.m_edgeRefrense = lineRef;
                 }
                 //  if (2 == referenceArray.Size)
                 //    break;
             }*/
            #endregion
            Objects.Dispose();

        }

        private LineAndRefrense getLineAndRef(Curve aCurve, List<LineAndRefrense> lineAndRefrenseList)
        {
            XYZ p1 = aCurve.GetEndPoint(0);
            XYZ p2 = aCurve.GetEndPoint(1);
            foreach (LineAndRefrense lr in lineAndRefrenseList)
            {
                XYZ pr = lr.m_line.GetEndPoint(0);
                XYZ pr2 = lr.m_line.GetEndPoint(1);
                if ((GeomUtil.IsEqual(p1, pr) || GeomUtil.IsEqual(p1, pr2)) && (GeomUtil.IsEqual(p2, pr) || GeomUtil.IsEqual(p2, pr2)))
                    return lr;
            }

            return null;
        }
        protected XYZ Transform(XYZ point)
        {
            // only invoke the TransformPoint() method.
            return GeomUtil.TransformPoint(point, m_transform);
        }


        /// <summary>
        /// Get the length of driving line
        /// </summary>
        /// <returns>the length of the driving line</returns>
        protected double GetDrivingLineLength()
        {
            return GeomUtil.GetLength(m_drivingVector);
        }

        /// <summary>
        /// Get two vectors, which indicate some edge direction which contain given point, 
        /// set the given point as the start point, the other end point of the edge as end
        /// </summary>
        /// <param name="point">a point of the swept profile</param>
        /// <returns>two vectors indicate edge direction</returns>
        protected List<XYZ> GetRelatedVectors(XYZ point)
        {
            // Initialize the return vector list.
            List<XYZ> vectors = new List<XYZ>();

            // Get all the edge which contain this point.
            // And get the vector from this point to another point
            foreach (Line line in m_edges)
            {
                if (GeomUtil.IsEqual(point, line.GetEndPoint(0)))
                {
                    XYZ vector = GeomUtil.SubXYZ(line.GetEndPoint(1), line.GetEndPoint(0));
                    vectors.Add(vector);
                }
                if (GeomUtil.IsEqual(point, line.GetEndPoint(1)))
                {
                    XYZ vector = GeomUtil.SubXYZ(line.GetEndPoint(0), line.GetEndPoint(1));
                    vectors.Add(vector);
                }
            }

            // only two vector(direction) should be found
            if (2 != vectors.Count)
            {
                throw new Exception("a point on swept profile should have only two direction.");
            }

            return vectors;
        }


        /// <summary>
        /// Offset the points of the swept profile to make the points inside swept profile
        /// </summary>
        /// <param name="offset">indicate how long to offset on two directions</param>
        /// <returns>the offset points</returns>
        protected List<XYZ> OffsetPoints(double offset)
        {
            // Initialize the offset point list.
            List<XYZ> offsetpoints = new List<XYZ>();

            // Get all points of the swept profile, and offset it in two related direction
            foreach (XYZ point in m_points)
            {
                // Get two related directions
                List<XYZ> directions = GetRelatedVectors(point);
                XYZ firstDir = directions[0];
                XYZ secondDir = directions[1];

                // offset the point in two direction
                XYZ movedPoint = GeomUtil.OffsetPoint(point, firstDir, offset);
                movedPoint = GeomUtil.OffsetPoint(movedPoint, secondDir, offset);

                // add the offset point into the array
                offsetpoints.Add(movedPoint);
            }

            return offsetpoints;
        }

        protected XYZ OffsetPointVertical(XYZ point, double hOffset, double vOffset)
        {
            // Initialize the offset point list.


            // Get all points of the swept profile, and offset it in two related direction

            // Get two related directions
            List<XYZ> directions = GetRelatedVectors(point);
            XYZ firstDir = directions[0];
            XYZ secondDir = directions[1];

            // offset the point in two direction
            XYZ movedPoint = GeomUtil.OffsetPoint(point, firstDir, hOffset);
            movedPoint = GeomUtil.OffsetPoint(movedPoint, secondDir, vOffset);

            // add the offset point into the array



            return movedPoint;
        }

        protected XYZ OffsetPointHorizontal(XYZ point, double offset)
        {
            // Initialize the offset point list.


            // Get all points of the swept profile, and offset it in two related direction

            // Get two related directions
            List<XYZ> directions = GetRelatedVectors(point);
            //XYZ firstDir = directions[0];//clean code
            XYZ secondDir = directions[1];

            // offset the point in two direction
            XYZ movedPoint = GeomUtil.OffsetPoint(point, secondDir, offset);
            //movedPoint = GeomUtil.OffsetPoint(movedPoint, secondDir, offset);

            // add the offset point into the array



            return movedPoint;
        }


        /// <summary>
        /// Find the inforamtion of the swept profile(face), 
        /// and store the points and edges of the profile(face) 
        /// </summary>
        /// <param name="solid">the solid reference</param>
        /// <returns>true if the swept profile can be gotten, otherwise false</returns>
        private bool GetSweptProfile(Solid solid)
        {
            // get the swept face
            Face sweptFace = GetSweptProfileFace(solid);
            // do some checks
            if (null == sweptFace || 1 != sweptFace.EdgeLoops.Size)
            {
                return false;
            }

            // get the points of the swept face
            foreach (XYZ point in sweptFace.Triangulate().Vertices)
            {
                m_points.Add(Transform(point));
            }

            // get the edges of the swept face
            m_edges = ChangeEdgeToLine(sweptFace.EdgeLoops.get_Item(0));
            m_lineAndRefrenseList = ChangeEdgeToLine2(sweptFace.EdgeLoops.get_Item(0));
            calcFaces(solid);
            // do some checks
            return (null != m_edges);
        }

        /// <summary>
        /// Get the swept profile(face) of the host object(family instance)
        /// </summary>
        /// <param name="solid">the solid reference</param>
        /// <returns>the swept profile</returns>
        private Face GetSweptProfileFace(Solid solid)
        {
            // Get a point on the swept profile from all points in solid
            XYZ refPoint = new XYZ();   // the point on swept profile
            foreach (Edge edge in solid.Edges)
            {
                List<XYZ> endPointsEdge = (List<XYZ>)edge.Tessellate();    //get end points of the edge
                if (2 != endPointsEdge.Count)                   // make sure all edges are lines
                {
                    throw new Exception("All edge should be line.");
                }

                // get two points of the edge. All points in solid should be transform first
                XYZ first = Transform(endPointsEdge[0]);  // start point of edge
                XYZ second = Transform(endPointsEdge[1]); // end point of edge

                // some edges should be parallelled with the driving line,
                // and the start point of that edge should be the wanted point
                XYZ edgeVector = GeomUtil.SubXYZ(second, first);
                if (GeomUtil.IsSameDirection(edgeVector, m_drivingVector))
                {
                    refPoint = first;
                    break;
                }
                if (GeomUtil.IsOppositeDirection(edgeVector, m_drivingVector))
                {
                    refPoint = second;
                    break;
                }
            }

            // Find swept profile(face)
            Face sweptFace = null;  // define the swept face
            foreach (Face face in solid.Faces)
            {
                if (null != sweptFace)
                {
                    break;
                }
                // the swept face should be perpendicular with the driving line
                if (!GeomUtil.IsVertical(face, m_drivingLine, m_transform, null))
                {
                    continue;
                }
                // use the gotted point to get the swept face
                foreach (XYZ point in face.Triangulate().Vertices)
                {
                    XYZ pnt = Transform(point); // all points in solid should be transform
                    if (GeomUtil.IsEqual(refPoint, pnt))
                    {
                        sweptFace = face;
                        break;
                    }
                }
            }

            return sweptFace;
        }

        /// <summary>
        /// Change the swept profile edges from EdgeArray type to line list
        /// </summary>
        /// <param name="edges">the swept profile edges</param>
        /// <returns>the line list which stores the swept profile edges</returns>
        private List<Line> ChangeEdgeToLine(EdgeArray edges)
        {
            // create the line list instance.
            List<Line> edgeLines = new List<Line>();

            // get each edge from swept profile,
            // and changed the geometry information in line list
            foreach (Edge edge in edges)
            {

                //get the two points of each edge
                List<XYZ> pointsEdge = (List<XYZ>)edge.Tessellate();
                XYZ first = Transform(pointsEdge[0]);
                XYZ second = Transform(pointsEdge[1]);

                // create new line and add them into line list
                edgeLines.Add(Line.CreateBound(first, second));
            }

            return edgeLines;
        }

        private List<LineAndRefrense> ChangeEdgeToLine2(EdgeArray edges)
        {
            // create the line list instance.
            List<LineAndRefrense> edgeLines = new List<LineAndRefrense>();

            // get each edge from swept profile,
            // and changed the geometry information in line list
            foreach (Edge edge in edges)
            {
                Face face = edge.GetFace(0) as PlanarFace;
                Reference faceRef = null;
                if (!GeomUtil.IsVertical(face, m_drivingLine, m_transform, null))
                {
                    // faceRef = face.Reference;
                }

                face = edge.GetFace(1) as PlanarFace;
                // Reference faceRef = null;
                if (!GeomUtil.IsVertical(face, m_drivingLine, m_transform, null))
                {
                    //  faceRef = new Reference(face as Element);
                }
                //get the two points of each edge
                List<XYZ> pointsEdge = (List<XYZ>)edge.Tessellate();
                XYZ first = Transform(pointsEdge[0]);
                XYZ second = Transform(pointsEdge[1]);

                LineAndRefrense obj = new LineAndRefrense(Line.CreateBound(first, second), faceRef);
                // create new line and add them into line list
                edgeLines.Add(obj);
            }

            return edgeLines;
        }


        internal FaceInfo getNearestFace(XYZ pc, XYZ loc, List<FaceInfo> _hFaces)
        {
            // List<XYZ> points;
            // List<FaceInfo> finfo = new List<FaceInfo>();
            GeomUtil.SubXYZ(pc, loc);
            Line.CreateBound(pc, loc);
            XYZ pj;
            int i = -1;
            FaceInfo nearestFace = null;
            int n = _hFaces.Count;
            for (i = 0; i < n; i++)
            {
                FaceInfo f = _hFaces[i];

                pj = GeomUtil.PointToLine(pc, f.points[1], f.points[2]);
                if (GeomUtil.LiesOnLine3f(pj, f.points[1], f.points[2], 0.01, 0.01))
                {
                    f.dis = GeomUtil.vectorLength3f(pc, pj, false);
                    // finfo.Add(f);
                    if (nearestFace == null)
                        nearestFace = f;
                    else
                    {
                        if (nearestFace.dis > f.dis)
                            nearestFace = f;
                    }
                }
            }

            return nearestFace;

        }
        private void calcFaces(Solid solid)
        {
            // create the line list instance.
            // List<LineAndRefrense> edgeLines = new List<LineAndRefrense>();

            if (m_vFaces == null)
                m_vFaces = new List<FaceInfo>();
            if (m_hFaces == null)
                m_hFaces = new List<FaceInfo>();
            // get each edge from swept profile,
            // and changed the geometry information in line list
            List<XYZ> pointsEdge;
            foreach (Face f in solid.Faces)
            {
                Face face = f as PlanarFace;
                // Reference faceRef = null;
                pointsEdge = new List<XYZ>();
                if (GeomUtil.IsVertical(face, m_drivingLine, m_transform, null, ref pointsEdge))
                {
                    // faceRef = face.Reference;
                    m_vFaces.Add(new FaceInfo(face, pointsEdge));
                }

                //face = edge.GetFace(1) as PlanarFace;
                // Reference faceRef = null;
                if (!GeomUtil.IsVertical(face, m_drivingLine, m_transform, null))
                {
                    m_hFaces.Add(new FaceInfo(face, pointsEdge));
                    //  faceRef = new Reference(face as Element);
                }
                //get the two points of each edge
                //List<XYZ> points = edge.Tessellate() as List<XYZ>;
                // Autodesk.Revit.DB.XYZ first = Transform(points[0]);
                //Autodesk.Revit.DB.XYZ second = Transform(points[1]);

                // LineAndRefrense obj = new LineAndRefrense(Line.CreateBound(first, second), faceRef);
                // create new line and add them into line list
                // edgeLines.Add(obj);
            }

            //return edgeLines;
        }
        internal List<LineAndRefrense> getLines(bool isVertical)
        {
            List<LineAndRefrense> lines = new List<LineAndRefrense>();
            try
            {

                foreach (LineAndRefrense li in m_lineAndRefrenseList)
                {

                    if (GeomUtil.IsVertical(li.m_line.GetEndPoint(0), li.m_line.GetEndPoint(1)))
                    {
                        if (isVertical)
                            lines.Add(li);
                    }
                    else
                    {
                        if (isVertical == false)
                            lines.Add(li);
                    }


                }


                return lines;
            }
            catch
            {
                return lines;
            }
        }


    }


    public class FaceInfo
    {
        public Face face;
        public List<XYZ> points;
        public float dis;

        public FaceInfo(Face _face, List<XYZ> _points)
        {
            face = _face;
            points = new List<XYZ>(_points);
        }


    }

    public class LineAndRefrense
    {
        public Line m_line;
        public Reference m_edgeRefrense;

        public LineAndRefrense(Line line, Reference edgeRefrense)
        {
            m_line = line;
            m_edgeRefrense = edgeRefrense;
        }
    }
}
