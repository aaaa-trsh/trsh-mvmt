using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementUtils : MonoBehaviour
{
    public LayerMask whatIsGround;
    public bool speedometer;
    public float minGroundDotProduct = 0.2f;
    public bool canJump = true;
    public bool canDoubleJump = true;
    public bool crouchInput { get; set; }
    public bool sprintInput { get; set; }

    private bool _holdCrouchInput = false;
    public bool holdCrouchInput { get { return _holdCrouchInput; } set { crouchInput = false; _holdCrouchInput = value; } }
    private bool _holdSprintInput = false;
    public bool holdSprintInput { get { return _holdSprintInput; } set{ sprintInput = false; _holdSprintInput = value; } }

    public Vector3 groundNormal { get; private set; }
    public Vector3 wallNormal { get; private set; }

    public int groundContactCount { get; private set; }
    public bool grounded => groundContactCount > 0;// { get; private set; }
    public bool jumpInput { get; private set; }
    public Vector2 dirInput { get; private set; }
    public Vector2 smoothDirInput { get; private set; }
    public Vector2 signedDirInput { get; private set; }
    private Vector2 prevDirInput;
    public int jumps { get; private set; }

    public delegate void OnEnterGround();
    public OnEnterGround onEnterGround;
    public delegate void OnExitGround();
    public OnExitGround onExitGround;
    public delegate void OnJump();
    public OnJump onJump;
    public delegate void OnReset();
    public OnReset onReset;
    public delegate void OnCrouchInput();
    public OnCrouchInput onCrouchInput;

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
        smoothDirInput = Vector2.Lerp(smoothDirInput, dirInput, Time.deltaTime * 20);
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
        } else {
            crouchInput = false;
        }
        
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
            GetComponent<PlayerLook>().SetHorzontalRotation(271.278f);
            onReset?.Invoke();
        }
    }

    public RaycastHit wallHit;
    public bool WallCheck() {
        Vector3 right = Vector3.Cross(Vector3.up, rb.velocity.normalized);
        bool retval = //Physics.Raycast(transform.position, right, out wallHit, capsuleCollider.radius + 0.4f) 
                    //|| Physics.Raycast(transform.position, -right, out wallHit, capsuleCollider.radius + 0.4f)
                    Physics.Raycast(transform.position, transform.right, out wallHit, capsuleCollider.radius + .6f) 
                    || Physics.Raycast(transform.position, -transform.right, out wallHit, capsuleCollider.radius + .6f);
        if (retval) 
            wallNormal = wallHit.normal;
        else
            wallNormal = Vector3.zero;
        // wallNormal = contacts.Count == 0 ? wallHit.normal : (contacts.Select(c => c.normal).Aggregate(Vector3.zero, (acc, v) => acc + v) / contacts.Count).normalized;
        return retval && Mathf.Abs(wallHit.normal.y) < 0.1f && !grounded && !Physics.Raycast(transform.position, -Vector3.up, capsuleCollider.height + 0.1f, whatIsGround) && Vector3.Dot(transform.forward, wallHit.normal) < 0.8f;
    }

    public int groundColliders { get; private set; }

    void FixedUpdate() {
        if (grounded) {
			groundNormal.Normalize();
            jumps = 0;
            // canJump = true;
            // canDoubleJump = true;
		}
		else {
			groundNormal = Vector3.up;
		}
        groundContactCount = 0;
    }
    void OnCollisionEnter(Collision collision) {
        if (!grounded && whatIsGround.value == (whatIsGround.value | (1 << collision.gameObject.layer))) {
            EvaluateCollision(collision);
            if (groundContactCount > 0) {
                canJump = true;
                canDoubleJump = true;
                if (onEnterGround != null) onEnterGround();
            }
        }
        
    }
    void OnCollisionStay(Collision collision) {
        EvaluateCollision(collision);
        if (groundContactCount > 0) {
            canJump = true;
            canDoubleJump = true;
        }
    }
    void EvaluateCollision(Collision collision) {
        // get flattest normal
        Vector3 _groundNormal = Vector3.up;
        for (int i = 0; i < collision.contactCount; i++) {
			Vector3 normal = collision.GetContact(i).normal;
			if (normal.y >= minGroundDotProduct) {
				groundContactCount += 1;
				_groundNormal += normal;
			}
		}
        groundNormal = _groundNormal.normalized;
    }

    public void JumpReset() {
        canJump = true;
        canDoubleJump = true;
        jumps = 0;
    }

    public Vector3 xzVelocity() { return Vector3.Scale(rb.velocity, new Vector3(1, 0, 1)); }
    public Vector3 transformify(Vector2 dir) { return transform.TransformDirection(new Vector3(dir.x, 0, dir.y)); }

    void OnGUI() {
        var boldStyle = new GUIStyle(GUI.skin.label);
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.alignment = TextAnchor.UpperCenter;

        void DrawLabel(Rect rect, string value, float outline) {
            GUI.color = Color.black;

            GUI.Label(new Rect(rect.x+outline, rect.y, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x, rect.y-outline, rect.width, rect.height), value, boldStyle);

            GUI.Label(new Rect(rect.x+outline, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x+outline, rect.y-outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y+outline, rect.width, rect.height), value, boldStyle);
            GUI.Label(new Rect(rect.x-outline, rect.y-outline, rect.width, rect.height), value, boldStyle);
            GUI.color = Color.white;
            GUI.Label(rect, value, boldStyle);
        }

        // get screen rect
        if(speedometer){
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 30, 100, 20), "Speed: " + (int)Mathf.Round(rb.velocity.magnitude*100)/50, 1);
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 60, 100, 20), "HSpeed: " + (int)Mathf.Round(xzVelocity().magnitude*100)/50, 1);
        }
    }
}
