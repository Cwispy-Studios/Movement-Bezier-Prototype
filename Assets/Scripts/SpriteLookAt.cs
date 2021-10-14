using UnityEngine;

public class SpriteLookAt : MonoBehaviour
{
  [SerializeField] private bool hasLerp = false;
  [SerializeField] private float lerpSpeed = 1f;
  

  private CharacterCameraTrack mainCamera;

  private void Awake()
  {
    mainCamera = Camera.main.GetComponent<CharacterCameraTrack>();
  }

  private void LateUpdate()
  {
    if (mainCamera.transform.hasChanged)
    {
      if (!hasLerp)
      {
        transform.LookAt(mainCamera.transform);
      }

      else
      {
        Vector3 direction = mainCamera.transform.position - transform.position;
        Quaternion toRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, lerpSpeed * Time.deltaTime);
      }
    }
  }
}
