using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Player")] 
    [SerializeField] private float _runSpeed;
    [SerializeField] private float _slideSpeed;
    [SerializeField] private float _slideSmoothness;
    [SerializeField] private float _maxSlideAmount;
    [SerializeField] private Transform _playerVisual;
    [SerializeField] private Camera _cam;

    private float _mousePositionX;
    private float _playerVisualPositionX;

    private Rigidbody _rigidbody;
    private bool _isPlayerInteract;
    
    private void Update()
    {
        ForwardMovement();
        SwerveMovement();
    }
    
    #region PlayerMovement

    private void ForwardMovement()
    {
        transform.Translate(Vector3.forward * _runSpeed * Time.deltaTime);
    }

    private void SwerveMovement()
    {
        if (!_isPlayerInteract)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _playerVisualPositionX = _playerVisual.localPosition.x;
                _mousePositionX = _cam.ScreenToViewportPoint(Input.mousePosition).x;
            }

            if (Input.GetMouseButton(0))
            {
                float currentMousePositionX = _cam.ScreenToViewportPoint(Input.mousePosition).x;
                float distance = currentMousePositionX - _mousePositionX;
                float positionX = _playerVisualPositionX + (distance * _slideSpeed);
                Vector3 position = _playerVisual.localPosition;
                position.x = Mathf.Clamp(positionX, -_maxSlideAmount, _maxSlideAmount);
                _playerVisual.localPosition = Vector3.Lerp(_playerVisual.localPosition, position, _slideSmoothness * Time.deltaTime);
            }
            else
            {
                Vector3 pos = _playerVisual.localPosition;
            }
        }
    }

    #endregion
    
}
