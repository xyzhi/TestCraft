using UnityEngine;
using System.Collections;

namespace AirStrikeKit
{
	public class EnemyDead : FlightOnDead
	{
		// giving score.
		public int ScoreAdd = 250;

		void Start ()
		{
			
		}
	
		// if Enemy on Dead
		public override void OnDead ()
		{
			if (flight.LatestHit != null) {// check if killer is exist
				// check if PlayerManager are included.
				if (flight.LatestHit.gameObject.GetComponent<PlayerManager> ()) {
					// find gameMAnager and Add score
					AirStrikeGame.gameManager.AddScore (ScoreAdd);
				}
			}
			base.OnDead ();
		}
	}
}