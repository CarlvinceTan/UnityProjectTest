// using UnityEngine;

// public class ExplosionController : MonoBehaviour
// {
//     [Header("Shader References")]
//     public ComputeShader computeShader;

//     [Header("Timing Parameters")]
//     [Tooltip("Time in seconds before the bulging effect starts")]
//     public float delayTime = 2.0f;
//     [Tooltip("How long the bulging effect lasts in seconds")]
//     public float bulgeDuration = 5.0f;
//     [Tooltip("How fast the bulge grows (higher = faster)")]
//     [Range(0.1f, 5f)]
//     public float bulgeSpeed = 1.0f;

//     [Header("Bulge Visual Parameters")]
//     [Range(0f, 1f)]
//     public float diffusionRate = 0.3f;       // How much the bulge spreads
//     [Range(0f, 0.5f)]
//     public float damping = 0.05f;            // Natural decay over time
//     [Range(0f, 3f)]
//     public float maxBulge = 1.0f;            // Maximum bulge amount
//     [Range(0f, 5f)]
//     public float displacementScale = 1.0f;   // Visual scale of displacement

//     [Header("Trigger Point")]
//     public Vector2 centerUV = new Vector2(0.5f, 0.5f);
//     public float triggerRadius = 0.3f;
//     public float triggerStrength = 2.0f;

//     [Header("Auto Start")]
//     public bool autoStart = true;

//     [Header("Debug")]
//     public bool showDebugInfo = true;
//     public bool testGPUWrite = false; // Test if GPU writes work at all

//     // Private variables
//     private RenderTexture bulgeField;
//     private RenderTexture bulgeVelocity;
//     private Material material;
//     private int textureWidth = 128;
//     private int textureHeight = 128;
//     private int kernelGrowth;
//     private int kernelTrigger;

//     // Mesh deformation
//     private Mesh originalMesh;
//     private Mesh deformedMesh;
//     private Vector3[] originalVertices;
//     private Vector3[] deformedVertices;
//     private Vector3[] normals;
//     private Vector2[] uvs;
//     private MeshFilter meshFilter;
//     private MeshCollider meshCollider;

//     // Timing
//     private float timer = 0f;
//     private bool hasStarted = false;
//     private bool isActive = false;
//     private float activeTimer = 0f;

//     // Debug
//     private float maxBulgeValueSeen = 0f;
//     private int debugFrameCounter = 0;
//     private bool gpuTestDone = false;

//     void Start()
//     {
//         // Validation
//         if (computeShader == null)
//         {
//             Debug.LogError("[BulgeController] ComputeShader is not assigned!");
//             enabled = false;
//             return;
//         }

//         // Get components
//         meshFilter = GetComponent<MeshFilter>();
//         if (meshFilter == null)
//         {
//             Debug.LogError("[BulgeController] MeshFilter component not found!");
//             enabled = false;
//             return;
//         }

//         meshCollider = GetComponent<MeshCollider>();
//         Renderer renderer = GetComponent<Renderer>();
//         if (renderer == null)
//         {
//             Debug.LogError("[BulgeController] Renderer component not found!");
//             enabled = false;
//             return;
//         }
//         material = renderer.material;

//         // Store original mesh and create working copy
//         originalMesh = meshFilter.sharedMesh;
//         if (originalMesh == null)
//         {
//             Debug.LogError("[BulgeController] MeshFilter has no mesh!");
//             enabled = false;
//             return;
//         }

//         deformedMesh = Instantiate(originalMesh);
//         deformedMesh.MarkDynamic();
//         meshFilter.mesh = deformedMesh;

//         // Cache mesh data
//         originalVertices = originalMesh.vertices;
//         deformedVertices = new Vector3[originalVertices.Length];
//         normals = originalMesh.normals;
//         uvs = originalMesh.uv;

//         if (uvs == null || uvs.Length == 0)
//         {
//             Debug.LogError("[BulgeController] Mesh has no UV coordinates!");
//             enabled = false;
//             return;
//         }

//         System.Array.Copy(originalVertices, deformedVertices, originalVertices.Length);

//         Debug.Log($"[BulgeController] Initialized - Mesh Vertices: {originalVertices.Length}, UVs: {uvs.Length}");

//         // Create render textures for bulge simulation
//         CreateRenderTextures();

//         // Get compute shader kernels
//         try
//         {
//             kernelGrowth = computeShader.FindKernel("GrowthSimulation");
//             kernelTrigger = computeShader.FindKernel("TriggerBulge");
//             Debug.Log($"[BulgeController] Compute shader kernels found - Growth: {kernelGrowth}, Trigger: {kernelTrigger}");
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError($"[BulgeController] Failed to find compute shader kernels: {e.Message}");
//             enabled = false;
//             return;
//         }

//         // Check compute shader support
//         if (!SystemInfo.supportsComputeShaders)
//         {
//             Debug.LogError("[BulgeController] This device does not support compute shaders!");
//             enabled = false;
//             return;
//         }

//         Debug.Log($"[BulgeController] Compute shader support confirmed - Max work groups: {SystemInfo.maxComputeWorkGroupSize}");

//         // Assign textures to material
//         material.SetTexture("_DisplacementMap", bulgeField);
//         material.SetFloat("_DisplacementScale", displacementScale);

//         Debug.Log($"[BulgeController] Setup complete - Auto-start: {autoStart}, Delay: {delayTime}s, Duration: {bulgeDuration}s, Speed: {bulgeSpeed}");

//         // Run GPU test if enabled
//         if (testGPUWrite)
//         {
//             TestGPUWrite();
//         }
//     }

//     void TestGPUWrite()
//     {
//         Debug.Log("[BulgeController] === GPU WRITE TEST START ===");

//         // Manually write some values to test
//         RenderTexture.active = bulgeField;
//         Texture2D testWrite = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);

//         // Fill with test pattern
//         Color[] colors = new Color[textureWidth * textureHeight];
//         for (int i = 0; i < colors.Length; i++)
//         {
//             colors[i] = new Color(0.5f, 0, 0, 1); // Write 0.5 to all pixels
//         }
//         testWrite.SetPixels(colors);
//         testWrite.Apply();

//         Graphics.Blit(testWrite, bulgeField);
//         RenderTexture.active = null;

//         // Read back
//         RenderTexture.active = bulgeField;
//         Texture2D readBack = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);
//         readBack.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
//         readBack.Apply();
//         RenderTexture.active = null;

//         float testValue = readBack.GetPixel(textureWidth / 2, textureHeight / 2).r;
//         Debug.Log($"[BulgeController] GPU Write Test - Expected: 0.5, Got: {testValue}");

//         if (Mathf.Abs(testValue - 0.5f) < 0.01f)
//         {
//             Debug.Log("[BulgeController] ✓ GPU Write/Read working correctly");
//         }
//         else
//         {
//             Debug.LogError("[BulgeController] ✗ GPU Write/Read NOT working!");
//         }

//         // Clear it again
//         RenderTexture.active = bulgeField;
//         GL.Clear(true, true, Color.black);
//         RenderTexture.active = null;

//         Destroy(testWrite);
//         Destroy(readBack);

//         Debug.Log("[BulgeController] === GPU WRITE TEST END ===");
//         gpuTestDone = true;
//     }

//     void CreateRenderTextures()
//     {
//         // Clean up existing textures
//         if (bulgeField != null) bulgeField.Release();
//         if (bulgeVelocity != null) bulgeVelocity.Release();

//         // Create new textures with proper format
//         bulgeField = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.RFloat);
//         bulgeField.enableRandomWrite = true;
//         bulgeField.filterMode = FilterMode.Bilinear;
//         bulgeField.wrapMode = TextureWrapMode.Clamp;
//         if (!bulgeField.Create())
//         {
//             Debug.LogError("[BulgeController] Failed to create bulgeField RenderTexture!");
//         }

//         bulgeVelocity = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.RFloat);
//         bulgeVelocity.enableRandomWrite = true;
//         bulgeVelocity.filterMode = FilterMode.Bilinear;
//         bulgeVelocity.wrapMode = TextureWrapMode.Clamp;
//         if (!bulgeVelocity.Create())
//         {
//             Debug.LogError("[BulgeController] Failed to create bulgeVelocity RenderTexture!");
//         }

//         // Initialize to zero
//         RenderTexture.active = bulgeField;
//         GL.Clear(true, true, Color.black);
//         RenderTexture.active = bulgeVelocity;
//         GL.Clear(true, true, Color.black);
//         RenderTexture.active = null;

//         Debug.Log($"[BulgeController] RenderTextures created - Size: {textureWidth}x{textureHeight}, Format: RFloat");
//     }

//     public void TriggerBulgeEffect()
//     {
//         if (!isActive)
//         {
//             isActive = true;
//             activeTimer = 0f;
//             hasStarted = true;
//             ApplyTrigger();
//             Debug.Log("[BulgeController] Bulge effect manually triggered");
//         }
//     }

//     public void TriggerBulgeAt(Vector2 uv, float radius, float strength)
//     {
//         centerUV = uv;
//         triggerRadius = radius;
//         triggerStrength = strength;
//         TriggerBulgeEffect();
//     }

//     void Update()
//     {
//         // Handle delay timer
//         if (autoStart && !hasStarted)
//         {
//             timer += Time.deltaTime;
//             if (timer >= delayTime)
//             {
//                 hasStarted = true;
//                 isActive = true;
//                 activeTimer = 0f;
//                 ApplyTrigger();
//                 Debug.Log($"[BulgeController] Delay complete ({delayTime}s) - Starting bulge effect");
//             }
//         }

//         // Handle active bulging
//         if (isActive)
//         {
//             activeTimer += Time.deltaTime;

//             // Run growth simulation
//             UpdateGrowthSimulation();

//             // Deform the actual mesh
//             DeformMesh();

//             // Debug logging every 30 frames
//             debugFrameCounter++;
//             if (showDebugInfo && debugFrameCounter % 30 == 0)
//             {
//                 Debug.Log($"[BulgeController] Active - Timer: {activeTimer:F2}s/{bulgeDuration}s, Max bulge value: {maxBulgeValueSeen:F4}");
//             }

//             // Check if duration is complete
//             if (activeTimer >= bulgeDuration)
//             {
//                 isActive = false;
//                 Debug.Log($"[BulgeController] Bulge duration complete ({bulgeDuration}s) - Effect finished, Max bulge: {maxBulgeValueSeen:F4}");
//             }
//         }

//         // Update material properties
//         material.SetTexture("_DisplacementMap", bulgeField);
//         material.SetFloat("_DisplacementScale", displacementScale);
//         material.SetFloat("_MaxBulge", maxBulge);
//     }

//     void ApplyTrigger()
//     {
//         Debug.Log($"[BulgeController] === APPLYING TRIGGER ===");
//         Debug.Log($"[BulgeController] Center UV: ({centerUV.x}, {centerUV.y})");
//         Debug.Log($"[BulgeController] Radius: {triggerRadius}, Strength: {triggerStrength}");
//         Debug.Log($"[BulgeController] Texture size: {textureWidth}x{textureHeight}");

//         // Set all parameters
//         computeShader.SetTexture(kernelTrigger, "_BulgeField", bulgeField);
//         computeShader.SetTexture(kernelTrigger, "_BulgeVelocity", bulgeVelocity);
//         computeShader.SetVector("_TriggerCenter", new Vector4(centerUV.x, centerUV.y, 0, 0));
//         computeShader.SetFloat("_TriggerRadius", triggerRadius);
//         computeShader.SetFloat("_TriggerStrength", triggerStrength);
//         computeShader.SetInt("_TexWidth", textureWidth);
//         computeShader.SetInt("_TexHeight", textureHeight);

//         int dispatchX = Mathf.CeilToInt(textureWidth / 8.0f);
//         int dispatchY = Mathf.CeilToInt(textureHeight / 8.0f);

//         Debug.Log($"[BulgeController] Dispatching compute shader - Groups: ({dispatchX}, {dispatchY}, 1)");
//         computeShader.Dispatch(kernelTrigger, dispatchX, dispatchY, 1);

//         // Immediately read back to verify
//         RenderTexture.active = bulgeField;
//         Texture2D verify = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);
//         verify.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
//         verify.Apply();
//         RenderTexture.active = null;

//         // Check center pixel
//         int centerX = Mathf.FloorToInt(centerUV.x * textureWidth);
//         int centerY = Mathf.FloorToInt(centerUV.y * textureHeight);
//         float centerValue = verify.GetPixel(centerX, centerY).r;

//         // Check a few random pixels
//         float max = 0f;
//         int nonZeroCount = 0;
//         for (int y = 0; y < textureHeight; y++)
//         {
//             for (int x = 0; x < textureWidth; x++)
//             {
//                 float val = verify.GetPixel(x, y).r;
//                 if (val > 0.001f) nonZeroCount++;
//                 max = Mathf.Max(max, val);
//             }
//         }

//         Debug.Log($"[BulgeController] Post-trigger check:");
//         Debug.Log($"  - Center pixel ({centerX}, {centerY}) value: {centerValue}");
//         Debug.Log($"  - Max value in texture: {max}");
//         Debug.Log($"  - Non-zero pixels: {nonZeroCount} / {textureWidth * textureHeight}");

//         Destroy(verify);

//         Debug.Log($"[BulgeController] === TRIGGER COMPLETE ===");
//     }

//     void UpdateGrowthSimulation()
//     {
//         float effectiveGrowthRate = bulgeSpeed * 1.0f;

//         computeShader.SetTexture(kernelGrowth, "_BulgeField", bulgeField);
//         computeShader.SetTexture(kernelGrowth, "_BulgeVelocity", bulgeVelocity);
//         computeShader.SetFloat("_DeltaTime", Time.deltaTime);
//         computeShader.SetFloat("_GrowthRate", effectiveGrowthRate);
//         computeShader.SetFloat("_DiffusionRate", diffusionRate);
//         computeShader.SetFloat("_Damping", damping);
//         computeShader.SetFloat("_MaxBulge", maxBulge);
//         computeShader.SetInt("_TexWidth", textureWidth);
//         computeShader.SetInt("_TexHeight", textureHeight);

//         int dispatchX = Mathf.CeilToInt(textureWidth / 8.0f);
//         int dispatchY = Mathf.CeilToInt(textureHeight / 8.0f);
//         computeShader.Dispatch(kernelGrowth, dispatchX, dispatchY, 1);
//     }

//     void DeformMesh()
//     {
//         RenderTexture.active = bulgeField;
//         Texture2D readTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);
//         readTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
//         readTexture.Apply();
//         RenderTexture.active = null;

//         maxBulgeValueSeen = 0f;
//         float totalDisplacement = 0f;

//         // Deform vertices based on bulge field
//         for (int i = 0; i < originalVertices.Length; i++)
//         {
//             Vector2 uv = uvs[i];
//             float bulgeValue = SampleTextureBilinear(readTexture, uv.x, uv.y);

//             maxBulgeValueSeen = Mathf.Max(maxBulgeValueSeen, bulgeValue);

//             float normalizedBulge = Mathf.Clamp01(bulgeValue / maxBulge);
//             float displacement = Mathf.Pow(normalizedBulge, 0.7f) * displacementScale;

//             totalDisplacement += displacement;

//             deformedVertices[i] = originalVertices[i] + normals[i] * displacement;
//         }

//         deformedMesh.vertices = deformedVertices;
//         deformedMesh.RecalculateBounds();
//         deformedMesh.RecalculateNormals();

//         if (meshCollider != null)
//         {
//             meshCollider.sharedMesh = null;
//             meshCollider.sharedMesh = deformedMesh;
//         }

//         if (showDebugInfo && debugFrameCounter % 30 == 0)
//         {
//             float avgDisplacement = totalDisplacement / originalVertices.Length;
//             Debug.Log($"[BulgeController] Mesh deformed - Avg displacement: {avgDisplacement:F4}, Max bulge: {maxBulgeValueSeen:F4}");
//         }

//         Destroy(readTexture);
//     }

//     float SampleTextureBilinear(Texture2D tex, float u, float v)
//     {
//         u = Mathf.Clamp01(u);
//         v = Mathf.Clamp01(v);

//         float x = u * (textureWidth - 1);
//         float y = v * (textureHeight - 1);

//         int x0 = Mathf.FloorToInt(x);
//         int y0 = Mathf.FloorToInt(y);
//         int x1 = Mathf.Min(x0 + 1, textureWidth - 1);
//         int y1 = Mathf.Min(y0 + 1, textureHeight - 1);

//         float fx = x - x0;
//         float fy = y - y0;

//         float c00 = tex.GetPixel(x0, y0).r;
//         float c10 = tex.GetPixel(x1, y0).r;
//         float c01 = tex.GetPixel(x0, y1).r;
//         float c11 = tex.GetPixel(x1, y1).r;

//         float c0 = Mathf.Lerp(c00, c10, fx);
//         float c1 = Mathf.Lerp(c01, c11, fx);
//         return Mathf.Lerp(c0, c1, fy);
//     }

//     void OnDestroy()
//     {
//         if (bulgeField != null) bulgeField.Release();
//         if (bulgeVelocity != null) bulgeVelocity.Release();
//         if (deformedMesh != null) Destroy(deformedMesh);
//         Debug.Log("[BulgeController] Cleaned up resources");
//     }
// }
