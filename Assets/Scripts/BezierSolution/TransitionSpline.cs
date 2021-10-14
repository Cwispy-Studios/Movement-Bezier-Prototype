using UnityEngine;

[System.Serializable]
public class TransitionSpline
{
  [SerializeField]
  [HideInInspector]
  // Inspector property, to show or hide following properties in Inspector
  public bool showProperties;

  [SerializeField]
  [HideInInspector]
  // Which key to press to transition to the destination spline
  private TransitionKey m_transitionKey;
  public TransitionKey transitionKey
  {
    get
    {
      return m_transitionKey;
    }

    set
    {
      m_transitionKey = value;
    }
  }

  [SerializeField]
  [HideInInspector]
  private Vector3 minTransitionPosition, maxTransitionPosition;

  [SerializeField]
  [HideInInspector]
  // Normalised T region where player can transition to next spline
  private float m_minTransitionNormalisedT;
  public float minTransitionNormalisedT
  {
    get
    {
      return m_minTransitionNormalisedT;
    }

    set
    { 
      m_minTransitionNormalisedT = Mathf.Clamp(value, 0, m_maxTransitionNormalisedT);
    }
  }

  [SerializeField]
  [HideInInspector]
  // Normalised T region where player can transition to next spline
  private float m_maxTransitionNormalisedT;
  public float maxTransitionNormalisedT
  {
    get
    {
      return m_maxTransitionNormalisedT;
    }

    set
    {
      m_maxTransitionNormalisedT = Mathf.Clamp(value, m_minTransitionNormalisedT, 1f);
    }
  }

  [SerializeField]
  [HideInInspector]
  // Point where player has to be at first before it is able to change spline (this point contacts both splines)
  private BezierSolution.BezierPoint m_contactPoint;
  public BezierSolution.BezierPoint contactPoint
  {
    get
    {
      return m_contactPoint;
    }

    set
    {
      m_contactPoint = value;
    }
  }

  [SerializeField]
  [HideInInspector]
  // Spline the player will transition to
  private BezierSolution.BezierSpline m_destinationSpline;
  public BezierSolution.BezierSpline destinationSpline
  {
    get
    {
      return m_destinationSpline;
    }

    set
    {
      m_destinationSpline = value;
    }
  }

  [SerializeField]
  [HideInInspector]
  // Normalised T of the spline to transition to
  private float m_destinationT;
  public float destinationT
  {
    get
    {
      return m_destinationT;
    }

    set
    {
      m_destinationT = value;
    }
  }

  public bool NormalisedTIsInRegion(float normalisedT)
  {
    if (normalisedT >= m_minTransitionNormalisedT && normalisedT <= m_maxTransitionNormalisedT)
    {
      return true;
    }

    else
    {
      return false;
    }
  }
}

public enum TransitionKey
{
  UP = 0,
  DOWN
}