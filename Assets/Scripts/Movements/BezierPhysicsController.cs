using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BezierPhysicsController : MonoBehaviour
{
  private BezierSolution.BezierRailWalker bezierWalker;
  private Rigidbody rigidBody;

  private Vector3 finalTargetPosition;
  private Vector3 consolidatedTargetPosition;
  private Vector3 positionOffset;

  private List<Dictionary<GameObject, int>> externalPositionOffsets;

  private bool awaitingChanges = false;
  private bool changesSinceTempCalculation = false;

  private void Awake()
  {
    bezierWalker = GetComponent<BezierSolution.BezierRailWalker>();
    rigidBody = GetComponent<Rigidbody>();
  }

  private void FixedUpdate()
  {
    if (ConsolidateForces())
    {
      //bezierWalker.UpdateInAirStatus();
      rigidBody.MovePosition(consolidatedTargetPosition);
    }
  }

  private bool ConsolidateForces()
  {
    bool hasChanges = positionOffset != Vector3.zero || consolidatedTargetPosition != finalTargetPosition;

    consolidatedTargetPosition += positionOffset;
    consolidatedTargetPosition = finalTargetPosition;

    positionOffset = Vector3.zero;

    awaitingChanges = false;
    changesSinceTempCalculation = false;

    return hasChanges;
  }

  //private Vector3 ConsolidateTempForces()
  //{
  //  if (changesSinceTempCalculation)
  //  {
  //    finalTargetPosition = consolidatedTargetPosition;
  //    finalTargetPosition += positionOffset;

  //    changesSinceTempCalculation = false;

  //    return finalTargetPosition;
  //  }

  //  else
  //  {
  //    return finalTargetPosition;
  //  }
  //}

  public void SetTargetBezierPosition(Vector3 targetBezierPosition, float previousGroundHeight, bool placeOnGround = false)
  {
    // Save this as it may be a different value from jumping
    float currentY = finalTargetPosition.y;
    float newGroundHeight = targetBezierPosition.y;

    Vector3 newPosition = targetBezierPosition;
    newPosition.y = placeOnGround ? newGroundHeight : (currentY > previousGroundHeight ? currentY : newGroundHeight);

    finalTargetPosition = newPosition;
  }

  public void SnapToGround()
  {
    finalTargetPosition.y = bezierWalker.GroundHeight;
  }

  public void AddHeightOffset(float y)
  {
    finalTargetPosition.y += y;

    //tempTargetPosition.y = Mathf.Clamp(tempTargetPosition.y, bezierWalker.GroundHeight, tempTargetPosition.y);
  }

  // Mainly used for forces controlled by external scripts, such as jumping
  public void AddPositionOffset(Vector3 offset)
  {
    positionOffset += offset;

    //awaitingChanges = true;
    //changesSinceTempCalculation = true;
  }

  // Mainly used for forces controlled by external scripts, such as jumping
  public void AddPositionOffset( float x, float y, float z )
  {
    positionOffset.x += x;
    positionOffset.y += y;
    positionOffset.z += z;

    awaitingChanges = true;
    changesSinceTempCalculation = true;
  }

  public void AddDissipatingForce(Vector3 force, float magnitudeDissipationRate)
  {
    awaitingChanges = true;
    changesSinceTempCalculation = true;
  }

  public Vector3 GetCurrentPosition()
  {
    //if (awaitingChanges)
    //{
    //  return ConsolidateTempForces();
    //}

    //else
    //{
      return consolidatedTargetPosition;
    //}
  }

  public Vector3 GetFinalPosition()
  {
    return finalTargetPosition;
  }

  public bool CheckObjectWillBeAboveHeight(float checkY)
  {
    return finalTargetPosition.y > checkY;
  }

  public bool CheckObjectCurrentlyAboveHeight( float checkY )
  {
    return consolidatedTargetPosition.y > checkY;
  }
}
