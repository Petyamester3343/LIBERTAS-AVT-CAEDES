using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // handling vars
    public float rotSpeed = 450;
    public float walkSpeed = 5;
    public float runSpeed = 8;

    // sys vars
    private Quaternion tgtRot;

    // components
    private CharacterController controller;
    private Camera cam;
    public Gun gun;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
    }

    void Update()
    {
        CtrlWithMouse();

        if (Input.GetButtonDown("Shoot")) gun.Shoot();
        else if (Input.GetButton("Shoot")) gun.ShootContinuous();
    }

    void CtrlWithMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos = cam.ScreenToWorldPoint(new(mousePos.x, mousePos.y, cam.transform.position.y - transform.position.y));
        tgtRot = Quaternion.LookRotation(mousePos - new Vector3(transform.position.x, 0, transform.position.z));
        transform.eulerAngles = Vector3.up * Mathf.MoveTowardsAngle(transform.eulerAngles.y, tgtRot.eulerAngles.y, rotSpeed * Time.deltaTime);

        Vector3 input = new(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 motion = input;
        motion *= (Mathf.Abs(input.x) is 1 && Mathf.Abs(input.z) is 1) ? (1 / Mathf.Sqrt(2)) : 1;
        motion *= Input.GetButton("Run") ? runSpeed : walkSpeed;
        motion += Vector3.up * -8;

        controller.Move(motion * Time.deltaTime);
    }

    /*void CtrlWithOnlyWASD()
    {
        Vector3 input = new(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (input != Vector3.zero)
        {
            tgtRot = Quaternion.LookRotation(input);
            transform.eulerAngles = Vector3.up * Mathf.MoveTowardsAngle(transform.eulerAngles.y, tgtRot.eulerAngles.y, rotSpeed * Time.deltaTime);
        }

        Vector3 motion = input;
        motion *= (Mathf.Abs(input.x) is 1 && Mathf.Abs(input.z) is 1) ? (1/Mathf.Sqrt(2)) : 1;
        motion *= Input.GetButton("Run") ? runSpeed : walkSpeed;
        motion += Vector3.up * -8;

        controller.Move(motion * Time.deltaTime);
    }*/
}
