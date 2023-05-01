using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    
    private bool isFacingRight = true;
    private bool isWalking;
    private bool isAttemptingToJump;
    private bool canNormalJump = true;
    private bool canWallJump;
    private bool checkJumpMultiplier;
    private bool canMove;
    private bool canFlip;
    private bool hasWallJumped;
    public bool isTouchingWall;
    public bool isWallSliding;
    public bool isGrounded;
    
    private bool isTouchingLedge;
    private bool canClimbLedge = false;
    private bool ledgeDetected;
    
    private int amountOfJumpsLeft;
    private int facingDirection = 1;
    private int lastWallJumpDirection;
    public int amountOfJumps = 1;

    private float movementInputDirection;
    private float jumpTimer;
    private float turnTimer;
    private float wallJumpTimer;

    public float movementSpeed = 9.0f;
    public float jumpForce = 20.0f;
    public float jumpTimerSet = 0.15f;
    public float turnTimerSet = 0.1f;
    public float wallJumpTimerSet = 0.5f;
    public float groundCheckRadius;
    public float wallCheckDistance = 0.65f;
    public float wallSlideSpeed = 1;
    public float movementForceInAir;
    public float airDragMultiplier = 0.95f; //how fast to stop x movement if we stop giving horizontal movement input mid-air (less = faster, 0 - immediately)
    public float variableJumpHeightMultiplier = 0.5f;
    public float wallHopForce = 10f;
    public float wallJumpForce = 30f; //old is 20

    public float ledgeClimbXOffset1 = 0f;
    public float ledgeClimbYOffset1 = 0f;
    public float ledgeClimbXOffset2 = 0f;
    public float ledgeClimbYOffset2 = 0f;
    

    private Vector2 ledgePositionBottom;
    private Vector2 ledgePosition1;
    private Vector2 ledgePosition2;
    
    public Vector2 wallHopDirection = new Vector2(1f, 0.5f);
    public Vector2 wallJumpDirection = new Vector2(1f, 2f);

    public Transform groundCheck;
    public Transform wallCheck;
    public Transform ledgeCheck;
    public LayerMask whatIsGround;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        amountOfJumpsLeft = amountOfJumps;
        wallHopDirection.Normalize();
        wallJumpDirection.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        CheckMovementDirection();
        UpdateAnimations();
        CheckIfCanJump();
        CheckIfWallSliding();
        CheckJump();
        CheckLedgeClimb();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        CheckSurroundings();
    }

    private void UpdateAnimations()
    {
        anim.SetBool("isWalking", isWalking);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isWallSliding", isWallSliding);
    }

    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded || (amountOfJumpsLeft > 0 && isTouchingWall))
            {
                NormalJump();
            }
            else
            {
                jumpTimer = jumpTimerSet;
                isAttemptingToJump = true;
            }
        }

        if (Input.GetButtonDown("Horizontal") && isTouchingWall) // wall jump timer (no matter which key pressed first
        //horizontal input or jump)
        {
            if (!isGrounded && movementInputDirection != facingDirection)
            {
                canMove = false;
                canFlip = false;

                turnTimer = turnTimerSet;
            }
        }

        if (turnTimer >= 0)
        {
            turnTimer -= Time.deltaTime;
            if (turnTimer <= 0)
            {
                canMove = true;
                canFlip = true;
            }
        }
        
        if (checkJumpMultiplier && !Input.GetButton("Jump")) // fall after releasing jump - variable jump height
        {
            checkJumpMultiplier = false;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeightMultiplier);
        }
    }
    
    private void CheckMovementDirection()
    {
        if (isFacingRight && movementInputDirection < 0)
        {
            Flip();
        }
        else if (!isFacingRight && movementInputDirection > 0)
        {
            Flip();
        }

        if (rb.velocity.x != 0)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }
    }
    
    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround); //transform.right is used to project to the right side of a character (so if we flip, it will work on left no problem)
        isTouchingLedge = Physics2D.Raycast(ledgeCheck.position, transform.right, wallCheckDistance, whatIsGround);

        if (isTouchingWall && !isTouchingLedge && !ledgeDetected) // added !ledgeDetected to only call this thing once
        {
            ledgeDetected = true;
            ledgePositionBottom = wallCheck.position;
        }
        
    }

    private void CheckLedgeClimb()
    {
        if (ledgeDetected && !canClimbLedge)
        {
            canClimbLedge = true;

            if (isFacingRight)
            {
                ledgePosition1 =
                    new Vector2(Mathf.Floor(ledgePositionBottom.x + wallCheckDistance) - ledgeClimbXOffset1,
                        Mathf.Floor(ledgePositionBottom.y) + ledgeClimbYOffset1);
                ledgePosition2 =
                    new Vector2(Mathf.Floor(ledgePositionBottom.x + wallCheckDistance) + ledgeClimbXOffset2,
                        Mathf.Floor(ledgePositionBottom.y) + ledgeClimbYOffset2);
            }
            else // facing left
            {
                ledgePosition1 = 
                    new Vector2(Mathf.Ceil(ledgePositionBottom.x - wallCheckDistance) + ledgeClimbXOffset1,
                        Mathf.Floor(ledgePositionBottom.y) + ledgeClimbYOffset1);
                ledgePosition2 = 
                    new Vector2(Mathf.Ceil(ledgePositionBottom.x - wallCheckDistance) - ledgeClimbXOffset2,
                        Mathf.Floor(ledgePositionBottom.y) + ledgeClimbYOffset2);
            }

            canMove = false;
            canFlip = false;
            
            anim.SetBool("canClimbLedge", canClimbLedge);
        }

        if (canClimbLedge)
        {
            transform.position = ledgePosition1; // hold player in ledgePosition1 every frame (maybe there is some more optimized way to set its transform position only in one frame)
        }
    }

    public void FinishLedgeClimb()
    {
        canClimbLedge = false;
        transform.position = ledgePosition2;
        canMove = true;
        canFlip = true;
        ledgeDetected = false;
        anim.SetBool("canClimbLedge", canClimbLedge);
    }

    private void CheckIfCanJump()
    {
        if (isGrounded && rb.velocity.y <= 0.01f)
        {
            amountOfJumpsLeft = amountOfJumps;
        }

        if (isTouchingWall)
        {
            checkJumpMultiplier = false;
            canWallJump = true;
        }

        if (amountOfJumpsLeft <= 0)
        {
            canNormalJump = false;
        }
        else
        {
            canNormalJump = true;
        }    
    }

    private void CheckIfWallSliding()
    {
        if (isTouchingWall && movementInputDirection == facingDirection && rb.velocity.y < 0 && !canClimbLedge)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void ApplyMovement()
    {
        
        if (!isGrounded && !isWallSliding && movementInputDirection == 0) 
        {
            rb.velocity = new Vector2(rb.velocity.x * airDragMultiplier, rb.velocity.y);// if in air and
            // no horizontal input, rb.velocity.x slowly reaching zero. To make it snappier we have airDragMultiplier,
            // which make rb.velocity.x reach zero faster. So if we remove airDragMultiplier, x velocity will be slowing
            // down longer and player will feel more heavy, less in our control.  
        }
        else if (canMove)// isGrounded
        {
            rb.velocity = new Vector2(movementSpeed * movementInputDirection, rb.velocity.y);// immediately set
            // rb.velocity.x to zero if no input and on the ground.
        }

        if (isWallSliding)
        {
            if (rb.velocity.y < -wallSlideSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }
    }
    
    private void CheckJump()
    {
        if(jumpTimer > 0)
        {
            if (!isGrounded && isTouchingWall && movementInputDirection != 0 && movementInputDirection != facingDirection)
            {
                WallJump();
            }
            else if (isGrounded)
            {
                NormalJump();
            }
        }
        
        if (isAttemptingToJump)
        {
            jumpTimer -= Time.deltaTime;
        }

        if (wallJumpTimer > 0)
        {
            if (hasWallJumped && movementInputDirection == -lastWallJumpDirection)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0.0f);
                hasWallJumped = false;
            }
            else if (wallJumpTimer <= 0)
            {
                hasWallJumped = false;
            }
            else
            {
                wallJumpTimer -= Time.deltaTime;
            }
        }
    }

    private void NormalJump()
    {
        if (canNormalJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            amountOfJumpsLeft--;
            jumpTimer = 0;
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
        }
    }

    private void WallJump()
    {
        if (canWallJump) // Wall Jump
        {
            rb.velocity = new Vector2(rb.velocity.x, 0.0f);
            isWallSliding = false;
            amountOfJumpsLeft = amountOfJumps;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * movementInputDirection,
                wallJumpForce * wallJumpDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
            jumpTimer = 0;
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
            turnTimer = 0;
            canMove = true;
            canFlip = true;
            hasWallJumped = true;
            wallJumpTimer = wallJumpTimerSet;
            lastWallJumpDirection = -facingDirection;
        }
    }
    
    private void Flip()
    {
        if (!isWallSliding && canFlip)
        {
            facingDirection *= -1;
            isFacingRight = !isFacingRight;
            transform.Rotate(0.0f, 180.0f, 0.0f);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y, wallCheck.position.z));
    }
}
