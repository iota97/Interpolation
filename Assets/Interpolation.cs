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

    public static MyQuat Slerp(MyQuat A, MyQuat B, float t) {
        float alpha = Mathf.Acos(MyQuat.Dot(A, B));
        Vector3 i = A.im * Mathf.Sin(alpha*(1-t))/Mathf.Sin(alpha) + B.im * Mathf.Sin(alpha*t)/Mathf.Sin(alpha);
        float r = A.re * Mathf.Sin(alpha*(1-t))/Mathf.Sin(alpha) + B.re * Mathf.Sin(alpha*t)/Mathf.Sin(alpha);
        return new MyQuat(i, r);
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

    public static DualQuaternion Slerp(DualQuaternion A, DualQuaternion B, float t) {
        if (MyQuat.Dot(A.p, B.p) < 0) {
            B.p = -B.p;
            B.d = -B.d;
        }
    
        MyQuat P = MyQuat.Slerp(A.p, B.p, t);
        MyQuat D = MyQuat.Slerp(A.d, B.d, t);

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
        DualQuaternionsSlerp,
        DualQuaternionsNlerp,
    }

    [Header("Target objects")]
    public GameObject A;
    public GameObject B;
    
    [Header("Interpolation settings")]
    public InterpolationType interpolationType;
    public bool interpolateTranslation = false;

    [Range(0.0f, 1.0f)]
    public float t = 0.0f;

    [Header("Animation settings")]
    public bool animate = false;
    [Range(0.0f, 1.0f)]
    public float speed = 0.5f;
    bool reverseAnim = false;

    // https://stackoverflow.com/questions/70462758/c-sharp-how-to-convert-quaternions-to-euler-angles-xyz
    Vector3 ToEulerAngles(Quaternion q) {
        Vector3 angles = new();

        // roll
        float sinr_cosp = 2 * (q.w * q.x + q.z * q.y);
        float cosr_cosp = 1 - 2 * (q.x * q.x + q.z * q.z);
        angles.x = Mathf.Atan2(sinr_cosp, cosr_cosp);

        // pitch
        float sinp = 2 * (q.w * q.z - q.y * q.x);
        if (Mathf.Abs(sinp) >= 1) {
            angles.z = sinp < 0 ? -Mathf.PI/2 : Mathf.PI/2;
        } else {
            angles.z = Mathf.Asin(sinp);
        }

        // yaw
        float siny_cosp = 2 * (q.w * q.y + q.x * q.z);
        float cosy_cosp = 1 - 2 * (q.z * q.z + q.y * q.y);
        angles.y = Mathf.Atan2(siny_cosp, cosy_cosp);

        return angles/Mathf.PI*180;
    }

    void Update() {
        Material mat = transform.GetChild(0).GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial;
        mat.SetMatrix("_mat", Matrix4x4.identity);
        mat.SetMatrix("_mat_inv", Matrix4x4.identity);

        if (animate) {
            t += (reverseAnim ? -1 : 1)*Time.deltaTime*speed;
            if (t > 1) {
                t  = 1;
                reverseAnim = true;
            } else if (t < 0) {
                t = 0;
                reverseAnim = false;
            }
        }

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
                if (Vector3.Dot(axisA, axisB) < 0) {
                    angleB *= -1;
                    axisB *= -1;
                }
                transform.rotation = Quaternion.AngleAxis(angleA+t*Mathf.DeltaAngle(angleA, angleB), Vector3.Slerp(axisA, axisB, t));
                break;

            case InterpolationType.EulerAngles:
                Vector3 rotA = ToEulerAngles(A.transform.rotation);
                Vector3 rotB = ToEulerAngles(B.transform.rotation);
                float yaw = rotA.y + t*Mathf.DeltaAngle(rotA.y, rotB.y);
                float pitch = rotA.z + t*Mathf.DeltaAngle(rotA.z, rotB.z);
                float roll = rotA.x + t*Mathf.DeltaAngle(rotA.x, rotB.x);
                transform.rotation = Quaternion.AngleAxis(roll, Vector3.right)*Quaternion.AngleAxis(pitch, Vector3.forward)*Quaternion.AngleAxis(yaw, Vector3.up);
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
                if (!interpolateTranslation) {
                    matrix.SetColumn(3, new Vector4(transform.position.x, transform.position.y, transform.position.z, 1.0f));
                }

                matrix *= transform.localToWorldMatrix.inverse;
                mat.SetMatrix("_mat", matrix);
                mat.SetMatrix("_mat_inv", matrix.inverse);
                break;
            
            case InterpolationType.DualQuaternionsNlerp:
                DualQuaternion DualA = DualQuaternion.FromTranslation(A.transform.position)*DualQuaternion.FromRotation(A.transform.rotation);
                DualQuaternion DualB = DualQuaternion.FromTranslation(B.transform.position)*DualQuaternion.FromRotation(B.transform.rotation);
                transform.rotation = DualQuaternion.Nlerp(DualA, DualB, t).rotation();
                if (interpolateTranslation)
                    transform.position = DualQuaternion.Nlerp(DualA, DualB, t).translation();
                break;
            
            case InterpolationType.DualQuaternionsSlerp:
                DualQuaternion DualAS = DualQuaternion.FromTranslation(A.transform.position)*DualQuaternion.FromRotation(A.transform.rotation);
                DualQuaternion DualBS = DualQuaternion.FromTranslation(B.transform.position)*DualQuaternion.FromRotation(B.transform.rotation);
                transform.rotation = DualQuaternion.Slerp(DualAS, DualBS, t).rotation();
                if (interpolateTranslation)
                    transform.position = DualQuaternion.Slerp(DualAS, DualBS, t).translation();
                break;
        }
    }
}