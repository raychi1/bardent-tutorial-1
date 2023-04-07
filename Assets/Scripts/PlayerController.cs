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
    
    private float movementInputDirection;
    private float movementSpeed = 10.0f;
    private float jumpForce = 16.0f;
    
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        CheckMovementDirection();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void UpdateAnimations()
    {
        anim.SetBool("isWalking", isWalking);
    }
    /*private void UpdateAnimations()
    {
        if(rb.velocity.x != 0)
            anim.SetBool("isWalking", isWalking);
        else
            anim.SetBool("isWalking", false);
    }*/
    
    private void CheckInput()
    {
        movementInputDirection = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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
            isWalking = true;
        else
            isWalking = false;
    }

    private void ApplyMovement()
    {
        rb.velocity = new Vector2(movementSpeed * movementInputDirection, rb.velocity.y);
        anim.SetBool("isWalking", isWalking);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.Rotate(0.0f,180.0f,0.0f);
    }
}
