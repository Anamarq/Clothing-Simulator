using UnityEngine;
using VectorXD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixXD = MathNet.Numerics.LinearAlgebra.Matrix<double>;
using DenseVectorXD = MathNet.Numerics.LinearAlgebra.Double.DenseVector;
using DenseMatrixXD = MathNet.Numerics.LinearAlgebra.Double.DenseMatrix;

public class Spring {

    #region InEditorVariables

    public float Stiffness;
    public float Damping;
    public Node nodeA;
    public Node nodeB;

    #endregion

    public enum SpringType { Stretch, Bend };
    public SpringType springType;

    public float Length0;
    public float Length;
    public Vector3 dir;

    private PhysicsManager Manager;

    public Spring(Node a, Node b, SpringType s)
    {
        nodeA = a;
        nodeB = b;
        springType = s;
    }

    public void Initialize(float stiffness, float damping, PhysicsManager m)
    {
        Manager = m;
        Stiffness = stiffness;
        Damping = damping; 
        UpdateState();
        Length0 = Length;
    }

    public void UpdateState()
    {
        dir = nodeA.Pos - nodeB.Pos;
        Length = dir.magnitude;

        if (Length > 1e-6f)
            dir /= Length;
        else
            dir = Vector3.zero;
    }

    public void GetForce(VectorXD force)
    {
        Vector3 deltaPos = nodeA.Pos - nodeB.Pos;
        float length = deltaPos.magnitude;

        if (length < 1e-6f) return;

        Vector3 dir = deltaPos / length;

        Vector3 fElastic = -Stiffness * (length - Length0) * dir;

        // Damping (Rayleigh)
        Vector3 deltaVel = nodeA.Vel - nodeB.Vel;
        float vRel = Vector3.Dot(deltaVel, dir);
        Vector3 fDamp = -Damping * vRel * dir;

        Vector3 f = fElastic + fDamp;

        force[nodeA.index] += f.x;
        force[nodeA.index + 1] += f.y;
        force[nodeA.index + 2] += f.z;

        force[nodeB.index] -= f.x;
        force[nodeB.index + 1] -= f.y;
        force[nodeB.index + 2] -= f.z;
    }

    public void GetForceJacobian(MatrixXD dFdx, MatrixXD dFdv)
    {
        MatrixXD dFadxa = new DenseMatrixXD(3);
        MatrixXD dFbdxb = new DenseMatrixXD(3);
        MatrixXD dFadxb = new DenseMatrixXD(3);
        MatrixXD dFbdxa = new DenseMatrixXD(3);
        MatrixXD I = DenseMatrixXD.CreateIdentity(3);

        VectorXD u = new DenseVectorXD(3);
        Vector3 v = (nodeA.Pos - nodeB.Pos).normalized;
        u[0] = v.x;
        u[1] = v.y;
        u[2] = v.z;

        dFadxa = -Stiffness * ((Length - Length0) / Length) * I - Stiffness * (Length0 / Length) * u.OuterProduct(u);
        dFbdxb = dFadxa;
        dFadxb = -dFadxa;
        dFbdxa = -dFadxa;

        dFdx.SetSubMatrix(nodeA.index, nodeA.index, dFdx.SubMatrix(nodeA.index, 3, nodeA.index, 3) + dFadxa);
        dFdx.SetSubMatrix(nodeA.index, nodeB.index, dFdx.SubMatrix(nodeA.index, 3, nodeB.index, 3) + dFadxb);
        dFdx.SetSubMatrix(nodeB.index, nodeA.index, dFdx.SubMatrix(nodeB.index, 3, nodeA.index, 3) + dFbdxa);
        dFdx.SetSubMatrix(nodeB.index, nodeB.index, dFdx.SubMatrix(nodeB.index, 3, nodeB.index, 3) + dFbdxb);

        MatrixXD dFadva = new DenseMatrixXD(3);
        MatrixXD dFbdvb = new DenseMatrixXD(3);
        MatrixXD dFadvb = new DenseMatrixXD(3);
        MatrixXD dFbdva = new DenseMatrixXD(3);

        dFadva = -Damping* u.OuterProduct(u);
        dFbdvb = dFadva;
        dFadvb = -dFadva;
        dFbdva = -dFadva;

        dFdv.SetSubMatrix(nodeA.index, nodeA.index, dFdv.SubMatrix(nodeA.index, 3, nodeA.index, 3) + dFadva);
        dFdv.SetSubMatrix(nodeA.index, nodeB.index, dFdv.SubMatrix(nodeA.index, 3, nodeB.index, 3) + dFadvb);
        dFdv.SetSubMatrix(nodeB.index, nodeA.index, dFdv.SubMatrix(nodeB.index, 3, nodeA.index, 3) + dFbdva);
        dFdv.SetSubMatrix(nodeB.index, nodeB.index, dFdv.SubMatrix(nodeB.index, 3, nodeB.index, 3) + dFbdvb);
    }

}
