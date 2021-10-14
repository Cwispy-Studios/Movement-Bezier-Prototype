using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Vector3 _heading;
    [SerializeField] private int damage;
    [SerializeField] private float speed;
    [SerializeField] private float dieTime;

    private float _deadTime;

    private HumanPlayer _player;

    private Vector3 _reflectDestination;

    private void Start()
    {
        _player = FindObjectOfType<HumanPlayer>();
        _heading = (_player.transform.position + new Vector3(0, .5f, 0) - transform.position).normalized;
        transform.LookAt(transform.position + _heading, Vector3.up);
        _deadTime = Time.time + dieTime;
    }

    public void SetReflectDestination(Vector3 dest)
    {
        _reflectDestination = dest;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += _heading * speed;

        if (_deadTime < Time.time)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (!_player.GetHit(transform.position.y, damage))
                Deflect();
            else
                Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponent<Enemy>().GetHit(transform.position.y, damage);
            Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("Dead"))
        {
            //do nothing
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Deflect()
    {
        _heading = (_reflectDestination + new Vector3(0, .5f, 0) - transform.position).normalized;;
        transform.LookAt(transform.position + _heading, Vector3.up);
    }
}
