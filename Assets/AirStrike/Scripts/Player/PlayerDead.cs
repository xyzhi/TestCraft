using UnityEngine;
using System.Collections;

namespace AirStrikeKit
{
	public class PlayerDead : FlightOnDead
	{
		void Start ()
		{
		}
	
		// if player dead
		public override void OnDead ()
		{
			// if player dead call GameOver in GameManager
			AirStrikeGame.gameManager.GameOver ();

			base.OnDead ();
		}
	}
}