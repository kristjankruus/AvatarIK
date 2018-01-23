using UnityEngine;

class OffsetTracking
{
    public CalibrationPointType pointType;
    //public TrackerRole role;
    public Transform targetTrans;
    public int deviceIndex;
    public Transform deviceTransform;

    GameObject child;

    //Pose GetPose(int index)
    //{
    //    Pose pose = new Pose { pos = deviceTransform.position, rot = deviceTransform.rotation };
    //    return pose;
    //}
    //Paneb childi paika
    public void StartTracking()
    {
        child = new GameObject("marker for " + pointType);
        child.transform.parent = deviceTransform;
        child.transform.position = targetTrans.position;
        child.transform.rotation = targetTrans.rotation;        
    }


    public void UpdateOffsetTracking()
    {
        targetTrans.position = child.transform.position;
        targetTrans.rotation = child.transform.rotation;
    }
    public void UpdateOffsetPosition()
    {
        targetTrans.position = child.transform.position;
    }
}
