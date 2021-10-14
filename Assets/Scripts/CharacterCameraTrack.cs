using UnityEngine;

public class CharacterCameraTrack : MonoBehaviour
{
  private float distanceFromChar = 10f;
  private float heightLevel = 10f;
  private const float BASE_HEIGHT = 0f;

  [SerializeField]
  private BezierSolution.BezierRailWalker playerCharacterWalker = null;

  private void Awake()
  {
    
  }

  private void LateUpdate()
  {
    if (!playerCharacterWalker.InTransition)
    {
      var bezierPointTuple = playerCharacterWalker.GetCorrespondingPoints();

      distanceFromChar = Mathf.Lerp(bezierPointTuple.point1.cameraBehaviour.distance, bezierPointTuple.point2.cameraBehaviour.distance, bezierPointTuple.t);
      heightLevel = Mathf.Lerp(bezierPointTuple.point1.cameraBehaviour.height, bezierPointTuple.point2.cameraBehaviour.height, bezierPointTuple.t);

      Vector3 trackPos = playerCharacterWalker.transform.position;
      Vector3 offsetVec = playerCharacterWalker.GetForwardDirection() * Vector3.right;

      trackPos += offsetVec * distanceFromChar;
      trackPos.y = heightLevel;

      transform.position = Vector3.Lerp(transform.position, trackPos, 0.08f);

      Quaternion currentRotation = transform.rotation;

      transform.LookAt(playerCharacterWalker.transform.position + (Vector3.up * BASE_HEIGHT));
      transform.rotation = Quaternion.Lerp(currentRotation, transform.rotation, 0.25f);
    }
  }
}
