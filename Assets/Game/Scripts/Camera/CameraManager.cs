using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine; 

public class CameraManager : MonoBehaviour
{
    [SerializeField] 
    public CameraState CameraState; 

    [SerializeField]

    private CinemachineCamera _fpsCamera; 

    [SerializeField]
    private CinemachineCamera _tpsCamera; 

    [SerializeField]
    private InputManager _inputManager;

    private void Start()
    {
        _inputManager.OnChangePOV += SwitchCamera;
    }

    private void OnDestroy()
    {
        _inputManager.OnChangePOV -= SwitchCamera;
    }

    public void SetFPSClampedCamera(bool isClamped, Vector3 playerRotation)
    {
        var panTilt = _fpsCamera.GetComponent<CinemachinePanTilt>();

        if (panTilt != null)
        {
            if (isClamped)
            {
                panTilt.PanAxis.Range = new Vector2(playerRotation.y - 45, playerRotation.y + 45);
                panTilt.PanAxis.Wrap = false;
            }
            else
            {
                panTilt.PanAxis.Range = new Vector2(-180, 180);
                panTilt.PanAxis.Wrap = true;
            }
        }
        else 
        {
            Debug.LogWarning("Komponen CinemachinePanTilt tidak ditemukan pada " + _fpsCamera.name);
        }
    }

    private void SwitchCamera()
    {
        if (CameraState == CameraState.ThirdPerson)
        {
            CameraState = CameraState.FirstPerson;
            _tpsCamera.gameObject.SetActive(false);
            _fpsCamera.gameObject.SetActive(true);
        }
        else
        {
            CameraState = CameraState.ThirdPerson;
            _tpsCamera.gameObject.SetActive(true);
            _fpsCamera.gameObject.SetActive(false);
        }
    }

    public void SetTPSFieldOFView(float fielOfView)
    {
        _tpsCamera.Lens.FieldOfView = fielOfView; 
    }
}