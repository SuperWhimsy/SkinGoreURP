using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinGoreRenderer : MonoBehaviour
{
    public SkinnedMeshRenderer skin;
    public Material goreMaterial;
    public int goreMapResolution = 64;

    // Optional cooldown if on low GPU devices
    private int frameCounter = 0;
    [SerializeField] private int cooldownFrames = 10;

    static Material damageRendererMat;
    static Material dilateMat;
    static Material addMat;

    SkinnedMeshRenderer damageRendererSkin;
    Camera damageRendererCam;
    Transform skinTransformRoot => skin.rootBone != null ? skin.rootBone : skin.transform;

    /// <summary>
    /// the accumulated gore map
    /// </summary>
    RenderTexture goreMap;

    MaterialPropertyBlock matProps;

    /// <summary>
    /// Temporary render tex. Used for pooling textures for efficiency when there are lots of characters taking damage.
    /// </summary>
    private class TempTex
    {
        public RenderTexture tex;
        public int resolution => tex != null ? tex.width : 0;
        public bool inUse;
        public void Free() => inUse = false;

        public TempTex(int resolution)
        {
            tex = new RenderTexture(resolution, resolution, 0);
        }
    }
    static List<TempTex> tempTexPool = new List<TempTex>();
    TempTex GetTempTex()
    {
        TempTex tex = tempTexPool.Find(t => !t.inUse && t.resolution == goreMapResolution);
        if (tex == null)
        {
            tex = new TempTex(goreMapResolution);
            tempTexPool.Add(tex);
        }
        tex.inUse = true;
        return tex;
    }

    const int renderLayer = 15; // use post processing layer - it should be empty

    bool hasBlendShapes;

    public delegate void OutputDebug(RenderTexture hit, RenderTexture damage);
    public event OutputDebug OnDebugOutput;

    // Shader property IDs
    int id_position = Shader.PropertyToID("_DamagePosition");
    int id_radius = Shader.PropertyToID("_DamageRadius");
    int id_amount = Shader.PropertyToID("_DamageAmount");

    [SerializeField] private float delayBetweenStages = 0.5f; // Public variable for delay time

    private void Start()
    {
        hasBlendShapes = skin.sharedMesh.blendShapeCount > 0;
        StartCoroutine(InitSkinWithDelay());
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    bool skinInit;
    IEnumerator InitSkinWithDelay()
    {
        // create materials
        if (damageRendererMat == null) damageRendererMat = new Material(Shader.Find("Hidden/DamageBaker"));
        if (addMat == null) addMat = new Material(Shader.Find("Hidden/AddBlit"));
        if (dilateMat == null) dilateMat = new Material(Shader.Find("Hidden/Dilate"));
        // UnityEngine.Debug.Log("Materials Initialized");
        yield return new WaitForSeconds(delayBetweenStages);

        // make copy of skin for rendering to damage buffer
        damageRendererSkin = Instantiate(skin.gameObject, skin.transform.parent).GetComponent<SkinnedMeshRenderer>();
        damageRendererSkin.sharedMaterials = new Material[] { damageRendererMat };
        damageRendererSkin.gameObject.layer = renderLayer;
        CleanSkinnedMeshGO(damageRendererSkin.gameObject);
        damageRendererSkin.gameObject.SetActive(false);
        // UnityEngine.Debug.Log("Copying materials");
        yield return new WaitForSeconds(delayBetweenStages);

        // create camera to render to damage buffer
        damageRendererCam = new GameObject("damageRenderer").AddComponent<Camera>();
        damageRendererCam.transform.parent = damageRendererSkin.transform;
        damageRendererCam.transform.localPosition = Vector3.forward * -10;
        damageRendererCam.transform.localRotation = Quaternion.identity;
        damageRendererCam.orthographic = true;
        damageRendererCam.orthographicSize = 5;
        damageRendererCam.farClipPlane = 15;
        damageRendererCam.cullingMask = 1 << renderLayer;
        damageRendererCam.clearFlags = CameraClearFlags.SolidColor;
        damageRendererCam.backgroundColor = Color.clear;
        damageRendererCam.enabled = false;
        damageRendererCam.useOcclusionCulling = false;
        // UnityEngine.Debug.Log("Making camera");
        yield return new WaitForSeconds(delayBetweenStages);

        goreMap = new RenderTexture(goreMapResolution, goreMapResolution, 0);
        // UnityEngine.Debug.Log("Gore Map Texture Created: " + goreMap);
        yield return new WaitForSeconds(delayBetweenStages);

        matProps = new MaterialPropertyBlock();
        matProps.SetTexture("_GoreDamage", goreMap);
        skin.SetPropertyBlock(matProps);
        //  UnityEngine.Debug.Log("Gore Map Texture Assigned to Material: " + goreMap);

        // Generate and set the random seed
        float randomSeed = UnityEngine.Random.Range(0f, 1000f);
        matProps.SetFloat("_GoreSeed", randomSeed);
        skin.SetPropertyBlock(matProps);

        skinInit = true;
    }

    public void AddDamage(Vector3 position, float radius, float amount)
    {
        // Increment frame counter
        frameCounter++;

        // Check if cooldown period has elapsed
        if (frameCounter < cooldownFrames)
        {
            return;
        }

        // Reset frame counter
        frameCounter = 0;

        if (!skinInit) StartCoroutine(InitSkinWithDelay());

        // Check if goreMap is still valid
        if (goreMap == null)
        {
            UnityEngine.Debug.LogError("Gore Map Texture is null.");
            return;
        }

        position = skinTransformRoot.transform.InverseTransformPoint(position);

        damageRendererSkin.gameObject.SetActive(true);
        damageRendererMat.SetVector(id_position, new Vector4(position.x, position.y, position.z, 0));
        damageRendererMat.SetFloat(id_radius, radius);
        damageRendererMat.SetFloat(id_amount, amount);
        var tex_damage = GetTempTex();
        var tex_dilated = GetTempTex();

        // Perform render target switch only if necessary
        if (damageRendererCam.targetTexture != tex_damage.tex)
        {
            damageRendererCam.targetTexture = tex_damage.tex;
        }

        damageRendererCam.Render();
        damageRendererSkin.gameObject.SetActive(false);

        // Add padding to damage spot
        Graphics.Blit(tex_damage.tex, tex_dilated.tex, dilateMat);
        // Additively blend with existing gore map
        Graphics.Blit(tex_dilated.tex, goreMap, addMat);

        OnDebugOutput?.Invoke(tex_dilated.tex, goreMap);

        // Free temporary textures back into pool
        tex_dilated.Free();
        tex_damage.Free();
    }

    private void LateUpdate()
    {
        if (skinInit && hasBlendShapes)
        {
            // Update blend shapes to match main renderer if necessary
            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++)
            {
                skin.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i));
            }
        }
    }

    /// <summary>
    /// Resets any damage this renderer has taken
    /// </summary>
    public void ResetDamage()
    {
        if (!skinInit) return;
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = goreMap;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
        OnDebugOutput?.Invoke(goreMap, goreMap);
    }

    /// <summary>
    /// Copy this GoreRenderer's data to a different one. I use this for making ragdolls keep the damage the alive enemy had!
    /// </summary>
    /// <param name="newGore">The new GoreRenderer to copy to</param>
    /// <param name="canReuseTextures">Whether we can reuse textures. If the old renderer is being deleted, leave true. Only needs to be false if you're creating a copy.</param>
    public void TransferToNewGoreRenderer(SkinGoreRenderer newGore, bool canReuseTextures = true)
    {
        newGore.goreMapResolution = goreMapResolution;
        if (!newGore.skinInit) StartCoroutine(newGore.InitSkinWithDelay());
        if (canReuseTextures) newGore.goreMap = goreMap;
        else Graphics.Blit(goreMap, newGore.goreMap);

        newGore.matProps.SetTexture("_GoreDamage", newGore.goreMap);
        newGore.skin.SetPropertyBlock(newGore.matProps);
    }

    private void CleanUp()
    {
        UnityEngine.Debug.Log("SkinGoreRenderer is being destroyed.");
        if (goreMap != null)
        {
            goreMap.Release();
            RenderTexture.Destroy(goreMap);
            goreMap = null;
            // UnityEngine.Debug.Log("Gore Map Texture Destroyed.");
        }
    }

    /// <summary>
    /// Remove any irrelevant components and children
    /// </summary>
    void CleanSkinnedMeshGO(GameObject go)
    {
        foreach (UnityEngine.Component c in go.GetComponents<UnityEngine.Component>())
        {
            if (!(c is Transform || c is SkinnedMeshRenderer))
            {
                Destroy(c);
            }
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            Destroy(go.transform.GetChild(i).gameObject);
        }
    }
}