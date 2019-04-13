using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;

namespace Swift.Math
{
    /// <summary>
    /// 集合相关
    /// </summary>
    public class Geo
    {
        // 判断相交求交点
        public static bool LineSegementsIntersect(Fix64 px, Fix64 py, Fix64 p2x, Fix64 p2y,
            Fix64 qx, Fix64 qy, Fix64 q2x, Fix64 q2y, out Vec2 interPt)
        {
            return LineSegementsIntersect(new Vec2(px, py), new Vec2(p2x, p2y),
                new Vec2(qx, qy), new Vec2(q2x, q2y), out interPt);
        }

        public static bool LineSegementsIntersect(Vec2 p, Vec2 p2, Vec2 q, Vec2 q2,
            out Vec2 intersection)
        {
            intersection = new Vec2();

            var r = p2 - p;
            var s = q2 - q;
            var rxs = r.Cross(s);
            var qpxr = (q - p).Cross(r);

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (rxs == 0 && qpxr == 0)
                return false;

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (rxs == 0 && qpxr != 0)
                return false;

            // t = (q - p) x s / (r x s)
            var t = (q - p).Cross(s) / rxs;

            // u = (q - p) x r / (r x s)

            var u = (q - p).Cross(r) / rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (rxs != 0 && (0 <= t && t <= 1) && (0 <= u && u <= 1))
            {
                // We can calculate the intersection point using either t or u.
                intersection = p + t * r;

                // An intersection was found.
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }

        // 给定点是否在多边形内
        public static bool IsPointInPolygon(Vec2[] polygon, Vec2 point)
        {
            int polygonLength = polygon.Length, i = 0;
            bool inside = false;
            // x, y for tested point.
            Fix64 pointX = point.x, pointY = point.y;
            // start / end point for the current polygon segment.
            Fix64 startX, startY, endX, endY;
            Vec2 endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;
            while (i < polygonLength)
            {
                startX = endX; startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.x; endY = endPoint.y;
                //
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }

            return inside;
        }

        // 判定给定点在直线的哪一侧
        public static bool IsPointOnRightSide(Vec2 lineStart, Vec2 lineEnd, Vec2 pt)
        {
            Fix64 startX = lineStart.x;
            Fix64 startY = lineStart.y;
            Fix64 endX = lineEnd.x;
            Fix64 endY = lineEnd.y;
            Fix64 pointX = pt.x;
            Fix64 pointY = pt.y;
            return IsPointOnRightSide(startX, startY, endX, endY, pointX, pointY);
        }
        public static bool IsPointOnRightSide(Fix64 startX, Fix64 startY, Fix64 endX, Fix64 endY, Fix64 pointX, Fix64 pointY)
        {
            var dx = endX - startX;
            var dy = endY - startY;
            if (dx == 0 && dy == 0)
                return false; // for no mean

            var x0 = pointX - startX;
            var y0 = pointY - startY;
            var t = ((x0 * dx) + (y0 * dy)) / (dx * dx + dy * dy);
            if (t < 0 || t > 1)
                return false; // project out the line segement

            var arc1 = (new Vec2(dx, dy)).Arc();
            var arc2 = (new Vec2(pointX - startX, pointY - startY)).Arc();
            return (arc2 - arc1).RangeInPi() < 0;
        }
    }
}
