using UnityEngine;

namespace AirStrikeKit
{
	public class BoostJetParticles : MonoBehaviour
	{
		public FlightSystem FlightSystem;
		public ParticleSystem[] JetParticles;
		public float BoostEmissionMultiplier = 1.8f;
		public float BoostStartSizeMultiplier = 1.25f;
		public float BoostStartSpeedMultiplier = 1.35f;

		private float[] baseEmissionRates;
		private float[] baseStartSizes;
		private float[] baseStartSpeeds;
		private bool initialized;

		void Awake ()
		{
			ResolveReferences ();
			CacheBaseValues ();
			ApplyState (false);
		}

		void OnEnable ()
		{
			ResolveReferences ();
			CacheBaseValues ();
		}

		void LateUpdate ()
		{
			ResolveReferences ();
			CacheBaseValues ();
			ApplyState (FlightSystem != null && FlightSystem.IsBoosting);
		}

		void OnDisable ()
		{
			ApplyState (false);
		}

		void ResolveReferences ()
		{
			if (FlightSystem == null) {
				FlightSystem = GetComponentInParent<FlightSystem> ();
			}
		}

		void CacheBaseValues ()
		{
			if (initialized || JetParticles == null || JetParticles.Length == 0) {
				return;
			}

			baseEmissionRates = new float[JetParticles.Length];
			baseStartSizes = new float[JetParticles.Length];
			baseStartSpeeds = new float[JetParticles.Length];

			for (int i = 0; i < JetParticles.Length; i++) {
				ParticleSystem particle = JetParticles[i];
				if (particle == null) {
					continue;
				}

				var emission = particle.emission;
				var main = particle.main;
				baseEmissionRates[i] = emission.rateOverTimeMultiplier;
				baseStartSizes[i] = main.startSizeMultiplier;
				baseStartSpeeds[i] = main.startSpeedMultiplier;
			}

			initialized = true;
		}

		void ApplyState (bool isBoosting)
		{
			if (!initialized || JetParticles == null) {
				return;
			}

			for (int i = 0; i < JetParticles.Length; i++) {
				ParticleSystem particle = JetParticles[i];
				if (particle == null) {
					continue;
				}

				var emission = particle.emission;
				var main = particle.main;
				float multiplier = isBoosting ? BoostEmissionMultiplier : 1f;
				emission.rateOverTimeMultiplier = baseEmissionRates[i] * multiplier;
				main.startSizeMultiplier = baseStartSizes[i] * (isBoosting ? BoostStartSizeMultiplier : 1f);
				main.startSpeedMultiplier = baseStartSpeeds[i] * (isBoosting ? BoostStartSpeedMultiplier : 1f);
			}
		}
	}
}
