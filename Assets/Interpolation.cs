using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class MyQuat {
    public Vector3 im;
    public float re;

    public MyQuat(Vector3 i, float r) {
        im = i; re = r;
    }

    public static MyQuat operator *(MyQuat A, MyQuat B) {
        return new MyQuat(A.re*B.im + B.re*A.im + Vector3.Cross(A.im, B.im), A.re*B.re - Vector3.Dot(A.im, B.im));
    }

    public static MyQuat operator +(MyQuat A, MyQuat B) {
        return new MyQuat(A.im + B.im, A.re + B.re);
    }

    public static MyQuat operator -(MyQuat A, MyQuat B) {
        return new MyQuat(A.im - B.im, A.re - B.re);
    }

    public static MyQuat operator -(MyQuat A) {
        return new MyQuat(-A.im, -A.re);
    }

    public static MyQuat operator /(MyQuat A, float k) {
        return new MyQuat(A.im/k, A.re/k);
    }

    public static MyQuat operator *(float k, MyQuat A) {
        return new MyQuat(A.im*k, A.re*k);
    }

    public MyQuat congiugated() {
        return new MyQuat(-im, re);
    }

    public static MyQuat Lerp(MyQuat A, MyQuat B, float t) {
        return new MyQuat(Vector3.Lerp(A.im, B.im, t), Mathf.Lerp(A.re, B.re, t));
    }

    public static float Dot(MyQuat A, MyQuat B) {
        return A.re*B.re + Vector3.Dot(A.im, B.im);
    }
}

class DualQuaternion {
    public MyQuat p, d;

    public DualQuaternion(MyQuat primal, MyQuat dual) {
        p = primal;
        d = dual;
    }

    public static DualQuaternion FromTranslation(Vector3 t) {
        return new DualQuaternion(new MyQuat(new Vector3(0, 0, 0), 1), new MyQuat(t/2, 0));
    }

    public static DualQuaternion FromRotation(Quaternion r) {
        float angle = 0.0f; 
        Vector3 axis = Vector3.zero;
        r.ToAngleAxis(out angle, out axis);
        return new DualQuaternion(new MyQuat(Mathf.Sin(angle/2*Mathf.Deg2Rad)*axis, Mathf.Cos(angle/2*Mathf.Deg2Rad)), new MyQuat(new Vector3(0, 0, 0), 0));
    }

    public static DualQuaternion operator *(DualQuaternion A, DualQuaternion B) {
        return new DualQuaternion(A.p*B.p, A.p*B.d + A.d*B.p);
    }

    public Vector3 translation() {
        return 2*(d*p.congiugated()).im;
    }

    public Quaternion rotation() {
        return Quaternion.AngleAxis(Mathf.Acos(p.re)*2*Mathf.Rad2Deg, Vector3.Normalize(p.im));
    }

    public static DualQuaternion Nlerp(DualQuaternion A, DualQuaternion B, float t) {
        if (MyQuat.Dot(A.p, B.p) < 0) {
            B.p = -B.p;
            B.d = -B.d;
        }
        MyQuat P = MyQuat.Lerp(A.p, B.p, t);
        MyQuat D = MyQuat.Lerp(A.d, B.d, t);

        float len = Mathf.Sqrt(MyQuat.Dot(P, P));
        P = P/len;
        D = D/len;

        D = D - MyQuat.Dot(P, D)*P;
        return new DualQuaternion(P, D);
    }
}

public class Interpolation : MonoBehaviour {
    public enum InterpolationType {
        QuaternionSlerp,
        QuaternionNlerp,
        AxisAngle,
        EulerAngles,
        Matrix,
        DualQuaternions,
    }

    [Header("Target objects")]
    public GameObject A;
    public GameObject B;
    
    [Header("Interpolation settings")]
    public InterpolationType interpolationType;
    public bool interpolateTranslation = false;

    [Range(0.0f, 1.0f)]
    public float t = 0.0f;

    // https://forum.unity.com/threads/how-to-assign-matrix4x4-to-transform.121966/
    Quaternion ExtractRotation(Matrix4x4 matrix) {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;
 
        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;
 
        return Quaternion.LookRotation(forward, upwards);
    }
 
    Vector3 ExtractPosition(Matrix4x4 matrix) {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }
 
    Vector3 ExtractScale(Matrix4x4 matrix) {
        Vector3 scale;
        scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
        scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
        scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
        return scale;
    }

    void Update() {
        // just reset using A one in case was broken by a matrix interpolation
        transform.localScale = A.transform.localScale;

        // before rotation, so dual quaternions will just overwrite it
        if (interpolateTranslation)
            transform.position = (1-t)*A.transform.position + t*B.transform.position;

        switch(interpolationType) {
            case InterpolationType.QuaternionSlerp:
                transform.rotation = Quaternion.Slerp(A.transform.rotation, B.transform.rotation, t);
                break;

            case InterpolationType.QuaternionNlerp:
                transform.rotation = Quaternion.Lerp(A.transform.rotation, B.transform.rotation, t);
                break;
                
            case InterpolationType.AxisAngle:
                // Unity docs inizializes like this, not sure if needed...
                float angleA, angleB = 0.0f; 
                Vector3 axisA, axisB = Vector3.zero;
                A.transform.rotation.ToAngleAxis(out angleA, out axisA);
                B.transform.rotation.ToAngleAxis(out angleB, out axisB);
                transform.rotation =  Quaternion.AngleAxis(angleA+t*Mathf.DeltaAngle(angleA, angleB), Vector3.Slerp(axisA, axisB, t));
                break;

            case InterpolationType.EulerAngles:
                float yaw = A.transform.eulerAngles.y + t*Mathf.DeltaAngle(A.transform.eulerAngles.y, B.transform.eulerAngles.y);
                float pitch = A.transform.eulerAngles.x + t*Mathf.DeltaAngle(A.transform.eulerAngles.x, B.transform.eulerAngles.x);
                float roll = A.transform.eulerAngles.z + t*Mathf.DeltaAngle(A.transform.eulerAngles.z, B.transform.eulerAngles.z);
                transform.eulerAngles = new Vector3(pitch, yaw, roll);
                break;

            case InterpolationType.Matrix:
                Matrix4x4 matrix = new Matrix4x4();
                Matrix4x4 MatA = A.transform.localToWorldMatrix;
                Matrix4x4 MatB = B.transform.localToWorldMatrix;
                if (A.transform.parent)
                    MatA = A.transform.parent.localToWorldMatrix.inverse*A.transform.localToWorldMatrix;
                if (B.transform.parent)
                    MatB = B.transform.parent.localToWorldMatrix.inverse*B.transform.localToWorldMatrix;

                // No Mat4 LERP, thx Unity API...
                matrix.SetColumn(0, Vector4.Lerp(MatA.GetColumn(0), MatB.GetColumn(0), t));
                matrix.SetColumn(1, Vector4.Lerp(MatA.GetColumn(1), MatB.GetColumn(1), t));
                matrix.SetColumn(2, Vector4.Lerp(MatA.GetColumn(2), MatB.GetColumn(2), t));
                matrix.SetColumn(3, Vector4.Lerp(MatA.GetColumn(3), MatB.GetColumn(3), t));

                transform.localScale = ExtractScale(matrix);
                transform.rotation = ExtractRotation(matrix);
                if (interpolateTranslation)
                    transform.position = ExtractPosition(matrix);
                break;
            
            case InterpolationType.DualQuaternions:
                DualQuaternion DualA = DualQuaternion.FromTranslation(A.transform.position)*DualQuaternion.FromRotation(A.transform.rotation);
                DualQuaternion DualB = DualQuaternion.FromTranslation(B.transform.position)*DualQuaternion.FromRotation(B.transform.rotation);
                transform.rotation = DualQuaternion.Nlerp(DualA, DualB, t).rotation();
                if (interpolateTranslation)
                    transform.position = DualQuaternion.Nlerp(DualA, DualB, t).translation();
                break;
        }
    }
}