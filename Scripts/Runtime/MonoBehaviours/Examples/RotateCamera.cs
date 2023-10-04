using UnityEngine;

namespace AssetLayer.Unity
{

    public class RotateCamera : MonoBehaviour
    {
        public float rotationSpeed = 2.0f; // rotation speed, editable in inspector
        public float distance = 10.0f; // distance from the center, editable in inspector
        public float pitchAdjustment = -6.0f; // pitch adjustment for the camera, editable in inspector
        public float verticalMovement = 0.5f; // amplitude of the vertical sinusoidal movement, editable in inspector
        public float verticalMovementSpeed = 0.5f; // speed of the vertical sinusoidal movement, editable in inspector

        private Vector3 centerPoint = Vector3.zero; // the center point of rotation, set to (0,0,0)

        void Update()
        {
            // Calculate the new position of the camera
            float horizontalPosition = distance * Mathf.Sin(Time.time * rotationSpeed);
            float verticalPosition = distance * Mathf.Cos(Time.time * rotationSpeed);

            // Add sinusoidal movement in the y axis to create a "wave-like" flight pattern
            float yOffset = verticalMovement * Mathf.Sin(Time.time * verticalMovementSpeed);

            // Update the position of the camera
            transform.position = new Vector3(horizontalPosition, yOffset, verticalPosition);

            // Make sure the camera is always looking at the center point
            transform.LookAt(centerPoint);

            // Adjust the pitch of the camera
            transform.Rotate(pitchAdjustment, 0, 0);
        }
    }
}
