using UnityEngine;

[RequireComponent(typeof(BezierSolution.BezierRailWalker))]
public class BezierSlopeBehaviour : MonoBehaviour
{
  // Components
  private BezierSolution.BezierRailWalker bezierRailWalker = null;

  public bool IsRolling { get; private set; } = false;

  private float rollSpeed = 0f;
  private bool rollDirectionIsForward;

  ////////////////////////////////////////////////////////////////////
  // NON-ANIMATION COMPONENTS, DEPRECATED WHEN ANIMATION IMPLEMENTED
  [SerializeField] private float rollTime = 1f;
  private float rollTimer = 0f;

  private void Awake()
  {
    bezierRailWalker = GetComponent<BezierSolution.BezierRailWalker>();
  }

  private void Update()
  {
    if (IsRolling)
    {
      bezierRailWalker.SetRollSpeed(rollSpeed, rollDirectionIsForward);
      rollTimer += Time.deltaTime;

      if (rollTimer >= rollTime)
      {
        IsRolling = false;
      }
    }
  }

  public void SetRolling(float speed, bool rollDirection)
  {
    if (!IsRolling)
    {
      IsRolling = true;
      rollDirectionIsForward = rollDirection;

      rollSpeed = speed;
      rollTimer = 0f;
    }
  }
}
