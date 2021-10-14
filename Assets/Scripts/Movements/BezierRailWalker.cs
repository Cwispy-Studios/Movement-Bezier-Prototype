using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BezierSolution
{
  [RequireComponent(typeof(BezierPhysicsController))]
  public class BezierRailWalker : BezierWalker
	{
    // Movement components
    private BezierPhysicsController physicsController = null;

    private BezierSlopeBehaviour bezierSlopeBehaviour;
    private bool canRoll = false;

    // Component variables
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float runWindupTime = 0.75f;
    [SerializeField] private float runAccelerationTime = 2.25f;

    private bool atMaxSpeed = false;
    private float timeSpentMoving = 0;

    [Header("In Air Movement")]
    [SerializeField] private float airFrictionModifier = 1.8f;
    [SerializeField] private float airMoveModifier = 0.8f;
    [SerializeField] private float airBoostModifier = 0.7f;

    [Header("Jumping")]
    [SerializeField] private float standardJumpHeight = 2f;
    [SerializeField] private float maxJumpHeight = 2.5f;
    [SerializeField] private float gravity = 30f;
    private float terminalVelocity = 10f;

    [Header("Wall Movement")]
    [SerializeField] private float minRollAngle = 25f;
    [SerializeField] private float minRunnableAngle = 50f;
    [SerializeField] private float maxRunnableAngle = 70f;
    [SerializeField] private float wallStickDuration = 0.5f;
    [SerializeField] private float wallSlideAcceleration = 1f;
    private float wallStickTimer = 0f;
    private float wallSlideSpeed = 0f;

    private float slideAcceleration = 2f;
    private float slideSpeed = 0f;

    private float initialJumpVelocity;

    private bool inAir = false;
    private bool firstContactGround = false;

    private float airFriction;
    private float airMoveForce;
    private float airBoost;

    private float moveSpeed;
    private float finalSpeed;

    private float airAcceleration = 0f;
    private bool firstDirectionMoveMadeInAir = false;
    private float airMoveSpeed;
    private float airMoveSpeedLimit;

    public float GroundHeight { get; private set; }

    // Transitions
    public bool InTransition { get; private set; } = false;
    private float beforeTransitionT;
    private BezierSpline beforeTransitionSpline;

    // Wall spline
    private BezierPoint oppositeWallPoint = null;
    private BezierPoint oppositeLinkedWallPoint = null;
    private bool isWallHanging = false;
    private bool IsWallHanging
    {
      get
      {
        return isWallHanging;
      }

      set
      {
        if (value == true && isWallHanging == false)
        {
          wallStickTimer = 0f;
        }

        isWallHanging = value;
      }
    }
    // Sliding down wall
    private bool isWallSliding = false;
    // Jumping from isWallHanging or isWallSliding
    private bool isWallJumping = false;
    // Sliding down a steep incline
    private bool isSliding = false;
    
    private bool forwardMovesTowardsWall;

    // Holds the previous spline the user was on
    // Resets to null when a new input action is made
    private BezierSpline previousSpline = null;
    // Holds the last non-wall spline, used when leaving a right angled wall
    private BezierSpline lastWalkableSpline = null;

    private BezierSpline doNotSwitchTo = null;

    [Space(8)]
    public BezierSpline spline;
    private Vector3 m_targetBezierPosition;
    private Vector3 targetBezierPosition
    {
      get
      {
        return m_targetBezierPosition;
      }

      set
      {
        m_targetBezierPosition = value;
        GroundHeight = value.y;
      }
    }
    private float currentAngleOfIncline = 0;
    private bool forwardMovesUpSlope;

    [SerializeField]
		[Range( 0f, 1f )]
		private float m_normalizedT = 0f;

		public override BezierSpline Spline { get { return spline; } }

		public override float NormalizedT
		{
			get { return m_normalizedT; }
			set { m_normalizedT = value; }
		}

		private bool isGoingForward = true;
		public override bool MovingForward { get { return ( finalSpeed > 0f ) == isGoingForward; } }

    // Spline data
    public TransitionSpline[] transitionData;
    public List<TransitionSpline> validTransitions = new List<TransitionSpline>();
    public TransitionSpline currentlyTransitioning;

    public struct BezierPointTuple
    {
      public readonly BezierPoint point1, point2;
      public readonly float t;

      public BezierPointTuple(BezierPoint point1, BezierPoint point2, float t)
      {
        this.point1 = point1;
        this.point2 = point2;
        this.t = t;
      }
    }

    /************************************************ UNITY FUNCTIONS ************************************************/

    private void Awake()
    {
      InitialiseMovementComponents();
      
      //added by tim
      _moveInput = FindObjectOfType<MoveInput>();
      _animationMover = GetComponentInChildren<AnimationMover>();

      moveSpeed = baseSpeed;

      airFriction = baseSpeed * airFrictionModifier;
      airMoveForce = (baseSpeed * airMoveModifier) + airFriction;
      airBoost = baseSpeed * airBoostModifier;

      UpdateSplineInformation();
    }

    private void Start()
    {
      InitialisePosition();
    }

    private void Update()
    {
      HandleMovements();
      CalculateNewPosition();
      ContactGround();

      // Reset
      finalSpeed = 0;
    }

    /************************************************ PUBLIC FUNCTIONS ************************************************/

    public Quaternion GetForwardDirection()
    {
      return Quaternion.LookRotation(spline.GetTangent(m_normalizedT));
    }

    // Deprecated when animation is implemented
    public void SetRollSpeed(float speed, bool rollDirection)
    {
      finalSpeed = speed;
      isGoingForward = rollDirection;
    }

    public void FirstMove()
    {
      doNotSwitchTo = null;

      if (inAir)
      {
        if (!firstDirectionMoveMadeInAir && airMoveSpeed == 0)
        {
          isGoingForward = _moveInput.GetMoveHorizontal() >= 0 ? true : false;

          firstDirectionMoveMadeInAir = true;
        }

        if ((_moveInput.GetMoveHorizontal() > 0 && isGoingForward) || (_moveInput.GetMoveHorizontal() < 0 && !isGoingForward))
        {
          airMoveSpeed += airBoost;
        }
      }

      else
      {
        timeSpentMoving = 0;
      }
    }

    public void Move()
    {
      if (!IsWallHanging && !GetRollingStatus())
      {
        if (!InTransition)
        {
          if (!inAir)
          {
            GroundMove();
          }

          else
          {
            InAirMove();
          }
        }

        else
        {
          GroundMove(true);
        }
      }
    }

    public void StopMove()
    {
      moveSpeed = baseSpeed;
      timeSpentMoving = 0;
      atMaxSpeed = false;

      if (currentAngleOfIncline >= minRunnableAngle && currentAngleOfIncline <= maxRunnableAngle)
      {
        isSliding = true;
      }
    }

    public void Jump()
    {
      bool allowedToJump = !InTransition && !inAir && !GetRollingStatus();

      // If player is on the ground
      if (allowedToJump)
      {
        doNotSwitchTo = null;

        // Player jumps higher the faster they run
        float t = (moveSpeed - baseSpeed) / (runSpeed - baseSpeed);
        float currentJumpHeight = Mathf.Lerp(standardJumpHeight, maxJumpHeight, t);
        initialJumpVelocity = Mathf.Sqrt(-2 * -gravity * currentJumpHeight);

        airAcceleration += initialJumpVelocity;
        physicsController.AddHeightOffset(airAcceleration * Time.deltaTime);
      }
    }

    public bool JumpOffPlatform()
    {
      doNotSwitchTo = null;

      if (spline.splineType == SplineType.Platform)
      {
        BezierSpline temp = spline;

        if (CheckForSplineBelow(MovingForward, true))
        {
          Debug.Log("Jump off");
          return true;
        }

        Debug.Log("No Spline below");
      }

      Debug.Log("Not on platform");

      return false;
    }

    public void StopJump()
    {
      if (inAir)
      {
        airAcceleration = airAcceleration > 0 ? 0 : airAcceleration;
      }
    }

    public BezierPointTuple GetCorrespondingPoints()
    {
      var pointTupleIndex = spline.GetNearestPointIndicesTo(m_normalizedT);

      return new BezierPointTuple(spline[pointTupleIndex.index1], spline[pointTupleIndex.index2], pointTupleIndex.t);
    }

    public void CheckTransitSplines( TransitionKey key )
    {
      // There is another spline the player can currently transit to
      if (validTransitions.Count > 0)
      {
        if (!inAir)
        {
          // Check if the key being pressed matches with any of the valid transitions
          for (int i = 0; i < validTransitions.Count; ++i)
          {
            if (key == validTransitions[i].transitionKey)
            {
              if (!InTransition || (InTransition && validTransitions[i].transitionKey != currentlyTransitioning.transitionKey))
              {
                currentlyTransitioning = validTransitions[i];
                beforeTransitionSpline = spline;
                beforeTransitionT = m_normalizedT;
                InTransition = true;
              }

              // Transition using this transition spline
              StopAllCoroutines();
              StartCoroutine("StartTransition", currentlyTransitioning);
            }
          } // for
        }
      } // if validTransitions Count
    } // func

    public void CancelTransitSpline()
    {
      if (InTransition)
      {
        StopAllCoroutines();
        StartCoroutine("CancelTransition", currentlyTransitioning);
      }
    }

    public override Vector3 Execute(float deltaTime)
    {
      float targetSpeed = (isGoingForward) ? finalSpeed : -finalSpeed;
      targetSpeed *= deltaTime;
      
      // Used only when player is on the wall and cannot transition
      bool doNotChangeSpline = false;
      bool movingForward = MovingForward;

      float distanceTraveled = 0f;

      if (spline.splineType != SplineType.Wall)
      {
        float tempT = m_normalizedT;

        // If in air, moving along the spline should ignore the y-axis until it hits the ground
        if (inAir)
        {
          Vector3 tempPos = spline.MoveThroughSplineInAir(ref tempT, targetSpeed, out distanceTraveled, out forwardMovesUpSlope);

          // InAirPhysics is calculated first, so the object's y-position will be up to date here
          // Check the y position if it is above or on/below the new ground height
          if (physicsController.CheckObjectWillBeAboveHeight(tempPos.y))
          {
            // Player can freely move through the spline
            targetBezierPosition = tempPos;
            m_normalizedT = tempT;
          }

          // Object has contacted the ground
          else
          {
            Debug.Log("HIT " + physicsController.GetFinalPosition().y.ToString("F10") + " " + tempPos.y.ToString("F10"));
            
            Vector3 actualFinalPos = new Vector3(tempPos.x, physicsController.GetFinalPosition().y, tempPos.z);
            targetBezierPosition = spline.FindIntersectionPointBetweenTwoPosT(physicsController.GetCurrentPosition(), actualFinalPos, m_normalizedT, tempT, out m_normalizedT);
            currentAngleOfIncline = spline.GetAngleOfIncline(m_normalizedT, ref forwardMovesUpSlope);

            Debug.Log("HIT After intersection " + physicsController.GetFinalPosition().y.ToString("F10") + " " + GroundHeight.ToString("F10"));
          }
        }

        // Move along spline normally, but check for incline first
        else
        {
          Vector3 tempPos = spline.RailWalkerMoveAlongSpline(ref tempT, targetSpeed, out distanceTraveled, out currentAngleOfIncline, out forwardMovesUpSlope);

          //Debug.Log(currentAngleOfIncline);

          // Check if object can walk up the spline
          if (currentAngleOfIncline < minRunnableAngle)
          {
            // As per normal
            targetBezierPosition = tempPos;
            m_normalizedT = tempT;

            isSliding = false;
          }
          
          // Steep incline
          else if (currentAngleOfIncline >= minRunnableAngle && currentAngleOfIncline <= maxRunnableAngle)
          {
            // Can only run up these angles
            if (atMaxSpeed || isSliding)
            {
              // As per normal
              targetBezierPosition = tempPos;
              m_normalizedT = tempT;
            }
          }

          // Very steep incline, have to jump up
          else
          {
            // Stuck
          }
        }
      }

      // Wall spline
      else
      {
        if (IsWallHanging)
        {
          doNotChangeSpline = true;
          // Do nothing
        }

        else if (isWallSliding)
        {
          // Find the point indices where the object is at, might be able to shift all this to a BezierSpline function
          var indices = spline.GetNearestPointIndicesTo(m_normalizedT);
          BezierPoint referencePoint = spline[indices.index1];

          targetBezierPosition = spline.MoveAlongSpline(ref m_normalizedT, targetSpeed, 1);

          if (m_normalizedT <= 0f || m_normalizedT >= 1f)
          {
            isWallSliding = false;
          }
        }

        // Not on wall, in air or running on it
        else
        {
          float tempT = m_normalizedT;
          Vector3 tempPos;

          // Object is currently in the air
          if (inAir)
          {
            if (isWallJumping)
            {
              isWallJumping = false;
              m_normalizedT = isGoingForward ? 1f : 0f;
            }

            else
            {
              // Right angled wall, simply need to check if the object is above the other point

              // Check for specific scenarios. Right angled walls SHOULD ALWAYS CONSIST OF ONLY 2 POINTS
              // From there, the next point either connects to nothing, or a new spline, we need to find out which is it
              if (oppositeLinkedWallPoint == null)
              {
                // This connects to nothing, so object should not be able to go over object
                tempPos = physicsController.GetFinalPosition();
              }

              else
              {
                // This connects to a new spline
                tempPos = oppositeWallPoint.position;
                tempT = oppositeWallPoint.tPosition;
              }

              // Check if above the current wall
              if (physicsController.CheckObjectWillBeAboveHeight(tempPos.y))
              {
                // Target bezier position then snaps to the next point
                targetBezierPosition = tempPos;
                m_normalizedT = tempT;
              }

              // Object has contacted the wall
              else
              {
                // Hang on wall
                GroundHeight = physicsController.GetFinalPosition().y;

                IsWallHanging = true;
                doNotChangeSpline = true;
              }
            }
          }

          // Object is already on the ground along the wall
          else
          {
            // Prevents wall hanging when player runs into it
            if (m_normalizedT != 0f && m_normalizedT != 1f)
            {
              // Stick to wall
              IsWallHanging = true;
              doNotChangeSpline = true;
            }
          }
        }
      }

      if (!InTransition && !doNotChangeSpline)
      {
        if (movingForward)
        {
          if (m_normalizedT >= 1f)
          {
            if (CheckForLinkedSpline(movingForward))
            {
              OvershootSpline(targetSpeed, distanceTraveled);
              airMoveSpeed = moveSpeed;
            }

            else if (CheckForSplineBelow(movingForward, true))
            {
              OvershootSpline(targetSpeed, distanceTraveled);
              airMoveSpeed = moveSpeed;
            }

            else
            {
              m_normalizedT = 1f;
            }
          }
        }

        else
        {
          if (m_normalizedT <= 0f)
          {
            if (CheckForLinkedSpline(movingForward))
            {
              OvershootSpline(targetSpeed, distanceTraveled);
              airMoveSpeed = moveSpeed;
            }

            else if (CheckForSplineBelow(movingForward, true))
            {
              OvershootSpline(targetSpeed, distanceTraveled);
              airMoveSpeed = moveSpeed;
            }

            else
            {
              m_normalizedT = 0f;
            }
          }
        }
      }

      return targetBezierPosition;
    }

    /************************************************ PRIVATE FUNCTIONS ************************************************/

    /// <summary>
    /// Only to be called on awake, initialises and sets the first position of the player on the spline
    /// </summary>
    private void InitialisePosition()
    {
      finalSpeed = 0;
      Execute(0);

      physicsController.SetTargetBezierPosition(targetBezierPosition, targetBezierPosition.y, true);
    }

    private void InitialiseMovementComponents()
    {
      physicsController = GetComponent<BezierPhysicsController>();

      bezierSlopeBehaviour = GetComponent<BezierSlopeBehaviour>();
      canRoll = bezierSlopeBehaviour == null ? false : true;
    }

    private void GroundMove( bool ignoreInput = false )
    {
      timeSpentMoving += Time.deltaTime;
      float timeInAcceleration = timeSpentMoving - runWindupTime;

      moveSpeed = Mathf.Lerp(baseSpeed, runSpeed, timeInAcceleration / runAccelerationTime);

      if (!ignoreInput)
      {
        isGoingForward = _moveInput.GetMoveHorizontal() >= 0 ? true : false;
      }

      if (!atMaxSpeed && Utils.FP.IsEqualWithin(moveSpeed, runSpeed))
      {
        atMaxSpeed = true;
      }

      finalSpeed = moveSpeed;
    }

    private void InAirMove()
    {
      float modifier = isGoingForward ? 1f : -1f;
      float horizontalAxisValue = _moveInput.GetMoveHorizontal();

      airMoveSpeed += horizontalAxisValue * airMoveForce * modifier * Time.deltaTime;

      if (!atMaxSpeed && Utils.FP.IsEqualWithin(airMoveSpeed, runSpeed))
      {
        atMaxSpeed = true;
      }

      finalSpeed = airMoveSpeed;
    }

    private void HandleMovements()
    {
      UpdateInAirStatus();

      float deltaTime = Time.deltaTime;

      InAirMovement(deltaTime);
      OnSlopeMovement(deltaTime);
      OnWallMovement(deltaTime);
    }

    private void ContactGround()
    {
      if (firstContactGround)
      {
        UpdateInAirStatus();
        firstContactGround = false;

        Debug.Log(currentAngleOfIncline);

        // Check the angle of incline at the point of intersection
        if (currentAngleOfIncline >= minRollAngle && currentAngleOfIncline < minRunnableAngle)
        {
          // Roll
          if (canRoll)
          {
            float speed = finalSpeed < baseSpeed ? baseSpeed : finalSpeed;
            bezierSlopeBehaviour.SetRolling(speed, !forwardMovesUpSlope);
          }
          Debug.Log("Roll");
        }

        else if (currentAngleOfIncline >= minRunnableAngle && currentAngleOfIncline <= maxRunnableAngle)
        {
          Debug.Log("Sliding");
          slideSpeed = finalSpeed < baseSpeed ? baseSpeed : finalSpeed;
          isSliding = true;
        }

        else if (currentAngleOfIncline > maxRunnableAngle)
        {
          Debug.Log("Falling");
        }
      }
    }

    private void InAirMovement( float deltaTime )
    {
      if (inAir)
      {
        // Slows down the jump speed over time
        airAcceleration -= gravity * deltaTime;
        airAcceleration = Mathf.Clamp(airAcceleration, -terminalVelocity, airAcceleration);

        // Slows down the player movement over time
        airMoveSpeed -= airFriction * deltaTime;
        airMoveSpeed = Mathf.Clamp(airMoveSpeed, 0, airMoveSpeedLimit);

        finalSpeed = airMoveSpeed;

        physicsController.AddHeightOffset(airAcceleration * deltaTime);

        CheckForSplineBelow(MovingForward, false);
      }
    }

    private void OnWallMovement( float deltaTime )
    {
      if (IsWallHanging)
      {
        if (wallStickTimer < wallStickDuration)
        {
          wallStickTimer += deltaTime;
          isWallSliding = false;
        }

        else
        {
          IsWallHanging = false;
          isWallSliding = true;
          wallSlideSpeed = 0f;

          isGoingForward = !forwardMovesTowardsWall;
          // Only find the t value here since this is the only place it is needed
          spline.FindNearestPointTo(physicsController.GetCurrentPosition(), out m_normalizedT);
        }
      }

      else if (isWallSliding)
      {
        wallSlideSpeed += wallSlideAcceleration * Time.deltaTime;
        finalSpeed = wallSlideSpeed;
      }
    }

    private void OnSlopeMovement( float deltaTime )
    {
      if (isSliding)
      {
        isGoingForward = !forwardMovesUpSlope;

        slideSpeed += slideAcceleration;
        slideSpeed = Mathf.Clamp(slideSpeed, 0f, runSpeed);

        finalSpeed = slideSpeed;
      }
    }

    private void UpdateInAirStatus()
    {
      float finalHeight = physicsController.GetFinalPosition().y;

      // On or below ground
      if (finalHeight - GroundHeight <= 0.0001f)
      {
        if (inAir)
        {
          Debug.Log("Contact ground!" + finalHeight.ToString("F10") + " " + GroundHeight.ToString("F10"));
          inAir = false;

          firstContactGround = true;
          airAcceleration = 0;
          airMoveSpeed = 0;
        }

        physicsController.SnapToGround();
      }

      else
      {
        // First time leaving the ground
        if (!inAir)
        {
          inAir = true;
          firstDirectionMoveMadeInAir = false;

          airMoveSpeed = finalSpeed;
          airMoveSpeedLimit = airMoveSpeed < baseSpeed ? baseSpeed : airMoveSpeed;

          if (IsWallHanging || isWallSliding)
          {
            isGoingForward = !forwardMovesTowardsWall;
            airMoveSpeed = finalSpeed = baseSpeed;

            isWallJumping = true;
            IsWallHanging = false;
            isWallSliding = false;
          }

          else if (isSliding)
          {
            isSliding = false;
            isGoingForward = !forwardMovesUpSlope;

            airMoveSpeed = slideSpeed;
          }
        }
      }
    }

    private void CalculateNewPosition()
    {
      if (finalSpeed != 0 || animating)
      {
        float cacheSpeed = finalSpeed;
        float previousGroundHeight = GroundHeight;
        BezierSpline currentSpline = spline;

        if (!animating)
        {
          Execute(Time.deltaTime);
        }
        else
        {
          ExecuteAnimation(_animationMover.GetDelta());
        }

        if (currentSpline != spline)
        {
          previousGroundHeight = GroundHeight;
        }

        // Need this as final speed gets adjusted when changing spline, which would affect the in air speed when leaving the ground
        finalSpeed = cacheSpeed;

        physicsController.SetTargetBezierPosition(targetBezierPosition, previousGroundHeight);
        UpdateAvailableTransitions();
      }
    }

    private bool CheckForLinkedSpline( bool movingForward )
    {
      BezierPoint point = null;

      if (movingForward)
      {
        point = spline[spline.Count - 1];
      }

      else
      {
        point = spline[0];
      }

      BezierPoint connectionPoint = point.linkedNodesManager.GetConnectionPoint();

      if (connectionPoint != null)
      {
        BezierSpline newSpline = connectionPoint.GetComponentInParent<BezierSpline>();
        float newT = connectionPoint.tPosition;

        SwitchSplines(newSpline, connectionPoint.position, newT);

        return true;
      }

      return false;
    }

    private bool CheckForSplineBelow( bool movingForward, bool leavingSpline )
    {
      BezierSpline splineBelow = null;
      splineBelow = FindClosestAlignedSplineBelow(out float newT, out Vector3 newTargetBezierPosition, leavingSpline);

      if (splineBelow != null && splineBelow != spline)
      {
        doNotSwitchTo = spline;
        SwitchSplines(splineBelow, newTargetBezierPosition, newT);

        return true;
      }

      return false;
    }

    private BezierSpline FindClosestAlignedSplineBelow( out float normalisedT, out Vector3 newTargetBezierPosition, bool leavingSpline )
    {
      BezierSpline[] allSplines = SplineCacher.GetAllSplines();
      BezierSpline closestSpline = null;
      float closestDistance = leavingSpline ? Mathf.Infinity : physicsController.GetCurrentPosition().y - GroundHeight;

      newTargetBezierPosition = Vector3.zero;

      normalisedT = -1f;

      BezierSpline checkSelf = leavingSpline ? spline : null;

      foreach (BezierSpline individualSpline in allSplines)
      {
        if (individualSpline != checkSelf && individualSpline != doNotSwitchTo)
        {
          float tempT;

          Vector3 nearestPoint = individualSpline.FindNearestPointOnXZPlane(physicsController.GetCurrentPosition(), out tempT);

          // Check if x and z values are aligned
          if (Utils.FP.IsEqualWithin(physicsController.GetCurrentPosition().x, nearestPoint.x) && Utils.FP.IsEqualWithin(physicsController.GetCurrentPosition().z, nearestPoint.z))
          {
            // Check if the player's y is above the spline
            if (physicsController.GetCurrentPosition().y >= nearestPoint.y)
            {
              float heightDistance = physicsController.GetCurrentPosition().y - nearestPoint.y;

              if (heightDistance < closestDistance)
              {
                closestDistance = heightDistance;
                closestSpline = individualSpline;

                normalisedT = tempT;

                newTargetBezierPosition = nearestPoint;
              }
            }
          }
        }
      }

      return closestSpline;
    }

    private Vector3 OvershootSpline( float targetSpeed, float distanceTraveled )
    {
      finalSpeed = Mathf.Abs(targetSpeed) - distanceTraveled;

      if (finalSpeed < 0)
      {
        finalSpeed = -finalSpeed;

        Debug.Log("Inaccurate calculation of distance travelled." + targetSpeed + " " + distanceTraveled);
      }

      return Execute(1f);
    }

    private void CheckForSpline()
    {
      BezierSpline[] allSplines = SplineCacher.GetAllSplines();

      foreach (BezierSpline checkSpline in allSplines)
      {
        if (checkSpline != spline && checkSpline != doNotSwitchTo)
        {
          Vector3 nearestPoint = checkSpline.FindNearestPointOnXZPlane(physicsController.GetCurrentPosition(), out float t);

          //Debug.Log(nearestPoint.ToString("F10") + " " + targetBezierPosition.ToString("F10"));

          if (Utils.FP.IsEqualWithin(physicsController.GetCurrentPosition().x, nearestPoint.x) && Utils.FP.IsEqualWithin(physicsController.GetCurrentPosition().z, nearestPoint.z))
          {
            // Check if the player's y is above it
            if (physicsController.GetCurrentPosition().y >= nearestPoint.y - 0.25f)
            {
              SwitchSplines(checkSpline, nearestPoint, t);

              return;
            }
          }
        }
      }
    }

    //private Quaternion GetTargetRotation()
    //{
    //  return isGoingForward ? Quaternion.LookRotation(spline.GetTangent(m_normalizedT)) : Quaternion.LookRotation(spline.GetTangent(-m_normalizedT));
    //}

    private void UpdateSplineInformation()
    {
      transitionData = spline.transitionSplines;
      validTransitions.Clear();
    }

    private void UpdateAvailableTransitions()
    {
      for (int i = 0; i < transitionData.Length; ++i)
      {
        if (transitionData[i].NormalisedTIsInRegion(m_normalizedT))
        {
          if (!validTransitions.Contains(transitionData[i]))
          {
            validTransitions.Add(transitionData[i]);
          }
        }

        else
        {
          validTransitions.Remove(transitionData[i]);
        }
      }
    }

    /// <summary>
    /// Switches to the next spline
    /// </summary>
    /// <param name="newSpline"></param>
    /// <param name="newGroundHeight"></param>
    /// <param name="t"></param>
    private void SwitchSplines( BezierSpline newSpline, Vector3 newTargetBezierPosition, float t )
    {
      previousSpline = spline;

      lastWalkableSpline = previousSpline.splineType != SplineType.Wall ? previousSpline : lastWalkableSpline;

      spline = newSpline;
      targetBezierPosition = newTargetBezierPosition;

      m_normalizedT = t;

      UpdateSplineInformation();

      // Finds the opposite linked point for this wall
      if (spline.splineType == SplineType.Wall)
      {
        if (m_normalizedT == 0)
        {
          oppositeWallPoint = spline[spline.Count - 1];
          oppositeLinkedWallPoint = oppositeWallPoint.linkedNodesManager.GetConnectionPoint();

          if (oppositeLinkedWallPoint)
          {
            // Determines if moving forward moves towards the wall or away from it
            forwardMovesTowardsWall = GroundHeight < oppositeLinkedWallPoint.position.y;
          }
          
          else
          {
            forwardMovesTowardsWall = isGoingForward;
          }
        }

        else if (m_normalizedT == 1f)
        {
          oppositeWallPoint = spline[0];
          oppositeLinkedWallPoint = oppositeWallPoint.linkedNodesManager.GetConnectionPoint();

          if (oppositeLinkedWallPoint)
          {
            // Determines if moving forward moves towards the wall or away from it
            forwardMovesTowardsWall = GroundHeight >= oppositeLinkedWallPoint.position.y;
          }

          else
          {
            forwardMovesTowardsWall = isGoingForward;
          }
        }

        else
        {
          oppositeLinkedWallPoint = null;
        }

      }
    }

    /************************************************ MOVEMENT COMPONENTS ************************************************/


    /************************************************ GETTERS & SETTERS ************************************************/

    private bool GetRollingStatus()
    {
      return canRoll && bezierSlopeBehaviour.IsRolling;
    }

    //private void SetInAir()
    //{
    //  if ()
    //}

    /************************************************ COROUTINES ************************************************/

    private IEnumerator StartTransition(TransitionSpline transitionData)
    {
      bool destinationReached = false;
      bool contactPointReached = false;

      float targetNormalisedT = 0;

      // Check if it has already reached the contact point (the spline would be the destination spline already)
      if (spline == transitionData.destinationSpline)
      {
        contactPointReached = true;

        // Target T is simply the destination T
        targetNormalisedT = transitionData.destinationT;
      }

      // Still has to reach contact point
      else
      {
        // Target T is the contact point's T
        targetNormalisedT = transitionData.contactPoint.tPosition;
      }

      moveSpeed = moveSpeed < baseSpeed ? baseSpeed : moveSpeed;

      // Loop until player has reached its destination (the transition spline T)
      while (!destinationReached)
      {
        // Moves the player towards the target T
        yield return TransitionMove(targetNormalisedT);

        // Once it reaches here, player has arrived at target T
        // Contact point not reached yet
        if (!contactPointReached)
        {
          // Then switch to the destination spline
          contactPointReached = true;

          // TODO: Should be easy to get the transition to point
          transitionData.destinationSpline.FindNearestPointTo(transitionData.contactPoint.position, out float newT);

          SwitchSplines(transitionData.destinationSpline, transitionData.contactPoint.position, newT);

          targetNormalisedT = transitionData.destinationT;
        }

        // Contact point already reached, so destination is reached
        else
        {
          destinationReached = true;
        }
      } // contactPointReached

      InTransition = false;
    } // func

    private IEnumerator CancelTransition(TransitionSpline transitionData)
    {
      // If player reached the point where transition first started
      bool departurePointReached = false;
      // If player already reached the contact point
      bool contactPointReached = false;
      float targetNormalisedT = 0;

      // Check if it has already reached the contact point (the spline would be the destination spline already)
      if (spline == transitionData.destinationSpline)
      {
        // Reverse of StartTransition, if contact point already reached, player has to first reach the contact point then departure point
        contactPointReached = true;

        targetNormalisedT = transitionData.contactPoint.tPosition;
      }

      else
      {
        targetNormalisedT = beforeTransitionT;
      }

      // Loop until transition complete
      while (!departurePointReached)
      {
        // Moves the player towards the target T
        yield return TransitionMove(targetNormalisedT);

        // Reverse
        if (contactPointReached)
        {
          contactPointReached = false;

          // TODO: Should be easy to get the transition to point
          beforeTransitionSpline.FindNearestPointTo(transitionData.contactPoint.position, out float newT);

          SwitchSplines(beforeTransitionSpline, transitionData.contactPoint.position, newT);

          targetNormalisedT = beforeTransitionT;
        }

        else
        {
          departurePointReached = true;
        }
      } // contactPointReached

      InTransition = false;
    } // func

    private IEnumerator TransitionMove(float targetNormalisedT)
    {
      if (m_normalizedT < targetNormalisedT)
      {
        isGoingForward = true;

        while (m_normalizedT < targetNormalisedT)
        {
          Move();

          yield return null;
        }
      }

      else
      {
        isGoingForward = false;

        while (m_normalizedT > targetNormalisedT)
        {
          Move();

          yield return null;
        }
      }
    }
    
/************************************************ TESTING FUNCTIONS ************************************************/
    
    //input
    private MoveInput _moveInput;    

    public Vector3 ExecuteAnimation(float deltaPosition) //very broken rn, will look at later ------------------------
    {
      float oldNormalisedT = m_normalizedT;

      targetBezierPosition = spline.MoveAlongSplineAnimation(_animationMover.baseValue, ref m_normalizedT, deltaPosition);
      
      float targetSpeed = (isGoingForward) ? finalSpeed : -finalSpeed;
      targetSpeed *= Time.deltaTime;

      //Vector3 targetPos = transform.position;
      // Used only when player is on the wall and cannot transition
      bool doNotChangeSpline = false;
      bool movingForward = MovingForward;

      if (spline.splineType == SplineType.Wall)
      {
        // If hanging on wall
        if (IsWallHanging)
        {
          doNotChangeSpline = true;
          // Do nothing
        }

        // Not on wall, in air or running on it
        else
        {
          // Find where the object will be at this frame
          float tempT = m_normalizedT;

          Vector3 tempPos = transform.position;

          // Object is currently in the air
          if (inAir)
          {
            // Moving towards wall in the air
            if (movingForward == forwardMovesTowardsWall)
            {
              tempPos = spline.MoveAlongSpline(ref tempT, targetSpeed);
            }

            // Moving away from wall in the air
            else
            {
              //tempPos = spline.MoveDownAlongWallSpline(ref tempT, targetSpeed);
            }

            // InAirPhysics is calculated first, so the object's y-position is up to date here
            // Check the y position if it is above or below the new ground height
            if (physicsController.CheckObjectWillBeAboveHeight(tempPos.y))
            {
              // Player can freely move through the wall
              targetBezierPosition = tempPos;
              m_normalizedT = tempT;
            }

            // Object has contacted the wall
            else
            {
              if (m_normalizedT != 0f && m_normalizedT != 1f)
              {
                // Stick to wall
                GroundHeight = tempPos.y;

                IsWallHanging = true;
                doNotChangeSpline = true;
              }
            }
          }

          // Object is already on the ground along the wall
          else
          {
            tempPos = spline.MoveAlongSpline(ref tempT, targetSpeed);

            // Object can run up the wall (must be moving towards the wall) if it is at max speed and the wall is at a certain angle
            if (atMaxSpeed && (isGoingForward == forwardMovesTowardsWall))
            {
              // Find the point indices where the object is at
              var indices = spline.GetNearestPointIndicesTo(m_normalizedT);

              Debug.Log(indices.index1 + " " + indices.index2);

              bool runnableAngle = false;

              // Point 0 -> Point 1, take the first index
              if (forwardMovesTowardsWall)
              {
                BezierPoint referencePoint = spline[indices.index1];

                if (referencePoint.angleToNextPoint <= maxRunnableAngle)
                {
                  runnableAngle = true;
                }
              }

              // Point 0 <- Point 1, take the second index
              else
              {
                BezierPoint referencePoint = spline[indices.index2];

                if (referencePoint.angleToPreviousPoint <= maxRunnableAngle)
                {
                  runnableAngle = true;
                }
              }

              if (runnableAngle)
              {
                targetBezierPosition = tempPos;
                m_normalizedT = tempT;
              }
            }

            else
            {
              if (m_normalizedT != 0f && m_normalizedT != 1f)
              {
                // Stick to wall
                GroundHeight = tempPos.y;

                IsWallHanging = true;
                doNotChangeSpline = true;
              }
            }
          }
        }
      }

      else
      {
        targetBezierPosition = spline.MoveAlongSpline(ref m_normalizedT, targetSpeed);
      }

      if (!InTransition && !doNotChangeSpline)
      {
        if (movingForward)
        {
          if (m_normalizedT >= 1f)
          {
            if (CheckForLinkedSpline(movingForward))
            {
              //targetBezierPosition = OvershootSpline(movingForward, targetSpeed, oldNormalisedT);
              airMoveSpeed = moveSpeed;
            }

            //else if (CheckForSplineBelow(movingForward))
            //{
            //  targetBezierPosition = OvershootSpline(movingForward, targetSpeed, oldNormalisedT);
            //  airMoveSpeed = moveSpeed;
            //}

            else
            {
              m_normalizedT = 1f;
            }
          }
        }

        else
        {
          if (m_normalizedT <= 0f)
          {
            if (CheckForLinkedSpline(movingForward))
            {
              //OvershootSpline(movingForward, targetSpeed, oldNormalisedT);
              airMoveSpeed = moveSpeed;
            }

            //else if (CheckForSplineBelow(movingForward))
            //{
            //  OvershootSpline(movingForward, targetSpeed, oldNormalisedT);
            //  airMoveSpeed = moveSpeed;
            //}

            else
            {
              m_normalizedT = 0f;
            }
          }
        }
      }

      return targetBezierPosition;
    }

    public bool animating;
    private AnimationMover _animationMover;

    public Vector3 GetMoveClosestSpline(Vector3 otherLocation)
    {
      return spline.FindNearestPointTo(otherLocation, out m_normalizedT);
    }

  } // class
}

 