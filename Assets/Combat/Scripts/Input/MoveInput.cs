using UnityEngine;

public class MoveInput : MonoBehaviour
{
  //for delta calculations
  private float _curX;
  private float _oldX;

  private float _curY;
  private float _oldY;

  //for holding stick
  private int _leftHoldCounter;
  private int _rightHoldCounter;
  private int _counterTreshold = 7;

  // for actions
  private float _actionLast;
  private float _actionCurrent;

  //for double flicking stick
  /*private float _doubleTapTime;
  private float _doubleTapInterval = .35f;
  private bool _leftDown;
  private bool _rightDown;*/

  //right trigger
  private float _rightTriggerLast;
  private float _rightTriggerCurrent;
  //private bool _rightTriggerDown;

  //dodging
  private bool _dodgeLeft;
  private bool _dodgeRight;

  //left trigger
  private float _leftTriggerLast;
  private float _leftTriggerCurrent;
  //private bool _leftTriggerDown;

  public bool active = true;

  private void ClearAll()
  {
    _leftHoldCounter = 0;
    _rightHoldCounter = 0;
    _curX = 0;
    _oldX = 0;
    _curY = 0;
    _oldY = 0;
    _actionCurrent = 0;
    _actionLast = 0;
  }

  // Update is called once per frame
  void Update()
  {
    if (active)
    {
      _oldX = _curX;
      _curX = Input.GetAxisRaw("MoveHorizontal");

      _oldY = _curY;
      _curY = Input.GetAxis("MoveVertical");

      if (_curX == 1)
        _rightHoldCounter++;
      else if (_curX == -1)
        _leftHoldCounter++;
      else
      {
        _rightHoldCounter = 0;
        _leftHoldCounter = 0;
      }

      /*_leftDown = false;
      _rightDown = false;

      if (LeftDown())
      {
          if (Time.time < _doubleTapTime + _doubleTapInterval)
          {
              _leftDown = true;
          }
          _doubleTapTime = Time.time;
      }
      else if (RightDown())
      {
          if (Time.time < _doubleTapTime + _doubleTapInterval)
          {
              _rightDown = true;
          }
          _doubleTapTime = Time.time;
      }*/

      _rightTriggerLast = _rightTriggerCurrent;
      _rightTriggerCurrent = Input.GetAxis("Dash");
      //_rightTriggerDown = false;
      _dodgeLeft = false;
      _dodgeRight = false;

      if (_curX > 0 && RightTriggerDown())
      {
        _dodgeRight = true;
      }
      else if (_curX < 0 && RightTriggerDown())
      {
        _dodgeLeft = true;
      }

      _leftTriggerLast = _leftTriggerCurrent;
      _leftTriggerCurrent = Input.GetAxis("Jump");

      _actionLast = _actionCurrent;
      _actionCurrent = Input.GetAxis("Action");
    }
    else
    {
      ClearAll();
    }
  }

  public bool StartMoving()
  {
    return (_oldX == 0 && _curX != 0);
  }

  public bool StopMoving()
  {
    return (_oldX != 0 && _curX == 0);
  }

  public bool Moving()
  {
    return (_oldX != 0 && _curX != 0);
  }

  public float GetMoveHorizontal()
  {
    return _curX;
  }

  public float GetMoveVertical()
  {
    return _curY;
  }

  public bool GetMoveVerticalDown()
  {
    return _oldY == 0 && _curY != 0;
  }

  public bool ActionUp()
  {
    return _actionLast != 1 && _actionCurrent == 1;
  }

  public bool ActionDown()
  {
    return _actionLast != -1 && _actionCurrent == -1;
  }

  public bool ActionReleased()
  {
    return (_actionLast == 1 && _actionCurrent != 1) || (_actionLast == -1 && _actionCurrent != -1);
  }

  public bool GetMoveVerticalUp()
  {
    return (_oldY == 1 && _curY != 1) || (_oldY == -1 && _curY != -1);
  }

  public bool HoldLeft()
  {
    return _leftHoldCounter > _counterTreshold;
  }

  public bool HoldRight()
  {
    return _rightHoldCounter > _counterTreshold;
  }

  private bool LeftDown()
  {
    return _curX == -1 && _oldX > -1;
  }

  private bool RightDown()
  {
    return _curX == 1 && _oldX < 1;
  }

  public bool RightTriggerDown()
  {
    return _rightTriggerCurrent == 1 && _rightTriggerLast != 1;
  }

  public bool LeftTriggerDown()
  {
    return _leftTriggerCurrent == 1 && _leftTriggerLast != 1;
  }

  public bool LeftTriggerUp()
  {
    return _leftTriggerCurrent == 0 && _leftTriggerLast != 0;
  }

  /*public bool DoubleLeft()
  {
      return _leftDown;
  }

  public bool DoubleRight()
  {
      return _rightDown;
  }*/

  public bool DodgeLeft()
  {
    return _dodgeLeft;
  }

  public bool DodgeRight()
  {
    return _dodgeRight;
  }
}
