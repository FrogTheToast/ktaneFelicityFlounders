using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class Template : MonoBehaviour {

    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMModSettings modSettings;
    public KMSelectable theModule;

    public GameObject cubeTemp;
    public GameObject diodeTemp;
    public GameObject diodeParent;
    public GameObject WireObj;

    public Color[] Colors;

    List<Diode> diodeList = new List<Diode>();
    List<Node> nodeList = new List<Node>();
    List<int> pathList = new List<int>();
    List<int> diodeListIds = new List<int>();

    Node badNode = new Node(-1);
    Node rootNode; Node endNode;
    int cellCount = 0; int cellLimit = 204;
    int killer = 0;
    int leng = 16; int area;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;
    
    bool Extend(Node curNode)
    {
        //Debug.Log(killer.ToString() + "<----------");
        killer++;
        List<Node> cands = new List<Node>();
        Node n = badNode;
        //Debug.Log("------------- " + CX(curNode.id).ToString() + "," + CY(curNode.id).ToString() + "    " + cellCount.ToString());
        for (int i = 0; i < 4; i++) {
            switch (i) {
                case 0: n = curNode.L; break;
                case 1: n = curNode.U; break;
                case 2: n = curNode.R; break;
                case 3: n = curNode.D; break;
            }
            if (n.id != -1 && !n.nada) { 
                int cost = aStarPathfinding(n, endNode);
                if (cost != -1)
                    cands.Add(n);
                processed.Clear(); toSearch.Clear();
                //Debug.Log(i.ToString() + ":  " + cost.ToString() + " " + CX(n.id).ToString() + "," + CY(n.id).ToString());
            }
        }
        if (cands.Count == 0)
            return false;
        cands.Shuffle();
        int rand = Ran(0, 5);
        //Debug.Log("farther or closer?    " + rand.ToString());
        IEnumerable<Node> query;
        if (rand == 0)
            query = cands.OrderBy(x => Distance(x, endNode));
        else
            query = cands.OrderByDescending(x => Distance(x, endNode));

        for (int i1 = 0; i1 < query.Count(); i1++) {
            n = query.ElementAt(i1);
            cellCount++; curNode.nada = true;

            if (cellCount == cellLimit || n.id == endNode.id) {
                pathList.Add(n.id);
                return true;
            }
                
            if (!Extend(n)) {
                if (killer >= killLimit)
                    return true;
                else {
                    cellCount--;
                    curNode.nada = false;
                }
            }
            else {
                pathList.Add(n.id);
                return true;
            }
        }
        return false;
    }
    int killLimit = 1500;

    List<Node> toSearch = new List<Node>();
    List<Node> processed = new List<Node>();
    int Distance(Node a, Node b) {
        int x = (CX(a.id) >= CX(b.id)) ? CX(a.id) - CX(b.id) : CX(b.id) - CX(a.id);
        int y = (CY(a.id) >= CY(b.id)) ? CY(a.id) - CY(b.id) : CY(b.id) - CY(a.id);
        return x + y;
    }
    int aStarPathfinding(Node start, Node target) {
        int cost = 0;
        toSearch.Add(start);
        while (toSearch.Any()) {
            var current = toSearch[0];
            foreach (var t in toSearch) {
                if (t.F() < current.F() || t.F() == current.F() && t.H < current.H)
                    current = t;
            }

            processed.Add(current);
            toSearch.Remove(current);

            if (current == target) {
                var currentPathCell = target;
                while (currentPathCell != start) {
                    cost++;
                    currentPathCell = currentPathCell.prev;
                }
                return cost;
            }

            Node n = badNode;
            for (int i = 0; i < 4; i++) {
                switch (i) {
                    case 0: n = current.L; break;
                    case 1: n = current.U; break;
                    case 2: n = current.R; break;
                    case 3: n = current.D; break;
                }
                if (n.id != -1 && !n.nada && 
                    !processed.Contains(n)) {
                    bool inSearch = toSearch.Contains(n);
                    int costToN = current.G + Distance(current, n);

                    if (!inSearch || costToN < n.G) {
                        n.G = costToN;
                        n.prev = current;

                        if (!inSearch) {
                            n.H = Distance(n, target);
                            toSearch.Add(n);
                        }
                    }
                }
            }
        }
        return -1;
    }

    void Awake() {
        moduleId = moduleIdCounter++;

        // STUPID STPINM DSPIT STIPID SUTPID STUPID 
        diodeTemp.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");

        // initialize empty nodes, empty maze, empty colors, empy
        area = leng * leng;
        for (int i = 0; i < area; i++)
            nodeList.Add(new Node(i));

        // give nodes a sense of direction for once
        for (int i = 0; i < area; i++) {
            if (CY(i + 1) == CY(i)) nodeList[i].R = nodeList[i + 1];
            else nodeList[i].R = badNode;
            if (CY(i - 1) == CY(i)) nodeList[i].L = nodeList[i - 1];
            else nodeList[i].L = badNode;
            if (i + leng < area) nodeList[i].U = nodeList[i + leng];
            else nodeList[i].U = badNode;
            if (i - leng >= 0) nodeList[i].D = nodeList[i - leng];
            else nodeList[i].D = badNode;
            nodeList[i].prev = badNode; nodeList[i].next = badNode;
        }

        // choose start and end for line. end node is not strict
        rootNode = nodeList[Ran(0, area - 1)]; 
        endNode = nodeList[Ran(0, area - 1)];
        while (Distance(rootNode, endNode) < 5)
            endNode = nodeList[Ran(0, area - 1)];

        // create path from root to end. select random spots for diodes to spawn
        Extend(rootNode);
        endNode = nodeList[pathList[0]]; rootNode = nodeList[pathList[pathList.Count - 1]]; //pathList.Reverse();
        Debug.Log("Root: " + CX(rootNode.id).ToString() + "," + CY(rootNode.id).ToString() + "    End: " + CX(endNode.id).ToString() + "," + CY(endNode.id).ToString());
        diodeListIds.AddRange(pathList); 
        int diodeCount = (int)(pathList.Count / 1.5f);
        for (int i = 0; i < diodeCount; i++) {
            diodeListIds.RemoveAt(Ran(1, diodeListIds.Count - 2));
        } diodeListIds.Sort();
        foreach (var node in nodeList)
            node.nada = false;
        foreach (var ind in diodeListIds)
            nodeList[ind].nada = true;

        // spawn & initialize diodes
        int id = 0;
        float gridSize = leng;
        float bound = .075f;
        for (float z = -bound; z < bound * 1.1f; z += bound * 2f / (gridSize - 1)) {
            for (float x = -bound; x < bound * 1.1f; x += bound * 2f / (gridSize - 1)) {
                var obj = Instantiate(diodeTemp, diodeParent.transform);
                var position = obj.transform.localPosition; position.x = x; position.y = diodeParent.transform.localPosition.y; position.z = z;
                obj.transform.localPosition = position;
                obj.name = id.ToString();

                var dio = new Diode(obj, id);
                nodeList[id].dio = dio; nodeList[id].trans = dio.obj.transform;
                if (!nodeList[id].nada) {
                    nodeList[id].dio.obj.GetComponent<MeshRenderer>().enabled = false;
                    Destroy(dio.obj);
                }
                else {
                    diodeList.Add(dio);
                    dio.sel.OnInteract += delegate () {
                        if (!dio.trans)
                        {
                            StartCoroutine(Push(dio, Ran(0,4)));
                            
                        }
                           
                        
                        return false;
                    };
                } 

                id++;
            }
        }

        GenerateWireMesh();
        End();
        
        Debug.Log(pathList.Count);
        //Destroy(diodeTemp);
        theModule.Children = diodeList.Select(x => x.sel).ToArray();
        theModule.UpdateChildrenProperly();

    }
    
    void Assign(Diode dio, int c) {
        dio.light.enabled = true;
        dio.light.color = Colors[c];
        dio.mat.SetColor("_EmissionColor", Colors[c]);
    }

    // debug: lets me see path before i did GenerateWire()
    void End() {
        float c = 0;
        foreach (var item in pathList) {
            if (!nodeList[item].nada)
                continue;
            //if (item != rootNode.id && item != endNode.id)
            //    continue;

            var cube = Instantiate(cubeTemp, diodeParent.transform);
            var obj = nodeList[item].dio.obj;
            var position = obj.transform.localPosition;
            cube.transform.localPosition = position;

            //cube.GetComponentInChildren<MeshRenderer>().material
            //    .SetColor("_Color", Color.Lerp(Color.white,Color.red, c / diodeListIds.Count));
            //cube.GetComponentInChildren<MeshRenderer>().material
            //    .SetColor("_Color", Color.black);

            nodeList[item].obj = cube;
            c++;
        }
        Destroy(cubeTemp);

    }

    IEnumerator Push(Diode dio, int c) {
        dio.trans = true;
        float t = 0f; var pos = dio.obj.transform.localPosition;
        while (t < 1)  {
            var obj = dio.obj;
            var position = obj.transform.localPosition; position.y -= .00025f;
            obj.transform.localPosition = position;
            t += .1f;
            yield return new WaitForSeconds(0.001f);
        } Assign(dio, c);
        t = 0f;
        while (t < 1) {
            var obj = dio.obj;
            var position = obj.transform.localPosition; position.y += .00025f;
            obj.transform.localPosition = position;
            t += .1f;
            yield return new WaitForSeconds(0.001f);
        }
        dio.obj.transform.localPosition = pos;
        dio.trans = false;
    }

    public float rad = 0;
    public int wireSides = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    Mesh mesh;
    void GenerateWireMesh() {
        mesh = new Mesh();
        WireObj.GetComponent<MeshFilter>().mesh = mesh;

        for (int i = 0; i < pathList.Count; i++) 
            GenerateCircleVertices(i);
        GenerateTriangles();

        Debug.Log(vertices.Count);
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        triangles.Clear();
        vertices.Clear();
    }
    void GenerateTriangles() {
        int c = 0;
        for (int circInd = 0; circInd < (pathList.Count - 1) * wireSides; circInd += wireSides) {
            for (int vertInd = 0; vertInd < wireSides; vertInd++) {
                // 0 1 2 3 4     5 6 7 8 9     10 11 12 13 14
                // 051 156
                // 950 951 952 953 954 955 956 957 958 959 960
                if (vertInd == wireSides - 1)
                    c = wireSides; 
                int cv = circInd + vertInd;

                triangles.AddRange(new List<int> {
                    cv,
                    cv + wireSides,
                    cv + 1 - c,

                    cv + 1 - c,
                    cv + wireSides,
                    cv + wireSides + 1 - c,
                });
                c = 0;
                //Debug.Log("--------");
                //Debug.Log(cv);
                //Debug.Log(cv + wireSides);
                //Debug.Log(cv + 1 - c);
                //Debug.Log(cv + 1 - c);
                //Debug.Log(cv + wireSides);
                //Debug.Log(cv + wireSides + 1 - c);
            }
        }
    }
    void GenerateCircleVertices(int i) {
        Vector3 current; Vector3 target; float corner = 1f;
        Vector3 actual = nodeList[pathList[i]].trans.localPosition;

        //Debug.Log(i.ToString() + " " + pathList.Count);
        if (i == 0) {
            current = nodeList[pathList[i]].trans.localPosition;
            target = nodeList[pathList[i + 1]].trans.localPosition;
        }
        else if (i != pathList.Count - 1) {
            current = nodeList[pathList[i - 1]].trans.localPosition;
            target = nodeList[pathList[i + 1]].trans.localPosition;
            if (CX(pathList[i + 1]) != CX(pathList[i - 1]) && CY(pathList[i + 1]) != CY(pathList[i - 1]))
                corner = 1.2f;
        }
        else {
            current = nodeList[pathList[i - 1]].trans.localPosition;
            target = nodeList[pathList[i]].trans.localPosition;
        }
        
        var forwardRot = Quaternion.LookRotation((target - current).normalized);
        for (int n = 0; n < wireSides; n++) {
            float x = rad * corner * Mathf.Sin(2 * Mathf.PI * n / wireSides);
            float y = rad * corner * Mathf.Cos(2 * Mathf.PI * n / wireSides);

            Vector3 vtcOffset = new Vector3(x, y, 0);
            Vector3 vtcRotated = forwardRot * vtcOffset;
            Vector3 newVtc = actual - vtcRotated;

            vertices.Add(newVtc);
        }
    }

    public class Diode {
        public GameObject obj;
        public Light light;
        public Material mat;
        public KMSelectable sel;
        public Color col;
        public int id;
        public Vector3 pos;
        public bool trans;
        public Diode(GameObject O, int ID)
        {
            obj = O; id = ID; 
            light = obj.GetComponentInChildren<Light>();
            mat = obj.GetComponent<MeshRenderer>().material;
            sel = obj.GetComponentInChildren<KMSelectable>();
            col = Color.black;
            pos = obj.transform.localPosition;
        }
    }
    public class Node
    {
        public int id;
        public GameObject obj;
        public Transform trans;
        public Diode dio;
        public Node U;
        public Node D;
        public Node L;
        public Node R;
        public bool nada = false;
        public int G = 0;
        public int H = 0;
        public Node next;
        public Node prev;

        public Node(int ID) {
            id = ID;
        }
        public int F() {
            return G + H;
        }
        

    }

    // self-explanatory
    IEnumerator StupidAssDumbAssPieceOfFuckYou() {
        yield return new WaitForSeconds(.25f);
        float scalar = transform.lossyScale.x;

        for (int i = 0; i < diodeList.Count; i++)
            diodeList[i].light.range *= scalar;
    }
    void Start() {
        StartCoroutine(StupidAssDumbAssPieceOfFuckYou());
    }

    string Con(int id) { return CX(id).ToString() + "," + CY(id).ToString(); }
    int CY(int id) { return (id >= 0) ? (int)Mathf.Floor(id / leng) : (int)Mathf.Floor(id / leng) - 1; }
    int CX(int id) { return M(id, leng); }
    int M(int x, int m) {
        int r = x % m;
        return r < 0 ? r + m : r;
    }
    int Ran(int a, int b) { return Random.Range(a, b + 1); }

    // Update is called once per frame
    void Update()
    {

    }
}
