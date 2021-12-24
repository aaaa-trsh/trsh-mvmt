

// mvmt3
using System;
using UnityEngine;

public class mvmt3 : MonoBehaviour {
    public float maxSpeed = 11f;
    public float accel = 100f;
    public float airAccel = 100f;
    public float friction = 5f;
    public bool speedometer;
    public float crouchHeight = 0.2f;
    public float jumpForce = 5f;
    public Vector2 inputScaling = Vector2.one;
    public bool lurchEnabled = true;
    private Rigidbody rb;
    private MovementUtils u;
    private bool slideQueued;
    private float slideTime;
    private float wallrunSpeed;
    private bool jumping;
    private bool wallrunning;
    private bool canWallrun = true;
    private bool wallrunEnabled = true;
    private float wallrunningTime;
    private float wallrunningDuration = 1.3f;
    private Vector3 oldWallrunningNormal = Vector3.zero;
    private float oldWallrunningHeight = Mathf.Infinity;
    private PlayerLook look;
    private Vector3 prevVelocity;
    private Tuple<Vector3, Vector3> wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);

    private void Start() {
        rb = GetComponent<Rigidbody>();
        look = GetComponent<PlayerLook>();
        u = GetComponent<MovementUtils>();
        u.onJump += Jump;
        u.onEnterGround += () => {
            oldWallrunningNormal = Vector3.zero;
            oldWallrunningHeight = Mathf.Infinity;
            wallrunEnabled = true;
            Debug.Log("wallrunENABLED");
            jumping = false;
            maxSpeed = 11f;
        };
        u.onExitGround += () => {
            if (u.crouchInput)
                slideQueued = true;
        };
        u.onCrouchInput += () => {
            slideQueued = true;
        };
        u.onReset += () => {
            WallrunExit();
            WallrunReset();
        };
        ApplyMovementSettings();
    }

    private void Jump() {
        Vector3 val = transform.TransformDirection(new Vector3(u.dirInput.x * inputScaling.x, 0f, u.dirInput.y * inputScaling.y));
        Vector3 wish = u.transformify(u.dirInput);
        jumping = true;
        if (!u.grounded) {
            Vector2 dirInput = u.dirInput;
            if (dirInput.magnitude > 0f) {
                float radians = Vector3.Angle(u.xzVelocity(), val) * ((float)Math.PI / 180f);
                float mag = Mathf.Max((0.4f * Mathf.Cos((radians * u.xzVelocity().magnitude) / 50) + 0.6f) * u.xzVelocity().magnitude, 10);
                rb.velocity = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * mag;// * Mathf.Cos();
            }
        }
        if (wallrunning) {
            // Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            // float wallSide = Mathf.Clamp(Vector3.Dot(u.transformify(u.smoothDirInput), wallDir) + Mathf.Lerp(Vector3.Dot(wallrunExitVel.normalized, wallDir), 0, wallrunningTime), -1, 1);
            // Vector3 velocity = wallDir.normalized * Mathf.Lerp(wallrunSpeed, 15f, 5f * wallrunningTime) * Mathf.Sign(wallSide) + u.wallHit.normal * 5f;

            // if (Vector3.Dot(u.wallHit.normal, transform.forward) < -0.7f)
            //     velocity = u.wallHit.normal * 7f;
                
            rb.velocity += u.wallHit.normal * 7f;
            WallrunExit();
            Invoke("EnableWallrun", 0.3f);
        }
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
    }

    private void EnableWallrun() {
        wallrunEnabled = true;
        Debug.Log("wallrunENABLED");
        u.onEnterGround -= EnableWallrun;
    }
    Vector3 wallrunStartVel;
    private void WallrunStart() {
        u.JumpReset();
        wallrunning = true;
        wallrunningTime = 0f;
        wallrunSpeed = Mathf.Max(Vector3.Scale(prevVelocity, new Vector3(1, 0, 1)).magnitude, 13f);
        wallrunStartVel = rb.velocity;
        rb.velocity = new Vector3(rb.velocity.x, Mathf.Max(rb.velocity.y, -1f), rb.velocity.z);
        wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
    }

    private bool WallrunCheck(bool onWall) {
        bool eject = false;
        if (wallData.Item1 != u.wallHit.normal) {
            // if (Vector3.Angle(wallData.Item1, u.wallNormal) > 90f) {
            //     eject = true;
            //     rb.velocity = ((u.wallHit.normal + wallData.Item1) * 3f);
            // }
            wallData = new Tuple<Vector3, Vector3>(u.wallHit.normal, u.wallHit.point);
        }
        Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
        // Debug.Log(Vector3.Dot(transform.forward, u.wallHit.normal));
        bool overtime = wallrunningTime > wallrunningDuration;
        // if (!Physics.Raycast(transform.position + Vector3.up, -u.wallHit.normal, u.capsuleCollider.radius + 0.4f, u.whatIsGround))
        //     climb = false;
        return overtime ||
                (onWall && Input.GetKey(KeyCode.LeftControl)) || 
                eject ||
                Vector3.Dot(transform.forward, u.wallHit.normal) > 0.8f;
    }

    private void WallrunExit() {
        canWallrun = false;
        wallrunEnabled = false;
        oldWallrunningHeight -= 0.4f;
    }

    private void WallrunReset() {
        wallrunning = false;
        Invoke("EnableWallrun", 0.3f);
        oldWallrunningHeight -= 0.4f;
        look.SetTargetDutch(0f);
        wallData = new Tuple<Vector3, Vector3>(Vector3.zero, Vector3.zero);
    }
    float airTime = 0;
    private void Update() {
        if (canWallrun) {
            airTime = 0;
            if (!wallrunning) {
                WallrunStart();
                Debug.Log("wallrunSTART");
            }
            // Debug.Log(u.wallHit.normal);
            bool onWall = Physics.Raycast(transform.position, -u.wallHit.normal, u.capsuleCollider.radius + .1f);
            if (WallrunCheck(onWall) || u.grounded) {
                rb.velocity += u.wallHit.normal * 4f;
                WallrunExit();
                Debug.Log("exit wallrun");
                return;
            }
            if (u.wallHit.normal.magnitude == 0f) {
                WallrunExit();
                return;
            }
            Vector3 wallDir = Vector3.Cross(wallData.Item1, Vector3.up);
            Vector3 smoothWish = u.transformify(u.smoothDirInput);
            Vector3 wallSpaceWish = new Vector3(Mathf.Clamp(Vector3.Dot(smoothWish, wallDir) + Mathf.Lerp(Vector3.Dot(wallrunStartVel.normalized, wallDir), 0, wallrunningTime), -1, 1), 0f, Vector3.Dot(smoothWish, wallData.Item1));
            Vector3 stickVelocity = -wallData.Item1 * 300f * Time.deltaTime * Vector3.Distance(transform.position - wallData.Item1 * u.capsuleCollider.radius, u.wallHit.point);
            Vector3 wallrunVelocity = wallDir * wallSpaceWish.x * Mathf.Lerp(wallrunSpeed, 15f, 5f * wallrunningTime) + stickVelocity;
            wallrunVelocity.y = Mathf.Max(wallrunStartVel.y, -1f) + -10*wallrunningTime;
            
            if (-0.8f > Vector3.Dot(transform.forward, wallData.Item1)) {
                if (u.dirInput.y > 0) {
                    wallrunVelocity.y = 5f;
                }
                // if (!Physics.Raycast(transform.position, -u.wallHit.normal, u.capsuleCollider.radius + 0.4f, u.whatIsGround)) {
                //     Physics.ComputePenetration(u.capsuleCollider, rb.position + transform.forward, Quaternion.identity, u.wallHit.collider, u.wallHit.collider.transform.position, u.wallHit.collider.transform.rotation, out Vector3 offset, out float dist);
                //     rb.position += transform.forward + offset*dist;
                // }
            }
            if (onWall) {
                rb.velocity = wallrunVelocity;
                wallrunningTime += Time.deltaTime;
            } else {
                rb.velocity += stickVelocity / 3;
            }

            rb.AddForce(-Vector3.up * 300f * Time.deltaTime, (ForceMode)5);
            

            oldWallrunningHeight = u.wallHit.point.y;
            look.SetTargetDutch(Mathf.Lerp(look.targetDutch, -15f * Mathf.Clamp01((wallrunningDuration + 0.5f - wallrunningTime) / wallrunningDuration) * Vector3.Dot(wallDir, transform.forward), 50f * Time.deltaTime));
            return;
        }
        // canWallrun = u.WallCheck() && (oldWallrunningNormal != u.wallHit.normal || oldWallrunningHeight > u.wallHit.point.y) && wallrunEnabled && !Input.GetKey((KeyCode)306) && u.dirInput.magnitude != 0f;
        canWallrun = u.WallCheck() && (oldWallrunningNormal == Vector3.zero ? true : (Mathf.Abs(Vector3.Angle(oldWallrunningNormal, u.wallHit.normal)) > 25f ? true : oldWallrunningHeight > u.wallHit.point.y)) && wallrunEnabled;
        
        if (wallrunning) {
            Debug.Log("wallrunOFF " + wallrunning);
            WallrunReset();
        }
        float speed = maxSpeed;

        float yVel = rb.velocity.y;
        Vector3 wish = u.transformify(u.dirInput);
        if (u.grounded) {
            airTime = 0;
            if (u.crouchInput && u.grounded && u.xzVelocity().magnitude > 5f)
                speed *= 1.5f;
            RaycastHit groundHit;
            if (Physics.Raycast(transform.position, -Vector3.up, out groundHit, 1.1f, u.whatIsGround))
                look.SetTargetHeight(u.crouchInput ? crouchHeight : 1f);

            if (Physics.Raycast(transform.position, Vector3.up, 0.4f, u.whatIsGround))
                u.crouchInput = true;

            u.capsuleCollider.height = u.crouchInput ? 0.5f : 2f;
            u.capsuleCollider.center = new Vector3(0f, u.crouchInput ? -0.5f : 0f, 0f);
            
            float walkAccel = accel;
            if (u.crouchInput) {
                if (rb.velocity.magnitude > 8f || slideQueued) {
                    if (slideQueued) {
                        slideQueued = false;
                        slideTime = 0f;
                    }
                    if (slideTime < 0.2f)
                        wish = u.xzVelocity().normalized;
                    else
                        wish = Vector3.zero;
                    slideTime += Time.deltaTime;
                    walkAccel = 7f;
                } else {
                    speed = 5f;
                }
            }

            if (rb.velocity.magnitude != 0f) {
                float drop = rb.velocity.magnitude * (u.crouchInput && rb.velocity.magnitude > 8f ? 0.2f : friction) * Time.deltaTime;
                rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0f) / rb.velocity.magnitude;
            }
            float curSpeed = Vector3.Dot(rb.velocity, wish);
            float addSpeed = Mathf.Clamp(speed - curSpeed, 0f, walkAccel * Time.deltaTime * speed);
            rb.velocity += addSpeed * wish;
            rb.velocity = new Vector3(rb.velocity.x, yVel, rb.velocity.z);
        }
        else {
            if (jumping && lurchEnabled) {
                float _lurchDirectionGain = airTime < .1f ? .5f : Mathf.Lerp(.5f, 0f, Mathf.Pow(((airTime - .1f) / (.4f - .1f)), 1));
                if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) && _lurchDirectionGain > 0) {
                    Vector2 newWish = Vector3.zero;
                    if (Input.GetKeyDown(KeyCode.W))
                        newWish += Vector2.up;
                    if (Input.GetKeyDown(KeyCode.A))
                        newWish += Vector2.left;
                    if (Input.GetKeyDown(KeyCode.S))
                        newWish += Vector2.down;
                    if (Input.GetKeyDown(KeyCode.D))
                        newWish += Vector2.right;
                    Vector3 transformifiedWish = u.transformify(newWish * new Vector2(1f, 1f)).normalized;
                    Vector3 xz = u.xzVelocity();
                    float multiplier = Mathf.Lerp(Mathf.Lerp(.7f, 1f, ((airTime - .2f) / (.4f - .2f))), 1f, Vector3.Dot(u.xzVelocity(), transformifiedWish));
                    rb.velocity = Vector3.Lerp(u.xzVelocity().normalized, transformifiedWish, _lurchDirectionGain).normalized * u.xzVelocity().magnitude * multiplier;
                    // maxSpeed *= multiplier;
                    rb.velocity += new Vector3(0, yVel, 0);
                }
            }
            wish = u.transformify(u.dirInput * new Vector2(1f, 1f)).normalized;
            float curSpeed = Vector3.Dot(rb.velocity, wish);
            float addSpeed = speed - curSpeed;
            float a = airAccel;
            if (u.collisionCount > 0)
                a *= 3f;
            if (addSpeed > 0f)
                rb.velocity += Mathf.Min(a * Time.deltaTime * speed, addSpeed) * wish;
            airTime += Time.deltaTime;
        }
        rb.AddForce(-Vector3.up * ((u.crouchInput && u.grounded) ? 1900 : 1160) * Time.deltaTime, ForceMode.Acceleration);
        prevVelocity = rb.velocity;
    }
    public void ApplyMovementSettings() {
        lurchEnabled = PlayerPrefs.GetInt("lurch_enabled", 0) == 0;
    }
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
        if(speedometer) {
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 30, 100, 20), "Speed: " + (int)Mathf.Round(rb.velocity.magnitude*100)/50, 1);
            DrawLabel(new Rect(Screen.width/2 - 50, Screen.height/2 + 60, 100, 20), "HSpeed: " + (int)Mathf.Round(u.xzVelocity().magnitude*100)/50, 1);
        }
    }
}
