//#########[---------------------------]#########
//#########[  GENERATED FROM TEMPLATE  ]#########
//#########[---------------------------]#########
#define USE_5D
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class ParticleSystem5D : MonoBehaviour {
    public static readonly int ModelMatrixID = Shader.PropertyToID("_ModelMatrix");
    public static readonly int ModelPositionID = Shader.PropertyToID("_ModelPosition");
#if USE_5D
    public static readonly int ModelMatrixC4ID = Shader.PropertyToID("_ModelMatrix_C4");
    public static readonly int ModelMatrixR4ID = Shader.PropertyToID("_ModelMatrix_R4");
    public static readonly int ModelMatrixVVID = Shader.PropertyToID("_ModelMatrix_VV");
    public static readonly int ModelPositionVID = Shader.PropertyToID("_ModelPosition_V");
#else
    public static readonly int ModelMatrixITID = Shader.PropertyToID("_ModelMatrixIT");
#endif
    public static readonly int ModelColorID = Shader.PropertyToID("_Color");

    [Header("Lifecycle")]
    public int maxSpawn = 50;
    public float lifetime = 5.0f;
    public float spawnRate = 10.0f;
    public float killBelowY = -1.0f;
    public float prewarmTime = 0.0f;
    public float prewarmDeltaTime = 0.0f;

    [Header("Dynamics")]
    public Vector5 minVelocity = (Vector5)Vector3.up;
    public Vector5 maxVelocity = (Vector5)Vector3.up;
    public Vector5 accel = Vector5.zero;
    public float velocityJitter = 0.0f;
    public float friction = 0.0f;
    public bool followCameraView = false;

    [Header("Color")]
    public Color randColorA = Color.white;
    public Color randColorB = Color.white;
    public float fadeInTime = 0.0f;
    public float fadeOutTime = 0.0f;
    public bool overlay = false;

    [Header("Geometry")]
    public float minScale = 1.0f;
    public float maxScale = 1.0f;
    public float scaleRate = 0.0f;
    public Vector5 scaleAxes = Vector5.one;
    public Mesh mesh;

    [Header("Rendering")]
    public bool billboard = false;
    public bool forceSlicePlane = false;
    public bool isShadow = false;
    public bool checkOcclusion = true;
    public bool ageOrdering = false;
    public Material[] meshMaterials;

    public struct Particle : IComparable {
        public Vector5 pos;
        public Matrix5x5 mat;
        public Vector5 velocity;
        public Color color;
        public float scale;
        public float age;
        public float alpha;

        public int CompareTo(object other) {
            return age < ((Particle)other).age ? 1 : -1;
        }
    }

    private Object5D obj5D;
    private BasicCamera5D cc;
    private Camera cam;

    protected Matrix4x4[] modelMatrixArr;
    protected Vector4[] modelPositionArr;
#if USE_5D
    protected Vector4[] modelMatrixC4Arr;
    protected Vector4[] modelMatrixR4Arr;
    protected float[] modelMatrixVVArr;
    protected float[] modelPositionVArr;
#endif
    protected Vector4[] modelColorArr;
    protected Particle[] particles;
    protected MaterialPropertyBlock mpb;
    protected int numActive = 0;
    protected int numVisible = 0;
    protected Vector5 bCenter = Vector5.zero;
    protected float bRadius = 0.0f;
    protected float spawnRemainder = 0.0f;
    protected Transform5D worldTransform;
    protected float frictionMul;
    protected float shadowDist;

    public void KillAllParticles() {
        numActive = 0;
    }

    protected virtual void Awake() {
        //Get the main camera
        cc = FindObjectOfType<BasicCamera5D>();
        obj5D = GetComponent<Object5D>();
        Debug.Assert(cc != null);
        Debug.Assert(obj5D != null);
        string camTag = (overlay ? "OverlayCamera" : "MainCamera");
        cam = GameObject.FindGameObjectWithTag(camTag)?.GetComponent<Camera>();

        //Calculate bounding spheres for the mesh
        SetMesh(mesh);

        //Allocate buffers
        mpb = new MaterialPropertyBlock();
        modelMatrixArr = new Matrix4x4[maxSpawn];
        modelPositionArr = new Vector4[maxSpawn];
#if USE_5D
        modelMatrixC4Arr = new Vector4[maxSpawn];
        modelMatrixR4Arr = new Vector4[maxSpawn];
        modelMatrixVVArr = new float[maxSpawn];
        modelPositionVArr = new float[maxSpawn];
#endif
        modelColorArr = new Vector4[maxSpawn];
        particles = new Particle[maxSpawn];

        shadowDist = meshMaterials[0].GetFloat(Occlusion5D.shadowDistID);
    }

    protected void LateUpdate() {
        while (prewarmTime > 0.0f) {
            UpdateParticles(prewarmDeltaTime);
            prewarmTime -= prewarmDeltaTime;
        }
        UpdateParticles(Time.deltaTime);
        DrawParticles();
    }

    protected void UpdateParticles(float deltaTime) {
        //Get camera position and orientation
        Matrix5x5 camTranspose = cc.camMatrix.transpose;
        Vector5 camPos = cc.camPosition5D;
        Vector5 camForward = cc.camMatrix.GetColumn(2);
        Vector5 camW = cc.camMatrix.GetColumn(5 - 1);
        float tanFOVY = Mathf.Tan(cc.sliceCam.fieldOfView * Mathf.Deg2Rad * 0.5f);
        float tanFOVX = tanFOVY * cc.sliceCam.aspect;

        //If following the camera, center the spawner
        if (followCameraView) {
            obj5D.localPosition5D = camPos;
        }

        //Calculate the world transform
        worldTransform = obj5D.WorldTransform5D();
        frictionMul = Mathf.Pow(2.0f, -friction * deltaTime);

        //Calculate the maximum number of particles to add this frame
        spawnRemainder += spawnRate * deltaTime;
        int numToAdd = Mathf.FloorToInt(spawnRemainder);
        int totalToAdd = numToAdd;
        spawnRemainder -= (float)numToAdd;

        //Make sure range of fades is valid
        fadeInTime = Mathf.Max(1e-6f, fadeInTime);
        fadeOutTime = Mathf.Max(1e-6f, fadeOutTime);

        //Iterate over the particle list
        for (int i = 0; i < numActive + numToAdd; ++i) {
            //If there is no more space to add particles, exit early
            if (i >= modelMatrixArr.Length) { break; }

            //Check if a particle should be added at the end of the list
            if (i >= numActive) {
                SpawnParticle(i);
                UpdateParticle(i, (totalToAdd - numToAdd) * deltaTime / totalToAdd);
                numToAdd -= 1;
                numActive += 1;
            } else if (CanKillParticle(i)) {
                //If a particle is killed with new particle incoming, just add it here
                if (numToAdd > 0) {
                    SpawnParticle(i);
                    UpdateParticle(i, (totalToAdd - numToAdd) * deltaTime / totalToAdd);
                    numToAdd -= 1;
                } else {
                    //Otherwise move a particle from the end into the slot and try again
                    numActive -= 1;
                    particles[i] = particles[numActive];
                    i -= 1;
                    continue;
                }
            } else {
                //Update the particle
                UpdateParticle(i, deltaTime);
            }
        }

        //Sort by age for rendering if applicable
        if (ageOrdering) {
            Array.Sort(particles, 0, numActive);
        }

        //Separate loop for rendering
        numVisible = 0;
        for (int i = 0; i < numActive; ++i) {
            //We have a valid particle, check it's occlusion to see if it should be visible
            bool occluded = false;
            bool occludedShadow = false;
            if (checkOcclusion) {
                Occlusion5D.CheckOcclusion(camTranspose, camPos,
                                            particles[i].mat, particles[i].pos, particles[i].scale,
                                            bCenter, bRadius,
                                            shadowDist, tanFOVX, tanFOVY,
                                            out occluded, out occludedShadow);
            }

            //Add particle to instanced arrays if it is visible
            if (!occluded || (isShadow && !occludedShadow)) {
                Matrix5x5 matrix = Transform5D.ScaleMatrix(particles[i].scale * scaleAxes);
                Vector5 pos = particles[i].pos;
                if (forceSlicePlane) {
                    pos -= Vector5.Dot(pos - camPos, camW) * camW;
                }
                if (billboard) {
                    Vector5 camDir = pos - camPos;
                    if (isShadow) {
                        //NOTE: The 2 vector alignment problem is expensive to solve, just approximate it
                        //      with the solution getting better each frame.
                        //TODO: This won't work correctly in 5D yet, needs another column assignment?
                        particles[i].mat.SetColumn(2, camDir);
                        particles[i].mat.SetColumn(5 - 1, camW);
                        particles[i].mat = Transform5D.OrthoIterate(particles[i].mat);
                        matrix = particles[i].mat * matrix;
                    } else {
                        Vector5 wUp = Vector5.zero; wUp.v = 1.0f;
                        matrix = Transform5D.FromToRotation(wUp, camDir) * matrix;
                    }
                } else {
                    matrix = particles[i].mat * matrix;
                }

#if USE_5D
                matrix.ToShaderVars(out modelMatrixArr[i], out modelMatrixC4Arr[i], out modelMatrixR4Arr[i], out modelMatrixVVArr[i]);
                modelPositionArr[i] = (Vector4)pos;
                modelPositionVArr[i] = pos.v;
#else
                modelMatrixArr[numVisible] = matrix;
                modelPositionArr[numVisible] = pos;
#endif
                modelColorArr[numVisible] = particles[i].color;
                numVisible += 1;
            }
        }
    }

    protected void DrawParticles() {
        //Check if the slice camera is enabled
        if (cam.isActiveAndEnabled) {
            //Update property blocks
            mpb.SetMatrixArray(ModelMatrixID, modelMatrixArr);
#if USE_5D
            mpb.SetVectorArray(ModelMatrixC4ID, modelMatrixC4Arr);
            mpb.SetVectorArray(ModelMatrixR4ID, modelMatrixR4Arr);
            mpb.SetFloatArray(ModelMatrixVVID, modelMatrixVVArr);
            mpb.SetFloatArray(ModelPositionVID, modelPositionVArr);
#else
            mpb.SetMatrixArray(ModelMatrixITID, modelMatrixArr);
#endif

            mpb.SetVectorArray(ModelPositionID, modelPositionArr);
            mpb.SetVectorArray(ModelColorID, modelColorArr);

            //Draw each submesh
            for (int i = 0; i < mesh.subMeshCount; ++i) {
                //Draw mesh to slice camera
                Graphics.DrawMeshInstanced(mesh, i, meshMaterials[i], modelMatrixArr, numVisible, mpb,
                    UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer, cam);
            }
        }
    }

    protected bool CanKillParticle(int ix) {
        return (particles[ix].age >= lifetime) ||
               (particles[ix].pos.y < killBelowY);
    }

    protected void SpawnParticle(int ix) {
        Color color = Color.LerpUnclamped(randColorA, randColorB, PseudoRandom.Float());
        float alpha = color.a; color.a = 0.0f;
        particles[ix] = new Particle {
            pos = SpawnPos(),
            mat = SpawnRot(),
            velocity = minVelocity + Vector5.Scale(maxVelocity - minVelocity, PseudoRandom.Uniform5D()),
            color = color,
            scale = Mathf.LerpUnclamped(minScale, maxScale, PseudoRandom.Float()),
            age = 0.0f,
            alpha = alpha,
        };
    }

    public virtual Vector5 SpawnPos() {
        return worldTransform.translation + worldTransform.matrix * PseudoRandom.Ball5D();
    }
    public virtual Matrix5x5 SpawnRot() {
        return PseudoRandom.RotationMatrix5D();
    }
    public virtual Color ColorOverTime(Particle particle) {
        Color color = particle.color;
        color.a = particle.alpha * Mathf.Clamp01(Mathf.Min(particle.age / fadeInTime, (lifetime - particle.age) / fadeOutTime));
        return color;
    }

    protected void UpdateParticle(int ix, float deltaTime) {
        Vector5 jitter = (velocityJitter > 0.0f ? PseudoRandom.Ball5D() * velocityJitter : Vector5.zero);
        Particle particle = particles[ix];
        particle.age += deltaTime;
        particle.velocity = frictionMul * (particle.velocity + (accel + jitter) * deltaTime);
        particle.pos += particle.velocity * deltaTime;
        particle.color = ColorOverTime(particle);
        particle.scale = Mathf.Max(particle.scale + scaleRate * deltaTime, 0.0f);
        particles[ix] = particle;
    }

    protected void SetMesh(Mesh newMesh) {
        Occlusion5D.ComputeBoundingSphere(newMesh, out bCenter, out bRadius);
        mesh = newMesh;
    }
}
