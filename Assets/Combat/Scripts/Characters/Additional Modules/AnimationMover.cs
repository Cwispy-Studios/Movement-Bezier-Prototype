using System;
using System.Collections;
using System.Collections.Generic;
using BezierSolution;
using UnityEngine;

public class AnimationMover : MonoBehaviour
{
  private Rigidbody _rigidBody;
  private Character _character;
  private BezierRailWalker _bezierRailWalker;

  private bool _inAnimation;
  [SerializeField] private float currentValue;
  public float baseValue;
  private bool _facingRight; //need to use value set at start of animation

  private void Awake()
  {
    _rigidBody = GetComponentInParent<Rigidbody>();
    _character = GetComponent<Character>();
    _bezierRailWalker = GetComponentInParent<BezierRailWalker>();
  }

  // Update is called once per frame
  public float GetDelta()
  {
    if (_facingRight)
      return currentValue / 2;
    else
      return -(currentValue / 2);
  }

  public void StartAnimationMove()
  {
    Debug.Log("HA");
    _inAnimation = true;
    _bezierRailWalker.animating = _inAnimation;
    baseValue = _bezierRailWalker.NormalizedT;
    _facingRight = _character.isFacingForward;
  }

  public void ReleaseAnimationMove()
  {
    _inAnimation = false;
    _bezierRailWalker.animating = _inAnimation;
  }
}
