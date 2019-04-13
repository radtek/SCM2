using UnityEngine;
using Swift;
using Swift.Math;
using SCM;
using System.Collections.Generic;

public class GridDisplay : SCMBehaviour
{
    public MapGround MG;

    LineRenderer[] lrs = null;
    List<GameObject> cells = null;
    bool inited = false;

    private void Update()
    {
        if (MG.Room == null)
            return;

        if (!inited)
        {
            Clear();
            Create();
            cells = new List<GameObject>();
            inited = true;
        }

        RefreshCells();
    }

    void RefreshCells()
    {
        var sz = MG.Room.MapSize;
        var cols = (int)sz.x;
        var rows = (int)sz.y;
        var n = 0;
        var cm = transform.Find("Cell").gameObject;
        FC.For2(cols, rows, (x, y) =>
        {
            if (MG.Room.CheckSpareSpace(x, y, 1))
                return;

            if (n >= cells.Count)
            {
                var c = Instantiate(cm) as GameObject;
                c.SetActive(true);
                c.transform.SetParent(transform, false);
                cells.Add(c);
            }

            cells[n++].transform.localPosition = new Vector3(x, 0.1f, y);
        });

        FC.For(n, cells.Count, (i) => { cells[i].SetActive(false); });
    }

    private void OnDisable()
    {
        Clear();
    }

    void Clear()
    {
        if (lrs != null)
            foreach (var l in lrs)
                Destroy(l.gameObject);

        lrs = null;

        if (cells != null)
            foreach (var c in cells)
                c.SetActive(false);

        inited = false;
    }

    void Create()
    {
        var sz = MG.Room.MapSize;
        var cols = (int)sz.x;
        var rows = (int)sz.y;

        var lm = transform.Find("Line").gameObject;
        var lst = new List<LineRenderer>();
        FC.For(rows + 2, (y) =>
        {
            var l = Instantiate(lm) as GameObject;
            var lr = l.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(0 - 0.5f, 0.1f, y - 0.5f));
            lr.SetPosition(1, new Vector3(cols + 0.5f, 0.1f, y - 0.5f));
            lr.gameObject.SetActive(true);
            lr.transform.SetParent(transform, false);
            lst.Add(lr);
        });
        FC.For(cols + 2, (x) =>
        {
            var l = Instantiate(lm) as GameObject;
            var lr = l.GetComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x - 0.5f, 0.1f, 0 - 0.5f));
            lr.SetPosition(1, new Vector3(x - 0.5f, 0.1f, rows + 0.5f));
            lr.gameObject.SetActive(true);
            lr.transform.SetParent(transform, false);
            lst.Add(lr);
        });

        lrs = lst.ToArray();
    }
}