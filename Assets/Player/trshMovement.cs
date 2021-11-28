using System;
using UnityEngine;

public class trshMovement : MonoBehaviour
{
    public float jumpForce = 10.0F;
    public float speed = 20.0F;
    public LayerMask whatIsGround;
    public bool acceptInput = true;
    private bool grounded;
    private Vector3 groundNormal;
    private bool canJump;
    private bool canDoubleJump;
    
    private bool wallrunEnabled = true;
    private bool wallrunning = false;
    private float wallrunningSpeed = 0;
    
    private float localMaxVel;
    private Vector3 prevVelocity = Vector3.zero;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerLook look;
    private float inputHorizontal, inputVertical;
    
    private int oldCrouchMode = 0;
    private bool crouched = false;
    private Vector3 wishdir;
    void Awake() {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        look = GetComponent<PlayerLook>();
        oldCrouchMode = PlayerPrefs.GetInt("crouchMode");
    }

    void Update() { 
        wishdir = transform.TransformDirection(new Vector3(inputHorizontal, 0, inputVertical)).normalized;

        int crouchMode = PlayerPrefs.GetInt("crouch_mode");
        if (oldCrouchMode != crouchMode) {
            oldCrouchMode = crouchMode;
            if (crouchMode == 1) {
                toggle = false;
            }
        }

        if (PlayerPrefs.GetInt("crouch_mode") == 1) {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftShift)) {
                toggle = !toggle;
                Debug.Log(toggle);
            }
        }

        inputHorizontal = acceptInput ? Input.GetAxisRaw("Horizontal") : 0;
        inputVertical = acceptInput ? Input.GetAxisRaw("Vertical") : 0; 

        if ((canJump || canDoubleJump) && (Input.GetButtonDown("Jump") && acceptInput)) {
            
            Vector3 newVel = rb.velocity;
            if (!grounded){
                newVel = (wishdir * xzVelocity().magnitude) + rb.velocity;
                if (WallCheck()) {
                    wallrunEnabled = false;
                    localMaxVel = xzVelocity().magnitude;
                    newVel += wallHit.normal * jumpForce/2;
                    Invoke("EnableWallrun", .3f);
                }
            }

            newVel.y = jumpForce;

            rb.velocity = newVel;
            if (canJump) {
                canJump = false;
            } else if (canDoubleJump) {
                canDoubleJump = false;
            }
        }
    }
    void EnableWallrun() { wallrunEnabled = true; }
    void FixedUpdate() {        
        float dutch = 0;

        look.SetTargetHeight(CrouchInput() ? .4f : 1f);

        if (crouched != (CrouchInput() && grounded)) {
            crouched = (CrouchInput() && grounded);
            if (crouched) {
                float boost = Mathf.Clamp((new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) / Time.deltaTime).magnitude / 60, 0, 0.7f) + 3.5f;
                Vector3 newVel = (rb.velocity.magnitude > 10 ? rb.velocity : (rb.velocity.normalized * 10)) + rb.velocity.normalized * boost;
                Debug.Log("speed" + newVel.magnitude);
                newVel.y = rb.velocity.y;
                rb.velocity = newVel;
            }
        }
        
        if (grounded) {
            if (CrouchInput()) {
                rb.AddForce(Vector3.up * -40, ForceMode.Acceleration);
                // rb.AddForce(xzVelocity().normalized * 2, ForceMode.Acceleration);
                // dutch = xzVelocity().magnitude * Vector3.Dot(rb.velocity.normalized, transform.right);
                prevVelocity = rb.velocity; 
                return;
            }
            else {
                var newVel = Vector3.Cross(new Vector3(wishdir.z, 0, -wishdir.x), groundNormal) * speed;
                newVel.y = rb.velocity.y;
                rb.velocity = Vector3.MoveTowards(rb.velocity, newVel, 50 * Time.fixedDeltaTime);
            }
        } else {
            if (!WallCheck()) {
                rb.AddForce(wishdir * 30, ForceMode.Acceleration);
                var newVel = Vector3.ClampMagnitude(xzVelocity() + rb.velocity.normalized/5, Math.Max(localMaxVel, 2));
                newVel.y = rb.velocity.y;
                rb.velocity = newVel;
            }
            else if (wallrunEnabled) {
                if (!wallrunning) {
                    wallrunning = true;
                    canJump = true; 
                    canDoubleJump = true;
                    wallrunningSpeed = Mathf.Max(xzVelocity().magnitude < 15 ? xzVelocity().magnitude + 3 : xzVelocity().magnitude, 10f);
                }

                Vector3 wallDir = Vector3.Cross(wallHit.normal, Vector3.up);
                Vector3 wallrunVelocity = (wallDir * wallrunningSpeed * Mathf.Sign(Vector3.Dot(wallDir, transform.forward))) - (wallHit.normal * Vector3.Distance(transform.position, wallHit.point));
                wallrunVelocity.y = rb.velocity.y;
                
                rb.velocity = wallrunVelocity;
                rb.AddForce(Vector3.up * 14, ForceMode.Acceleration);

                dutch = -15 * Vector3.Dot(wallDir, transform.forward);
            }
        }
        
        if (!WallCheck() && wallrunning) {
            wallrunning = false;
        }

        look.SetTargetDutch(dutch);

        // clamp rb xz velocity to 20
        float yVel = rb.velocity.y;
        Vector3 clamped = Vector3.ClampMagnitude(xzVelocity(), 20);
        rb.velocity = new Vector3(clamped.x, yVel, clamped.z);
        rb.AddForce(Vector3.up * -20, ForceMode.Acceleration);
        prevVelocity = rb.velocity;
    }

    void OnCollisionStay(Collision collision) {
        if (!grounded && whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer)) && collision.contacts[0].normal.y > 0.2f) {
            grounded = true;
            canJump = true;
            canDoubleJump = true;
            if (groundNormal != collision.contacts[0].normal) {
                groundNormal = collision.contacts[0].normal;
            }
        }
        
        localMaxVel = xzVelocity().magnitude;
    }

    Vector3 xzVelocity() {
        return Vector3.Scale(rb.velocity, new Vector3(1, 0, 1));
    }

    RaycastHit wallHit;
    public bool WallCheck() {
        bool retval = false;
        Vector3 right = Vector3.Cross(Vector3.up, rb.velocity.normalized);
        if (Physics.Raycast(transform.position, right, out wallHit, capsuleCollider.radius + 0.9f))
        {
            retval = true;
        }
        else if(Physics.Raycast(transform.position, -right, out wallHit, capsuleCollider.radius + 0.9f))
        {
            retval = true;
        }
        return retval && Mathf.Abs(wallHit.normal.y) < 0.1f && wallrunEnabled && !grounded;
    }

    void OnCollisionExit(Collision collision) {
        if (whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer))) {
            grounded = false;
            groundNormal = Vector3.zero;
        }
    }

    bool toggle;
    bool CrouchInput() {
        if (!acceptInput)
            return false;

        bool inp = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift));
        if (PlayerPrefs.GetInt("crouch_mode") == 1) {
            return toggle;
        } else {
            return inp;
        }
    }

    // draw speed on screen
    // void OnGUI() {
    //     GUI.Label(new Rect(800, 400, 100, 20), "Speed: " + Mathf.Round(xzVelocity().magnitude*100)/100);
    // }
}
