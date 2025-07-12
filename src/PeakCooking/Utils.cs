using System;
using System.Collections.Generic;
using UnityEngine;
using static PeakCooking.CookingPot;

namespace PeakCooking;

public class PeakCookingException : Exception
{
    public PeakCookingException(string message) : base(message)
    {
    }
}

public static class Utils
{
    public static T NonNullGet<T>(T? obj)
    {
        if (obj == null)
        {
            throw new PeakCookingException("NonNullGet: Field not initialized.");
        }
        return obj;
    }

    public static void NonNullSet<T>(ref T obj, T? value)
    {
        if (value == null)
        {
            throw new PeakCookingException("NonNullSet: Value is null.");
        }
        obj = value;
    }

    public static Item Item(this PotItem cookingPotItem)
    {
        ItemDatabase.TryGetItem(cookingPotItem.ID, out Item item);
        return item;
    }

    // used to create the dummy objects in the soup
    public static GameObject CloneItemMeshesOnly(GameObject prefab, out Bounds bounds)
    {
        GameObject root = new GameObject("DummyItem");
        // used to center the mesh, since items are typically offset from the origin
        GameObject adjust = new GameObject("Adjust");
        adjust.transform.parent = root.transform;
        // recursively copy meshes
        GameObject go = CloneItemMeshesHelper(prefab, out bool hasMesh);
        go.transform.parent = adjust.transform;
        // destroy hands
        string[] hands = { "Hand_L", "Hand_R" };
        foreach (var handName in hands)
        {
            GameObject? hand = go.transform.Find(handName)?.gameObject;
            if (hand != null)
            {
                UnityEngine.Object.Destroy(hand);
            }
        }
        // adjust cloned object to center of root
        bounds = ColliderUtils.GetCollidersBounds(prefab);
        adjust.transform.localPosition = -(bounds.center - prefab.transform.position);
        return root;
    }
    private static GameObject CloneItemMeshesHelper(GameObject obj, out bool hasMesh)
    {
        hasMesh = false;
        GameObject newObj = new GameObject(obj.name);
        foreach (Transform child in obj.transform)
        {
            if (!child.gameObject.activeSelf)
            {
                continue;
            }
            GameObject newChild = CloneItemMeshesHelper(child.gameObject, out bool childHasMesh);
            if (childHasMesh)
            {
                hasMesh = true;
                newChild.transform.parent = newObj.transform;
                newChild.transform.localPosition = child.localPosition;
                newChild.transform.localRotation = child.localRotation;
                newChild.transform.localScale = child.localScale;
            }
            // kill branches without meshes
            else
            {
                UnityEngine.Object.Destroy(newChild);
            }
        }
        // copy Mesh
        MeshFilter mesh = obj.GetComponent<MeshFilter>();
        if (mesh != null)
        {
            MeshFilter newMesh = newObj.AddComponent<MeshFilter>();
            newMesh.mesh = mesh.mesh;
        }
        // copy MeshRenderer
        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            MeshRenderer newMeshRenderer = newObj.AddComponent<MeshRenderer>();
            newMeshRenderer.materials = meshRenderer.materials;
            newMeshRenderer.shadowCastingMode = meshRenderer.shadowCastingMode;
        }
        hasMesh = hasMesh || (mesh != null && meshRenderer != null);
        return newObj;
    }

    public static class ColliderUtils
    {
        /// <summary>
        /// Finds all colliders in a GameObject and its children recursively, 
        /// then calculates the merged bounds of all colliders.
        /// </summary>
        /// <param name="gameObject">The root GameObject to search</param>
        /// <returns>The merged bounds of all colliders, or Bounds default if no colliders found</returns>
        public static Bounds GetCollidersBounds(GameObject gameObject)
        {
            List<Collider> colliders = new List<Collider>();
            FindCollidersRecursive(gameObject.transform, colliders);

            return CalculateCollidersBounds(colliders);
        }

        /// <summary>
        /// Recursively finds all colliders in the transform hierarchy
        /// </summary>
        /// <param name="transform">Current transform to search</param>
        /// <param name="colliders">List to store found colliders</param>
        private static void FindCollidersRecursive(Transform transform, List<Collider> colliders)
        {
            // Get all colliders in the current GameObject
            Collider[] currentColliders = transform.GetComponents<Collider>();

            // Add them to our list (excluding triggers if desired)
            foreach (Collider collider in currentColliders)
            {
                if (collider.enabled) // Only include enabled colliders
                {
                    colliders.Add(collider);
                }
            }

            // Recursively search children
            foreach (Transform child in transform)
            {
                FindCollidersRecursive(child, colliders);
            }
        }

        /// <summary>
        /// Calculates the merged bounds of all colliders
        /// </summary>
        /// <param name="colliders">List of colliders to calculate center for</param>
        /// <returns>The merged bounds of all colliders</returns>
        private static Bounds CalculateCollidersBounds(List<Collider> colliders)
        {
            if (colliders.Count == 0)
            {
                Debug.LogWarning("No colliders found to calculate center point");
                return default;
            }

            // Initialize bounds with the first collider
            Bounds combinedBounds = colliders[0].bounds;

            // Encapsulate all other colliders
            for (int i = 1; i < colliders.Count; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }

            return combinedBounds;
        }
    }
}