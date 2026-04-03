using UnityEngine;
using System.Collections.Generic;
using VectorXD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixXD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

/// <summary>
/// Basic mass-spring model component which can be dropped onto
/// a game object and configured so that the set of nodes and
/// edges behave as a mass-spring model.
/// </summary>
public class MassSpring : MonoBehaviour, ISimulable
{
    /// <summary>
    /// Default constructor. All zero. 
    /// </summary>
    public MassSpring()
    {
        Manager = null;
    }

    #region EditorVariables

    public List<Node> Nodes;
    public List<Spring> Springs;
    public List<Edge> Edges; //Crear lista edges

    public float Mass;
    public float StiffnessStretch;
    public float StiffnessBend;
    public float DampingAlpha;
    public float DampingBeta;



    #endregion

    #region OtherVariables
    private PhysicsManager Manager;

    private int index;
    #endregion

    #region MonoBehaviour

    public void Awake()
    {
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Transform trans = this.GetComponent<Transform>();

        Nodes = new List<Node>();
        for (int i = 0; i < vertices.Length; ++i)
        {
            Nodes.Add(new Node(trans.TransformPoint(vertices[i])));
        }

        Springs = new List<Spring>();

        // Diccionario para detectar aristas duplicadas
        Dictionary<(int, int), int> edgeMap = new Dictionary<(int, int), int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];

            ProcessEdge(a, b, c, edgeMap);
            ProcessEdge(b, c, a, edgeMap);
            ProcessEdge(c, a, b, edgeMap);
        }
    }

    private void ProcessEdge(int i1, int i2, int opposite, Dictionary<(int, int), int> edgeMap)
    {
        // Clave ordenada
        (int, int) key = (Mathf.Min(i1, i2), Mathf.Max(i1, i2));
        if (!edgeMap.ContainsKey(key))
        {
            // Primera vez -> muelle estructural
            edgeMap[key] = opposite;
            Springs.Add(new Spring(Nodes[i1], Nodes[i2], Spring.SpringType.Stretch));
        }
        else
        {
            // Ya existe -> muelle de bending entre vértices opuestos
            int otherOpposite = edgeMap[key];
            Springs.Add(new Spring(Nodes[opposite], Nodes[otherOpposite], Spring.SpringType.Bend));
        }
    }

    public void FixedUpdate()
    {
        Transform trans = this.GetComponent<Transform>();
        Mesh mesh = this.GetComponent<MeshFilter>().mesh;
        Vector3[] vertex = new Vector3[Nodes.Count];

        for (int i = 0; i < Nodes.Count; ++i)
        {
            vertex[i] = trans.InverseTransformPoint(Nodes[i].Pos);
        }
        mesh.vertices = vertex;
    }
    #endregion

    #region ISimulable

    public void Initialize(int ind, PhysicsManager m, List<Fixer> fixers)
    {
        
        Manager = m;
        index = ind;
        float DampingNodes = DampingAlpha; 
        float DampingSpring = DampingBeta * StiffnessStretch;

        // Start scene nodes/edges
        for (int f = 0; f < fixers.Count; ++f)
        {
            for (int n = 0; n < Nodes.Count; ++n)
            {
                Nodes[n].Initialize(index + 3 * n, Mass / Nodes.Count, DampingNodes, Manager); // Prepare

                if (fixers[f].IsInside(Nodes[n].Pos))
                {
                    Nodes[n].Fixed = true;
                }
            }
             
        }

        for (int i = 0; i < Springs.Count; ++i)
        {
            if(Springs[i].springType == Spring.SpringType.Stretch)
                Springs[i].Initialize(StiffnessStretch, DampingSpring, Manager); // Prepare
            else
                Springs[i].Initialize(StiffnessBend, DampingSpring, Manager); // Prepare
        }
    }

    public int GetNumDoFs()
    {
        return 3 * Nodes.Count;
    }

    public void GetPosition(VectorXD position)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetPosition(position);
    }

    public void SetPosition(VectorXD position)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].SetPosition(position);
        for (int i = 0; i < Springs.Count; ++i)
            Springs[i].UpdateState();
    }

    public void GetVelocity(VectorXD velocity)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetVelocity(velocity);
    }

    public void SetVelocity(VectorXD velocity)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].SetVelocity(velocity);
    }

    public void GetForce(VectorXD force)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetForce(force);
        for (int i = 0; i < Springs.Count; ++i)
            Springs[i].GetForce(force);
    }

    public void GetForceJacobian(MatrixXD dFdx, MatrixXD dFdv)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetForceJacobian(dFdx, dFdv);
        for (int i = 0; i < Springs.Count; ++i)
            Springs[i].GetForceJacobian(dFdx, dFdv);
    }

    public void GetMass(MatrixXD mass)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetMass(mass);
    }

    public void GetMassInverse(MatrixXD massInv)
    {
        for (int i = 0; i < Nodes.Count; ++i)
            Nodes[i].GetMassInverse(massInv);
    }

    public void FixVector(VectorXD v)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            Nodes[i].FixVector(v);
        }
    }

    public void FixMatrix(MatrixXD M)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            Nodes[i].FixMatrix(M);
        }
    }

    #endregion

    #region OtherMethods

    #endregion

}

public class Edge
{
    public int a;
    public int b;
    public int c;
    public Edge( int _a, int _b, int _c)
    {
        a = _a;
        b = _b;
        c = _c;
    }

    public bool iguales(Edge e2) 
    {
        return a == e2.b && b == e2.a;
    }

    
}

public class EdgeComparer : IComparer<Edge>
{
    public int Compare(Edge a, Edge b)
    {
        if (a.a==b.a && a.b==b.b)
            return 0;
        else if (a.a<b.a || (a.a==b.a && a.b<b.b))  //Si b es mayor que a
            return -1;
        else
            return 1;
    }
}
