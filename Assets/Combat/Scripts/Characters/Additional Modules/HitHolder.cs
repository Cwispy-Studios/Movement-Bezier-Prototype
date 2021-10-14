using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitHolder : MonoBehaviour
{
    [SerializeField] public int baseDamage; 
    public Vector3 position;
    [SerializeField] public Vector3 halfSize;

    private void Update()
    {
        position = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, halfSize*2);
    }
}
