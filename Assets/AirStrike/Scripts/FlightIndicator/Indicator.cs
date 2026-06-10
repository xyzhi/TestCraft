/// <summary>
/// Nav mode. this script tracks cockpit cameras for HUD mode selection.
/// </summary>
using UnityEngine;

namespace AirStrikeKit
{
	public enum NavMode
	{
		Third,
		Cockpit,
		None
	}

	public class Indicator : MonoBehaviour
	{
		public float Alpha = 0.7f;
// GUI opacity.
	
		public Camera[] CockpitCamera;
// cameras list
		public int PrimaryCameraIndex;
// which camera is the cockpit camera
	
		public bool Show = true;
	
		[HideInInspector]
		public NavMode Mode = NavMode.Third;
// camera mode
		[HideInInspector]
		public FlightSystem flight;
// core plane system

		void Awake ()
		{
			if (CockpitCamera.Length <= 0) {
				if (this.transform.GetComponentsInChildren (typeof(Camera)).Length > 0) {
					var cams = this.transform.GetComponentsInChildren (typeof(Camera));
					CockpitCamera = new Camera[cams.Length];
					for (int i = 0; i < cams.Length; i++) {
						CockpitCamera [i] = cams [i].GetComponent<Camera>();
					}
				}
			}
			flight = this.GetComponent<FlightSystem> ();
		}

		void Start ()
		{

		}

		public Camera CurrentCamera;

		void Update ()
		{
			if (CurrentCamera == null) {
			
				CurrentCamera = Camera.main;
			
				if (CurrentCamera == null)
					CurrentCamera = Camera.current;
			}
			if (Camera.current != null) {
				if (CurrentCamera != Camera.current) {
					CurrentCamera = Camera.current;
				}
			}
		
			// check a current camera
			Mode = NavMode.Third;
			for (int i = 0; i < CockpitCamera.Length; i++) {
				if (CockpitCamera [i] != null) {
					if (CockpitCamera [i].GetComponent<Camera>().enabled) {
						if (i == PrimaryCameraIndex)
							Mode = NavMode.Cockpit;	
					} 
				}
			}
		}
	}
}
