using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private InputManager _input;

    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private CapsuleCollider _collider;

    [SerializeField]
    private float _walkSpeed;

    [SerializeField]
    private float _sprintSpeed;

    [SerializeField]
    private float _crouchSpeed = 325f; 

    [SerializeField]
    private float _walkSprintTransition;

    private float _speed;

    [SerializeField]
    private float _rotationSmoothTime = 0.1f;

    private float _rotationSmoothVelocity;

    [SerializeField]
    private float _jumpForce;

    [SerializeField]
    private Transform _groundDetector;

    [SerializeField]
    private float _detectorRadius;

    [SerializeField]
    private LayerMask _groundLayer;

    private bool _isGrounded;

    [SerializeField]
    private Vector3 _upperStepOffset;

    [SerializeField]
    private float _stepCheckerDistance;

    [SerializeField]
    private float _stepForce;

    [SerializeField]
    private Transform _climbDetector;

    [SerializeField]
    private float _climbCheckDistance;

    [SerializeField]
    private LayerMask _climbableLayer;

    [SerializeField]
    private Vector3 _climbOffset;

    [SerializeField]
    private float _climbSpeed;

    [SerializeField]
    private Transform _cameraTransform;

    [SerializeField]
    private CameraManager _cameraManager;

    private PlayerStance _playerStance;
    private Animator _animator;

    private void Awake()
    {
        _rigidbody    = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _collider = GetComponent<CapsuleCollider>();
        _speed        = _walkSpeed;
        _playerStance = PlayerStance.Stand;

        HideAndLockCursor();
    }

    private void Start()
    {
        _input.OnMoveInput   += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput   += Jump;
        _input.OnClimbInput  += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _cameraManager.OnChangePerspective += ChangePerspective;
        _input.OnCrouchInput += Crouch; 
    }

    private void OnDestroy()
    {
        _input.OnMoveInput   -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput   -= Jump;
        _input.OnClimbInput  -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _cameraManager.OnChangePerspective -= ChangePerspective;
        _input.OnCrouchInput -= Crouch;
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;

        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouching = _playerStance == PlayerStance.Crouch; 

        if (isPlayerCrouching)
        {
            _speed = _crouchSpeed;
        }

        if (isPlayerStanding || isPlayerCrouching) 
        {
            switch (_cameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1f)
                    {
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y)
                                              * Mathf.Rad2Deg
                                              + _cameraTransform.eulerAngles.y;

                        float smoothAngle = Mathf.SmoothDampAngle(
                            transform.eulerAngles.y,
                            rotationAngle,
                            ref _rotationSmoothVelocity,
                            _rotationSmoothTime);

                        transform.rotation  = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection   = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;

                        _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    }
                    break;

                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);

                    Vector3 verticalDirection   = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection           = verticalDirection + horizontalDirection;

                    _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    break;
            }
            Vector3 velocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
        }
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical   = axisDirection.y * transform.up;
            movementDirection  = horizontal + vertical;

            _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
        }
    }

    private void Sprint(bool isSprint)
    {
        if (_playerStance == PlayerStance.Crouch) return;

        if (isSprint)
        {
            if (_speed < _sprintSpeed)
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
        }
        else
        {
            if (_speed > _walkSpeed)
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
            _animator.SetTrigger("Jump");
        }
    }

    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
    }

    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,
                                              transform.forward,
                                              _stepCheckerDistance);

        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset,
                                              transform.forward,
                                              _stepCheckerDistance);

        if (isHitLowerStep && !isHitUpperStep)
        {
            _rigidbody.AddForce(0, _stepForce, 0);
        }
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position,
                                                       transform.forward,
                                                       out RaycastHit hit,
                                                       _climbCheckDistance,
                                                       _climbableLayer);

        bool isNotClimbing = _playerStance != PlayerStance.Climb;

        if (isInFrontOfClimbingWall && _isGrounded && isNotClimbing)
        {
            Vector3 offset     = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;

            _playerStance             = PlayerStance.Climb;
            _rigidbody.useGravity     = false;


            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOFView(40);
        }
    }

    private void CancelClimb()
    {
        if (_playerStance == PlayerStance.Climb)
        {
            _playerStance             = PlayerStance.Stand;
            _rigidbody.useGravity     = true;
            transform.position       -= transform.forward * 1f;


            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldOFView(70);
        }
    }

    private void Crouch()
    {
        if (_playerStance == PlayerStance.Stand)
        {
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            
            _collider.height = 1.3f;
            _collider.center = new Vector3(0, 0.66f, 0);
        }
        else if (_playerStance == PlayerStance.Crouch)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);

            _collider.height = 2f;
            _collider.center = new Vector3(0, 1f, 0);
        }
    }
}