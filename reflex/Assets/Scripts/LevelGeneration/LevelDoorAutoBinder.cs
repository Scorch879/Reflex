using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelDoorAutoBinder
{
    private const float SortTolerance = 0.05f;

    public static List<LevelDoor> FindOrCreateDoors()
    {
        List<LevelDoor> doors = FindExistingDoors();

        if (doors.Count == 0)
        {
            List<Transform> candidates = FindDoorCandidates();

            for (int i = 0; i < candidates.Count; i++)
            {
                LevelDoor door = candidates[i].gameObject.AddComponent<LevelDoor>();
                doors.Add(door);
            }
        }

        for (int i = 0; i < doors.Count; i++)
        {
            EnsureInteractionCollider(doors[i]);
        }

        SortDoors(doors);
        return doors;
    }

    private static List<LevelDoor> FindExistingDoors()
    {
        LevelDoor[] foundDoors = UnityEngine.Object.FindObjectsByType<LevelDoor>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        List<LevelDoor> generationDoors = new List<LevelDoor>();
        for (int i = 0; i < foundDoors.Length; i++)
        {
            if (foundDoors[i] != null && foundDoors[i].ParticipateInGeneration)
            {
                generationDoors.Add(foundDoors[i]);
            }
        }

        return generationDoors;
    }

    private static List<Transform> FindDoorCandidates()
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        List<Transform> singularDoors = new List<Transform>();
        List<Transform> doorGroups = new List<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            string normalizedName = Normalize(candidate.name);

            if (IsSingularDoorName(normalizedName))
            {
                singularDoors.Add(candidate);
            }
            else if (IsDoorGroupName(normalizedName))
            {
                doorGroups.Add(candidate);
            }
        }

        return singularDoors.Count > 0 ? singularDoors : doorGroups;
    }

    private static bool IsSingularDoorName(string normalizedName)
    {
        if (normalizedName == "door" ||
            normalizedName.StartsWith("door (", StringComparison.Ordinal) ||
            normalizedName == "door wall")
        {
            return true;
        }

        return false;
    }

    private static bool IsDoorGroupName(string normalizedName)
    {
        return normalizedName == "doors" ||
               normalizedName.StartsWith("doors ", StringComparison.Ordinal) ||
               normalizedName.StartsWith("doorrs ", StringComparison.Ordinal);
    }

    private static void EnsureInteractionCollider(LevelDoor door)
    {
        if (door.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        BoxCollider interactionCollider = door.gameObject.AddComponent<BoxCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.center = Vector3.up * 1.5f;
        interactionCollider.size = new Vector3(3f, 3f, 3f);
    }

    private static void SortDoors(List<LevelDoor> doors)
    {
        doors.Sort(CompareDoors);
    }

    private static int CompareDoors(LevelDoor left, LevelDoor right)
    {
        bool leftHasOrder = left.RouteOrder >= 0;
        bool rightHasOrder = right.RouteOrder >= 0;

        if (leftHasOrder && rightHasOrder && left.RouteOrder != right.RouteOrder)
        {
            return left.RouteOrder.CompareTo(right.RouteOrder);
        }

        if (leftHasOrder != rightHasOrder)
        {
            return leftHasOrder ? -1 : 1;
        }

        Vector3 leftPosition = left.transform.position;
        Vector3 rightPosition = right.transform.position;

        int zCompare = CompareFloat(leftPosition.z, rightPosition.z);
        if (zCompare != 0)
        {
            return zCompare;
        }

        int xCompare = CompareFloat(leftPosition.x, rightPosition.x);
        if (xCompare != 0)
        {
            return xCompare;
        }

        return string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareFloat(float left, float right)
    {
        if (Mathf.Abs(left - right) <= SortTolerance)
        {
            return 0;
        }

        return left < right ? -1 : 1;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
