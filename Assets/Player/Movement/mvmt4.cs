using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AirSettings {
    public float gravity;
    public float jumpForce;
    public float speed;
    public float acceleration;
    public float coyoteTime = 0.2f;


    [Header("Lurch Settings")]
    public float lurchPeriodMin = 0.2f;
    public float lurchPeriodMax = 0.5f;
    public float lurchDirectionGain = 0.7f;
    public float lurchVelocityGain = 0.7f;
    public Image lurchDisplay;
}

[System.Serializable]
public class RunSettings {
    public float friction;
    public float walkSpeed;
    public float runSpeed;
    public float acceleration;
}

[System.Serializable]
public class SlideSettings {
    public float friction;
    public float cameraHeight;
    public float jumpForce;
    public float slideSpeed;
    public float acceleration;
    public float decceleration;
}
public class mvmt4 : MonoBehaviour
{
    public AirSettings airSettings;
    public RunSettings groundSettings;
    public SlideSettings slideSettings;

    private MovementUtils u;
    private mvmt4State state;

    [HideInInspector]
    public Rigidbody rb;
    [HideInInspector]
    public Vector3 oldWallNormal;
    [HideInInspector]
    public float oldWallHeight;
    public bool wallrunTimeout = false;

    void Start() {
        oldWallHeight = Mathf.Infinity;
        u = GetComponent<MovementUtils>();  
        rb = GetComponent<Rigidbody>();
        if (u.grounded)
            state = new mvmt4Run(this, u);
        else
            state = new mvmt4Air(this, u, false);
    }

    void Update() {
        state.Update();
    }

    void FixedUpdate() {
        state.FixedUpdate();
    }

    void LateUpdate() {
        var newState = state.CheckTransition();
        if (newState != null) {
            Debug.Log("Transitioning to " + newState.GetType().Name);
            state.End();
            state = newState;
            state.Start();
        }
    }

    public void WallrunTimeout() {
        wallrunTimeout = true;
        Invoke("StopWallrunTimeout", .3f);
    }
    void StopWallrunTimeout() {
        wallrunTimeout = false;
    }
}

public abstract class mvmt4State {
    public abstract void Start();
    public abstract void Update();
    public abstract void FixedUpdate();
    public abstract void End();
    public abstract mvmt4State CheckTransition();
}

class mvmt4Run : mvmt4State {
    MovementUtils u;
    mvmt4 mvmt;
    Rigidbody rb;
    bool jumping = false;
    public mvmt4Run(mvmt4 mvmt, MovementUtils u) {
        this.mvmt = mvmt;
        this.u = u;
        this.rb = mvmt.rb;
    }

    public override void Start() {
        u.onJump += Jump;
        mvmt.GetComponent<PlayerLook>().SetTargetHeight();
    }
    public void Jump() {
        jumping = true;
    }
    public override void Update() {
    }

    public override void FixedUpdate() {
        if (!jumping) {
            Vector3 wish = u.transformify(u.dirInput);
            wish = Vector3.Cross(new Vector3(wish.z, 0, -wish.x), u.groundNormal);
            
            // friction
            if (rb.velocity.magnitude != 0) {
                float drop = rb.velocity.magnitude * mvmt.groundSettings.friction * Time.deltaTime;
                rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0) / rb.velocity.magnitude; 
            }
            
            var curSpeed = Vector3.Dot(rb.velocity, wish);
            var _accel = mvmt.groundSettings.acceleration;
            
            var speed = mvmt.groundSettings.walkSpeed;
            if (u.dirInput.y > 0)
                speed = mvmt.groundSettings.runSpeed;

            var addSpeed = speed - curSpeed;
            if (addSpeed > 0) {
                var _accelSpeed = Mathf.Min(_accel * Time.deltaTime * speed, addSpeed);
                rb.velocity += wish * _accelSpeed;
            }
        }
    }

    public override void End() {
        u.onJump -= Jump;
    }

    public override mvmt4State CheckTransition() {
        if (!u.grounded) {
            return new mvmt4Air(mvmt, u, jumping);
        } else {
            if (u.crouchInput) {
                return new mvmt4Slide(mvmt, u);
            }
        }
        if (jumping) {
            rb.velocity = new Vector3(rb.velocity.x, mvmt.airSettings.jumpForce, rb.velocity.z);
        }
        return null;
    }
}

class mvmt4Slide : mvmt4State {
    MovementUtils u;
    mvmt4 mvmt;
    Rigidbody rb;
    bool jumping;
    Vector3 dir;
    float slideTime = 0;
    public mvmt4Slide(mvmt4 mvmt, MovementUtils u) {
        this.mvmt = mvmt;
        this.u = u;
        this.rb = mvmt.rb;
        this.dir = rb.velocity.normalized;
    }

    public override void Start() {
        u.onJump += Jump;
        mvmt.GetComponent<PlayerLook>().SetTargetHeight(mvmt.slideSettings.cameraHeight);
    }
    public void Jump() { jumping = true; }
    public override void Update() { slideTime += Time.deltaTime; }
    public override void FixedUpdate() {
        if (!jumping) {
            Vector3 wish = mvmt.transform.forward;
            if (slideTime > 0.2f) 
                wish = Vector3.zero;

            // friction
            if (rb.velocity.magnitude != 0) {
                float drop = rb.velocity.magnitude * mvmt.slideSettings.friction * Time.deltaTime;
                rb.velocity *= Mathf.Max(rb.velocity.magnitude - drop, 0) / rb.velocity.magnitude; 
            }
            
            var curSpeed = Vector3.Dot(rb.velocity, wish);
            var _accel = mvmt.slideSettings.acceleration;
            var speed = mvmt.slideSettings.slideSpeed;

            var addSpeed = speed - curSpeed;
            var _accelSpeed = Mathf.Min(_accel * Time.deltaTime * speed, addSpeed);
            rb.velocity += wish * _accelSpeed;
        }
    }

    public override void End() {
        u.onJump -= Jump;
    }

    public override mvmt4State CheckTransition() {
        if (!u.grounded) {
            return new mvmt4Air(mvmt, u, jumping);
        } else {
            if (!u.crouchInput) {
                return new mvmt4Run(mvmt, u);
            }
        }
        if (jumping) {
            rb.velocity = new Vector3(rb.velocity.x, mvmt.airSettings.jumpForce, rb.velocity.z);
        }
        return null;
    }
}

class mvmt4Air : mvmt4State {
    MovementUtils u;
    mvmt4 mvmt;
    Rigidbody rb;
    bool fromJump;
    float airTime;
    bool jumping;
    PlayerLook look;
    public mvmt4Air(mvmt4 mvmt, MovementUtils u, bool fromJump) {
        this.mvmt = mvmt;
        this.u = u;
        this.rb = mvmt.rb;
        this.fromJump = fromJump;
        this.look = mvmt.GetComponent<PlayerLook>();
        airTime = 0;
    }

    public override void Start() {
        u.onJump += Jump;
    }
    public void Jump() {
        jumping = true;
    }

    public override void Update() {
        airTime += Time.deltaTime;
        if (airTime > mvmt.airSettings.coyoteTime && u.canJump && !u.grounded) {
            u.canJump = false;
            // Debug.Log("OFFF!!");
        }
    }

    public override void FixedUpdate() {
        Vector3 wish = u.transformify(u.dirInput);
        float _lurchDirectionGain = airTime < mvmt.airSettings.lurchPeriodMin ? mvmt.airSettings.lurchDirectionGain : Mathf.Lerp(mvmt.airSettings.lurchDirectionGain, 0, (airTime - mvmt.airSettings.lurchPeriodMin) / (mvmt.airSettings.lurchPeriodMax - mvmt.airSettings.lurchPeriodMin));
        if (fromJump) {
            mvmt.airSettings.lurchDisplay.fillAmount = _lurchDirectionGain;//1 - (airTime / mvmt.airSettings.lurchPeriodMax);
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
                Vector3 transformifiedWish = u.transformify(newWish);
                float yVel = rb.velocity.y;
                Vector3 xz = u.xzVelocity();
                rb.velocity = Vector3.Lerp(u.xzVelocity(), transformifiedWish * u.xzVelocity().magnitude * mvmt.airSettings.lurchVelocityGain, _lurchDirectionGain);
                rb.velocity += new Vector3(0, yVel, 0);
            }
        }
        var curSpeed = Vector3.Dot(rb.velocity, wish);
        var _accel = mvmt.airSettings.acceleration;
        var addSpeed = mvmt.airSettings.speed - curSpeed;
        if (addSpeed > 0) {
            var _accelSpeed = Mathf.Min(_accel * Time.deltaTime * mvmt.airSettings.speed, addSpeed);
            rb.velocity += _accelSpeed * wish;
        }
        rb.AddForce(Vector3.up * -mvmt.airSettings.gravity, ForceMode.Acceleration);

        // project capsule to velocity
        // Vector3 capsuleOffset = mvmt.transform.position + Vector3.up * (u.capsuleCollider.height/2 - u.capsuleCollider.radius);
    }

    public override void End() {
        u.onJump -= Jump;
        look.SetTargetDutch(0);
    }

    public override mvmt4State CheckTransition() {
        if (u.grounded) {
            return u.crouchInput ? (mvmt4State)new mvmt4Slide(mvmt, u) : (mvmt4State)new mvmt4Run(mvmt, u);
        }
        if (jumping) {
            Vector3 wish = u.transformify(u.dirInput);
            var radians = Vector3.Angle(u.xzVelocity(), wish) * Mathf.Deg2Rad;
            var mag = Mathf.Max((0.4f * Mathf.Cos((radians * u.xzVelocity().magnitude) / 50) + 0.6f) * u.xzVelocity().magnitude, 10);
            Vector3 newVel = ((wish * Mathf.Clamp(Mathf.Abs(Vector3.Dot(rb.velocity.normalized, wish)) + .5f, .5f, 1)) + rb.velocity.normalized).normalized * mag;// * Mathf.Cos();
            newVel.y = mvmt.airSettings.jumpForce;
            rb.velocity = newVel;

            // Vector3 newVel = u.xzVelocity(); 
            // if (u.dirInput.magnitude > 0.1f)
            //     newVel = u.xzVelocity().magnitude * Vector3.ClampMagnitude(u.xzVelocity().normalized + , 1);
            // newVel.y = mvmt.airSettings.jumpForce;
            // rb.velocity = newVel;
            return new mvmt4Air(mvmt, u, jumping);
        }
        if (u.WallCheck() && (Mathf.Abs(Vector3.Angle(u.wallHit.normal, mvmt.oldWallNormal)) > 25 || mvmt.oldWallHeight > u.wallHit.point.y) && !mvmt.wallrunTimeout) {
            return new mvmt4Wallrun(mvmt, u);
        }
        return null;
    }
}

class mvmt4Wallrun : mvmt4State {
    MovementUtils u;
    mvmt4 mvmt;
    Rigidbody rb;
    PlayerLook look;
    bool jumping;
    bool onWall;
    float wallrunSpeed;
    float wallTime;
    float wallDuration = 1.75f;
    public mvmt4Wallrun(mvmt4 mvmt, MovementUtils u) {
        this.mvmt = mvmt;
        this.u = u;
        this.rb = mvmt.rb;
        this.look = mvmt.GetComponent<PlayerLook>();
    }

    public override void Start() {
        u.onJump += Jump;
        wallrunSpeed = u.xzVelocity().magnitude;
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y/2, rb.velocity.z);
        u.JumpReset();
    }
    public void Jump() {
        jumping = true;
    }

    public override void Update() {
        wallTime += Time.deltaTime;
        u.JumpReset();
    }

    public override void FixedUpdate() {
        if (!jumping) {
            var prevOnWall = onWall;
            onWall = Vector3.Distance(u.wallHit.point, u.transform.position) < u.capsuleCollider.radius + 0.1f;
            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            Vector3 smoothWish = u.transformify(u.smoothDirInput);
            Vector3 wallSpaceWish = new Vector3(Vector3.Dot(smoothWish, wallDir), 0, Vector3.Dot(smoothWish, u.wallHit.normal));
            Vector3 stickVelocity = (-u.wallHit.normal * 300 * Time.fixedDeltaTime * Vector3.Distance(mvmt.transform.position-(u.wallHit.normal*u.capsuleCollider.radius), u.wallHit.point));
            Vector3 wallrunVelocity = wallDir * wallSpaceWish.x * Mathf.Lerp(wallrunSpeed, 15, 5 * wallTime) + stickVelocity;
            wallrunVelocity.y = rb.velocity.y;
            look.SetTargetDutch(Mathf.Lerp(look.targetDutch, -15 * Mathf.Clamp01((wallDuration+.5f-wallTime)/wallDuration) * Vector3.Dot(wallDir, mvmt.transform.forward), 50*Time.deltaTime));

            rb.velocity = wallrunVelocity;
            rb.AddForce(-Vector3.up * 300 * Time.deltaTime, ForceMode.Acceleration);
        }
    }

    public override void End() {
        u.onJump -= Jump;
        mvmt.oldWallNormal = u.wallHit.normal;
        mvmt.oldWallHeight = mvmt.transform.position.y;
        MovementUtils.OnEnterGround resetWall = () => {
            mvmt.oldWallNormal = Vector3.zero;
            mvmt.oldWallHeight = Mathf.Infinity;
            Debug.Log("wall reset");
        };
        u.onEnterGround += resetWall;
        u.onEnterGround += () => {
            u.onEnterGround -= resetWall;
        };
        look.SetTargetDutch(0);    
        mvmt.WallrunTimeout();    
    }

    public override mvmt4State CheckTransition() {
        if (jumping) {
            Vector3 wallDir = Vector3.Cross(u.wallHit.normal, Vector3.up);
            float wallSide = Vector3.Dot(wallDir, (mvmt.transform.forward * Mathf.Sign(u.signedDirInput.y) + mvmt.transform.right * u.signedDirInput.x).normalized);
            Vector3 newVel = wallDir.normalized * Mathf.Lerp(wallrunSpeed, 15, 5 * wallTime) * Mathf.Sign(wallSide) + u.wallHit.normal * 8;

            if (Vector3.Dot(u.wallHit.normal, mvmt.transform.forward) < -0.7f)
                newVel = u.wallHit.normal * 4f;
            newVel.y = mvmt.airSettings.jumpForce;
            rb.velocity = newVel;
            return new mvmt4Air(mvmt, u, jumping);
        }
        if (!Physics.Raycast(mvmt.transform.position, -u.wallHit.normal, u.capsuleCollider.radius + 0.1f, u.whatIsGround) && !u.WallCheck()) {
            return new mvmt4Air(mvmt, u, false);
        }
        return null;
    }
}