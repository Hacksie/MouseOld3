using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace HackedDesign
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PhysicsController : MonoBehaviour
    {
        [Header("Game Objects")]
        [SerializeField] private Rigidbody2D body = null;
        [Header("Settings")]
        [SerializeField] private PhysicsSettings settings = null;
        [SerializeField] private LayerMask environmentMask;

        [Header("Events")]
        [SerializeField] private UnityEvent fallDeathEvent;
        [Header("Data")]
        [SerializeField] private bool headBlocked;
        [SerializeField] private bool onGround;
        [SerializeField] private bool onWall;
        [SerializeField] private Vector2 contactNormal;

        [Header("Settings")]
        [SerializeField] private Transform ledgeDetectionPoint;
        [SerializeField] private float ledgeRadius = 0.2f;
        [SerializeField] public Vector3 ledgeOffsetStart;
        [SerializeField] public Vector3 ledgeOffsetEnd;


        public Rigidbody2D Body { get { return body; } }
        public bool OnGround { get => onGround; private set => onGround = value; }
        public bool OnWall { get => onWall; private set => onWall = value; }
        public float Friction { get; private set; }
        public Vector2 ContactNormal { get => contactNormal; private set => contactNormal = value; }
        public bool WallJumping { get; private set; }

        public float FallTime { get => lastFallTime; }

        private float lastFallTime = 0;
        private float fallingTime = 0;
        private Vector2 velocity;
        private int jumpPhase;
        private float coyoteCounter, jumpBufferCounter;
        private bool isJumping;
        private float wallDirectionX;
        private float movementSpeed = 0;

        public bool climbingLedge = false;

        
        //public Vector3 ledgePosition;

        private bool canGrabLedge = true;

        public void Stop() => body.linearVelocity = Vector2.zero;

        public void Freeze()
        {
            Stop();
            body.gravityScale = 0;
        }

        public void Unfreeze()
        {
            body.gravityScale = 1;
        }

        private bool DetectEnvForLedgeGrab() => Physics2D.OverlapCircle(ledgeDetectionPoint.position, ledgeRadius, environmentMask) != null;

        private bool DetectClearAirForLedgeGrab()
        {
            return Physics2D.OverlapCircle(ledgeDetectionPoint.position + new Vector3(0, 2 * ledgeRadius, 0), ledgeRadius, environmentMask) == null;
        } 


        public bool LedgeEdgeStart()
        {
            if (climbingLedge) return false;
            Debug.DrawRay(ledgeDetectionPoint.position, transform.right * -0.35f, Color.white);

            //if(Physics2D.OverlapCircle(ledgeDetectionPoint.position, ledgeRadius, environmentMask) != null)
            //{
            //    Debug.Log("Test");
            //}

            // FIXME: Don't use onwall, use a circle, and a box on top
            //https://www.youtube.com/watch?v=Kh5n63A-YBw
            return DetectEnvForLedgeGrab() && DetectClearAirForLedgeGrab();
        }

        public void LedgeEdgeEnd()
        {
            Debug.Log("Ledge end " + OnWall);
            body.transform.position = transform.position + new Vector3(ledgeOffsetEnd.x * Mathf.Sign(transform.right.x), ledgeOffsetEnd.y, 0);
            body.gravityScale = 1;
            climbingLedge = false;
            Invoke(nameof(ClearCanGrabLedge), 0.1f); // FIXME:
            
        }

        private void ClearCanGrabLedge()
        {
            canGrabLedge = true;
        }

        

        public void FixedMovement(Vector2 desiredVelocity, bool jumpFlag, bool jumpHoldFlag)
        {
            velocity = body.linearVelocity;

            var acceleration = OnGround ? settings.maxAcceleration : settings.maxAirAcceleration;
            var maxSpeedChange = acceleration * Time.fixedDeltaTime;

            var contactPerp = -1 * Vector2.Perpendicular(ContactNormal).normalized;

            if (body.transform.position.y < settings.fallingDeathYLimit)
            {
                Debug.Log("Falling death");
                body.linearVelocity = Vector3.zero;
                body.gravityScale = 0;
                fallDeathEvent.Invoke();
                return;
            }

            var forcedMove = Vector2.zero;

            if (canGrabLedge && LedgeEdgeStart())
            {
                canGrabLedge = false;
                Debug.Log("Ledge Edge Start");
                climbingLedge = true;
                body.transform.position = ledgeDetectionPoint.position + new Vector3(ledgeOffsetStart.x * Mathf.Sign(transform.right.x), ledgeOffsetStart.y, 0);
                body.linearVelocity = Vector3.zero;
                body.gravityScale = 0;
                //return;
                //    forcedMove = (transform.right + transform.up) * Time.fixedDeltaTime * 3;
                //    //desiredVelocity += new Vector2(upright.x, upright.y);
            }

            if (climbingLedge)
            {

                //Debug.Log("Climbing");
                //Debug.Break();
                //climbingLedge = false;
                return;
            }

            //if (LedgeEdgeStart())
            //{
            //    forcedMove = (transform.right + transform.up) * Time.fixedDeltaTime * 3;
            //    /*
            //    Debug.Log("Ledge Edge");
                
            //    Debug.DrawRay(transform.position, upright, Color.magenta);*/
            //    //body.AddForce(upright, ForceMode2D.Impulse);
            //    //desiredVelocity += 
            //    //velocity += 
            //    //body.MovePosition(upright * Time.fixedDeltaTime * 10);
            //    //body.AddForce(upright, ForceMode2D.Impulse);
            //    //body.MovePosition(body.position += ;
            //}

            


            if (OnWall)
            {
                //if (velocity.y < -settings.wallSlideMaxSpeed)
                //{
                //    velocity.y = -settings.wallSlideMaxSpeed;
                //}
            }

            //if ((OnWall && velocity.x == 0) || OnGround)
            //{
            //    WallJumping = false;
            //}

            if (jumpFlag && OnWall && !OnGround)
            {
                if (-wallDirectionX == desiredVelocity.x)
                {
                    velocity = new Vector2(settings.wallJumpClimb.x * wallDirectionX, settings.wallJumpClimb.y);
                    WallJumping = true;
                    jumpFlag = false;
                }
                else if (desiredVelocity.x == 0)
                {
                    velocity = new Vector2(settings.wallJumpBounce.x * wallDirectionX, settings.wallJumpBounce.y);
                    WallJumping = true;
                    jumpFlag = false;
                }
                else
                {
                    velocity = new Vector2(settings.wallJumpLeap.x * wallDirectionX, settings.wallJumpLeap.y);
                    WallJumping = true;
                    jumpFlag = false;
                }
            }

            if (OnGround) // && body.linearVelocity.y == 0)
            {
                jumpPhase = 0;
                coyoteCounter = settings.coyoteTime;
                if (fallingTime > 0)
                {
                    lastFallTime = Time.time - fallingTime;
                }

                fallingTime = 0;

                isJumping = false;
            }
            else
            {

                coyoteCounter -= Time.fixedDeltaTime;
            }


            if (jumpFlag)
            {
                jumpBufferCounter = settings.jumpBufferTime;
            }
            else if (!jumpFlag && jumpBufferCounter > 0)
            {
                jumpBufferCounter -= Time.fixedDeltaTime;
            }

            if (jumpBufferCounter > 0)
            {
                CalcJumpVelocity();
            }

            if (OnWall && settings.wallStick && !jumpHoldFlag && !DetectClearAirForLedgeGrab())
            {
                velocity = new Vector2(0, -settings.wallSlideMaxSpeed);
                body.gravityScale = 0;

                if (fallingTime > 0)
                {
                    lastFallTime = Time.time - fallingTime;
                }

                fallingTime = 0;
            }
            else if (jumpHoldFlag && body.linearVelocity.y > 0)
            {
                body.gravityScale = settings.upwardMovementMultiplier;
                if (fallingTime > 0)
                {
                    lastFallTime = Time.time - fallingTime;
                }
                fallingTime = 0;
            }
            else if (body.linearVelocity.y == 0)
            {
                body.gravityScale = settings.defaultGravityScale;
                if (fallingTime > 0)
                {
                    lastFallTime = Time.time - fallingTime;
                }
                fallingTime = 0;
            }
            else if (OnGround && body.linearVelocity.y < 0)
            {
                body.gravityScale = settings.defaultGravityScale;

            }
            else if (!OnWall && (!jumpHoldFlag || body.linearVelocity.y < 0))
            {
                body.gravityScale = settings.downwardMovementMultiplier;
                if (fallingTime == 0)
                {
                    fallingTime = Time.time;
                }

            }

            movementSpeed = Mathf.MoveTowards(movementSpeed, desiredVelocity.x, maxSpeedChange);

            if (OnGround && !OnWall && !jumpFlag)
            {
                velocity.x = contactPerp.x * movementSpeed;
            }
            else
            {
                velocity.x = movementSpeed;

            }

            //velocity.y += desiredVelocity.y;
            velocity += forcedMove;

            body.linearVelocity = velocity;
        }

        private void CalcJumpVelocity()
        {
            if (coyoteCounter > 0 || (jumpPhase < settings.maxAirJumps && isJumping))
            {
                if (isJumping)
                {
                    jumpPhase += 1;
                }

                jumpBufferCounter = 0;
                coyoteCounter = 0;
                var jumpSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * settings.jumpHeight);
                isJumping = true;

                if (velocity.y > 0f)
                {
                    jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
                }
                else if (velocity.y < 0f)
                {
                    jumpSpeed += Mathf.Abs(body.linearVelocity.y);
                }
                velocity.y += jumpSpeed;
            }
        }


        private void OnCollisionExit2D(Collision2D collision)
        {
            //FIXME: Tilemap collision boxes cause this to flash
            OnGround = false;
            OnWall = false;
            Friction = 0;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            EvaluateCollision(collision);
            RetrieveFriction(collision);
            if (OnWall && !OnGround && WallJumping)
            {
                body.linearVelocity = Vector2.zero;
            }

            //if (fallingTime > 0 && Time.time - fallingTime > settings.fallingTimeDeath)
            //{
            //    Debug.Log("Falling death");
            //    body.linearVelocity = Vector3.zero;
            //    body.gravityScale = 0;
            //    fallDeathEvent.Invoke();
            //}
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            EvaluateCollision(collision);
            RetrieveFriction(collision);
        }

        private void EvaluateCollision(Collision2D collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactNormal = collision.GetContact(i).normal;
                OnGround |= ContactNormal.y >= 0.5f;
                OnWall |= Mathf.Abs(ContactNormal.x) >= 0.9f;
            }
        }

        private void RetrieveFriction(Collision2D collision)
        {
            var material = collision?.rigidbody?.sharedMaterial;

            Friction = 0;

            if (material != null)
            {
                Friction = material.friction;
            }
        }
    }
}