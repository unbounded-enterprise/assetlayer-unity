
using UnityEngine;

namespace AssetLayer.Unity
{

    public class PlayerController : MonoBehaviour
    {
        public float speed = 5.0f;
        private Animator animator;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (!animator)
            {
                animator = GetComponent<Animator>();
                // Debug.Log("Animator component acquired: " + (animator != null));
            }
            if (animator == null)
            {
                // Debug.Log("Animator component is missing");
                return;
            }
            if (!ParameterExists("isAttacking") || !ParameterExists("isDefending") || !ParameterExists("isDizzy") || !ParameterExists("isJumping"))
            {
                Debug.Log("One or more animator parameters are missing");
                return;
            }


            if (!animator)
            {
                animator = GetComponent<Animator>();
            }
            if (animator == null || !ParameterExists("isAttacking") || !ParameterExists("isDefending") || !ParameterExists("isDizzy") || !ParameterExists("isJumping"))
                return;

            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

            animator.SetBool("isAttacking", false);
            animator.SetBool("isDefending", false);
            animator.SetBool("isDizzy", false);
            animator.SetBool("isJumping", false);
            /* animator.SetBool("isTest", false);

            if (Input.GetKey(KeyCode.T))
            {
                animator.SetBool("isTest", true);
            } */

            if (Input.GetKey(KeyCode.UpArrow))
            {
                animator.SetBool("isAttacking", true);
                Debug.Log("Player isAttacking: " + animator.GetBool("isAttacking"));
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                animator.SetBool("isDefending", true);
                Debug.Log("Player isDefending: " + animator.GetBool("isDefending"));
            }
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
            {
                animator.SetBool("isDizzy", true);
                Debug.Log("Player isDizzy: " + animator.GetBool("isDizzy"));
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                animator.SetBool("isJumping", true);
                Debug.Log("Player isJumping: " + animator.GetBool("isJumping"));
            }

            if (Input.anyKey)
            {
                Debug.Log("A key has been pressed");
            }
        }

        private bool ParameterExists(string parameterName)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == parameterName)
                    return true;
            }
            return false;
        }
    }
}