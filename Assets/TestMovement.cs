using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class TestMovement : MonoBehaviour
{
    public float MAX_SPEED = 10f;
    public float MAX_ACCELERATION = 100f;
    public float MAX_AIR_ACCELERATION = 100f;
    public float friction = 5f;
    public float jumpForce = 5f;
    public Vector2 inputScaling = Vector2.one;
    private Rigidbody rb;
    private MovementUtils u;
    private bool slideQueued = false;
    private float slideTime = 0;
    private float wallrunSpeed = 0;
    private bool wallrunning = false, canWallrun = true, wallrunEnabled = true;
    private float wallrunningTime = 0;
    private float wallrunningDuration = 3;
    private Vector3 oldWallrunningNormal = Vector3.zero;
    private PlayerLook look;

    void Start() {
        rb = GetComponent<Rigidbody>();
        look = GetComponent<PlayerLook>();
        u = GetComponent<MovementUtils>();
        u.onJump += Jump;
        u.onEnterGround += () => {
            oldWallrunningNormal = Vector3.zero;
        };

        u.onCrouchInput += () => {
            Debug.Log("Crouch");
            slideQueued = true;
        };
    }

    void Jump() {
        Vector3 wish = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0, u.dirInput.y * inputScaling.y));
        if (!u.grounded && u.dirInput.magnitude > 0)
            rb.velocity = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * u.xzVelocity().magnitude;
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        // Debug.Log("Jump");

        if (wallrunning) {
            canWallrun = false;
            // Debug.Log("Walljump");
            Vector3 newVel = (u.wallHit.normal * 5 + transform.forward * wallrunSpeed).normalized * wallrunSpeed;
            newVel.y = jumpForce;
            rb.velocity = newVel;
            // Invoke("EnableWallrun", .3f);
        }
        else {
            oldWallrunningNormal = Vector3.zero;
        }
    }

    void EnableWallrun() { wallrunEnabled = true; }

    void Update() {
        look.SetTargetHeight(u.crouchInput ? .4f : 1f);
        if (u.WallCheck() && canWallrun) {
            if (!wallrunning) {
                u.JumpReset();
                wallrunning = true;
                wallrunningTime = 0;
                wallrunSpeed = Mathf.Max(u.xzVelocity().magnitude, 13f);
            }
            if (wallrunningTime > wallrunningDuration || Input.GetKey(KeyCode.LeftControl)) {
                canWallrun = false;
                rb.velocity += u.wallHit.normal * 2;
                return;
            }

            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            Vector3 wallrunVelocity = (wallDir * wallrunSpeed * Vector3.Dot(wallDir, transform.forward)) - (u.wallHit.normal *300 * Time.deltaTime * Vector3.Distance(transform.position-(u.wallHit.normal*u.capsuleCollider.radius), u.wallHit.point));
            wallrunVelocity.y = 0;

            rb.velocity = wallrunVelocity;
            oldWallrunningNormal = u.wallHit.normal;
            wallrunningTime += Time.deltaTime;

            look.SetTargetDutch(-15 * Mathf.Clamp01((wallrunningDuration+.5f-wallrunningTime)/wallrunningDuration) * Vector3.Dot(wallDir, transform.forward));
            
            return;
        }
        if (wallrunning) {
            wallrunning = false;
            wallrunSpeed = 0;
            look.SetTargetDutch(0);
        }

        canWallrun = u.WallCheck() && oldWallrunningNormal != u.wallHit.normal && wallrunEnabled; 

        u.onJump -= Jump;
        u.onJump += Jump;

        Vector3 wish = (u.crouchInput && u.grounded) ? Vector3.zero : transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0, u.dirInput.y * inputScaling.y));

        if (slideTime < .2f) {
            wish = u.xzVelocity().normalized;
        }

        if (u.grounded && rb.velocity.magnitude != 0) {
            if (slideQueued) {
                slideQueued = false;
                slideTime = 0;
            }
            float drop = rb.velocity.magnitude * (u.crouchInput ? .2f : friction) * Time.deltaTime;
            // float drop = rb.velocity.magnitude * friction * Time.deltaTime;

            float yVel = rb.velocity.y;
            rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0) / rb.velocity.magnitude; 
            rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
        }

        var current_speed = Vector3.Dot(rb.velocity, wish);
        var max_accel = (u.grounded ? MAX_ACCELERATION : MAX_AIR_ACCELERATION);
        var max_speed = MAX_SPEED;
        if (u.crouchInput && u.grounded)
            max_speed = MAX_SPEED * 1.5f;

        var add_speed = Mathf.Clamp(max_speed - current_speed, 0, max_accel * Time.deltaTime);

        rb.velocity = rb.velocity + add_speed * wish;
        // rb.AddForce(-Vector3.up * (u.crouchInput && u.grounded ? 60 : 20), ForceMode.Acceleration);
        rb.AddForce(-Vector3.up * 1100 * Time.deltaTime, ForceMode.Acceleration);
        slideTime += Time.deltaTime;
    }
}
