using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Anchor : MonoBehaviour
{
    public static Anchor FindRandomAnchor(string tag, Vector3 position, float range)
    {
        List<Anchor> locations = GetAvailableAnchors(tag, position, range);

        return locations[Random.Range(0, locations.Count)];
    }

    private static List<Anchor> GetAvailableAnchors(string tag, Vector3 position, float range)
    {
        Collider[] colliders = Physics.OverlapSphere(position, range, 256);
        List<Anchor> possibleLocations = new List<Anchor>();
        
        foreach (var col in colliders)
        {
            Anchor temp = col.GetComponent<Anchor>();
            if (col.CompareTag(tag) && !temp.occupied)
            {
                possibleLocations.Add(temp);
            }
        }

        return possibleLocations;
    }

    public static Anchor FindFurthestAnchor(string tag, Vector3 position, float range)
    {
        List<Anchor> locations = GetAvailableAnchors(tag, position, range);

        int id = 0;
        float largestDistance = 0;
        for (int i = 0; i < locations.Count; i++)
        {
            float curDistance = Vector3.Distance(position, locations[i].location);
            if (curDistance > largestDistance)
            {
                largestDistance = curDistance;
                id = i;
            }
        }

        return locations[id];
    }

    public bool occupied;
    public Vector3 location;

    private void Awake()
    {
        Physics.IgnoreLayerCollision(8, 9);
        location = transform.position;
    }
}
