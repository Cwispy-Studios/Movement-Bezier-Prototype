using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BezierSolution;
using UnityEngine;

public class ArcherEnemyBehaviour : MonoBehaviour
{
    [SerializeField] private float treeRange;
    [SerializeField] private float engagedRange;
    [SerializeField] private float transitionTime;
    
    private Vector3[] _inRangeTrees;
    private float _curPlayerDistance;

    private bool _moving;
    private bool _hiding;

    private float _startTime;
    private float _endTime;
    private Vector3 _startPosition;
    private Vector3 _curGoalPosition;

    private Enemy _enemy;
    private Anchor _lastAnchor;
    private bool _hasLastAnchor;

    private bool _regenerating;

    private BezierRailWalker _bezierRailWalker;
    private Vector3 _parentOffset;
    private Vector3 _archerDistance;

    private Collider[] _fellowEnemies = new Collider[3];

    // Start is called before the first frame update
    void Start()
    {
        _enemy = GetComponent<Enemy>();
        _bezierRailWalker = GetComponentInParent<BezierRailWalker>();
        _parentOffset = transform.localPosition;
        _archerDistance = new Vector3(engagedRange / 2, engagedRange / 2, engagedRange / 2);
    }

    // Update is called once per frame
    void Update()
    {
        _curPlayerDistance = Vector3.Distance(transform.position, _enemy.player.transform.position);

        if (_hiding && !_moving)
        {
            if (_curPlayerDistance <= engagedRange)
            {
                MoveTree();
            }
            else if (_enemy.curStamina == 0)
            {
                MoveRoad();
            }
        }
        else if (!_hiding && !_moving)
        {
            if (_enemy.curStamina < _enemy.maxStamina)
            {
                if (_curPlayerDistance <= engagedRange)
                {
                    _enemy.animator.SetBool("inrange", true);
                }
                else
                {
                    _enemy.animator.SetBool("inrange", false);
                }
            }
            else
            {
                MoveTree();
            }
        }
        else if (_moving)
            MovePosition();
    }

    void MoveTree()
    {
        Anchor temp = Anchor.FindFurthestAnchor("ArcherAnchor", transform.position, treeRange);
        _curGoalPosition = temp.location;
        _enemy.animator.SetTrigger("movetree");
        _moving = true;
        GetComponent<CapsuleCollider>().enabled = false;

        _hiding = true;

        _startTime = Time.time;
        _endTime = _startTime + transitionTime;
        _startPosition = transform.position;

        temp.occupied = true;
        
        if (_hasLastAnchor)
            _lastAnchor.occupied = false;
        else
            _hasLastAnchor = true;

        _lastAnchor = temp;
    }

    void MoveRoad()
    {
        _curGoalPosition = FindClosestSpline();
        _enemy.animator.SetTrigger("movetoroad");
        _moving = true;
        GetComponent<CapsuleCollider>().enabled = false;

        _hiding = false;

        _startTime = Time.time;
        _endTime = _startTime + transitionTime;
        _startPosition = transform.position;

        _lastAnchor.occupied = false;
        _hasLastAnchor = false;
    }

    void StopMove()
    {
        _moving = false;
        GetComponent<CapsuleCollider>().enabled = true;

        if (_hiding)
        {
            _enemy.animator.SetTrigger("arrivedtree");
        }
        else
        {
            _enemy.animator.SetTrigger("arrivedroad");
        }
    }

    void MovePosition()
    {
        float t = (Time.time - _startTime) / transitionTime;

        if (t <= 1)
            transform.position = Vector3.Lerp(_startPosition, _curGoalPosition, t);
        else
            StopMove();
    }

    //change later for spline
    Vector3 FindClosestSpline()
    {
        Vector3 newPos = _bezierRailWalker.GetMoveClosestSpline(transform.position) + _parentOffset;

        return newPos;
    }

    //call as event in animation
    public void StaminaRegen()
    {
        if (!_regenerating)
        {
            StartCoroutine(Restamina());
            _regenerating = true;
        }
    }

    private IEnumerator Restamina()
    {
        yield return new WaitForSeconds(5);
        _enemy.RegenerateStamina(_enemy.maxStamina/2);
        _regenerating = false;
    }

    public void CheckEnemies()
    {
        int total = Physics.OverlapBoxNonAlloc(transform.position, _archerDistance, _fellowEnemies, Quaternion.identity, 512);

        for (int i = 0; i < total; i++)
        {
            if (_fellowEnemies[i].CompareTag("Enemy"))
            {
                if (_fellowEnemies[i].gameObject != gameObject)
                {
                    int temp = Random.Range(0, 2);
                    if (temp == 0)
                        _enemy.animator.SetTrigger("goaround");
                    else
                        _enemy.animator.SetTrigger("retreat");
                    break;
                }
            }
        }
    }
}
