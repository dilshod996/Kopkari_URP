using UnityEngine;
using System.Collections.Generic;

public class BoosterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject boosterPrefab;
    [SerializeField] private Transform[] points;
    [SerializeField] private int prewarm = 16;
    [SerializeField] private int maxSize = 64;

    private void Awake()
    {
        // Poolni bitta joyda yaratish — GameManager yoki SceneBootstrap’ga ham bo‘ladi
        SimplePool.CreatePool(boosterPrefab, prewarm: prewarm, maxSize: maxSize, expandable: true);
    }

    private void Start()
    {
        foreach (var p in points)
        {
            SpawnAt(p.position, p.rotation);
        }
    }

    public void SpawnAt(Vector3 pos, Quaternion rot)
    {
        SimplePool.Spawn(boosterPrefab, pos, rot);
    }
}
