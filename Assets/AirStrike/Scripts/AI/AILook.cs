/// <summary>
/// AI look. this script using as Terrent AI
/// </summary> 
using UnityEngine;
using System.Collections;
using HWRWeaponSystem;

namespace AirStrikeKit
{
	// add all necessary components.
	[RequireComponent (typeof(WeaponController))]

	public class AILook : MonoBehaviour
	{
		public string[] TargetTag = new string[1]{ "Enemy" };
		// this AI will only shooting an objects are as same tag as within TargetTag[].
		private int indexWeapon;
		private GameObject target;
		private WeaponController weapon;
		private float timeAIattack;
		private float delay;
		public float RandomDelay = 10;

		void Start ()
		{
			delay = Random.Range (0, RandomDelay);
			//define a weapon controller.
			weapon = (WeaponController)this.GetComponent<WeaponController> ();
		}

	
		void Update ()
		{
			if (Time.time < delay) {
				return;
			}
			// if target is exist.
			if (target) {
				// rotation facing to the target.
				Quaternion targetlook = Quaternion.LookRotation (target.transform.position - this.transform.position);
				this.transform.rotation = Quaternion.Lerp (this.transform.rotation, targetlook, Time.deltaTime * 3);
				// find a direction from the target
				Vector3 dir = (target.transform.position - transform.position).normalized;
				float direction = Vector3.Dot (dir, transform.forward);
				// check if in front direction
				if (direction > 0.9f) {
					if (weapon) {
						// shooting!!.
						weapon.LaunchWeapon (indexWeapon);
					}
				}
				// AI attack the target for a while (3 sec)
				if (Time.time > timeAIattack + 3) {
					target = null;	
					// AI forget this target and try to looking new target
				}
			} else {
				for (int t = 0; t < TargetTag.Length; t++) {
					// AI find target only in TargetTag list

					if (AirStrike.AIPool == null) {
						Debug.LogError ("Need AIManager placed in this scene, please go to AirStrike/Prefabs/Game/AIManager and placing AIManager prefab to the scene");
						return;
					}
					TargetCollector targetCollector = AirStrike.AIPool.FindTargetTag (TargetTag [t]);
				
					if (targetCollector != null && targetCollector.Targets.Length > 0) {
						
						// find all objects in Tags list.
						float distance = int.MaxValue;
						for (int i = 0; i < targetCollector.Targets.Length; i++) {
							if (targetCollector.Targets [i] != null) {
								// find the distance from the target.
								float dis = Vector3.Distance (targetCollector.Targets [i].transform.position, transform.position);
								// check if in ranged.
								if (distance > dis) {
									// Select closer target
									distance = dis;
									target = targetCollector.Targets [i];
									if (weapon) {
										// random weapons
										indexWeapon = Random.Range (0, weapon.WeaponLists.Length);
									}
									timeAIattack = Time.time;
								}
							}
						}
					}
				}	
			}
		}
	}
}