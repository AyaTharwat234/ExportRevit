using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace EFRvt
{

    using Element = Element;


    /// <summary>
    /// The class which give the base geometry operation, it is a static class.
    /// </summary>
    public static class GeomUtil
    {
        // Private members
        const double Precision = 0.00001;    //precision when judge whether two doubles are equal

        const double _mmToFeet = 0.0032808399;
        const double _meterToFeet = 3.280839895013123;
        const double _feetToMeter = 0.3048;
        public static double Feet_to_Inch = 12;
        public static double InchToFeet(double inch)
        {
            return inch / Feet_to_Inch;
        }
        public static double MmToFeet(double mmValue)
        {
            return mmValue * _mmToFeet;
        }

        public static double meterToFeet(double mValue)
        {
            return mValue * _meterToFeet;
        }

        public static double FeetToMeter(double feetValue)
        {
            return feetValue * _feetToMeter;
        }

        public static double FeetToMm(double feetValue)
        {
            return feetValue / _mmToFeet;
        }

        public static double FeetToInch(double feet)
        {
            return feet * Feet_to_Inch;
        }


        public static double FeetToCm(double feetValue)
        {
            return feetValue / (_mmToFeet * 10);
        }
        /// <summary>
        /// Judge whether the two double data are equal
        /// </summary>
        /// <param name="d1">The first double data</param>
        /// <param name="d2">The second double data</param>
        /// <returns>true if two double data is equal, otherwise false</returns>
        public static bool IsEqual(double d1, double d2)
        {
            d1 = Math.Round(d1, 3);
            d2 = Math.Round(d2, 3);
            //get the absolute value;
            double diff = Math.Abs(d1 - d2);
            return diff < Precision;
        }

        public static bool IsEqual(double d1, double d2, double tol)
        {
            d1 = Math.Round(d1, 3);
            d2 = Math.Round(d2, 3);
            //get the absolute value;
            double diff = Math.Abs(d1 - d2);
            return diff < tol;
        }
        /// <summary>
        /// Judge whether the two Autodesk.Revit.DB.XYZ point are equal
        /// </summary>
        /// <param name="first">The first Autodesk.Revit.DB.XYZ point</param>
        /// <param name="second">The second Autodesk.Revit.DB.XYZ point</param>
        /// <returns>true if two Autodesk.Revit.DB.XYZ point is equal, otherwise false</returns>
        public static bool IsEqual(XYZ first, XYZ second, bool useZ = true, float tol = 0.1f)
        {
            bool flag = IsEqual(first.X, second.X, tol);
            flag = flag && IsEqual(first.Y, second.Y, tol);
            if (useZ)
                flag = flag && IsEqual(first.Z, second.Z, tol);
            return flag;
        }


        /// <returns>return true when line is perpendicular to the face</returns>


        /// <summary>
        /// Judge whether the line is perpendicular to the face
        /// </summary>
        ///// <param name="face">the face reference</param>
        ///// <param name="line">the line reference</param>
        ///// <param name="faceTrans">the transform for the face</param>
        ///// <param name="lineTrans">the transform for the line</param>
        /// <returns>true if line is perpendicular to the face, otherwise false</returns>
        //public static bool IsVertical(Face face, Line line,
        //                                        Transform faceTrans, Transform lineTrans)
        //{
        //    //get points which the face contains
        //    List<XYZ> points = face.Triangulate().Vertices as List<XYZ>;
        //    if (3 > points.Count)    // face's point number should be above 2
        //    {
        //        return false;
        //    }

        //    // get three points from the face points
        //    Autodesk.Revit.DB.XYZ first = points[0];
        //    Autodesk.Revit.DB.XYZ second = points[1];
        //    Autodesk.Revit.DB.XYZ third = points[2];

        //    // get start and end point of line
        //    Autodesk.Revit.DB.XYZ lineStart = line.GetEndPoint(0);
        //    Autodesk.Revit.DB.XYZ lineEnd = line.GetEndPoint(1);

        //    // transForm the three points if necessary
        //    if (null != faceTrans)
        //    {
        //        first = TransformPoint(first, faceTrans);
        //        second = TransformPoint(second, faceTrans);
        //        third = TransformPoint(third, faceTrans);
        //    }

        //    // transform the start and end points if necessary
        //    if (null != lineTrans)
        //    {
        //        lineStart = TransformPoint(lineStart, lineTrans);
        //        lineEnd = TransformPoint(lineEnd, lineTrans);
        //    }

        //    // form two vectors from the face and a vector stand for the line
        //    // Use SubXYZ() method to get the vectors
        //    Autodesk.Revit.DB.XYZ vector1 = SubXYZ(first, second);    // first vector of face
        //    Autodesk.Revit.DB.XYZ vector2 = SubXYZ(first, third);     // second vector of face
        //    Autodesk.Revit.DB.XYZ vector3 = SubXYZ(lineStart, lineEnd);   // line vector

        //    // get two dot products of the face vectors and line vector
        //    double result1 = DotMatrix(vector1, vector3);
        //    double result2 = DotMatrix(vector2, vector3);

        //    // if two dot products are all zero, the line is perpendicular to the face
        //    return (IsEqual(result1, 0) && IsEqual(result2, 0));
        //}

        /// <summary>
        /// judge whether the two vectors have the same direction
        /// </summary>
        /// <param name="firstVec">the first vector</param>
        /// <param name="secondVec">the second vector</param>
        /// <returns>true if the two vector is in same direction, otherwise false</returns>
        public static bool IsSameDirection(XYZ firstVec, XYZ secondVec)
        {
            // get the unit vector for two vectors
            XYZ first = UnitVector(firstVec);
            XYZ second = UnitVector(secondVec);

            // if the dot product of two unit vectors is equal to 1, return true
            double dot = DotMatrix(first, second);
            return (IsEqual(dot, 1));
        }

        /// <summary>
        /// Judge whether the two vectors have the opposite direction
        /// </summary>
        /// <param name="firstVec">the first vector</param>
        /// <param name="secondVec">the second vector</param>
        /// <returns>true if the two vector is in opposite direction, otherwise false</returns>
        public static bool IsOppositeDirection(XYZ firstVec, XYZ secondVec)
        {
            // get the unit vector for two vectors
            XYZ first = UnitVector(firstVec);
            XYZ second = UnitVector(secondVec);

            // if the dot product of two unit vectors is equal to -1, return true
            double dot = DotMatrix(first, second);
            return (IsEqual(dot, -1));
        }

        /// <summary>
        /// multiplication cross of two Autodesk.Revit.DB.XYZ as Matrix
        /// </summary>
        /// <param name="p1">The first XYZ</param>
        /// <param name="p2">The second XYZ</param>
        /// <returns>the normal vector of the face which first and secend vector lie on</returns>
        public static XYZ CrossMatrix(XYZ p1, XYZ p2)
        {
            //get the coordinate of the XYZ
            double u1 = p1.X;
            double u2 = p1.Y;
            double u3 = p1.Z;

            double v1 = p2.X;
            double v2 = p2.Y;
            double v3 = p2.Z;

            double x = v3 * u2 - v2 * u3;
            double y = v1 * u3 - v3 * u1;
            double z = v2 * u1 - v1 * u2;

            return new XYZ(x, y, z);
        }




        /// <summary>
        /// Set the vector into unit length
        /// </summary>
        /// <param name="vector">the input vector</param>
        /// <returns>the vector in unit length</returns>
        public static XYZ UnitVector(XYZ vector)
        {
            // calculate the distance from grid origin to the XYZ
            double length = GetLength(vector);
            if (length == 0)
                length = 1;
            // changed the vector into the unit length
            double x = vector.X / length;
            double y = vector.Y / length;
            double z = vector.Z / length;
            return new XYZ(x, y, z);
        }

        /// <summary>
        /// calculate the distance from grid origin to the XYZ(vector length)
        /// </summary>
        /// <param name="vector">the input vector</param>
        /// <returns>the length of the vector</returns>
        public static double GetLength(XYZ vector)
        {
            double x = vector.X;
            double y = vector.Y;
            double z = vector.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static double GetLength(XYZ p1, XYZ p2)
        {
            XYZ vector = SubXYZ(p2, p1);
            double x = vector.X;
            double y = vector.Y;
            double z = vector.Z;
            return Math.Sqrt(x * x + y * y + z * z);
        }

        /// <summary>
        /// Subtraction of two points(or vectors), get a new vector 
        /// </summary>
        /// <param name="p1">the first point(vector)</param>
        /// <param name="p2">the second point(vector)</param>
        /// <returns>return a new vector from point p2 to p1</returns>
        public static XYZ SubXYZ(XYZ p1, XYZ p2)
        {
            double x = p1.X - p2.X;
            double y = p1.Y - p2.Y;
            double z = p1.Z - p2.Z;

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Add of two points(or vectors), get a new point(vector) 
        /// </summary>
        /// <param name="p1">the first point(vector)</param>
        /// <param name="p2">the first point(vector)</param>
        /// <returns>a new vector(point)</returns>
        public static XYZ AddXYZ(XYZ p1, XYZ p2)
        {
            double x = p1.X + p2.X;
            double y = p1.Y + p2.Y;
            double z = p1.Z + p2.Z;

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Multiply a verctor with a number
        /// </summary>
        /// <param name="vector">a vector</param>
        /// <param name="rate">the rate number</param>
        /// <returns></returns>
        public static XYZ MultiplyVector(XYZ vector, double rate)
        {
            double x = vector.X * rate;
            double y = vector.Y * rate;
            double z = vector.Z * rate;

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Transform old coordinate system in the new coordinate system 
        /// </summary>
        /// <param name="point">the Autodesk.Revit.DB.XYZ which need to be transformed</param>
        /// <param name="transform">the value of the coordinate system to be transformed</param>
        /// <returns>the new Autodesk.Revit.DB.XYZ which has been transformed</returns>
        public static XYZ TransformPoint(XYZ point, Transform transform)
        {
            //get the coordinate value in X, Y, Z axis
            double x = point.X;
            double y = point.Y;
            double z = point.Z;

            //transform basis of the old coordinate system in the new coordinate system
            XYZ b0 = transform.get_Basis(0);
            XYZ b1 = transform.get_Basis(1);
            XYZ b2 = transform.get_Basis(2);
            XYZ origin = transform.Origin;

            //transform the origin of the old coordinate system in the new coordinate system
            double xTemp = x * b0.X + y * b1.X + z * b2.X + origin.X;
            double yTemp = x * b0.Y + y * b1.Y + z * b2.Y + origin.Y;
            double zTemp = x * b0.Z + y * b1.Z + z * b2.Z + origin.Z;

            return new XYZ(xTemp, yTemp, zTemp);
        }

        /// <summary>
        /// Move a point a give offset along a given direction
        /// </summary>
        /// <param name="point">the point need to move</param>
        /// <param name="direction">the direction the point move to</param>
        /// <param name="offset">indicate how long to move</param>
        /// <returns>the moved point</returns>
        public static XYZ OffsetPoint(XYZ point, XYZ direction, double offset)
        {
            XYZ directUnit = UnitVector(direction);
            XYZ offsetVect = MultiplyVector(directUnit, offset);
            return AddXYZ(point, offsetVect);
        }

        /// <summary>
        /// get the orient of hook accroding to curve direction, rebar normal and hook direction
        /// </summary>
        /// <param name="curveVec">the curve direction</param>
        /// <param name="normal">rebar normal direction</param>
        /// <param name="hookVec">the hook direction</param>
        /// <returns>the orient of the hook</returns>
        public static RebarHookOrientation GetHookOrient(XYZ curveVec, XYZ normal, XYZ hookVec)
        {
            XYZ tempVec = normal;

            for (int i = 0; i < 4; i++)
            {
                tempVec = CrossMatrix(tempVec, curveVec);
                if (IsSameDirection(tempVec, hookVec))
                {
                    if (i == 0)
                    {
                        return RebarHookOrientation.Right;
                    }
                    else if (i == 2)
                    {
                        return RebarHookOrientation.Left;
                    }
                }
            }

            throw new Exception("Can't find the hook orient according to hook direction.");
        }

        /// <summary>
        /// Judge the vector is in right or left direction
        /// </summary>
        /// <param name="normal">The unit vector need to be judged its direction</param>
        /// <returns>if in right dircetion return true, otherwise return false</returns>
        public static bool IsInRightDir(XYZ normal)
        {
            double eps = 1.0e-8;
            if (Math.Abs(normal.X) <= eps)
            {
                if (normal.Y > 0) return false;
                else return true;
            }
            if (normal.X > 0) return true;
            if (normal.X < 0) return false;
            return true;
        }

        /// <summary>
        /// dot product of two Autodesk.Revit.DB.XYZ as Matrix
        /// </summary>
        /// <param name="p1">The first XYZ</param>
        /// <param name="p2">The second XYZ</param>
        /// <returns>the cosine value of the angle between vector p1 an p2</returns>
        private static double DotMatrix(XYZ p1, XYZ p2)
        {
            //get the coordinate of the Autodesk.Revit.DB.XYZ 
            double v1 = p1.X;
            double v2 = p1.Y;
            double v3 = p1.Z;

            double u1 = p2.X;
            double u2 = p2.Y;
            double u3 = p2.Z;

            return v1 * u1 + v2 * u2 + v3 * u3;
        }

        public static bool IsPointInList(List<XYZ> points, XYZ p)
        {
            if (points != null)
            {
                if (points.Count == 0)
                    return false;
                foreach (XYZ x in points)
                {
                    if (IsEqual(x, p))
                        return true;
                }
                return false;
            }
            return false;
        }

        //----
        internal static Parameter GetparameterBy(Element el, BuiltInParameter Bparameter)
        {

            return el.get_Parameter(Bparameter);
        }

        internal static bool GetParameterValue(Parameter param, out double val)
        {

            val = 0;
            if (param.StorageType == StorageType.Double)
            {
                val = param.AsDouble();
                return true;
            }
            else if (param.StorageType == StorageType.Integer)
            {
                val = param.AsInteger();
                return true;
            }
            else
            {
                return false;
            }

        }

        internal static XYZ MidPoint(XYZ ps, XYZ pe)
        {
            return new XYZ((ps.X + pe.X) * 0.5, (ps.Y + pe.Y) * 0.5, (ps.Z + pe.Z) * 0.5);
        }

        internal static float getMinZ(List<XYZ> points)
        {
            List<double> zList = new List<double>();
            foreach (XYZ p in points)
                zList.Add(p.Z);
            zList.Sort();
            return (float)zList[0];
        }

        public static double calcAngle(XYZ p1, XYZ p2)
        {
            double angle;
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if (dx == 0.0f)
            {
                angle = 90.0f;
                return angle;
            }
            angle = Atanxy(dy, dx);
            angle = (float)(180.0 * angle / Math.PI);
            return angle;
        }


        public static double Atanxy(double y, double x)
        {
            double x1 = x, y1 = y;

            if ((x1 == 0.0) && (y1 == 0.0)) return 0.0f;
            if (x1 == 0.0) return (float)(y1 > 0.0 ? 0.5 * Math.PI : 1.5 * Math.PI);
            if (y1 == 0.0) return (float)(x1 > 0.0 ? 0.0 : Math.PI);

            if ((x1 > 0.0) && (y1 > 0.0)) return (float)(Math.Atan(y1 / x1));                              // First  Qaudrant.
            if ((x1 < 0.0) && (y1 > 0.0)) return (float)(Math.PI - Math.Atan(y1 / Math.Abs(x1)));          // Second Qaudrant.
            if ((x1 < 0.0) && (y1 < 0.0)) return (float)(Math.PI + Math.Atan(y1 / x1));                    // Third  Qaudrant.
            if ((x1 > 0.0) && (y1 < 0.0)) return (float)(2.0 * Math.PI - Math.Atan(Math.Abs(y1) / x1));    // Fourth Qaudrant.

            return 0.0f;
        }
        //---

        public static bool IsVertical(XYZ startPoint, XYZ endPoint)
        {
            double dx = endPoint.X - startPoint.X;
            return (Math.Abs(dx) < Precision);
        }

        public static bool IsHorizontal(XYZ startPoint, XYZ endPoint)
        {
            double dy = endPoint.Y - startPoint.Y;
            return (Math.Abs(dy) < Precision);
        }

        /// <summary>
        /// Judge whether the line is perpendicular to the face
        /// </summary>
        /// <param name="face">the face reference</param>
        /// <param name="line">the line reference</param>
        /// <param name="faceTrans">the transform for the face</param>
        /// <param name="lineTrans">the transform for the line</param>
        /// <returns>true if line is perpendicular to the face, otherwise false</returns>
        public static bool IsVertical(Face face, Line line,
                                                Transform faceTrans, Transform lineTrans)
        {
            //get points which the face contains
            List<XYZ> points = (List<XYZ>)face.Triangulate().Vertices;
            if (3 > points.Count)    // face's point number should be above 2
            {
                return false;
            }

            // get three points from the face points
            XYZ first = points[0];
            XYZ second = points[1];
            XYZ third = points[2];

            // get start and end point of line
            XYZ lineStart = line.GetEndPoint(0);
            XYZ lineEnd = line.GetEndPoint(1);

            // transForm the three points if necessary
            if (null != faceTrans)
            {
                first = TransformPoint(first, faceTrans);
                second = TransformPoint(second, faceTrans);
                third = TransformPoint(third, faceTrans);
            }

            // transform the start and end points if necessary
            if (null != lineTrans)
            {
                lineStart = TransformPoint(lineStart, lineTrans);
                lineEnd = TransformPoint(lineEnd, lineTrans);
            }

            // form two vectors from the face and a vector stand for the line
            // Use SubXYZ() method to get the vectors
            XYZ vector1 = SubXYZ(first, second);    // first vector of face
            XYZ vector2 = SubXYZ(first, third);     // second vector of face
            XYZ vector3 = SubXYZ(lineStart, lineEnd);   // line vector

            // get two dot products of the face vectors and line vector
            double result1 = DotMatrix(vector1, vector3);
            double result2 = DotMatrix(vector2, vector3);

            // if two dot products are all zero, the line is perpendicular to the face
            return (IsEqual(result1, 0) && IsEqual(result2, 0));
        }

        public static bool IsVertical(Face face, Line line,
                                               Transform faceTrans, Transform lineTrans, ref List<XYZ> transPoints)
        {
            //get points which the face contains
            List<XYZ> points = (List<XYZ>)face.Triangulate().Vertices;
            if (3 > points.Count)    // face's point number should be above 2
            {
                return false;
            }

            // get three points from the face points
            XYZ first = points[0];
            XYZ second = points[1];
            XYZ third = points[2];
            XYZ forth = points[3];
            // get start and end point of line
            XYZ lineStart = line.GetEndPoint(0);
            XYZ lineEnd = line.GetEndPoint(1);

            // transForm the three points if necessary
            if (null != faceTrans)
            {
                first = TransformPoint(first, faceTrans);
                second = TransformPoint(second, faceTrans);
                third = TransformPoint(third, faceTrans);
                forth = TransformPoint(forth, faceTrans);
            }

            transPoints.Add(first);
            transPoints.Add(second);
            transPoints.Add(third);
            transPoints.Add(forth);
            // transform the start and end points if necessary
            if (null != lineTrans)
            {
                lineStart = TransformPoint(lineStart, lineTrans);
                lineEnd = TransformPoint(lineEnd, lineTrans);
            }

            // form two vectors from the face and a vector stand for the line
            // Use SubXYZ() method to get the vectors
            XYZ vector1 = SubXYZ(first, second);    // first vector of face
            XYZ vector2 = SubXYZ(first, third);     // second vector of face
            XYZ vector3 = SubXYZ(lineStart, lineEnd);   // line vector

            // get two dot products of the face vectors and line vector
            double result1 = DotMatrix(vector1, vector3);
            double result2 = DotMatrix(vector2, vector3);

            // if two dot products are all zero, the line is perpendicular to the face
            return (IsEqual(result1, 0) && IsEqual(result2, 0));
        }

        //---
        //---
        public static XYZ getPointOnLine(XYZ start, XYZ end, double distanceFromStart, bool useZ = false)
        {
            double dz = end.Z - start.Z;

            if (!useZ)
                dz = 0.0;

            XYZ vector = new XYZ(end.X - start.X, end.Y - start.Y, dz);

            var unitVector = UnitVector(vector);
            double sx = start.X + unitVector.X * distanceFromStart;
            double sy = start.Y + unitVector.Y * distanceFromStart;
            double sz = start.Z + unitVector.Z * distanceFromStart;

            if (!useZ)
                sz = start.Z;

            var returnedPoint = new XYZ(sx, sy, sz);
            return returnedPoint;
        }
        internal static bool isDiagonal(XYZ p1, XYZ p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if (Math.Abs(dx) > 0 || Math.Abs(dy) > 0)
            {
                return true;
            }
            return false;
        }

        public static XYZ PointToLine(XYZ point, XYZ start, XYZ end)
        {
            // point.z = start.z = end.z = 0;

            double y0, z0;
            var x1 = point.X;
            var y1 = point.Y;
            var z1 = point.Z;

            var x2 = start.X;
            var y2 = start.Y;
            var z2 = start.Z;

            var x3 = end.X;
            var y3 = end.Y;
            var z3 = end.Z;

            var x0 = y0 = z0 = 0.0f;

            XYZ rePoint = start;

            try
            {
                //bool result = (PointToLineIntersection(x1, y1, z1, x2, y2, z2, x3, y3, z3, out x0, out y0, out z0) > 0);
                PointToLineIntersection(x1, y1, z1, x2, y2, z2, x3, y3, z3, out x0, out y0, out z0);
                rePoint = new XYZ(x0, y0, z0);
                return rePoint;
            }
            catch
            {
                return rePoint;
            }
        }


        public static int PointToLineIntersection(double x1, double y1, double z1, double x2, double y2, double z2,
                                                double x3, double y3, double z3, out double x0, out double y0, out double z0)
        {
            x0 = x2;
            y0 = y2;
            z0 = z2;

            var dx1 = x1 - x2;
            var dy1 = y1 - y2;
            var dz1 = z1 - z2;

            var dx2 = x3 - x2;
            var dy2 = y3 - y2;
            var dz2 = z3 - z2;
            var L2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2 + dz2 * dz2);

            if (L2 == 0.0) return 1;

            var dp = dx1 * dx2 + dy1 * dy2 + dz1 * dz2;
            var d = dp / L2;
            var t = d / L2;

            x0 = (float)(x2 + t * dx2);
            y0 = (float)(y2 + t * dy2);
            z0 = (float)(z2 + t * dz2);

            return 1;

        }



        public static bool LiesOnLine3f(XYZ point, XYZ start, XYZ end, double distance, double endEpsilon)
        {
            //point.z = start.z = end.z = 0.0f;
            double dist = 0.0f;

            double len = vectorLength3f(start, end);
            if (len == 0)
            {
                return IsEqualPoint3f_xy(point, start, endEpsilon) || IsEqualPoint3f_xy(point, end, endEpsilon);
            }

            if (PointLineDistance3d(point, start, end, endEpsilon, ref dist) == 0)
                return false;
            else if (dist < distance || Math.Abs(dist - distance) <= Precision)
                return true;
            else
                return false;
        }

        public static int PointLineDistance3d(XYZ P0, XYZ P1, XYZ P2, double epsilon, ref double distance)
        {
            //int r = 3;//round//Clean Up

            double dif = ((P0.X - P1.X) * (P2.X - P1.X)) +
                ((P0.Y - P1.Y) * (P2.Y - P1.Y));
            //+((P0.ZRound(3) - P1.ZRound(r)) * (P2.ZRound(r) - P1.ZRound(r)));

            distance = 0;
            double LineMag = vectorLength3f(P2, P1);

            if (LineMag == 0)
                return 0;

            var U = dif / (LineMag * LineMag);

            if (U < 0)
            {
                if (Math.Abs(U) <= epsilon)
                    U = 0;
            }
            else if (U > 1)
            {
                if (Math.Abs(U - 1) <= epsilon)
                    U = 1;
            }

            if (U < 0 || U > 1)
                return 0;   // closest point does not fall within the line segment

            var xi = P1.X + U * (P2.X - P1.X);
            var yi = P1.Y + U * (P2.Y - P1.Y);
            var zi = P1.Z + U * (P2.Z - P1.Z);
            XYZ pnew = new XYZ(xi, yi, zi);
            XYZ p0New = new XYZ(P0.X, P0.Y, P0.Z);
            distance = vectorLength3f(p0New, pnew);

            return 1;
        }
        public static bool IsEqualPoint3f_xy(XYZ p1, XYZ p2, double tolerance)
        {
            double dx = Math.Abs(p1.X - p2.X);
            double dy = Math.Abs(p1.Y - p2.Y);
            return ((dx <= tolerance) && (dy <= tolerance));
        }

        public static float vectorLength3f(XYZ p1, XYZ p2, bool useZ = true)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            double dz = p2.Z - p1.Z;
            if (useZ == false)
                dz = 0;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            // return (float)MathLib.vecLength(dx, dy, dz);


        }


        public static bool isClosedPolygon(List<XYZ> Points)
        {
            try
            {
                if (Points != null && Points.Count > 1)
                {
                    if (IsEqual(Points[0], Points[Points.Count - 1]))
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Elibre.Net.Debug.ErrorHandler.ReportException(ex);
                return false;
            }
        }


        public static bool isHorizontal(PlanarFace pf)
        {
            return isVertical(pf.FaceNormal);
        }
        public static bool isZero(double a, double tolerance)
        {
            return tolerance > Math.Abs(a);
        }

        public static bool isZero(double a)
        {
            return isZero(a, Precision);
        }
        public static bool isVertical(XYZ v)
        {
            return isZero(v.X) && isZero(v.Y);
        }
        static bool getBoundries(ref List<XYZ> polygon, ref List<List<XYZ>> OpenPolygons, Solid solid, bool topface = true)
        {


            PlanarFace topst = null;
            FaceArray faces = solid.Faces;

            foreach (Face f in faces)
            {
                PlanarFace pf = f as PlanarFace;

                if ((null != pf) && (isHorizontal(pf)))
                {
                    if ((null == topst) || ((topface ? (pf.Origin.Z > topst.Origin.Z) : (pf.Origin.Z < topst.Origin.Z))))
                    {
                        topst = pf;
                    }
                }
            }

            if (null != topst)
            {
                getBoundFromPlanerFace(ref polygon, ref OpenPolygons, topst);
            }

            return (null != topst);
        }

        public static void getBoundFromPlanerFace(ref List<XYZ> polygon, ref List<List<XYZ>> OpenPolygons, PlanarFace planarFace)
        {
            polygon = new List<XYZ>();
            OpenPolygons = new List<List<XYZ>>();
            double ofsset = 0.0;
            List<List<XYZ>> tmpPolygons = new List<List<XYZ>>();
            XYZ /*p,*/ q = XYZ.Zero;
            int i, n, k = -1;
            int mainIndex = -1;
            double a, maxArea = 0.0;
            EdgeArrayArray loops = planarFace.EdgeLoops;

            foreach (EdgeArray loop in loops)
            {
                k++;

                List<XYZ> vertices = new List<XYZ>();

                foreach (Edge e in loop)
                {
                    IList<XYZ> points = e.Tessellate();

                    n = points.Count;
                    q = points[n - 1];

                    for (i = 0; i < n - 1; ++i)
                    {
                        XYZ v = points[i];
                        v -= ofsset * XYZ.BasisZ;
                        vertices.Add(v);
                    }
                }

                q -= ofsset * XYZ.BasisZ;
                List<UV> flat_polygons = Flatten(vertices);
                a = getSignedPolygonArea(flat_polygons);

                if (Math.Abs(maxArea) < Math.Abs(a))
                {
                    maxArea = a;
                    mainIndex = k;
                }
                tmpPolygons.Add(vertices);
            }

            if (mainIndex > -1)
                polygon = tmpPolygons[mainIndex];

            n = tmpPolygons.Count;

            for (i = 0; i < n; i++)
            {
                if (i != mainIndex)
                    OpenPolygons.Add(tmpPolygons[i]);
            }

        }

        public static double getSignedPolygonArea(List<UV> p)
        {
            int n = p.Count;
            double sum = p[0].U * (p[1].V - p[n - 1].V); // loop at beginning

            for (int i = 1; i < n - 1; ++i)
            {
                sum += p[i].U * (p[i + 1].V - p[i - 1].V);
            }

            sum += p[n - 1].U * (p[0].V - p[n - 2].V); // loop at end
            return 0.5 * sum;
        }
        static UV Flatten(XYZ point)
        {
            return new UV(point.X, point.Y);
        }
        public static List<UV> Flatten(List<XYZ> polygon)
        {
            //double z = polygon[0].Z;
            List<UV> a = new List<UV>(polygon.Count);

            foreach (XYZ p in polygon)
            {
                a.Add(Flatten(p));
            }

            return a;
        }




    }

    public class PointAndT
    {
        public XYZ Point;
        public double tVal = 0d;

        public PointAndT(XYZ pt, double t)
        {
            Point = pt;
            tVal = t;
        }
    }
}
