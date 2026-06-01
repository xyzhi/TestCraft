using UnityEngine;
using System.Collections;

namespace AirStrikeKit
{
	public class GameManager : MonoBehaviour
	{
		// basic game score
		public int Score = 0;
		public int Killed = 0;

		void Awake ()
		{
			AirStrikeGame.gameManager = this;
		}

		void Start ()
		{
			Score = 0;
			Killed = 0;
		}
	
		// Update is called once per frame
		void Update ()
		{
		
		}
		// add score function
		public void AddScore (int score)
		{
			Score += score;
			Killed += 1;
		}

		void OnGUI ()
		{
			//GUI.Label(new Rect(20,20,300,30),"Kills "+Score);
		}
		// game over fimction
		public void GameOver ()
		{
			if (AirStrikeGame.gameUI) {
				AirStrikeGame.gameUI.Mode = 1;	
			}
		}
	}

	public static class AirStrikeGame
	{
		public static GameManager gameManager;
		public static GameUI gameUI;
		public static PlayerController playerController;
	}
}