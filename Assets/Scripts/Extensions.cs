using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static int Replace<T>(this IList<T> source, T oldValue, T newValue)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        var index = source.IndexOf(oldValue);
        if (index != -1)
            source[index] = newValue;
        return index;
    }

    //Linear Equation y-y1 = m(x-x1)
    public static float LeftOrRightFromLine(Vector3 origin, Vector3 target, Vector3 point)
    {
        var slope = origin.x - target.x / origin.z - target.z;
        return point.x - origin.x - slope * (point.z - origin.z);
    }

    public static void AssignChildAndKeepLocalTransform(Transform parent, Transform child)
    {
        Vector3 localPos = child.localPosition;
        Vector3 localScale = child.localScale;
        Quaternion localRot = child.localRotation;
        child.parent = parent;
        child.localPosition = localPos;
        child.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        child.localRotation = localRot;
        parent = child;
    }
}
