using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DLA : MonoBehaviour
{
    private struct Particle
    {
        public Vector3 position;
        public float radius;
        public float sign;
    }

    public ComputeShader csDLA;
    public float boxSize = 10f;
    public float particleSize = 2f;
    public float pullStrength = 0.1f;
    public float centerPullStrength = 0.1f;
    public float randomStrength = 0.2f;
    public float gravityPullStrength = 0.1f;
    public float windStrength = 0.0f;
    public int maxIter = 1000000;
    public float constrainSphere = 40f;
    public Material objMat;
    public Mesh objMesh;
    private float dist = 50f;
    public float Dist {get{return dist;}}
    private bool fetchedData = false;
    private int maxParticles = 256 * 512;
    const int instance_max = 1023;
    const int wanted_instances = 256 * 512;



    private ComputeBuffer particleBuffer;
    private int CS_KERNEL_ID;
    private int index = 0;
    private Particle[] particles;
    private Matrix4x4[][] transformList;
    private bool csDone = false;

    void Start()
    {
        transformList = new Matrix4x4[wanted_instances / instance_max][];
        particles = new Particle[maxParticles];

        particles[0].position = Vector3.zero;
        particles[0].radius = particleSize;
        particles[0].sign = -1;

        for (int i = 1; i < maxParticles; i++)
        {
            float radius = Random.Range(particleSize + particleSize * 0.5f, boxSize);
            float ang1 = Random.Range(0, Mathf.PI);
            float ang2 = Random.Range(0, Mathf.PI * 2);
            float x =radius * Mathf.Sin(ang1) * Mathf.Cos(ang2);
            float y = radius * Mathf.Sin(ang1) * Mathf.Sin(ang2);
            float z = radius * Mathf.Cos(ang1);

            particles[i].position = new Vector3(x, y, z);
            particles[i].radius = particleSize;
            particles[i].sign = 1;
        }
        //Create buffer for particles
        particleBuffer = new ComputeBuffer(maxParticles, 5 * sizeof(float), ComputeBufferType.Default);

        //Set the buffer data as freeParticles array;
        particleBuffer.SetData(particles);
        CS_KERNEL_ID = csDLA.FindKernel("DLASolver");
        csDLA.SetBuffer(CS_KERNEL_ID, "particleBuffer", particleBuffer);

        csDLA.SetFloat("pullStrength", pullStrength);
        csDLA.SetFloat("randomStrength", randomStrength);
        csDLA.SetFloat("centerPullStrength", centerPullStrength);
        csDLA.SetFloat("gravityPullStrength", gravityPullStrength);
        csDLA.SetFloat("constrainSphere", constrainSphere * constrainSphere);
        csDLA.SetFloat("windStrength", windStrength);

    }
    private void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }
    }

    // Update is called once per frame
    void Update()
    {
        csDLA.SetFloat("time", Time.time*0.01f);
        if (index < maxIter)
        {
            csDLA.Dispatch(CS_KERNEL_ID, 512, 1, 1);
            particleBuffer.GetData(particles);
            
            bool isDone = true;

            for (int i = 0; i < maxParticles; i++)
            {
                if (particles[i].sign > 0) isDone = false;
               
                if (isDone == false)
                {
                    break;
                }
            }
            csDone = isDone;
            if (true)
            {
                updateParticles();
                fetchedData = true;
            }
            if(!csDone) index++;
        }
        else
        {
            if (!fetchedData)
            {
                updateParticles();
                fetchedData = true;
            }
        }
        if (Time.frameCount % 100 == 0)
        {
            Debug.Log(index);
        }
        if (fetchedData)
        {
            for (int set = 0; set < wanted_instances / instance_max; set++)
            {
                int instances = instance_max;
                if (set == (wanted_instances / instance_max) - 1)
                {
                    instances = wanted_instances % instance_max;
                }
                Graphics.DrawMeshInstanced(objMesh, 0, objMat, transformList[set], instances);
                if (!Application.isEditor)
                {
                    ScreenCapture.CaptureScreenshot("D:/videos/unity/aspng/screenShots/" + Time.frameCount.ToString() + ".png");
                }
            }
            fetchedData = false;
        }
        if (Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
    }
    private void updateParticles()
    {
        int idx = 0;
        float distTT = 0;
        for (int set = 0; set < wanted_instances / instance_max; set++)
        {
            int instances = instance_max;
            if (set == (wanted_instances / instance_max) - 1)
            {
                instances = wanted_instances % instance_max;
            }
            transformList[set] = new Matrix4x4[instances];

            for (int i = 0; i < instances; i++)
            {
                Matrix4x4 matrix = new Matrix4x4();
                if (particles[idx].sign<0)
                {
                    float distT = Vector3.SqrMagnitude(particles[idx].position);
                    if (distTT < distT) distTT = distT;
                    matrix.SetTRS(particles[idx].position, Quaternion.Euler(Vector3.zero), new Vector3(particles[idx].radius, particles[idx].radius, particles[idx].radius));
                    transformList[set][i] = matrix;
                    idx++;
                }
                else
                {
                    matrix.SetTRS(particles[idx].position, Quaternion.Euler(Vector3.zero), new Vector3(particles[idx].radius/2f, particles[idx].radius/2f, particles[idx].radius/2f));
                    transformList[set][i] = matrix;
                    idx++;
                }
            }
        }
        dist = Mathf.Sqrt(distTT);
    }
}
