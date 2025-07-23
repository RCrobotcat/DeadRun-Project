using System.Collections.Generic;
using UnityEngine;

public class Buoyancy : MonoBehaviour
{
    [SerializeField] private int slicesPerAxis = 2;
    [SerializeField] private int voxelsLimit = 16;

    [SerializeField] private float density = 500;

    private float VoxelHalfHeight { get; set; }

    private const float waterDensity = 1000;

    // Damping coefficient
    private const float DAMPFER = 0.1f;

    private Vector3 LocalArchimedesForce { get; set; }

    [HideInInspector] public List<Vector3> Voxels { get; set; }
    private bool IsMeshCollider { get; set; }
    private Rigidbody rb { get; set; }
    private Collider collider { get; set; }

    [HideInInspector] public List<Vector3[]> Forces { get; set; }

    public float waterHeight = 7f;
    public bool isCreature = false;

    void Start()
    {
        InitializeComponents();
        CalculateVoxelHalfHeight();
        SetUpRigidBody();
        CreateVoxels();
        CalculateArchimedesForce();
    }

    void FixedUpdate()
    {
        if (!isCreature)
        {
            Forces.Clear();
            foreach (var point in Voxels)
            {
                ApplyBuoyancyForce(point);
            }
        }
    }

    void InitializeComponents()
    {
        Forces = new List<Vector3[]>();
        collider = GetComponent<Collider>() ?? gameObject.AddComponent<MeshCollider>();
        IsMeshCollider = collider is MeshCollider;
        rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
    }

    void CalculateVoxelHalfHeight()
    {
        var bounds = collider.bounds;
        VoxelHalfHeight = Mathf.Min(bounds.size.x, bounds.size.y, bounds.size.z) / (2 * slicesPerAxis);
    }

    void SetUpRigidBody()
    {
        var bouns = collider.bounds;
        rb.centerOfMass = transform.InverseTransformPoint(bouns.center);
    }

    Vector3 CalculateVoxelPoint(Bounds bounds, int ix, int iy, int iz)
    {
        float x = bounds.min.x + bounds.size.x / slicesPerAxis * (0.5f + ix);
        float y = bounds.min.y + bounds.size.y / slicesPerAxis * (0.5f + iy);
        float z = bounds.min.z + bounds.size.z / slicesPerAxis * (0.5f + iz);
        return transform.InverseTransformPoint(new Vector3(x, y, z));
    }

    List<Vector3> SliceConvex()
    {
        var points = new List<Vector3>();
        var bounds = collider.bounds;
        for (int ix = 0; ix < slicesPerAxis; ix++)
        {
            for (int iy = 0; iy < slicesPerAxis; iy++)
            {
                for (int iz = 0; iz < slicesPerAxis; iz++)
                {
                    points.Add(CalculateVoxelPoint(bounds, ix, iy, iz));
                }
            }
        }

        return points;
    }

    // Merge the closest points to reach the target count
    private static void WeldPoints(IList<Vector3> list, int targetCount)
    {
        if (list.Count <= 2 || targetCount < 2)
            return;

        while (list.Count > targetCount)
        {
            int first, second;
            FindClosestPoints(list, out first, out second);

            var mixed = (list[first] + list[second]) * 0.5f;
            list.RemoveAt(second); // Remove the larger index first
            list.RemoveAt(first); // Remove the smaller index next
            list.Add(mixed);
        }
    }

    private static void FindClosestPoints(IList<Vector3> list, out int firstIndex, out int secondIndex)
    {
        float minDistance = float.MaxValue;
        firstIndex = 0;
        secondIndex = 1;

        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                float distance = Vector3.Distance(list[i], list[j]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    firstIndex = i;
                    secondIndex = j;
                }
            }
        }
    }

    void CreateVoxels()
    {
        Voxels = SliceConvex();
        WeldPoints(Voxels, voxelsLimit);
    }

    // F = ρ * V * g
    void CalculateArchimedesForce()
    {
        float volume = rb.mass / density;
        float ArchimedesForceMagnitude = waterDensity * Mathf.Abs(Physics.gravity.y) * volume;
        LocalArchimedesForce = new Vector3(0, ArchimedesForceMagnitude, 0) / Voxels.Count;
    }

    float GetWaterLevel()
    {
        return waterHeight - transform.position.y - 0.5f;
    }

    public void ApplyBuoyancyForce(Vector3 point)
    {
        var worldPoint = transform.TransformPoint(point);
        float waterLevel = GetWaterLevel();

        if (worldPoint.y - VoxelHalfHeight < waterLevel)
        {
            float k = Mathf.Clamp01((waterLevel - worldPoint.y) / (2 * VoxelHalfHeight) + 0.5f);
            var velocity = rb.GetPointVelocity(worldPoint);
            var localDampingForce = -velocity * DAMPFER * rb.mass; // Damping force
            var force = localDampingForce * k + LocalArchimedesForce; // Buoyancy force
            rb.AddForceAtPosition(force, worldPoint);

            if (SoundController.Instance != null && !SoundController.Instance.sfxSource_water.isPlaying)
                SoundController.Instance.PlayWaterSplash(0.5f, 2f);

            Forces.Add(new[] { worldPoint, force });
        }
    }

    private void OnDrawGizmos()
    {
        if (Voxels == null || Forces == null)
            return;

        const float gizmoSize = 0.1f;
        Gizmos.color = Color.yellow;
        foreach (var p in Voxels)
        {
            Gizmos.DrawCube(transform.TransformPoint(p), Vector3.one * gizmoSize);
        }

        Gizmos.color = Color.cyan;
        foreach (var force in Forces)
        {
            Gizmos.DrawCube(force[0], Vector3.one * gizmoSize); // Draw Buoyancy position
            Gizmos.DrawLine(force[0], force[0] + force[1] / rb.mass); // Draw Buoyancy direction
        }
    }
}