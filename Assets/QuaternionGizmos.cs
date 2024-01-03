using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionGizmos : MonoBehaviour {
    public bool enable = false;
    public float size = 1.0f;

    void OnDrawGizmos() {
        if (enable) {
            float ang = Mathf.Acos(transform.rotation.w)*2.0f;
            Vector3 dir = new Vector3(transform.rotation.x, transform.rotation.y, transform.rotation.z).normalized;
            if (dir == Vector3.zero) {
                dir = Vector3.up;
            }

            Gizmos.color = Color.green;
            for (int n = 0; n < 2; n++) {
                Gizmos.DrawRay(transform.position, dir*size);
                Vector3 angDir = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9 ? Vector3.Cross(dir, Vector3.right).normalized : Vector3.Cross(dir, Vector3.up).normalized;
                Gizmos.DrawRay(transform.position+dir*size, angDir*size*0.4f);
                Gizmos.DrawRay(transform.position+dir*size, Quaternion.AngleAxis(ang*Mathf.Rad2Deg, dir)*angDir*size*0.4f);
                int segment = (int)(Mathf.Abs(ang)*Mathf.Rad2Deg/10.0f);
                for (int i = 0; i < segment; i++) {
                    Gizmos.DrawLine(
                        transform.position+dir*size+Quaternion.AngleAxis(ang*Mathf.Rad2Deg*i/segment, dir)*angDir*size*0.4f,
                        transform.position+dir*size+Quaternion.AngleAxis(ang*Mathf.Rad2Deg*(i+1)/segment, dir)*angDir*size*0.4f
                    );
                }

                dir = -dir;
                ang = -ang;
            }
        }
    }
}
