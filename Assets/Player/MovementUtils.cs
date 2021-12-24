using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementUtils : MonoBehaviour
{
    public LayerMask whatIsGround;
    public bool crouchInput { get; set; }
    public bool sprintInput { get; set; }

    private bool _holdCrouchInput = false;
    public bool holdCrouchInput { get { return _holdCrouchInput; } set { crouchInput = false; _holdCrouchInput = value; } }
    private bool _holdSprintInput = false;
    public bool holdSprintInput { get { return _holdSprintInput; } set{ sprintInput = false; _holdSprintInput = value; } }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }
    public bool grounded { get; private set; }
    public bool jumpInput { get; private set; }
    public Vector2 dirInput { get; private set; }
    public Vector2 smoothDirInput { get; private set; }
    public Vector2 signedDirInput { get; private set; }
    private Vector2 prevDirInput;
    public bool canJump { get; private set; }
    public bool canDoubleJump { get; private set; }

    public delegate void OnEnterGround();
    public OnEnterGround onEnterGround;
    public delegate void OnExitGround();
    public OnExitGround onExitGround;
    public delegate void OnJump();
    public OnJump onJump;
    public delegate void OnCrouchInput();
    public OnCrouchInput onCrouchInput;
    public delegate void OnReset();
    public OnReset onReset;

    private Rigidbody rb;
    public CapsuleCollider capsuleCollider { get; private set; }
    public List<ContactPoint> contacts { get; private set; }

    void Awake() {
        holdSprintInput = true;
        holdCrouchInput = true;
        rb = GetComponent<Rigidbody>();
        contacts = new List<ContactPoint>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Update() {
        dirInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        smoothDirInput = Vector2.Lerp(smoothDirInput, dirInput, Time.deltaTime * 10);
        if (dirInput.magnitude > 0) {
            prevDirInput = dirInput;
        }
        signedDirInput = Vector3.Lerp(signedDirInput, prevDirInput, 10 * Time.deltaTime);
        
        if (PlayerPrefs.GetInt("crouch_mode") == 0 != holdCrouchInput)
            holdCrouchInput = PlayerPrefs.GetInt("crouch_mode") == 0;
        
        if (!WallCheck()) {
            var tempCrouchIn = crouchInput;
            if (holdCrouchInput) { crouchInput = Input.GetKey(KeyCode.LeftControl); }
            else if (Input.GetKeyDown(KeyCode.LeftControl)) { Debug.Log(crouchInput);crouchInput = !crouchInput; }
            if (tempCrouchIn != crouchInput && crouchInput) { onCrouchInput?.Invoke(); }
        } else { crouchInput = false; }
        
        if (holdSprintInput) { sprintInput = Input.GetKey(KeyCode.LeftShift); }
        else if (Input.GetKeyDown(KeyCode.LeftShift)) { sprintInput = !sprintInput; }

        jumpInput = Input.GetButton("Jump");
        if ((canJump || canDoubleJump) && Input.GetButtonDown("Jump")) {
            if (onJump != null) onJump();
            
            if (canJump) { canJump = false; }
            else if (canDoubleJump) { canDoubleJump = false; }
        }

        if (Input.GetKeyDown(KeyCode.R) || transform.position.y < -20) {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = new Vector3(0, 5f, 0);
            transform.rotation = Quaternion.Euler(0, 268, 0);
            GetComponent<PlayerLook>().SetHorzontalRotation(271.278f);
            onReset?.Invoke();
        }
    }
    public int collisionCount;
    public RaycastHit wallHit;
    public bool WallCheck() {
        Vector3 right = Vector3.Cross(Vector3.up, rb.velocity.normalized);
        bool retval = //Physics.Raycast(transform.position, right, out wallHit, capsuleCollider.radius + 0.4f) 
                    //|| Physics.Raycast(transform.position, -right, out wallHit, capsuleCollider.radius + 0.4f)
                    Physics.Raycast(transform.position - Vector3.up*.9f, transform.forward, out wallHit, capsuleCollider.radius + 0.4f, whatIsGround) 
                    || Physics.Raycast(transform.position, transform.right, out wallHit, capsuleCollider.radius + 0.4f, whatIsGround) 
                    || Physics.Raycast(transform.position, -transform.right, out wallHit, capsuleCollider.radius + 0.4f, whatIsGround);
        if (retval) 
            wallNormal = wallHit.normal;
        else
            wallNormal = Vector3.zero;
        // wallNormal = contacts.Count == 0 ? wallHit.normal : (contacts.Select(c => c.normal).Aggregate(Vector3.zero, (acc, v) => acc + v) / contacts.Count).normalized;
        return retval && Mathf.Abs(wallHit.normal.y) < 0.1f && !grounded && !Physics.Raycast(transform.position, -Vector3.up, capsuleCollider.height + 0.1f);
    }

    public int groundColliders { get; private set; }
    void OnCollisionEnter(Collision collision) {
        collisionCount++;
        // if (whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer)))
        //     groundColliders += 1;
        // contacts.Add(collision.contacts[0]);
    }
    void OnCollisionStay(Collision collision) {
        if (!grounded && whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer)) && collision.contacts[0].normal.y > 0.65f) {
            grounded = true;
            canJump = true;
            canDoubleJump = true;

            if (groundNormal != collision.contacts[0].normal)
                groundNormal = collision.contacts[0].normal;

            if (onEnterGround != null) onEnterGround();
        }
    }

    void OnCollisionExit(Collision collision) {
        collisionCount--;
        if (whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer))) {
            grounded = false;
            groundNormal = Vector3.zero;

            if (onExitGround != null) onExitGround();
            // groundColliders -= 1;
        }
        // contacts = contacts.Where(c => c.otherCollider != collision.collider).ToList();
    }

    public void JumpReset() {
        canJump = true;
        canDoubleJump = true;
    }

    public Vector3 xzVelocity() { return Vector3.Scale(rb.velocity, new Vector3(1, 0, 1)); }
    public Vector3 transformify(Vector2 dir) { return transform.TransformDirection(new Vector3(dir.x, 0, dir.y)); }

}
