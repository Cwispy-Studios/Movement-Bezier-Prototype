using UnityEngine;

namespace Utils
{
  public static class FP
  {
    public static bool IsEqualWithin( float val1, float val2, float accuracy = 0.01f )
    {
      if (Mathf.Abs(val1 - val2) <= accuracy)
      {
        return true;
      }

      else return false;
    }

    //public static bool IsEqual( double val1, double val2, double accuracy = 0.001f )
    //{
    //  if (Mathf.Abs(val1 - val2) <= accuracy)
    //  {
    //    return true;
    //  }

    //  else return false;
    //}
  }
}