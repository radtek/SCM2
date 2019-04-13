using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;
using Swift.AStar;

namespace SCM
{
    /// <summary>
    /// 地图格子，表达占位信息
    /// </summary>
    public class MapGrid<T>
    {
        T[,] grid = null;
        int w;
        int h;

        int MinR { get { return w > h ? h : w; } }
        int MaxR { get { return w > h ? w : h; } }

        public MapGrid(int width, int height)
        {
            w = width;
            h = height;
            grid = new T[w, h];

            BuildPathFinder();
        }

        public T this[int x, int y]
        {
            get { return grid[x, y]; }
            set { grid[x, y] = value; }
        }

        // 寻找离指定位置最近的一个可容纳指定半径单位的点，从离中心 fromR 的距离开始找
        public bool FindNearestSpareSpace(int cx, int cy, int r, int fromR, out int ox, out int oy)
        {
            var found = false;
            var _ox = 0;
            var _oy = 0;
            FC.SquareFor2(cx, cy, 0, MaxR, (x, y) =>
            {
                _ox = x;
                _oy = y;
                found = x >= 0 && x < w
                    && y >= 0 && y < h
                    && CheckSpareSpace(x, y, r);
            }, FC.SquareForSeq.Default, () => !found);

            ox = _ox;
            oy = _oy;
            return found;
        }

        // 检查指定区域是否被占用
        public bool CheckSpareSpace(int cx, int cy, int r, T[] asEmptyValue = null)
        {
            var free = true;
            ForArea(cx, cy, r, (x, y) =>
            {
                free = x >= 0 && x < w
                        && y >= 0 && y < h
                        && (object.Equals(grid[x, y], default(T)) || asEmptyValue.FirstIndexOf(grid[x, y]) >= 0);
            }, () => free);

            return free;
        }

        // 将指定区域设置为指定值
        public void SetBlock(int cx, int cy, int r, T v)
        {
            SetBlock(cx, cy, r, v, default(T));
        }

        // 将指定区域设置为空
        public void UnsetBlock(int cx, int cy, int r, T checkValue)
        {
            SetBlock(cx, cy, r, default(T), checkValue);
        }

        void SetBlock(int cx, int cy, int r, T v, T checkValue)
        {
            ForArea(cx, cy, r, (x, y) =>
            {
                if (x < 0 || x >= w
                    || y < 0 || y >= h
                    || !object.Equals(grid[x, y], checkValue))
                {
                    var msg = "set grid value conflicted at: " + x + ", " + y;
                    if (x >= 0 && x < w && y >= 0 && y < h)
                        msg += " (" + (object.Equals(grid[x, y], default(T)) ? "*" : grid[x, y].ToString()) + " => " +
                            (object.Equals(v, default(T)) ? "*" : v.ToString()) + " : checkValue = " + (object.Equals(checkValue, default(T)) ? "*" : checkValue.ToString()) + ")";

                    throw new Exception(msg);
                }

                grid[x, y] = v;
            });
        }

        // 遍历指定区域
        void ForArea(int cx, int cy, int r, Action<int, int> fun, Func<bool> continueCondition = null)
        {
            if (r <= 0)
                return;

            r--;
            FC.For2(cx - r, cx + r + 1, cy - r, cy + r + 1, fun, continueCondition);
        }

        public void Clear()
        {
            grid = new T[w, h];
        }

        #region 寻路相关

        class PathNode : IPathNode<KeyValuePair<int, T[]>>
        {
            public Vec2 Pos { get; private set; }
            Func<int, int, int, T[], bool> CheckSpareSpace = null;

            public PathNode(int x, int y, Func<int, int, int, T[], bool> checker)
            {
                Pos = new Vec2(x, y);
                CheckSpareSpace = checker;
            }

            public Boolean IsWalkable(KeyValuePair<int, T[]> ps)
            {
                return CheckSpareSpace((int)Pos.x, (int)Pos.y, ps.Key, ps.Value);
            }
        }

        PathNode[,] pathNodes = null;
        SpatialAStar<PathNode, KeyValuePair<int, T[]>> pathFinder = null;

        // 构建寻路器
        void BuildPathFinder()
        {
            pathNodes = new PathNode[w, h];
            FC.For2(w, h, (x, y) => pathNodes[x, y] = new PathNode(x, y, CheckSpareSpace));
            pathFinder = new SpatialAStar<PathNode, KeyValuePair<int, T[]>>(pathNodes);
        }

        // 寻路
        public List<Vec2> FindPath(Vec2 src, Vec2 dst, int radius, params T[] ignoreUIDs)
        {
            var path = new List<Vec2>();

            var dx = (int)dst.x;
            var dy = (int)dst.y;
            //if (!CheckSpareSpace(dx, dy, radius)
            //    && !FindNearestSpareSpace(dx, dy, radius, 1, out dx, out dy))
            //    return path;

            var nodes = pathFinder.Search((int)src.x, (int)src.y, dx, dy, new KeyValuePair<int, T[]>(radius, ignoreUIDs), 5);
            if (nodes != null)
            {
                nodes.RemoveFirst(); // remove the src node
                foreach (var n in nodes)
                    path.Add(n.Pos);
            }

            return path;
        }

        #endregion
    }
}