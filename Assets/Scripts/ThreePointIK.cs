using UnityEngine;

public class ThreePointIK : MonoBehaviour
{

    public Transform IKBone1;
    public Transform IKBone2;
    public Transform IKBone3;
    public Transform IKBone4;
    public Transform target;
    public Transform secondTarget;

    // whether UpdateIK() is called by Update() or called externally
    public bool manualUpdateIK = false;
    public bool trackSecondBone = false;
    public BendNormalStrategy bendNormalStrategy = BendNormalStrategy.followTarget;
    public Vector3 defaultBendNormal;

    public enum BendNormalStrategy
    {
        followTarget,
        rightArm,
        leftArm,
        spine
    }

    public static Quaternion RotationToLocalSpace(Quaternion space, Quaternion rotation)
    {
        return Quaternion.Inverse(Quaternion.Inverse(space) * rotation);
    }

    public class Bone
    {

        public float length;
        public Transform trans;
        private Quaternion targetToLocalSpace;
        private Vector3 defaultLocalBendNormal;

        public void Initiate(Vector3 childPosition, Vector3 bendNormal)
        {
            // Get default target rotation that looks at child position with bendNormal as up
            Quaternion defaultTargetRotation = Quaternion.LookRotation(childPosition - trans.position, bendNormal);

            // Covert default target rotation to local space
            targetToLocalSpace = RotationToLocalSpace(trans.rotation, defaultTargetRotation);

            defaultLocalBendNormal = Quaternion.Inverse(trans.rotation) * bendNormal;
        }


        public Quaternion GetRotation(Vector3 direction, Vector3 bendNormal)
        {
            return Quaternion.LookRotation(direction, bendNormal) * targetToLocalSpace;
        }

        public Vector3 GetBendNormalFromCurrentRotation()
        {
            return trans.rotation * defaultLocalBendNormal;
        }

        public Vector3 GetBendNormalFromCurrentRotation(Vector3 defaultNormal)
        {
            return trans.rotation * defaultNormal;
        }
    }

    private Bone bone1;
    private Bone bone2;
    private Bone bone3;
    private Bone bone4;

    private bool initialized = false;

    // Use this for initialization
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (!manualUpdateIK)
        {
            UpdateIK();
        }
    }

    Vector3 GetBendDirection(Vector3 IKPosition, Vector3 bendNormal)
    {
        Vector3 direction = IKPosition - bone1.trans.position;
        if (direction == Vector3.zero) return Vector3.zero;

        float directionSqrMag = direction.sqrMagnitude;
        float directionMagnitude = (float)Mathf.Sqrt(directionSqrMag);

        float x = (directionSqrMag + bone1.length * bone1.length - bone2.length * bone2.length) / 2f / directionMagnitude;
        float y = (float)Mathf.Sqrt(Mathf.Clamp(bone1.length * bone1.length - x * x, 0, Mathf.Infinity));

        Vector3 yDirection = Vector3.Cross(direction, bendNormal);
        return Quaternion.LookRotation(direction, yDirection) * new Vector3(0f, y, x);
    }

    Vector3 GetSecondBoneBendDirection(Vector3 IKPosition, Vector3 bendNormal)
    {
        Vector3 direction = IKPosition - bone1.trans.position;
        if (direction == Vector3.zero) return Vector3.zero;

        float directionSqrMag = direction.sqrMagnitude;
        float directionMagnitude = (float)Mathf.Sqrt(directionSqrMag);

        float x = (directionSqrMag + bone1.length * bone1.length) / 2f / directionMagnitude;
        float y = (float)Mathf.Sqrt(Mathf.Clamp(bone1.length * bone1.length - x * x, 0, Mathf.Infinity));

        Vector3 yDirection = Vector3.Cross(direction, bendNormal);
        return Quaternion.LookRotation(direction, yDirection) * new Vector3(0f, y, x);
    }

    public void UpdateIK()
    {
        //clamp target if distance to target is longer than bones combined
        Vector3 actualTargetPos;
        float overallLength = Vector3.Distance(bone1.trans.position, target.position);
        if (overallLength > bone1.length + bone2.length)
        {
            actualTargetPos = bone1.trans.position + (target.position - bone1.trans.position).normalized * (bone1.length + bone2.length);
            overallLength = bone1.length + bone2.length;
        }
        else
            actualTargetPos = target.position;

        //calculate bend normal
        //you may need to change this based on the model you chose
        Vector3 bendNormal = GetBendNormalStrategy(bendNormalStrategy, actualTargetPos);

        //calculate bone1, bone2 rotation
        Vector3 bendDirection = GetBendDirection(actualTargetPos, bendNormal);

        if (IKBone4 != null)
        {
            bone4.trans.rotation = bone4.GetRotation(bendDirection, bendNormal);
        }

        if (trackSecondBone && secondTarget != null && bendNormalStrategy != BendNormalStrategy.spine)
        {
            Vector3 secondActualTargetPos;
            float secondOverallLength = Vector3.Distance(bone1.trans.position, secondTarget.position);
            if (secondOverallLength > bone1.length)
            {
                secondActualTargetPos = bone1.trans.position + (secondTarget.position - bone1.trans.position).normalized * bone1.length;
                secondOverallLength = bone1.length;
            }
            else
            {
                secondActualTargetPos = secondTarget.position;
            }

            Vector3 secondbendNormal = GetBendNormalStrategy(bendNormalStrategy, secondActualTargetPos);
            //calculate bone2 rotation
            Vector3 secondbendDirection = GetSecondBoneBendDirection(secondActualTargetPos, secondbendNormal);

            // Rotating Shoulder/Thigh
            bone1.trans.rotation = bone1.GetRotation(secondbendDirection, secondbendNormal);
            //Set Elbow/Knee position
            bone2.trans.position = secondActualTargetPos;
           // bone2.trans.rotation = secondTarget.rotation;
            //Set Hand/Foot position
            bone3.trans.position = actualTargetPos;

            bone2.trans.rotation = bone2.GetRotation(actualTargetPos - bone2.trans.position, bone2.GetBendNormalFromCurrentRotation(bendNormal));
        }
        else
        {
            // Rotating bone1
            if (bendNormalStrategy == BendNormalStrategy.leftArm || bendNormalStrategy == BendNormalStrategy.rightArm)
            {
                bone1.trans.rotation = bone1.GetRotation(bendDirection, bendNormal) * Quaternion.Euler(-45, 0, 0);
            }
            else
            {
                bone1.trans.rotation = bone1.GetRotation(bendDirection, bendNormal);
            }

            // Rotating bone 2
            bone2.trans.rotation = bone2.GetRotation(actualTargetPos - bone2.trans.position, bone2.GetBendNormalFromCurrentRotation(defaultBendNormal));
        }
        bone3.trans.rotation = target.rotation;
        bone3.trans.position = actualTargetPos;
    }

    private Vector3 GetBendNormalStrategy(BendNormalStrategy bendNormalStrategy, Vector3 actualTargetPos)
    {
        Vector3 bendNormal = Vector3.zero;
        switch (bendNormalStrategy)
        {
            case BendNormalStrategy.followTarget:
                bendNormal = -Vector3.Cross(actualTargetPos - bone1.trans.position, target.forward);
                break;
            case BendNormalStrategy.rightArm:
                bendNormal = Vector3.down;
                break;
            case BendNormalStrategy.leftArm:
                bendNormal = Vector3.up;
                break;
            case BendNormalStrategy.spine:
                bendNormal = bone1.GetBendNormalFromCurrentRotation();
                break;
            default:
                break;
        }
        return bendNormal;
    }

    void Init()
    {
        if (IKBone1 == null || IKBone2 == null || IKBone3 == null || target == null)
        {
            Debug.LogError("bone or target empty, IK aborted");
            return;
        }

        bone1 = new Bone { trans = IKBone1 };
        bone2 = new Bone { trans = IKBone2 };
        bone3 = new Bone { trans = IKBone3 };

        bone1.length = Vector3.Distance(bone1.trans.position, bone2.trans.position);
        bone2.length = Vector3.Distance(bone2.trans.position, bone3.trans.position);
        if (IKBone4 != null)
        {
            bone4 = new Bone { trans = IKBone4 };
            bone4.length = Vector3.Distance(bone4.trans.position, bone3.trans.position);
        }
        Vector3 bendNormal = defaultBendNormal == Vector3.zero ? GetBendNormalStrategy(bendNormalStrategy, target.position) : defaultBendNormal;

        bone1.Initiate(bone2.trans.position, bendNormal);

        bone1.Initiate(bone2.trans.position, bendNormal);
        if (trackSecondBone && secondTarget != null)
        {
            if (bendNormalStrategy != BendNormalStrategy.spine)
            {
                secondTarget.position = bone2.trans.position;
                secondTarget.rotation = bone2.trans.rotation;
            }
            else
            {
                secondTarget.position = bone1.trans.position;
            }
        }
        bone2.Initiate(bone3.trans.position, bendNormal);

        if (IKBone4 != null)
        {
            bone4.Initiate(bone1.trans.position, bendNormal);
        }
        initialized = true;

        if (trackSecondBone && secondTarget != null)
        {
            secondTarget.position = bone2.trans.position;
            secondTarget.rotation = bone2.trans.rotation;
        }
        //bone3.trans.position = target.position;
        //bone3.trans.rotation = target.rotation;
        target.position = bone3.trans.position;
        target.rotation = bone3.trans.rotation;
    }
}
