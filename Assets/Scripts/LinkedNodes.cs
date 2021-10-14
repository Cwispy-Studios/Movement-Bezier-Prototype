using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class LinkedNode
{
  private BezierSolution.BezierPoint containingPoint;

  public BezierSolution.BezierPoint linkedPoint;
  public LinkedNodeBehaviour linkedNodeBehaviour;

#if UNITY_EDITOR
  public bool Show_Information = true;
#endif

  public LinkedNode( BezierSolution.BezierPoint containingPoint_, BezierSolution.BezierPoint linkedPoint_, LinkedNodeBehaviour linkedNodeBehaviour_, bool linkPointTo )
  {
    containingPoint = containingPoint_;
    linkedPoint = linkedPoint_;
    linkedNodeBehaviour = linkedNodeBehaviour_;

    BezierSolution.BezierPoint givesVariables;
    BezierSolution.BezierPoint receivesVariables;

    if (linkPointTo)
    {
      givesVariables = containingPoint;
      receivesVariables = linkedPoint;
    }

    else
    {
      givesVariables = linkedPoint;
      receivesVariables = containingPoint;
    }

    if (linkedNodeBehaviour.linkYPosition)
    {
      receivesVariables.position = givesVariables.position;
    }

    else
    {
      Vector3 ignoreY = givesVariables.position;
      ignoreY.y = receivesVariables.position.y;

      receivesVariables.position = ignoreY;
    }

    if (linkedNodeBehaviour.linkRotation) receivesVariables.rotation = givesVariables.rotation;

    if (linkedNodeBehaviour.linkScale) receivesVariables.localScale = givesVariables.localScale;

    if (linkedNodeBehaviour.linkControlPoints)
    {
      receivesVariables.precedingControlPointLocalPosition = givesVariables.precedingControlPointLocalPosition;
      receivesVariables.followingControlPointLocalPosition = givesVariables.followingControlPointLocalPosition;
      receivesVariables.handleMode = givesVariables.handleMode;
    }

    if (linkedNodeBehaviour.linkCameraBehaviour) receivesVariables.cameraBehaviour = givesVariables.cameraBehaviour;
  }
}

[System.Serializable]
public class LinkedNodeBehaviour
{
  public bool linkYPosition;
  public bool linkRotation;
  public bool linkScale;
  public bool linkControlPoints;
  public bool linkCameraBehaviour;

  public bool isConnectionPoint { get { return linkYPosition && linkRotation && linkScale; } }

  public void CheckAll()
  {
    linkYPosition = true;
    linkRotation = true;
    linkScale = true;
    linkControlPoints = true;
    linkCameraBehaviour = true;
  }

  public void UncheckAll()
  {
    linkYPosition = false;
    linkRotation = false;
    linkScale = false;
    linkControlPoints = false;
    linkCameraBehaviour = false;
  }
}

[System.Serializable]
public class LinkedNodesManager
{
  [SerializeField]
  private BezierSolution.BezierPoint containingPoint;

  public List<LinkedNode> linkedNodes;

  public int Number_Of_Linked_Points { get { return linkedNodes.Count; } }

#if UNITY_EDITOR

  private LinkedNodesManager()
  {
  }

  public LinkedNodesManager( BezierSolution.BezierPoint containingPoint_ )
  {
    containingPoint = containingPoint_;

    linkedNodes = new List<LinkedNode>();
  }

  public LinkageErrors AddLinkedPoint( BezierSolution.BezierPoint containingPoint_, BezierSolution.BezierPoint pointToAdd, LinkedNodeBehaviour linkedNodeBehaviour, bool linkPointTo )
  {
    containingPoint = containingPoint_;
    // First check if the point is legal

    // Point being added should not be itself
    if (pointToAdd == null)
    {
      return LinkageErrors.NullError;
    }

    if (pointToAdd == containingPoint)
    {
      return LinkageErrors.EqualError;
    }

    if (pointToAdd.Internal_Spline == containingPoint.Internal_Spline)
    {
      return LinkageErrors.SameSplineError;
    }

    if (CheckPointExistsInLinkedNodes(pointToAdd, null))
    {
      return LinkageErrors.ExistsInChainError;
    }

    Undo.RegisterCompleteObjectUndo(containingPoint, "Add New Linked Node");
    Undo.RecordObject(containingPoint.transform, "Add New Linked Node");
    Undo.RegisterCompleteObjectUndo(pointToAdd, "Add New Linked Node");
    Undo.RecordObject(pointToAdd.transform, "Add New Linked Node");

    LinkedNode newLinkedNode = new LinkedNode(containingPoint, pointToAdd, linkedNodeBehaviour, linkPointTo);

    linkedNodes.Add(newLinkedNode);

    // Adds a link to this point with the new linked point as well
    pointToAdd.linkedNodesManager.AddLinkedPoint(pointToAdd, containingPoint, linkedNodeBehaviour, !linkPointTo);

    return LinkageErrors.Success;
  }

  public bool ContainsLinkedPoint( BezierSolution.BezierPoint point )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (point == linkedNodes[i].linkedPoint)
      {
        return true;
      }
    }

    return false;
  }

  public void Revalidate()
  {
    if (linkedNodes == null)
    {
      linkedNodes = new List<LinkedNode>();
    }

    for (int i = linkedNodes.Count - 1; i >= 0; --i)
    {
      if (linkedNodes[i].linkedPoint == null)
      {
        Undo.RecordObject(containingPoint, "Remove deleted linked point");

        linkedNodes.RemoveAt(i);
      }
    }
  }

  public void RemoveLinkedPoint( BezierSolution.BezierPoint pointToRemove )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedPoint == pointToRemove)
      {
        Undo.RecordObject(containingPoint, "Remove Linked Point");

        linkedNodes.RemoveAt(i);

        pointToRemove.linkedNodesManager.RemoveLinkedPoint(containingPoint);

        break;
      }
    }
  }

  public LinkedNode GetLinkedNode( BezierSolution.BezierPoint containingThisPoint )
  {
    LinkedNode linkedNode = null;

    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedPoint == containingThisPoint)
      {
        linkedNode = linkedNodes[i];
        break;
      }
    }

    return linkedNode;
  }

  public BezierSolution.BezierPoint GetConnectionPoint()
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.isConnectionPoint)
      {
        return linkedNodes[i].linkedPoint;
      }
    }

    return null;
  }

  /*********************************************************************** LINKERS ***********************************************************************/

  public void LinkVariable( CameraBehaviour cameraBehaviour, CameraBehaviour valueToChange )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkCameraBehaviour && linkedNodes[i].linkedPoint.cameraBehaviour != cameraBehaviour)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");
        Undo.RecordObject(linkedNodes[i].linkedPoint.cameraBehaviour, "Link Variable");

        valueToChange.CopyFrom(cameraBehaviour);

        linkedNodes[i].linkedPoint.linkedNodesManager.LinkVariable(cameraBehaviour, linkedNodes[i].linkedPoint.cameraBehaviour);
      }
    }
  }

  public void LinkPosition( Vector3 position  )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      Vector3 comparePosition = position;

      if (!linkedNodes[i].linkedNodeBehaviour.linkYPosition)
      {
        comparePosition.y = linkedNodes[i].linkedPoint.position.y;
      }

      if (linkedNodes[i].linkedPoint.position != comparePosition)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.position = comparePosition;
      }
    }
  }

  public void LinkLocalRotation( Quaternion localRotation )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkRotation && linkedNodes[i].linkedPoint.localRotation != localRotation)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.localRotation = localRotation;
      }
    }
  }

  public void LinkRotation( Quaternion rotation )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkRotation && linkedNodes[i].linkedPoint.rotation != rotation)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.localRotation = rotation;
      }
    }
  }

  public void LinkLocalEulerAngles( Vector3 localEulerAngles )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkRotation && linkedNodes[i].linkedPoint.localEulerAngles != localEulerAngles)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.localEulerAngles = localEulerAngles;
      }
    }
  }

  public void LinkEulerAngles( Vector3 eulerAngles )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkRotation && linkedNodes[i].linkedPoint.eulerAngles != eulerAngles)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.eulerAngles = eulerAngles;
      }
    }
  }

  public void LinkLocalScale( Vector3 localScale )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkScale && linkedNodes[i].linkedPoint.localScale != localScale)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint.transform, "Link Variable");

        linkedNodes[i].linkedPoint.localScale = localScale;
      }
    }
  }

  public void LinkPrecedingControlPointLocalPosition( Vector3 precedingControlPointLocalPosition )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkControlPoints && linkedNodes[i].linkedPoint.precedingControlPointLocalPosition != precedingControlPointLocalPosition)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");

        linkedNodes[i].linkedPoint.precedingControlPointLocalPosition = precedingControlPointLocalPosition;
      }
    }
  }

  public void LinkPrecedingControlPointPosition( Vector3 precedingControlPointPosition )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkControlPoints && linkedNodes[i].linkedPoint.precedingControlPointPosition != precedingControlPointPosition)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");

        linkedNodes[i].linkedPoint.precedingControlPointPosition = precedingControlPointPosition;
      }
    }
  }

  public void LinkFollowingControlPointLocalPosition( Vector3 followingControlPointLocalPosition )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkControlPoints && linkedNodes[i].linkedPoint.followingControlPointLocalPosition != followingControlPointLocalPosition)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");

        linkedNodes[i].linkedPoint.followingControlPointLocalPosition = followingControlPointLocalPosition;
      }
    }
  }

  public void LinkFollowingControlPointPosition( Vector3 followingControlPointPosition )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkControlPoints && linkedNodes[i].linkedPoint.followingControlPointPosition != followingControlPointPosition)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");

        linkedNodes[i].linkedPoint.followingControlPointPosition = followingControlPointPosition;
      }
    }
  }

  public void LinkHandleMode( BezierSolution.BezierPoint.HandleMode handleMode )
  {
    for (int i = 0; i < linkedNodes.Count; ++i)
    {
      if (linkedNodes[i].linkedNodeBehaviour.linkControlPoints && linkedNodes[i].linkedPoint.handleMode != handleMode)
      {
        Undo.RecordObject(linkedNodes[i].linkedPoint, "Link Variable");

        linkedNodes[i].linkedPoint.handleMode = handleMode;
      }
    }
  }

  public bool CheckPointExistsInLinkedNodes( BezierSolution.BezierPoint point, BezierSolution.BezierPoint excludeInSearch )
  {
    if (point == excludeInSearch)
    {
      return true;
    }

    else
    {
      for (int i = 0; i < linkedNodes.Count; ++i)
      {
        if (linkedNodes[i].linkedPoint != excludeInSearch)
        {
          if (linkedNodes[i].linkedPoint == point)
          {
            return true;
          }

          else
          {
            // Search within the whole linkage
            if (linkedNodes[i].linkedPoint.linkedNodesManager.CheckPointExistsInLinkedNodes(point, containingPoint))
            {
              return true;
            }
          }
        }
      }
    }

    return false;
  }

#endif
}
 
public enum LinkageErrors
{
  Success = 0,
  NullError = 1,
  EqualError = 2,
  SameSplineError = 3,
  ExistsInChainError = 4
}