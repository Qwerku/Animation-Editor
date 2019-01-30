using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public Vector3 lookAtCoords = Vector3.zero;
    public float lookSpeed = .03f;

    float rotationX = 0.0f;
    float rotationY = 0.0f;
    float lookAtX = 0.0f;
    float lookAtY = 0.0f;
    float lookAtZ = 0.0f;

    float cameraDistance = 0.0f;

    void Awake()
    {
        rotationX = transform.rotation.eulerAngles.y;
        rotationY = -transform.rotation.eulerAngles.x;

        cameraDistance = Vector3.Distance(transform.position, lookAtCoords);

    }
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            rotationX = Input.GetAxis("Mouse X") * -1.5f * lookSpeed;
            rotationY = Input.GetAxis("Mouse Y") * -lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -89, 89);

            transform.Translate(new Vector3(rotationX, rotationY, 0 ));
            transform.LookAt(lookAtCoords);

            transform.Translate(new Vector3(0.0f, 0.0f, (Vector3.Distance(transform.position, lookAtCoords) - cameraDistance)));

            //transform.RotateAround(lookAtCoords, new Vector3(0, 1, 0), rotationX);
            //transform.RotateAround(lookAtCoords, new Vector3(0, 0, 1), rotationY);
        }
        
        if (Input.GetMouseButton(2))
        {
            lookAtX = Input.GetAxis("Mouse X") * lookSpeed;
            lookAtY = Input.GetAxis("Mouse Y") * lookSpeed;
            Vector3 oldPosition = transform.position;

            transform.Translate(new Vector3(lookAtX, lookAtY, 0));

            lookAtCoords += (transform.position - oldPosition);

            //lookAtCoords += new Vector3(lookAtX, lookAtY, 0.0f);
            transform.LookAt(lookAtCoords);
            transform.Translate(new Vector3(0.0f, 0.0f, (Vector3.Distance(transform.position, lookAtCoords) - cameraDistance)));

        }

        cameraDistance -= Input.GetAxisRaw("Mouse ScrollWheel");
        //print("Cameradist: " + cameraDistance.ToString());
        transform.Translate(new Vector3(0.0f, 0.0f, Input.GetAxisRaw("Mouse ScrollWheel")));

    }

    public void ResetView()
    {
        lookAtCoords = Vector3.zero;
        transform.LookAt(lookAtCoords);

        transform.Translate(new Vector3(0.0f, 0.0f, (Vector3.Distance(transform.position, lookAtCoords) - cameraDistance)));
    }
    

    public void MoveCameraFromMouseInputs()
    {
        float X, Y;

        //transform.Translate(new Vector3(0.3f*Input.GetAxis("Horizontal"), 0.3f*Input.GetAxis("Vertical"), 0.02f * Input.GetAxisRaw("Mouse ScrollWheel")));

        transform.Translate(new Vector3(0.0f, 0.0f, 0.02f * Input.GetAxisRaw("Mouse ScrollWheel")));
        if (Input.GetMouseButton(1))
        {
            transform.Rotate(new Vector3(-1 * Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0));
            X = transform.rotation.eulerAngles.x;
            Y = transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(X, Y, 0);
        }
    }




}
