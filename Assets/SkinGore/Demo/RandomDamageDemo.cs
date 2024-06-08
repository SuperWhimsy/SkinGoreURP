using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class RandomDamageDemo : MonoBehaviour
{
    public float timeBetweenDamage = 1.0f;
    public float minDamage = 0.1f;
    public float maxDamage = 1.0f;
    public float damageRadius = 0.1f;

    private SkinGoreRenderer goreRenderer;
    private Mesh mesh;
    private float timer;

    private void Start()
    {
        goreRenderer = GetComponent<SkinGoreRenderer>();
        mesh = goreRenderer.skin.sharedMesh;
        if (goreRenderer == null)
        {
            UnityEngine.Debug.LogError("SkinGoreRenderer component not found.");
        }
        if (mesh == null)
        {
            UnityEngine.Debug.LogError("Mesh component not found.");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeBetweenDamage)
        {
            ApplyRandomDamage();
            timer = 0f;
        }
    }

    private void ApplyRandomDamage()
    {
        if (goreRenderer != null && mesh != null)
        {
            Vector3 randomPoint = GetRandomPointOnMesh();
            float randomDamage = UnityEngine.Random.Range(minDamage, maxDamage);
            goreRenderer.AddDamage(randomPoint, damageRadius, randomDamage);
        }
    }

    private Vector3 GetRandomPointOnMesh()
    {
        int randomTriangle = UnityEngine.Random.Range(0, mesh.triangles.Length / 3);
        Vector3 vertex1 = mesh.vertices[mesh.triangles[randomTriangle * 3]];
        Vector3 vertex2 = mesh.vertices[mesh.triangles[randomTriangle * 3 + 1]];
        Vector3 vertex3 = mesh.vertices[mesh.triangles[randomTriangle * 3 + 2]];

        Vector3 randomPoint = RandomPointInTriangle(vertex1, vertex2, vertex3);
        return goreRenderer.skin.transform.TransformPoint(randomPoint);
    }

    private Vector3 RandomPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float a = UnityEngine.Random.Range(0f, 1f);
        float b = UnityEngine.Random.Range(0f, 1f);
        if (a + b > 1)
        {
            a = 1 - a;
            b = 1 - b;
        }
        float c = 1 - a - b;
        return a * v1 + b * v2 + c * v3;
    }
}
