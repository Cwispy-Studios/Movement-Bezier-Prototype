using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[ExecuteInEditMode]

public class ShadowCaster : MonoBehaviour
{

	[SerializeField] private bool use3DLighting;
	
	// Use this for initialization
	void Start () {
		if (use3DLighting) 
			GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
		GetComponent<Renderer>().receiveShadows = use3DLighting;
	}
}
