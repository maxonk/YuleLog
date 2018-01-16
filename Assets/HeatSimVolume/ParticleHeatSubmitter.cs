using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleHeatSubmitter : MonoBehaviour {

    [SerializeField] ParticleSystem particleSystem;

    ParticleSystem.Particle[] particles;
    Vector4[] heatPoints;
    
	void Update () {
        if (particles == null) particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        if (heatPoints == null) heatPoints = new Vector4[particles.Length];

        int numParticles = particleSystem.GetParticles(particles);
        int i = 0;
        //var v
        for(;  i < numParticles; i++)
        {
          //  v = transform.TransformDirection[particles[i].position];
            heatPoints[i] = new Vector4(particles[i].position.x, particles[i].position.y, particles[i].position.z, particles[i].GetCurrentColor(particleSystem).r);
           // heatPoints[i] = transform.TransformPoint[heatPoints[i]];
        }
        for(; i < heatPoints.Length; i++)
        {
            heatPoints[i] = Vector4.zero;
        }

        HeatVis.SubmitHeatPoints(heatPoints);
    }
}
