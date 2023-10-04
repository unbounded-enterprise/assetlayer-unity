using UnityEngine;

namespace AssetLayer.Unity
{

    public class CoinPickup : MonoBehaviour
    {
        // OnTriggerEnter is called when the collider other enters the trigger
        private void OnTriggerEnter(Collider other)
        {
            // Check if the other object has a tag 'Player'
            if (other.CompareTag("Player"))
            {
                // Get the AudioSource from the current GameObject
                AudioSource audioSource = this.GetComponent<AudioSource>();

                if (audioSource == null)
                {
                    Debug.LogError("AudioSource component not found on the current GameObject.");
                    return;
                }

                AudioClip pickupSound = audioSource.clip;

                if (pickupSound == null)
                {
                    Debug.LogError("No Audio clip found in the AudioSource.");
                    return;
                }

                // Play the sound
                audioSource.PlayOneShot(pickupSound);

                // Disable the coin's collider so it can't be picked up again
                GetComponent<Collider>().enabled = false;

                // Also disable the coin's renderer so it becomes invisible
                GetComponent<Renderer>().enabled = false;

                // Destroy the coin after the length of the pickupSound clip, so it doesn't destroy the coin before the sound has a chance to play.
                Destroy(gameObject, pickupSound.length);
            }
        }
    }
}