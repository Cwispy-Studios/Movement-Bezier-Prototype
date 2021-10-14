using UnityEngine;

public class CameraBehaviour : MonoBehaviour
{
  [SerializeField]
  public float distance, height;

  public void CopyFrom(CameraBehaviour cameraBehaviour)
  {
    distance = cameraBehaviour.distance;
    height = cameraBehaviour.height;
  }

  public bool Equal(CameraBehaviour cameraBehaviour)
  {
    if (distance != cameraBehaviour.distance)
    {
      return false;
    }

    if (height != cameraBehaviour.height)
    {
      return false;
    }

    return true;
  }
}
