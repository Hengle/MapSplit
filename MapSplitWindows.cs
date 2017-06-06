using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;

struct Block
{
    public List<Vector3> linePosList;
}

public class MapSplitWindows : EditorWindow
{
    public const string editorScene = "mapSplit.unity";

    public const string savedPath = "mapSplit/";

    public static string outputPath = Application.dataPath + "/mapSplitData/";

    public Color[] blockColors = { Color.red, Color.gray, Color.yellow, Color.blue, Color.green, Color.white, Color.cyan, Color.magenta, Color.black };

    [MenuItem("场景/分割场景地图")]
    public static void SplitSceneMap()
    {
        MapSplitWindows windows = EditorWindow.GetWindow<MapSplitWindows>();

        windows.autoRepaintOnSceneChange = true;
        windows.titleContent = new GUIContent("场景地图分割工具");
        windows.minSize = new Vector2(300, 600);

        windows.Show();
    }

    private GameObject map;

    private Material material = null;

    private float vertexSize = 3f;

    private bool showVertexs = false;

    private bool showAddLines = false;

    private Color lineColor = Color.red;

    private float lineSize = 2.0f;

    private int BlockCnt = 0;

    private List<GameObject> lineList = new List<GameObject>();

    private List<Block> blockList = new List<Block>();

    public List<GameObject> lineVertices = new List<GameObject>();

    private Mesh _Mesh;

    private GameObject[] _Vertices;

    //序列化对象
    protected SerializedObject _serializedObject;

    //序列化属性
    protected SerializedProperty _lineVerticesProperty;

    private List<GameObject> meshList = new List<GameObject>();

    void OnEnable()
    {
        //使用当前类初始化
        _serializedObject = new SerializedObject(this);
        //获取当前类中可序列话的属性
        _lineVerticesProperty = _serializedObject.FindProperty("lineVertices");
    }

    void OnGUI()
    {
        MapEditTool.GUILabelType();

        GUILayout.Label("场景地图选择");

        MapEditTool.GUILabelType(TextAnchor.UpperLeft);

        GUILayout.Label("当前的场景为 ：" + EditorSceneManager.GetActiveScene().name);

        map = MapEditTool.GUIobject_CaneditArea("加载地形文件", map, true, LoadMap);

        GUILayout.Space(2);
        MapEditTool.CreateSplit();

        if (showVertexs && map != null)
        {
            ShowVertexs();
        }

        if (showAddLines)
        {
            ShowAddLines();
        }

        if (lineList.Count > 0)
        {
            ShowOutput();
        }

        test();
    }

    private void ShowVertexs()
    {
        MapEditTool.GUILabelType();

        GUILayout.Label("场景地图顶点生成");

        MapEditTool.GUILabelType(TextAnchor.UpperLeft);

        GUILayout.BeginHorizontal();

        GUILayout.Label("顶点材质");

        material = (Material)EditorGUILayout.ObjectField(material, typeof(Material), false);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("顶点大小");

        vertexSize = EditorGUILayout.FloatField(vertexSize, GUILayout.Width(100));

        GUILayout.EndHorizontal();

        if (GUILayout.Button("生成网点"))
        {
            GenVertex();
        }

        MapEditTool.CreateSplit();
    }

    private void ShowAddLines()
    {
        MapEditTool.GUILabelType();

        GUILayout.Label("场景地图分割");

        MapEditTool.GUILabelType(TextAnchor.UpperLeft);

        GUILayout.BeginHorizontal();

        GUILayout.Label("网线颜色");

        lineColor = EditorGUILayout.ColorField(lineColor, GUILayout.Width(100));

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label("网线粗细");

        lineSize = EditorGUILayout.FloatField(lineSize, GUILayout.Width(100));

        GUILayout.EndHorizontal();

        if (lineList.Count > 0 && GUILayout.Button("撤销上次连线区域块"))
        {
            ClearLastBlock();
        }

        if (lineList.Count > 0 && GUILayout.Button("清除所有连线区域块"))
        {
            ClearAllBlocks();
        }


        GUIStyle labelstyle = new GUIStyle();
        labelstyle.alignment = TextAnchor.UpperLeft;
        labelstyle.fontSize = 14;
        labelstyle.normal.textColor = Color.red;

        GUILayout.Label("Tips: 请顺时针编辑节点！", labelstyle);

        //开始检查是否有修改
        EditorGUI.BeginChangeCheck();

        //显示属性
        //第二个参数必须为true，否则无法显示子节点即List内容
        EditorGUILayout.PropertyField(_lineVerticesProperty, true);

        //结束检查是否有修改
        if (EditorGUI.EndChangeCheck())
        {//提交修改
            _serializedObject.ApplyModifiedProperties();
        }

        if (GUILayout.Button("添加选中顶点"))
        {
            AddLineVertexs();
        }

        if (lineVertices.Count > 3 && GUILayout.Button("生成区域块"))
        {
            CreateBlock();
        }

        MapEditTool.CreateSplit();
    }

    private void ShowOutput()
    {
        MapEditTool.GUILabelType();

        GUILayout.Label("导出地图分块数据");

        MapEditTool.GUILabelType(TextAnchor.UpperLeft);

        GUIStyle labelstyle = new GUIStyle();
        labelstyle.alignment = TextAnchor.UpperLeft;
        labelstyle.fontSize = 14;
        labelstyle.normal.textColor = Color.red;

        GUILayout.Label("Tips: 请确认编辑分割数据正确后再导出！", labelstyle);

        if (GUILayout.Button("导出区域块"))
        {
            ExportToFile();
        }

        if (GUILayout.Button("清空导出文件"))
        {
            ClearExportFile();
        }

    }

    private void test()
    {
        MapEditTool.CreateSplit();

        MapEditTool.GUILabelType();

        GUILayout.Label("仅测试：导出数据读取并生成网格");

        MapEditTool.GUILabelType(TextAnchor.UpperLeft);

        if (GUILayout.Button("逆向生成区域块"))
        {
            ReverseCreateBlock();
        }

        if (GUILayout.Button("读取并生成"))
        {
            ReadAndCreate();
        }

        if (GUILayout.Button("清除所有生成面片"))
        {
            ClearAllMesh();
        }
    }

    private void Clear()
    {

    }

    void OnDestroy()
    {

    }

    private void LoadMap()
    {
        if (Application.isPlaying)
        {
            UtilityCanOrCantWindows.CreateWindows("Error", "正在运行请停止后重试", null, null);
            return;
        }

        if (map != null)
        {
            EditorSceneManager.OpenScene(editorScene);
            GameObject.Instantiate(map);
            map.transform.localScale = Vector3.one;
            showVertexs = true;
        }
    }

    private void GenVertex()
    {
        if (material == null)
        {
            material = map.GetComponent<MeshRenderer>().sharedMaterial;
        }

        #region 销毁顶点
        if (_Vertices != null)
        {
            for (int i = 0; i < _Vertices.Length; ++i)
            {
                GameObject.DestroyImmediate(_Vertices[i]);
            }
            _Vertices = null;
        }
        #endregion

        #region 识别顶点
        List<List<int>> _AllVerticesGroupList = new List<List<int>>();
        List<Vector3> _AllVerticesList = new List<Vector3>(map.GetComponent<MeshFilter>().sharedMesh.vertices);
        List<Vector3> _VerticesList = new List<Vector3>(map.GetComponent<MeshFilter>().sharedMesh.vertices);
        List<int> _VerticesRemoveList = new List<int>();

        //循环遍历并记录重复顶点
        for (int i = 0; i < _VerticesList.Count; i++)
        {
            EditorUtility.DisplayProgressBar("识别顶点", "正在识别顶点（" + i + "/" + _VerticesList.Count + "）......", 1.0f / _VerticesList.Count * i);
            //已存在于删除集合的顶点不计算
            if (_VerticesRemoveList.IndexOf(i) >= 0)
                continue;

            List<int> _VerticesSubList = new List<int>();
            _VerticesSubList.Add(i);
            int j = i + 1;
            //发现重复顶点，将之记录在内，并加入待删除集合
            while (j < _VerticesList.Count)
            {
                if (_VerticesList[i] == _VerticesList[j])
                {
                    _VerticesSubList.Add(j);
                    _VerticesRemoveList.Add(j);
                }
                j++;
            }
            //记录重复顶点集合
            _AllVerticesGroupList.Add(_VerticesSubList);
        }
        //整理待删除集合
        _VerticesRemoveList.Sort();
        //删除重复顶点
        for (int i = _VerticesRemoveList.Count - 1; i >= 0; i--)
        {
            _VerticesList.RemoveAt(_VerticesRemoveList[i]);
        }
        _VerticesRemoveList.Clear();
        #endregion

        #region 识别三角面
        List<List<int>> _AllTriangleList = new List<List<int>>();
        int[] _Total = map.GetComponent<MeshFilter>().sharedMesh.triangles;
        List<int> _Triangle;
        for (int i = 0; i + 2 < _Total.Length; i += 3)
        {
            _Triangle = new List<int>();
            _Triangle.Add(_Total[i]);
            _Triangle.Add(_Total[i + 1]);
            _Triangle.Add(_Total[i + 2]);
            _AllTriangleList.Add(_Triangle);
        }
        #endregion

        #region 创建顶点
        int _VerticesNum = _VerticesList.Count;
        int _VertexNumber = _VerticesNum;
        //创建顶点，应用顶点大小设置，顶点位置为删除重复顶点之后的集合
        _Vertices = new GameObject[_VerticesNum];
        for (int i = 0; i < _VerticesNum; i++)
        {
            EditorUtility.DisplayProgressBar("创建顶点", "正在创建顶点（" + i + "/" + _VerticesNum + "）......", 1.0f / _VerticesNum * i);
            _Vertices[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _Vertices[i].name = "Vertex";
            _Vertices[i].transform.localScale = new Vector3(vertexSize, vertexSize, vertexSize);
            _Vertices[i].transform.position = map.transform.localToWorldMatrix.MultiplyPoint3x4(_VerticesList[i]);
            _Vertices[i].GetComponent<MeshRenderer>().sharedMaterial = material;
            //_Vertices[i].AddComponent<VertexIdentity>();
            //_Vertices[i].GetComponent<VertexIdentity>()._Identity = i;
            //_Vertices[i].transform.SetParent(map.transform);
        }
        float _LastVertexSize = vertexSize;
        EditorUtility.ClearProgressBar();
        #endregion

        showAddLines = true;
    }

    private void ClearLastBlock()
    {
        if (lineList.Count > 0)
        {
            int idx_1 = lineList.Count - 1;
            GameObject.DestroyImmediate(lineList[idx_1]);
            lineList.RemoveAt(idx_1);
        }
        if (blockList.Count > 0)
        {
            int idx_2 = blockList.Count - 1;
            blockList.RemoveAt(idx_2);
            --BlockCnt;
        }
    }

    private void ClearAllBlocks()
    {
        for (int i = 0; i < lineList.Count; ++i)
        {
            GameObject.DestroyImmediate(lineList[i]);
        }

        lineList.Clear();
        BlockCnt = 0;
        blockList.Clear();
    }

    private void AddLineVertexs()
    {
        GameObject[] objs = Selection.gameObjects;
        for (int i = 0; i < objs.Length; ++i)
        {
            lineVertices.Add(objs[i]);
        }
        //更新
        _serializedObject.Update();
    }

    /// <summary>
    /// 创建区域块
    /// </summary>
    private void CreateBlock()
    {
        if (lineVertices.Count > 3)
        {
            Vector3 start = lineVertices[0].transform.position;
            Vector3 end = lineVertices[lineVertices.Count - 1].transform.position;

            if (!(Mathf.FloorToInt(start.x * 1000) == Mathf.FloorToInt(end.x * 1000) && Mathf.FloorToInt(start.y * 1000) == Mathf.FloorToInt(end.y * 1000) && Mathf.FloorToInt(start.z * 1000) == Mathf.FloorToInt(end.z * 1000)))
            {
                EditorUtility.DisplayDialog("提示", "该区域块不是封闭的！", "确定");
                return;
            }

            GameObject line = new GameObject();
            LineRenderer _lineRenderer = line.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            _lineRenderer.SetColors(lineColor, lineColor);
            _lineRenderer.SetWidth(lineSize, lineSize);
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.SetVertexCount(lineVertices.Count);

            Block newBlock = new Block();
            if (newBlock.linePosList == null)
            {
                newBlock.linePosList = new List<Vector3>();
            }

            for (int i = 0; i < lineVertices.Count; ++i)
            {
                _lineRenderer.SetPosition(i, lineVertices[i].transform.position);
                newBlock.linePosList.Add(lineVertices[i].transform.position);
            }
            blockList.Add(newBlock);

            line.name = "CreateBlock_" + BlockCnt;
            ++BlockCnt;
            lineList.Add(line);

            lineVertices.Clear();
            _serializedObject.Update();
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "顶点数少于4，无法生成封闭的区域块！", "确定");
        }
    }

    private void ExportToFile()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        using (FileStream stream = File.Open(outputPath + map.name + ".txt", FileMode.OpenOrCreate, FileAccess.Write))
        {
            stream.SetLength(0);

            byte[] bys = System.Text.Encoding.UTF8.GetBytes(GenInfos());
            stream.Write(bys, 0, bys.Length);
            stream.Close();
        }
    }

    private void ClearExportFile()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        using (FileStream stream = File.Open(outputPath + map.name + ".txt", FileMode.OpenOrCreate, FileAccess.Write))
        {
            stream.SetLength(0);
            stream.Close();
        }
    }

    private string GenInfos()
    {
        StringBuilder sb = new StringBuilder(2048);
        for (int i = 0; i < blockList.Count; ++i)
        {
            sb.AppendLine(blockList[i].linePosList.Count.ToString());
            for (int j = 0; j < blockList[i].linePosList.Count; ++j)
            {
                sb.AppendLine(blockList[i].linePosList[j].x + "," + blockList[i].linePosList[j].y + "," + blockList[i].linePosList[j].z);
            }
        }
        return sb.ToString();
    }

    private void ReverseCreateBlock()
    {
        if (blockList.Count > 0)
        {
            EditorUtility.DisplayDialog("提示", "请清空所有生成的区域块！", "确定");
            return;
        }

        BlockCnt = 0;
        blockList.Clear();
        lineList.Clear();

        for (int k = 0; k < this.meshList.Count; ++k)
        {
            Vector3[] vertices = meshList[k].GetComponent<MeshFilter>().sharedMesh.vertices;

            GameObject line = new GameObject();
            LineRenderer _lineRenderer = line.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            _lineRenderer.SetColors(lineColor, lineColor);
            _lineRenderer.SetWidth(lineSize, lineSize);
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.SetVertexCount(vertices.Length + 1);

            Block newBlock = new Block();
            if (newBlock.linePosList == null)
            {
                newBlock.linePosList = new List<Vector3>();
            }

            for (int i = 0; i < vertices.Length; ++i)
            {
                _lineRenderer.SetPosition(i, vertices[i]);
                newBlock.linePosList.Add(vertices[i]);
            }

            _lineRenderer.SetPosition(vertices.Length, vertices[0]);
            newBlock.linePosList.Add(vertices[0]);

            blockList.Add(newBlock);

            line.name = "CreateBlock_" + BlockCnt;
            ++BlockCnt;
            lineList.Add(line);
        }
    }

    private void ReadAndCreate()
    {
        if (meshList.Count > 0)
        {
            EditorUtility.DisplayDialog("提示", "请清空所有生成的面片！", "确定");
            return;
        }

        try
        {
            string filePath = outputPath + map.name + ".txt";
            if (File.Exists(Path.GetFullPath(filePath)))
            {
                List<string> list = new List<string>(File.ReadAllLines(filePath));
                List<Block> bList = new List<Block>();
                int idx = 0;
                while (idx < list.Count)
                {
                    Block block = new Block();
                    if (block.linePosList == null)
                    {
                        block.linePosList = new List<Vector3>();
                    }
                    int cnt = int.Parse(list[idx]);
                    for (int i = idx + 1; i < idx + 1 + cnt; ++i)
                    {
                        string[] strs = list[i].Split(',');
                        Vector3 pos = new Vector3(float.Parse(strs[0]), float.Parse(strs[1]), float.Parse(strs[2]));
                        block.linePosList.Add(pos);
                    }
                    idx = idx + 1 + cnt;
                    bList.Add(block);
                }

                CreateMesh(bList);
            }
        }
        catch (Exception ex)
        {
            LogMgr.LogError(ex);
        }
    }

    private void CreateMesh(List<Block> blist)
    {
        for (int k = 0; k < blist.Count; ++k)
        {
            List<Vector3> list = blist[k].linePosList;
            Vector3[] vertices = new Vector3[list.Count - 1];
            bool[] IsObtuse = new bool[list.Count - 1];
            int[] triangles = new int[3 * (list.Count - 3)];

            for (int i = 0; i < list.Count - 1; ++i)
            {
                vertices[i] = list[i];
            }

            int startIdx = -1;
            for (int i = 0; i < list.Count - 1; ++i)
            {
                Vector3 a = list[(i + list.Count - 2) % (list.Count - 1)] - list[i];
                Vector3 b = list[(i + 1) % (list.Count - 1)] - list[i];
                if (Vector3.Cross(a.normalized, b.normalized).y > 0)
                {
                    IsObtuse[i] = true;
                    if (startIdx == -1)
                    {
                        startIdx = i;
                    }
                }
                else
                {
                    IsObtuse[i] = false;
                }
            }

            startIdx = startIdx > 0 ? startIdx : 0;
            Queue<int> queue = new Queue<int>();
            for (int i = 0; i < list.Count - 1; ++i)
            {
                queue.Enqueue((startIdx + i) % (list.Count - 1));
            }

            int flagCnt = 0;
            int tIdx = 0;
            int lastIdx = -1;
            List<int> vlist = new List<int>();
            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (IsObtuse[idx])
                {
                    if (vlist.Count > 0 && flagCnt == 0)
                    {
                        for (int q = 0; q < vlist.Count; q++)
                        {
                            queue.Enqueue(vlist[q]);
                        }
                        vlist.Clear();
                    }

                    queue.Enqueue(idx);
                    IsObtuse[idx] = false;
                    ++flagCnt;

                    if (flagCnt == 1)
                    {
                        lastIdx = idx;
                    }
                    else
                    {
                        Vector3 a = list[(lastIdx - 1) % (list.Count - 1)] - list[lastIdx];
                        Vector3 b = list[idx] - list[lastIdx];
                        if (Vector3.Cross(a.normalized, b.normalized).y > 0)
                        {
                            IsObtuse[lastIdx] = true;
                        }

                        a = list[lastIdx] - list[idx];
                        b = list[(idx + 1) % (list.Count - 1)] - list[idx];
                        if (Vector3.Cross(a.normalized, b.normalized).y > 0)
                        {
                            IsObtuse[idx] = true;
                        }
                    }
                }
                vlist.Add(idx);

                if (flagCnt > 0 && flagCnt % 2 == 0 || queue.Count == 0)
                {

                    if (flagCnt == 1 && queue.Count == 0)
                    {
                        vlist.RemoveAt(0);
                    }

                    for (int i = 0; i < vlist.Count - 2; ++i)
                    {
                        triangles[3 * (i + tIdx)] = vlist[vlist.Count - 1];//固定第一个点
                        triangles[3 * (i + tIdx) + 1] = vlist[i];
                        triangles[3 * (i + tIdx) + 2] = vlist[i + 1];
                    }
                    tIdx += (vlist.Count - 2);
                    flagCnt = 0;
                    lastIdx = -1;
                    vlist.Clear();
                }
            }

            GameObject target = new GameObject();
            target.name = "BlockMesh_" + k;
            target.transform.position = new Vector3(0, 30, 0);

            MeshFilter filter = target.AddComponent<MeshFilter>();
            target.AddComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Diffuse"));
            material.color = blockColors[k % 9];
            target.GetComponent<MeshRenderer>().material = material;

            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            List<Color> colors = new List<Color>();
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                colors.Add(blockColors[k % 9]);
            }
            mesh.SetColors(colors);
            //mesh.uv = GetComponent<MeshFilter>().sharedMesh.uv;
            mesh.name = "mesh";
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            meshList.Add(target);
        }
    }

    private void ClearAllMesh()
    {
        for (int i = 0; i < meshList.Count; ++i)
        {
            GameObject.DestroyImmediate(meshList[i]);
        }
        meshList.Clear();
    }
}
