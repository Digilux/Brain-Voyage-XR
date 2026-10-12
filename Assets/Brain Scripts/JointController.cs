using System.Collections.Generic;
using UnityEngine;

public class JointController : MonoBehaviour
{
    public Transform[] joints;  // Array of joints to control
    public float[] openAngles;  // Array of custom angles for each joint
    public float speed = 2f;  // Speed of opening/closing

    private Quaternion[] initialRotations;
    private bool[] isJointOpen;  // Array to track which joints are open
    private int currentJointIndex = 0;

    void Start()
    {
        // Store the initial rotations of each joint
        initialRotations = new Quaternion[joints.Length];
        isJointOpen = new bool[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            initialRotations[i] = joints[i].localRotation;
            isJointOpen[i] = false;  // All joints start closed
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            OpenNextJoint();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            CloseAllAtOnce();
        }

        UpdateJointRotations();
    }

    void OpenNextJoint()
    {
        if (currentJointIndex < joints.Length)
        {
            isJointOpen[currentJointIndex] = true;
            currentJointIndex++;
        }
    }

    void UpdateJointRotations()
    {
        for (int i = 0; i < joints.Length; i++)
        {
            Quaternion targetRotation = isJointOpen[i]
                ? initialRotations[i] * Quaternion.Euler(0, openAngles[i], 0)
                : initialRotations[i];

            joints[i].localRotation = Quaternion.Slerp(joints[i].localRotation, targetRotation, Time.deltaTime * speed);
        }
    }

    public void CloseAllAtOnce()
    {
        currentJointIndex = 0;
        for (int i = 0; i < joints.Length; i++)
        {
            isJointOpen[i] = false;
        }

        UpdateJointRotations();
    }
}
