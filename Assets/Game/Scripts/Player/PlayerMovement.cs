using System;
using System.Collections; 
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

    [Header("Glide Settings")]
    [SerializeField]
    private float _glideSpeed = 70f;

    [SerializeField]
    private float _airDrag = 5f;

    [SerializeField]
    private Vector3 _glideRotationSpeed = new Vector3(20, 40, 20);

    [SerializeField]
    private float _minGlideRotationX = -10f;

    [SerializeField]
    private float _maxGlideRotationX = 14f;

    private bool _isPunching;
    private int _combo = 0;

    [SerializeField]
    private float _resetComboInterval;
    private Coroutine _resetCombo;

    [Header("Punch Settings")]
    [SerializeField] 
    private Transform _hitDetector;
    [SerializeField] 
    private float _hitDetectorRadius;
    [SerializeField] 
    private LayerMask _hitLayer;



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
        _input.OnGlideInput  += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch; 
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
        _input.OnGlideInput  -= StartGlide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        
        Glide();
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
        bool isPlayerGliding = _playerStance == PlayerStance.Glide; // Cek status Glide

        if (isPlayerCrouching)
        {
            _speed = _crouchSpeed;
        }

        if ((isPlayerStanding || isPlayerCrouching) && !_isPunching) 
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

            _animator.SetFloat("ClimbVelocityX", axisDirection.x * _rigidbody.linearVelocity.magnitude);
            _animator.SetFloat("ClimbVelocityY", axisDirection.y * _rigidbody.linearVelocity.magnitude);
        }
        else if (isPlayerGliding)
        {
            Vector3 rotationDegree = transform.rotation.eulerAngles;

            rotationDegree.x += axisDirection.y * _glideRotationSpeed.x * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp((rotationDegree.x <= 180) ? rotationDegree.x : rotationDegree.x - 360, 
                                            _minGlideRotationX, _maxGlideRotationX);

            rotationDegree.y += axisDirection.x * _glideRotationSpeed.y * Time.deltaTime;
            rotationDegree.z = -axisDirection.x * _glideRotationSpeed.z;

            transform.rotation = Quaternion.Euler(rotationDegree);
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

        if (_isGrounded && _playerStance == PlayerStance.Glide)
        {
            CancelGlide();
        }
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

            _animator.SetBool("IsClimbing", true);
            _collider.center = Vector3.up * 1.3f; 

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

            _animator.SetBool("IsClimbing", false);
            _collider.center = Vector3.up * 1f; 

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


    private void StartGlide()
    {
        if (!_isGrounded && _playerStance != PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("IsGliding", true);
            
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
        }
    }

    private void CancelGlide()
    {
        if (_playerStance == PlayerStance.Glide)
        {
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsGliding", false);
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
        }
    }

    private void Glide()
    {
        if (_playerStance != PlayerStance.Glide) return;

        float lift = transform.rotation.eulerAngles.x;
        if (lift > 180) lift -= 360; 

        Vector3 liftForce = transform.up * (lift + _airDrag);
        Vector3 glideForce = transform.forward * _glideSpeed;

        _rigidbody.AddForce(liftForce + glideForce);
    }

    private void Punch()
    {
        if (!_isPunching && _playerStance == PlayerStance.Stand)
        {
            _isPunching = true;
            if (_combo <3)
            {
                _combo = 1;
            }
            else
            {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");
        }

    }

    public void EndPunch()
    {
        _isPunching = false;
        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo()); 
    }

    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(5f);
        _combo = 0;
    }

    public void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);

        for (int i = 0; i <  hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject != null)
            {
                Destroy(hitObjects[i].gameObject);
            }
        }
    }
}