using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementState {
    Running,
    Falling,
    Sliding,
    Crouching
}
public class trshMovement2 : MonoBehaviour
{
    public float jumpForce = 10.0F;
    public float walkSpeed = 8.0F;
    public float crouchSpeed = 5.0F;
    public float sprintBoost = 0.5f;
    public LayerMask whatIsGround;
    public bool acceptInput = true;
    public bool autoSprint = true;

    private bool sprint = false, crouch = false;
    private bool grounded, canJump, canDoubleJump, wallrunEnabled;
    private Vector3 wishdir, inputDir, groundNormal;
    private Rigidbody rb;
    private PlayerLook look;
    private CapsuleCollider capsuleCollider;
    private float inputHorizontal, inputVertical;

    public MovementState state = MovementState.Falling;
    private MovementState prevState = MovementState.Falling;

    void Start() {
        rb = GetComponent<Rigidbody>();
        look = GetComponent<PlayerLook>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Update() {
        if (acceptInput) {
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical"); 

            crouch = Input.GetKey(KeyCode.LeftShift);
            if (autoSprint) { sprint = inputVertical > 0; }
            else { sprint = Input.GetKey(KeyCode.LeftControl); }
        } else {
            inputHorizontal = 0;
            inputVertical = 0;
            sprint = false;
            crouch = false;
        }

        inputDir = new Vector3(inputHorizontal, 0, inputVertical);
        wishdir = transform.TransformDirection(inputDir).normalized;


        if ((canJump || canDoubleJump) && (Input.GetButtonDown("Jump") && acceptInput)) {
            state = MovementState.Falling;
            Vector3 newVel = rb.velocity;
            if (!grounded){
                newVel = (wishdir * xzVelocity().magnitude);
            }

            newVel.y = jumpForce;
            rb.velocity = newVel;

            if (canJump) { canJump = false; }
            else if (canDoubleJump) { canDoubleJump = false; }
        }
    }

    void FixedUpdate() {
        if (grounded) {
            Vector3 newVel = rb.velocity;
            Vector3 rightWishdir = new Vector3(wishdir.z, 0, -wishdir.x);
            if (crouch) {
                if (rb.velocity.magnitude < 7f) {
                    // crouch
                    Debug.Log(xzVelocity().magnitude);
                    newVel = Vector3.Cross(rightWishdir, groundNormal) * crouchSpeed;
                    state = MovementState.Crouching;
                    rb.velocity = newVel;
                } else {
                    // slide
                    rb.AddForce(Vector3.up * -60);
                    state = MovementState.Sliding;
                }
            } else {
                Debug.DrawRay(transform.position-Vector3.up/2, Vector3.Cross(rightWishdir, groundNormal), Color.red);
                // walk/sprint
                newVel = (Vector3.Cross(rightWishdir, groundNormal).normalized + (Vector3.Cross(rightWishdir, groundNormal).normalized * sprintBoost * (sprint ? 1 : 0))) * walkSpeed;
                state = MovementState.Running;
                rb.velocity = newVel;
            }
        } else {
            state = MovementState.Falling;
            rb.AddForce(Vector3.up * -20, ForceMode.Acceleration);
        }
    }

    void LateUpdate() {
        if (prevState != state) {
            StateExit(prevState);
            StateEnter(state, prevState);
            prevState = state;
        }
    }
    
    void StateExit(MovementState state) {}
    void StateEnter(MovementState startState, MovementState prev) {
        switch (startState) {
            case MovementState.Falling:
                // switch to sliding if correct velocity and raycast down
                if (prev == MovementState.Sliding && crouch && rb.velocity.magnitude > 9) {
                    state = MovementState.Sliding;
                }
                break;
            case MovementState.Running:
                break;
            case MovementState.Crouching:
                if (rb.velocity.magnitude > 7f) {
                    state = MovementState.Sliding;
                }
                break;
            case MovementState.Sliding:
                rb.velocity *= 1.5f;
                break;
        }
    }
    RaycastHit wallHit;
    public bool WallCheck() {
        Vector3 right = Vector3.Cross(Vector3.up, rb.velocity.normalized);

        bool retval = Physics.Raycast(transform.position, right, out wallHit, capsuleCollider.radius + 0.9f) 
                    || Physics.Raycast(transform.position, -right, out wallHit, capsuleCollider.radius + 0.9f);
        return retval && Mathf.Abs(wallHit.normal.y) < 0.1f && wallrunEnabled && !grounded;
    }

    void OnCollisionStay(Collision collision) {
        if (!grounded && whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer)) && collision.contacts[0].normal.y > 0.2f) {
            grounded = true;
            canJump = true;
            canDoubleJump = true;

            if (groundNormal != collision.contacts[0].normal) {
                groundNormal = collision.contacts[0].normal;
            }

            if (crouch) {
                if (xzVelocity().magnitude < 7f) {
                    // crouch
                    Debug.Log(xzVelocity().magnitude);
                    state = MovementState.Crouching;
                } else {
                    // slide
                    state = MovementState.Sliding;
                }
            }
        }
    }

    void OnCollisionExit(Collision collision) {
        if (whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer))) {
            grounded = false;
            groundNormal = Vector3.zero;
        }
    }

    void EnableWallrun() { wallrunEnabled = true; }
    Vector3 xzVelocity() { return Vector3.Scale(rb.velocity, new Vector3(1, 0, 1)); }

    void OnGUI() {
        // GUI.Label(new Rect(800, 400, 100, 20), "Speed: " + Mathf.Round(rb.velocity.magnitude*100)/100);
    }
}
