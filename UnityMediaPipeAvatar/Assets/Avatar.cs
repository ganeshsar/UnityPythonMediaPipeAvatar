using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Avatar : MonoBehaviour
{
    public Camera cam;
    public PipeServer server;
    public Animator animator;
    public LayerMask ground;
    public bool footTracking = true;
    public float footGroundOffset = .1f;

    private Dictionary<HumanBodyBones, CalibrationData> parentCalibrationData = new Dictionary<HumanBodyBones, CalibrationData>();
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Quaternion targetRot;
    private CalibrationData spineUpDown, hipsTwist,chest,head;

    private void Start()
    {
        initialRotation = transform.rotation;
        initialPosition = transform.position;
    }
    public void Calibrate()
    {
        parentCalibrationData.Clear();

        // A bit of a dirty method, I still need to figure out how to properly handle this.
        spineUpDown = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Spine), animator.GetBoneTransform(HumanBodyBones.Neck),
            server.GetVirtualHip(), server.GetVirtualNeck());
        hipsTwist = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Hips), animator.GetBoneTransform(HumanBodyBones.Hips),
            server.GetLandmark(Landmark.RIGHT_HIP), server.GetLandmark(Landmark.LEFT_HIP));
        chest = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Chest), animator.GetBoneTransform(HumanBodyBones.Chest),
            server.GetLandmark(Landmark.RIGHT_HIP), server.GetLandmark(Landmark.LEFT_HIP));
        head = new CalibrationData(animator.GetBoneTransform(HumanBodyBones.Neck), animator.GetBoneTransform(HumanBodyBones.Head),
            server.GetVirtualNeck(), server.GetLandmark(Landmark.NOSE));

        AddCalibration(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            server.GetLandmark(Landmark.RIGHT_SHOULDER), server.GetLandmark(Landmark.RIGHT_ELBOW));
        AddCalibration(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            server.GetLandmark(Landmark.RIGHT_ELBOW), server.GetLandmark(Landmark.RIGHT_WRIST));

        AddCalibration(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
            server.GetLandmark(Landmark.RIGHT_HIP), server.GetLandmark(Landmark.RIGHT_KNEE));
        AddCalibration(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
            server.GetLandmark(Landmark.RIGHT_KNEE), server.GetLandmark(Landmark.RIGHT_ANKLE));

        AddCalibration(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
            server.GetLandmark(Landmark.LEFT_SHOULDER), server.GetLandmark(Landmark.LEFT_ELBOW));
        AddCalibration(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            server.GetLandmark(Landmark.LEFT_ELBOW), server.GetLandmark(Landmark.LEFT_WRIST));

        AddCalibration(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
            server.GetLandmark(Landmark.LEFT_HIP), server.GetLandmark(Landmark.LEFT_KNEE));
        AddCalibration(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            server.GetLandmark(Landmark.LEFT_KNEE), server.GetLandmark(Landmark.LEFT_ANKLE));

        if (footTracking)
        {
            AddCalibration(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
                server.GetLandmark(Landmark.LEFT_ANKLE), server.GetLandmark(Landmark.LEFT_FOOT_INDEX));
            AddCalibration(HumanBodyBones.RightFoot, HumanBodyBones.RightToes,
                server.GetLandmark(Landmark.RIGHT_ANKLE), server.GetLandmark(Landmark.RIGHT_FOOT_INDEX));
        }

        animator.enabled = false; // disable animator to stop interference.
    }
    private void AddCalibration(HumanBodyBones parent, HumanBodyBones child, Transform trackParent,Transform trackChild)
    {
        parentCalibrationData.Add(parent, 
            new CalibrationData(animator.GetBoneTransform(parent), animator.GetBoneTransform(child),
            trackParent,trackChild));
    }

    private void Update()
    {
        if(parentCalibrationData.Count > 0)
        {
            float displacement = 0;
            RaycastHit h1;
            if (Physics.Raycast(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position, Vector3.down, out h1, 100f, ground, QueryTriggerInteraction.Ignore)){
                displacement = (h1.point - animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).y;
            }
            if (Physics.Raycast(animator.GetBoneTransform(HumanBodyBones.RightFoot).position, Vector3.down, out h1, 100f, ground, QueryTriggerInteraction.Ignore)){
                float displacement2 = (h1.point - animator.GetBoneTransform(HumanBodyBones.RightFoot).position).y;
                if (Mathf.Abs(displacement2) < Mathf.Abs(displacement))
                {
                    displacement = displacement2;
                }
            }
            transform.position = Vector3.Lerp(transform.position,initialPosition+ Vector3.up * displacement + Vector3.up * footGroundOffset,
                Time.deltaTime*5f);
        }

        foreach(var i in parentCalibrationData)
        {
            Quaternion deltaRotTracked = Quaternion.FromToRotation(i.Value.initialDir, i.Value.CurrentDirection);

            if (i.Key == HumanBodyBones.LeftFoot || i.Key == HumanBodyBones.RightFoot)
            {
                Vector3 d = Vector3.Slerp(i.Value.initialDir, i.Value.CurrentDirection, 1f);
                deltaRotTracked = Quaternion.FromToRotation(i.Value.initialDir,d);
                i.Value.parent.rotation = deltaRotTracked * i.Value.initialRotation;
                continue; 
            }

            i.Value.parent.rotation = deltaRotTracked * i.Value.initialRotation;
        }

        // Deal with spine as a special case.
        if(parentCalibrationData.Count > 0)
        {
            Vector3 hd = Vector3.SlerpUnclamped(head.initialDir, head.CurrentDirection, 1);
            Quaternion headr = Quaternion.FromToRotation(head.initialDir, hd);
            Quaternion twist = Quaternion.FromToRotation(hipsTwist.initialDir, 
                Vector3.Slerp(hipsTwist.initialDir,hipsTwist.CurrentDirection,.25f));
            Quaternion updown = Quaternion.FromToRotation(spineUpDown.initialDir,
                Vector3.Slerp(spineUpDown.initialDir, spineUpDown.CurrentDirection, .25f));

            Quaternion h = updown * updown * updown * twist*twist;
            Quaternion s = h * twist * updown;
            Quaternion c = s * twist * twist;
            float speed = 10f;
            hipsTwist.Tick(h * hipsTwist.initialRotation, speed);
            spineUpDown.Tick(s * spineUpDown.initialRotation, speed);
            chest.Tick(c * chest.initialRotation, speed);
            head.Tick(updown * twist * headr * head.initialRotation, speed);

            Vector3 d = Vector3.Slerp(hipsTwist.initialDir, hipsTwist.CurrentDirection, .25f);
            d.y *= 0.5f;
            Quaternion deltaRotTracked = Quaternion.FromToRotation(hipsTwist.initialDir, d);
            targetRot= deltaRotTracked * initialRotation;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * speed);

            if (cam)
            {
                Quaternion q = Quaternion.LookRotation((animator.GetBoneTransform(HumanBodyBones.Chest).transform.position - cam.transform.position).normalized, Vector3.up);
                cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, q, Time.deltaTime * 3f);
            }
        }

    }

    class CalibrationData
    {
        public Transform parent, child,tparent,tchild;
        public Vector3 initialDir;
        public Quaternion initialRotation;

        public Quaternion targetRotation;
        public void Tick(Quaternion newTarget, float speed)
        {
            parent.rotation = newTarget;
            parent.rotation = Quaternion.Lerp(parent.rotation, targetRotation, Time.deltaTime * speed);
        }

        public Vector3 CurrentDirection => (tchild.position - tparent.position).normalized;

        public CalibrationData(Transform fparent, Transform fchild,Transform tparent,Transform tchild)
        {
            initialDir = (tchild.position - tparent.position).normalized;
            initialRotation = fparent.rotation;
            this.parent = fparent;
            this.child = fchild;
            this.tparent = tparent;
            this.tchild = tchild;
        }
    }
}
