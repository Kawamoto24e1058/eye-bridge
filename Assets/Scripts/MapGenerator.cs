using UnityEngine;
using System.Collections.Generic;

namespace EyeTrackingGame.Runtime
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        [Tooltip("Width of the path (Meters)")]
        public float pathWidth = 6.0f; // Wide path
        [Tooltip("Length of each path segment (Meters)")]
        public float segmentLength = 5.0f;
        [Tooltip("Total length of the course (Meters)")]
        public float totalLength = 100.0f;

        [Header("Obstacle Settings")]
        [Tooltip("Probability of a gap appearing (0.0 - 1.0)")]
        [Range(0, 1)] public float gapProbability = 0.1f; // Low probability
        [Tooltip("Width of the jump gap (Meters)")]
        public float gapSize = 3.0f;
        
        [Tooltip("Probability of a low gate appearing (0.0 - 1.0)")]
        [Range(0, 1)] public float gateProbability = 0.2f;
        [Tooltip("Height clearance for low gate")]
        public float gateClearance = 1.2f;

        [Header("Goal")]
        public float goalPlatformSize = 10.0f;
        public Material pathMaterial;
        public Material goalMaterial;
        public Material obstacleMaterial;

        private void Start()
        {
            GenerateMap();
        }

        [ContextMenu("Regenerate Map")]
        public void GenerateMap()
        {
            // Cleanup existing children
            foreach (Transform child in transform)
            {
                DestroyImmediate(child.gameObject);
            }

            float currentZ = 0;
            int segmentCount = Mathf.CeilToInt(totalLength / segmentLength);
            
            // Create Start Platform
            CreatePlatform(new Vector3(0, 0, 0), new Vector3(pathWidth, 1, 10), "StartPlatform", pathMaterial);
            currentZ += 5.0f;

            for (int i = 0; i < segmentCount; i++)
            {
                // Determine next segment type
                float rand = Random.value;
                
                if (rand < gapProbability)
                {
                    // Create Gap: Just move currentZ forward without creating floor
                    currentZ += gapSize;
                    // Ensure we have a landing pad after gap
                    CreatePlatform(new Vector3(0, 0, currentZ + segmentLength/2), new Vector3(pathWidth, 1, segmentLength), $"Segment_{i}_Landing", pathMaterial);
                    currentZ += segmentLength;
                }
                else
                {
                    // Create Normal Path
                    Vector3 pos = new Vector3(0, 0, currentZ + segmentLength/2);
                    CreatePlatform(pos, new Vector3(pathWidth, 1, segmentLength), $"Segment_{i}", pathMaterial);

                    // Check for Gate (Low obstacle)
                    if (Random.value < gateProbability)
                    {
                        CreateGate(new Vector3(0, 0, currentZ + segmentLength/2));
                    }

                    currentZ += segmentLength;
                }
            }

            // Create Goal Platform
            CreatePlatform(new Vector3(0, 0, currentZ + goalPlatformSize/2), new Vector3(goalPlatformSize, 1, goalPlatformSize), "GoalPlatform", goalMaterial);
        }

        private void CreatePlatform(Vector3 position, Vector3 scale, string name, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(this.transform);
            
            // Adjust Y so top surface is at 0
            position.y = -0.5f; 
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;

            if (mat != null)
            {
                cube.GetComponent<Renderer>().material = mat;
            }
        }

        private void CreateGate(Vector3 position)
        {
            // A gate consists of two pillars and a top bar, or just a floating bar.
            // Simplified: One floating block that requires crouching to pass under.
            
            GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gate.name = "CrouchGate";
            gate.transform.SetParent(this.transform);

            // Gate bottom should be at 'gateClearance' height
            // Cube pivot is center. 
            // Scale Y = 2 (arbitrary thickness)
            // Center Y = gateClearance + (Scale Y / 2)
            float barThickness = 1.0f;
            float centerY = gateClearance + (barThickness * 0.5f);

            gate.transform.localPosition = new Vector3(position.x, centerY, position.z);
            gate.transform.localScale = new Vector3(pathWidth, barThickness, 0.5f); // Thin obstacle

            if (obstacleMaterial != null)
            {
                gate.GetComponent<Renderer>().material = obstacleMaterial;
            }
        }
    }
}
