using UnityEngine;
using Valve.VR;

public class CalibrationPoint
{
    public CalibrationPointType Type { get; set; }
    public ETrackedDeviceClass DeviceClass { get; set; }
    public int DeviceIndex{ get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 DevicePosition { get; set; }
    public float DistanceFromLine { get; set; }
    public float DistanceFromCenter { get; set; }
}
