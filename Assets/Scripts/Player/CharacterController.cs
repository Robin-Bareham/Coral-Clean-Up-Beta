using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Rendering;

public class CharacterController : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset PlayerControls;

    [SerializeField]
    private Transform cameraRotation;

    [SerializeField]
    private float moveForce = 10f;
    [SerializeField]
    private float decelerationMultiplier = 2.0f;
    [SerializeField]
    private float decelerationCutoff = 0.5f;


    [SerializeField] GameManager manager;


    private Rigidbody rBody;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction riseAction;
    private InputAction sinkAction;
    private InputAction interactAction;

    private Vector2 moveInput;
    private int verticalInput;

    private bool can_enter_base = false;


    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();

        InputActionMap map = PlayerControls.FindActionMap("Player");

        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        riseAction = map.FindAction("Rise");
        sinkAction = map.FindAction("Sink");
        interactAction = map.FindAction("Interact");
    }

    private void OnEnable()
    {

        moveAction.Enable();
        lookAction.Enable();
        riseAction.Enable();
        sinkAction.Enable();
        interactAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        riseAction.Disable();
        sinkAction.Disable();
        interactAction.Disable();
    }

    private void Update()
    {
        if (manager.getCanMove())
        {
            // Read input every frame
            moveInput = moveAction.ReadValue<Vector2>();

            //Debug.Log("X = " + moveInput.x);
            //Debug.Log("Y = " + moveInput.y);
            //Debug.Log("Angular: x = " + rBody.angularVelocity.x + ", y = " + rBody.angularVelocity.y);
            //Debug.Log("Linear: x = " + rBody.linearVelocity.x + ", y = " + rBody.linearVelocity.y + ", z = " + rBody.linearVelocity.z);        

            verticalInput = 0;
            if (riseAction.IsPressed())
            {
                verticalInput += 1;
            }
            if (sinkAction.IsPressed())
            {
                verticalInput -= 1;
            }
            if (interactAction.WasPressedThisFrame())
            {
                //If they're in the space to enter the base AND this isn't being activated the same frame the player's exited the base.
                if (can_enter_base && manager.getJustExitedBase() != Time.frameCount)
                {
                    manager.setEnterBase(true);
                    //Kills character movement so they're not floating around whilst in base screen.
                    rBody.angularVelocity = Vector3.zero;
                    rBody.linearVelocity = Vector3.zero;
                }
            }
            
        }        
    }

    private void FixedUpdate()
    {
        if (manager.getCanMove())
        {
            //Movement handling here as rigidbody force is handled during physics step
            HandleMovement();
            HandleDeceleration();
        }
    }

    private void HandleMovement()
    {
        //Use of Rigidbody.AddForce to emulate underwater movement
        if (moveInput.x != 0f || moveInput.y != 0f || verticalInput != 0f)
        {
            rBody.isKinematic = false;
        }

        // Get orientation
        Vector3 forward = cameraRotation.forward;
        Vector3 right = cameraRotation.right;
        Vector3 up = cameraRotation.up;

        //Calculate movement relative to camera
        // Horizontal movement: X and Z axes
        Vector3 movement = (right * moveInput.x) + (forward * moveInput.y);

        //Calculate vertical input movement relative to plane 
        // Vertical movement: Y axis
        movement.y += (float)verticalInput;

        // Apply force across all three axes
        // Left/Right, Up/Down, Forward/Back
        rBody.AddForce(movement.x * moveForce, movement.y * moveForce, movement.z * moveForce, ForceMode.Force);
    }

    private void HandleDeceleration()
    {
        // Get the world space velocity from the Rigidbody
        Vector3 worldVelocity = rBody.linearVelocity;
        // Convert it into the camera's local coordinate space
        Vector3 relativeVelocity = cameraRotation.InverseTransformDirection(worldVelocity);
        // Interpret the axes relative to the camera's view
        float forwardSpeed = relativeVelocity.z; // Positive = moving with camera view, Negative = moving toward camera
        float rightSpeed = relativeVelocity.x;   // Positive = moving right, Negative = moving left
        float upSpeed = worldVelocity.y;      // Positive = moving up, Negative = moving down. world velocity used as not tied to camera

        float decelForce = moveForce * decelerationMultiplier;

        // Left/Right deceleration
        if (moveInput.x == 0)
        {
            //Debug.Log("MoveInput x is Zero: " + moveInput.x);
            //Debug.Log("Right Speed: " + rightSpeed);
            if (rightSpeed >= -decelerationCutoff && rightSpeed <= decelerationCutoff)
            {
                Debug.Log("0 X Speed");
            }
            else
            {
                float decelerationDirection = rightSpeed > 0f ? -1f : 1f;

                Vector3 deceleration = cameraRotation.right * decelerationDirection;

                rBody.AddForce(deceleration * decelForce, ForceMode.Force);
            }
        }
        // Forward/Backward deceleration
        if (moveInput.y == 0)
        {
            //Debug.Log("MoveInput y is: " + moveInput.y);
            //Debug.Log("Forward Speed: " + forwardSpeed);
            if (forwardSpeed >= -decelerationCutoff && forwardSpeed <= decelerationCutoff)
            {
                Debug.Log("0 Z Speed");
            }
            else
            {
                float decelerationDirection = forwardSpeed > 0f ? -1f : 1f;

                Vector3 deceleration = cameraRotation.forward * decelerationDirection;

                rBody.AddForce(deceleration * decelForce, ForceMode.Force);
            }
        }
        //Vertical deceleration
        if (verticalInput == 0)
        {
            //Debug.Log("Vertical Input is: " + verticalInput);
            //Debug.Log("Up Speed: " + upSpeed);
            if (upSpeed >= -decelerationCutoff && upSpeed <= decelerationCutoff)
            {
               Debug.Log("0 Y Speed");
            }
            else
            {
                float decelerationDirection = upSpeed > 0f ? -1f : 1f;

                rBody.AddForce(Vector3.up * decelerationDirection * decelForce, ForceMode.Force);
            }
        }       

        //Set rbody to kinematic when minimal speed and no input
        if (moveInput.x == 0 && moveInput.y == 0 && verticalInput == 0 && !rBody.isKinematic)
        {
            bool isMovingX = true;
            bool isMovingY = true;
            bool isMovingZ = true;

            if (Mathf.Abs(rBody.linearVelocity.x) < decelerationCutoff)
            {
                isMovingX = false;
            }

            if (Mathf.Abs(rBody.linearVelocity.y) < decelerationCutoff)
            {
                isMovingY = false;
            }

            if (Mathf.Abs(rBody.linearVelocity.z) < decelerationCutoff)
            {
                isMovingZ = false;
            }

            if (!isMovingX && !isMovingY && !isMovingZ)
            {
                Debug.Log("Kinematic trigger");
                rBody.isKinematic = true;
            }
        }
    }

    //If the player enters the base's trigger box.
    //(not specific for now as that's the only trigger box we have, will need to have more checks if there are more triggers)
    private void OnTriggerEnter(Collider other)
    {
        can_enter_base = true;
    }

    private void OnTriggerExit(Collider other)
    {
        can_enter_base = false;
    }






    public bool IsPositive(float number)
    {
        return number > 0;
    }



}
