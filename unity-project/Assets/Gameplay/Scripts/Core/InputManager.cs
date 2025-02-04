using UnityEngine;

namespace Core
{
    public class InputManager : MonoBehaviour
    {
        // Don't create more than 1 instance, it will emit the event multiple times
        public static Vector2 GetMouse() => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        public static Vector2 GetMovement() => new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        private static bool instanceExists = false;
        private void Awake()
        {
            if (instanceExists)
            {
                Destroy(gameObject);
            }
            else
            {
                instanceExists = true;
                DontDestroyOnLoad(gameObject);
            }
        }
        void Update()
        {
            if (Input.GetButtonDown("Jump"))
            {
                EventManager.emitter.Emit(EventManager.PLAYER_JUMP);
            }

            if (Input.GetButton("Fire"))
            {
                EventManager.emitter.Emit(EventManager.PLAYER_FIRE);
            }
        }
    }
}