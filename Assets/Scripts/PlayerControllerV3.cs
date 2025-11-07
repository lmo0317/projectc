using UnityEngine;

namespace DefaultNamespace
{
    public class PlayerControllerV3 : MonoBehaviour
    {
        private float _moveX;

        [SerializeField] 
        private float _moveSpeed;
        
        [SerializeField]
        private Animator _animator;

        [SerializeField] 
        private Rigidbody2D _rigidBody;
        
        private void Update()
        {
            //이동 처리
            _moveX = Input.GetAxis("Horizontal");

            if (_moveX == 0)
            {
                SetIdle();
                _rigidBody.linearVelocity = new Vector2(0, _rigidBody.linearVelocity.y);
            }
            else
            {
                SetRun();
                _rigidBody.linearVelocity = new Vector2(_moveX * _moveSpeed, _rigidBody.linearVelocity.y);
            }

            //점프 처리
            if (Input.GetButtonDown("Jump"))
            {
                
            }
        }

        private void SetIdle()
        {
            _animator.SetInteger("state", 0);
        }

        private void SetRun()
        {
            _animator.SetInteger("state", 1);
        }

        private void SetJump()
        {
            
        }
        
        private void FlipSprite(float direction)
        {
            if (direction > 0)
            {
                // Moving right, flip sprite to the right
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (direction < 0)
            {
                // Moving left, flip sprite to the left
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }
}