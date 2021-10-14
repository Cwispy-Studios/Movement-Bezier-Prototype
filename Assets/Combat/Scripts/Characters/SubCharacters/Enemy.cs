using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    
#pragma warning disable 0649
    
    [Header("Combat Attributes")] 
    [SerializeField] private int blockStaminaCost;
    [Header("Base Resistance to attacks, lower is stronger Resistance")]
    [Range(0.0f, 1.0f)] 
    public float highResistance; //multiplier of incoming damage depending on height
    [Range(0.0f, 1.0f)] 
    public float lowResistance;

    [Header("Detection of Player")] 
    [SerializeField] private float maxSightDistance;
    
    [Header("Prefabs for projectiles, if any")]
    [SerializeField] private GameObject[] projectiles;
    [SerializeField] private Vector3[] projectileSpawn;
    [SerializeField] private int debugSpawn;

    [Header("Dead Prefab")] 
    [SerializeField] private GameObject deadGameObject;

    private Collider[] _collider_buffer = new Collider[10];

    public HumanPlayer player;

    public bool playerInSight = false;
    
    //temp
    [SerializeField] private AudioClip _oof;
    [SerializeField] private AudioClip _swoosh;
    [SerializeField] private AudioClip _counter;
    [SerializeField] private AudioClip _arrow;
    private AudioSource _audioSource;
    
#pragma warning restore 0649

    private void Awake()
    {
        Init();
        player = FindObjectOfType<HumanPlayer>();
        
        //temp
        _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (curHealth <= 0)
        {
            Instantiate(deadGameObject, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        if (isFacingForward)
            spriteRenderer.flipX = false;
        else
            spriteRenderer.flipX = true;

        PlayerDetection();
    }

    private void PlayerDetection()
    {
        RaycastHit lineOfSight;
        Vector3 myPos = transform.position;
        Vector3 playerDirection = player.transform.position - myPos;

        playerInSight = false;

        if (Physics.Raycast(myPos, playerDirection, out lineOfSight, maxSightDistance))
        {
            if (lineOfSight.transform.CompareTag("Player"))
                playerInSight = true;
        }
        
        animator.SetBool("insight", playerInSight);

        if (playerInSight)
        {
            isFacingForward = Camera.main.WorldToScreenPoint(player.transform.position).x > Camera.main.WorldToScreenPoint(myPos).x;
            Debug.DrawRay(myPos, playerDirection, Color.green);
        }
        else
        {
            Debug.DrawRay(myPos, playerDirection, Color.red);
        }
    }

    public void Hit(HumanPlayer.AttackType attack)
    {
        int dmg = hitHolder[(int) attack].baseDamage;
        if (isFacingForward)
            hitHolders.transform.localRotation = new Quaternion();
        else
            hitHolders.transform.localRotation = Quaternion.Euler(0, 180, 0);
        Vector3 hitLocation = hitHolder[(int) attack].position;
        int total = Physics.OverlapBoxNonAlloc(hitLocation, hitHolder[(int) attack].halfSize,
            _collider_buffer, Quaternion.identity, 512);

        for (int i = 0; i < total; i++)
        {
            if (_collider_buffer[i].CompareTag("Player"))
                if (!_collider_buffer[i].GetComponent<HumanPlayer>().GetHit(hitLocation.y, dmg))
                    animator.SetTrigger("parry");
        }
        
        //temp
        _audioSource.clip = _swoosh;
        _audioSource.Play();
    }

    public void GetHit(float height, int damage)
    {
        float waistHeight = transform.position.y;

        if (curStamina > 0)
        {
            OverrideUseStamina((blockStaminaCost*damage));
            animator.SetTrigger("parry");
            
            //temp
            _audioSource.clip = _counter;
            _audioSource.Play();
        }
        else
        {
            if (height > waistHeight)
            {
                animator.SetTrigger("hurtup");
                Wound((int)(damage*highResistance));
            }
            else
            {
                animator.SetTrigger("hurtup");
                Wound((int)(damage*lowResistance));
            }   
            
            //temp
            _audioSource.clip = _oof;
            _audioSource.Play();
        }
    }

    public void ShootProjectile(int projectile)
    {
        GameObject temp;
        if (isFacingForward)
            temp = Instantiate(projectiles[projectile], transform.position + projectileSpawn[projectile], transform.rotation);
        else
            temp = Instantiate(projectiles[projectile], transform.position - projectileSpawn[projectile], transform.rotation);
        temp.GetComponent<Projectile>().SetReflectDestination(transform.position);
        
        //temp
        _audioSource.clip = _arrow;
        _audioSource.Play();
    }

    private void OnDrawGizmosSelected()
    {
        if (projectiles.Length > 0)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(transform.position + projectileSpawn[debugSpawn], .2f);
        }
    }
}
