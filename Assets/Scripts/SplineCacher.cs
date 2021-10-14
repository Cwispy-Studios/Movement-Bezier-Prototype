using System.Collections.Generic;
using UnityEngine;

public class SplineCacher : MonoBehaviour
{
  private static BezierSolution.BezierSpline[] allSplines;
  private static List<BezierSolution.BezierSpline> normalSplines = new List<BezierSolution.BezierSpline>();
  private static List<BezierSolution.BezierSpline> platformSplines = new List<BezierSolution.BezierSpline>();
  private static List<BezierSolution.BezierSpline> wallSplines = new List<BezierSolution.BezierSpline>();

  private void Awake()
  {
    allSplines = FindObjectsOfType<BezierSolution.BezierSpline>();

    foreach (BezierSolution.BezierSpline spline in allSplines)
    {
      switch (spline.splineType)
      {
        case BezierSolution.SplineType.Normal:
          normalSplines.Add(spline);
          break;

        case BezierSolution.SplineType.Platform:
          platformSplines.Add(spline);
          break;

        case BezierSolution.SplineType.Wall:
          wallSplines.Add(spline);
          break;
      }
    }
  }

  public static BezierSolution.BezierSpline[] GetAllSplines()
  {
    return allSplines;
  }

  public static BezierSolution.BezierSpline[] GetAllNormalSplines()
  {
    return normalSplines.ToArray();
  }

  public static BezierSolution.BezierSpline[] GetAllPlatformSplines()
  {
    return platformSplines.ToArray();
  }

  public static BezierSolution.BezierSpline[] GetAllWallSplines()
  {
    return wallSplines.ToArray();
  }
}
