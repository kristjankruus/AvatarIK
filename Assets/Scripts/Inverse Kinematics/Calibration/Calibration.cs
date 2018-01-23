using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;

public class Calibration
{
    private List<CalibrationPoint> _calibrationPoints = new List<CalibrationPoint>();

    private Dictionary<CalibrationPointArea, float> calibrationPointAreas = new Dictionary<CalibrationPointArea, float>
    {
        {CalibrationPointArea.HeadMin, 0.9f },
        {CalibrationPointArea.WaistMax, 0.4f },
        {CalibrationPointArea.KneeMax, 0.35f },
        {CalibrationPointArea.FootMax, 0.15f },
    };

    public List<CalibrationPoint> GetCalibrationPoints()
    {
        return _calibrationPoints;
    }

    public List<CalibrationPoint> Calibrate(Dictionary<int, SteamVR_Controller.Device> trackedDevices)
    {
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 targetForward = Camera.main.transform.rotation * Vector3.forward;

        foreach (var trackedDevice in trackedDevices)
        {
            var distanceFromLine = Extensions.LeftOrRightFromLine(cameraPosition, targetForward, trackedDevice.Value.transform.pos);
            _calibrationPoints.Add(new CalibrationPoint()
            {
                Position = trackedDevice.Value.transform.pos,
                Rotation = trackedDevice.Value.transform.rot,
                DeviceClass = OpenVR.System.GetTrackedDeviceClass((uint)trackedDevice.Key),
                DeviceIndex = trackedDevice.Key,
                DistanceFromLine = distanceFromLine
            });
        }
        AssignTypesForCalibrationPoints(_calibrationPoints);
        return _calibrationPoints;
    }

    private void AssignTypesForCalibrationPoints(List<CalibrationPoint> calibrationPoints)
    {
        //var controllers = calibrationPoints.Where(x => x.DeviceClass == ETrackedDeviceClass.Controller).ToList();
        //if (controllers.Any())
        //{
        //    AssignControllerCalibrationPoints(controllers);
        //}

        //var trackers = calibrationPoints.Where(x => x.DeviceClass == ETrackedDeviceClass.GenericTracker).OrderBy(x => x.Position.y).ToList();
        if (calibrationPoints.Any())
        {
            AssignLowerBodyCalibrationPoints(calibrationPoints);
            var unassignedPoints = calibrationPoints.Where(x => x.Type == CalibrationPointType.None).ToList();
            if (unassignedPoints.Any())
            {
                AssignUpperBodyCalibrationPoints(unassignedPoints);
            }
        }
    }

    private void AssignControllerCalibrationPoints(List<CalibrationPoint> controllers)
    {
        if (controllers.Count() == 1)
        {
            if (controllers.First().DistanceFromLine >= 0)
            {
                controllers.First().Type = CalibrationPointType.RightHand;
            }
            else
            {
                controllers.First().Type = CalibrationPointType.LeftHand;
            }
        }
        if (controllers.Count() == 2)
        {
            if (controllers[0].DistanceFromLine < controllers[1].DistanceFromLine)
            {
                controllers[0].Type = CalibrationPointType.LeftHand;
                controllers[1].Type = CalibrationPointType.RightHand;
            }
            else
            {
                controllers[0].Type = CalibrationPointType.RightHand;
                controllers[1].Type = CalibrationPointType.LeftHand;
            }
        }
    }

    private void AssignLowerBodyCalibrationPoints(List<CalibrationPoint> calibrationPoints)
    {
        for (int i = 0; i < calibrationPoints.Count; i++)
        {
            var calibrationPoint = calibrationPoints[i];
            var trackerHeightPercentage = calibrationPoint.Position.y / Camera.main.transform.position.y;

            if (trackerHeightPercentage < calibrationPointAreas[CalibrationPointArea.FootMax])
            {
                var leftFoot = calibrationPoints.FirstOrDefault(x => x.Type == CalibrationPointType.LeftFoot);
                if (leftFoot == null)
                {
                    calibrationPoint.Type = CalibrationPointType.LeftFoot;
                }
                else
                {
                    if (calibrationPoint.DistanceFromLine < leftFoot.DistanceFromLine)
                    {
                        calibrationPoint.Type = CalibrationPointType.LeftFoot;
                        leftFoot.Type = CalibrationPointType.RightFoot;
                    }
                    else
                    {
                        calibrationPoint.Type = CalibrationPointType.RightFoot;
                    }
                }
            }

            if (trackerHeightPercentage > calibrationPointAreas[CalibrationPointArea.FootMax] && trackerHeightPercentage < calibrationPointAreas[CalibrationPointArea.KneeMax])
            {
                var leftKnee = calibrationPoints.FirstOrDefault(x => x.Type == CalibrationPointType.LeftKnee);
                if (leftKnee == null)
                {
                    calibrationPoint.Type = CalibrationPointType.LeftKnee;
                }
                else
                {
                    if (calibrationPoint.DistanceFromLine < leftKnee.DistanceFromLine)
                    {
                        calibrationPoint.Type = CalibrationPointType.LeftKnee;
                        leftKnee.Type = CalibrationPointType.RightKnee;
                    }
                    else
                    {
                        calibrationPoint.Type = CalibrationPointType.RightKnee;
                    }
                }
            }
            if (!calibrationPoints.Any(x => x.Type == CalibrationPointType.LeftHand) || !calibrationPoints.Any(x => x.Type == CalibrationPointType.RightHand))
            {
                if (trackerHeightPercentage < calibrationPointAreas[CalibrationPointArea.HeadMin] && trackerHeightPercentage > calibrationPointAreas[CalibrationPointArea.WaistMax])
                {
                    var leftHand = calibrationPoints.FirstOrDefault(x => x.Type == CalibrationPointType.LeftHand);
                    if (leftHand == null)
                    {
                        calibrationPoint.Type = CalibrationPointType.LeftHand;
                    }
                    else
                    {
                        if (calibrationPoint.DistanceFromLine < leftHand.DistanceFromLine)
                        {
                            calibrationPoint.Type = CalibrationPointType.LeftHand;
                            leftHand.Type = CalibrationPointType.RightHand;
                        }
                        else
                        {
                            calibrationPoint.Type = CalibrationPointType.RightHand;
                        }
                    }
                }
            }
        }
    }

    private void AssignUpperBodyCalibrationPoints(List<CalibrationPoint> calibrationPoints)
    {
        for (int i = 0; i < calibrationPoints.Count; i++)
        {
            var calibrationPoint = calibrationPoints[i];

            var trackerPos = calibrationPoint.Position;
            trackerPos.y = 0;
            var cameraPos = Camera.main.transform.position;
            cameraPos.y = 0;
            calibrationPoint.DistanceFromCenter = (trackerPos - cameraPos).magnitude;
        }

        calibrationPoints.Sort((a, b) => a.DistanceFromCenter.CompareTo(b.DistanceFromCenter));

        calibrationPoints.First().Type = CalibrationPointType.LowerBack;

        for (int i = 1; i < calibrationPoints.Count; i++)
        {
            if (calibrationPoints[i].DistanceFromLine < 0)
            {
                calibrationPoints[i].Type = CalibrationPointType.LeftElbow;
            }
            else
            {
                calibrationPoints[i].Type = CalibrationPointType.RightElbow;
            }
        }
    }
}
