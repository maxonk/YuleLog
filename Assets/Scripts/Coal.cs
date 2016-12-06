using UnityEngine;
using System.Collections;

public class Coal : MonoBehaviour, HeatSource {
    
	public float getHeat(Vector3 pos) {
        return 0.2f;
    } 
}
