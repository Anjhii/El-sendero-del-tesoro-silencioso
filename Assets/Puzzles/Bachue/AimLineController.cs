using UnityEngine;

public class AimLineController : MonoBehaviour
{
    public Transform spawnPoint;         // Mismo spawnPoint del cañón
    public LineRenderer line;            // LineRenderer usado como aimline
    public LayerMask wallMask;           // Walls del escenario
    public float maxDistance = 20f;

    void Update()
    {
        UpdateAimLine();
    }

    void UpdateAimLine()
    {
        if (line == null || spawnPoint == null) return;

        // 🔥 Dirección REAL del disparo (idéntica al shooter)
        Vector3 dir = spawnPoint.forward;
        dir = Vector3.ProjectOnPlane(dir, Vector3.back);
        dir.Normalize();

        Vector3 start = spawnPoint.position;
        Vector3 end = start + dir * maxDistance;

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        // Rebote (1 bounce opcional)
        if (Physics.Raycast(start, dir, out RaycastHit hit, maxDistance, wallMask))
        {
            Vector3 reflect = Vector3.Reflect(dir, hit.normal);
            reflect = Vector3.ProjectOnPlane(reflect, Vector3.back);
            reflect.Normalize();

            Vector3 end2 = hit.point + reflect * (maxDistance * 0.5f);

            line.positionCount = 3;
            line.SetPosition(2, end2);
        }
    }
}
