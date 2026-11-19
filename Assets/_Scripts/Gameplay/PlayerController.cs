using Assets._Scripts._Core.Configs;
using Assets._Scripts._Core.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Assets._Scripts.Gameplay
{
    public sealed class PlayerController : NetworkBehaviourBase
    {
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Camera _camera;

        private CharacterConfig _config;
        private float _verticalSpeed;
        private float _upDownCamLimit;


        [Inject]
        private void Construct(CharacterConfig config)
        {
            _config = config;
        }


        protected override void OnSpawned()
        {
            var isOwner = IsOwner;

            if (_camera != null)
                _camera.gameObject.SetActive(isOwner);

            if (!isOwner) return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (!IsOwner) return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (!IsOwner || _config == null) return;

            Look();
            Move();
        }

        private void Look()
        {
            var mouse = Mouse.current;
            if (mouse == null || _cameraPivot == null) return;

            var delta = mouse.delta.ReadValue() * (_config.MouseSensitivity * 0.1f);

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            _upDownCamLimit = Mathf.Clamp(_upDownCamLimit - delta.y, -80f, 80f);
            _cameraPivot.localEulerAngles = new Vector3(_upDownCamLimit, 0f, 0f);
        }

        private void Move()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _controller == null) return;

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;

            var direction = (transform.right * input.x + transform.forward * input.y).normalized;
            var speed = keyboard.leftShiftKey.isPressed ? _config.SprintSpeed : _config.MoveSpeed;

            if (_controller.isGrounded && _verticalSpeed < 0f)
                _verticalSpeed = -2f;

            if (_controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
                _verticalSpeed = Mathf.Sqrt(_config.JumpHeight * -2f * Physics.gravity.y);

            _verticalSpeed += Physics.gravity.y * Time.deltaTime;

            var velocity = direction * speed + Vector3.up * _verticalSpeed;
            _controller.Move(velocity * Time.deltaTime);
        }
    }
}