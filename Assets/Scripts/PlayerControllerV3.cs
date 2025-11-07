using UnityEngine;

namespace DefaultNamespace
{
    public class PlayerControllerV3 : MonoBehaviour
    {
        private float _moveX;

        [SerializeField] 
        private float _moveSpeed = 5.0f;

        [SerializeField] 
        private float _jumpForce = 10.0f;
        
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
                Flip(_moveX);
                SetRun();
                _rigidBody.linearVelocity = new Vector2(_moveX * _moveSpeed, _rigidBody.linearVelocity.y);
            }

            //점프 처리
            if (Input.GetButtonDown("Jump"))
            {
                Jump(_jumpForce);
            }
            
            if (Input.GetButtonDown("Fire1"))
            {
                Attack();
            }
        }
        
        private void Jump(float jumpForce)
        {
            _rigidBody.linearVelocity = new Vector2(_rigidBody.linearVelocity.x, 0); // Zero out vertical velocity
            _rigidBody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            _animator.SetTrigger("jump");
        }

        private void Attack()
        {
            _animator.SetTrigger("attack");
        }
        
        private void Flip(float direction)
        {
            if (direction == 0)
                return;
           
            transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
        }

        private void SetIdle()
        {
            _animator.SetInteger("state", 0);
        }

        private void SetRun()
        {
            _animator.SetInteger("state", 1);
        }
    }
}