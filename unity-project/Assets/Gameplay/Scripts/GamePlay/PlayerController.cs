using Core;
using Mirror;
using UnityEngine;

namespace GamePlay
{
    public class PlayerController : NetworkBehaviour
    {
        public float MouseSensitivity = 1f;
        float rotationX = 0f;
        readonly float MoveSpeed = 4f;
        readonly float JumpForce = 4f;
        readonly float GravityDownForce = 10f;
        Animator Animator;
        CharacterController Controller;
        public PlayerHealth Health;
        public Camera PlayerCamera;
        private Vector3 velocity = new();

        void Awake()
        {
            Controller = GetComponent<CharacterController>();
            Animator = GetComponentInChildren<Animator>();
            PlayerCamera = GetComponentInChildren<Camera>();
            Health = GetComponent<PlayerHealth>();
        }

        void Start()
        {
            Init();
        }

        void Init()
        {
            PlayerCamera.gameObject.SetActive(isLocalPlayer);

            Health.Init();
        }

        void OnEnable()
        {
            // add player jump event listener
            EventManager.emitter.On(EventManager.PLAYER_JUMP, HandleJump);
        }

        void OnDisable()
        {
            // remove player jump event listener
            EventManager.emitter.Off(EventManager.PLAYER_JUMP);
        }

        void Update()
        {
            if (!isLocalPlayer) return;
            if (Health.IsDeath()) return;

            HandleLook();
            HandleMovement();
            UpdateAnimator();
        }

        void HandleLook()
        {
            // lock if show cursor (interact with UI)
            if (Cursor.visible) return;

            // get mouse input for looking around
            Vector2 mouse = InputManager.GetMouse();

            // move horizontal -> rotate player transform
            transform.Rotate(0, mouse.x * MouseSensitivity, 0);

            // move vertical -> rotate camera only
            rotationX += mouse.y * MouseSensitivity;
            rotationX = Mathf.Clamp(rotationX, -89f, 89f); // limit rotation
            PlayerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }

        void HandleMovement()
        {
            // get movement input (WASD or arrow keys)
            Vector2 movement = InputManager.GetMovement();

            // create a movement vector based on player input
            float y = velocity.y;
            velocity = (transform.right * movement.x + transform.forward * movement.y) * MoveSpeed;
            velocity.y = y;

            // if player falling (not on ground) apply the gravity to the velocity
            if (!Controller.isGrounded)
            {
                velocity.y -= GravityDownForce * Time.deltaTime;
            }

            // handle move player with input
            Controller.Move(velocity * Time.deltaTime);
        }

        void HandleJump()
        {
            if (!isLocalPlayer) return;
            if (Health.IsDeath()) return;

            // check if user is on ground then apply jump force
            if (Controller.isGrounded)
            {
                velocity.y = JumpForce;
                Animator.SetTrigger("PLAYER_JUMP");
            }
        }

        void UpdateAnimator()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(Controller.velocity);
            Animator.SetBool("isRunning", localVelocity.z != 0 || localVelocity.x != 0);
            Animator.SetFloat("horizontal", localVelocity.z);
            Animator.SetFloat("vertical", localVelocity.x);
        }
    }
}