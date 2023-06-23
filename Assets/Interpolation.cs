using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
                // TODO
                break;
        }
    }
}