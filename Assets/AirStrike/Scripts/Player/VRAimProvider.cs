using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace AirStrikeKit
{
	public class VRAimProvider : MonoBehaviour
	{
		public enum AimSource
		{
			Head,
			RightController,
			LeftController
		}

		public AimSource CurrentAimSource = AimSource.RightController;
		public Transform Head;
		public Transform LeftController;
		public Transform RightController;
		public Transform LeftControllerAim;
		public Transform RightControllerAim;
		public Camera VRCamera;
		public bool PreferRayInteractorOrigin = true;

		public bool IsXRActive ()
		{
			return XRSettings.isDeviceActive;
		}

		public Transform GetAimTransform ()
		{
			RefreshReferences ();
			switch (CurrentAimSource) {
			case AimSource.LeftController:
				if (LeftControllerAim)
					return LeftControllerAim;
				if (LeftController)
					return LeftController;
				break;
			case AimSource.RightController:
				if (RightControllerAim)
					return RightControllerAim;
				if (RightController)
					return RightController;
				break;
			}
			if (Head)
				return Head;
			return this.transform;
		}

		public Camera GetAimCamera ()
		{
			RefreshReferences ();
			if (VRCamera)
				return VRCamera;
			if (Head)
				return Head.GetComponentInChildren<Camera> ();
			return Camera.main;
		}

		public void RefreshReferences ()
		{
			if (!VRCamera) {
				VRCamera = Camera.main;
			}
			if (!VRCamera) {
				Camera[] cameras = GameObject.FindObjectsOfType<Camera> (true);
				for (int i = 0; i < cameras.Length; i++) {
					if (cameras [i].CompareTag ("MainCamera")) {
						VRCamera = cameras [i];
						break;
					}
				}
			}
			if (!Head && VRCamera) {
				Head = VRCamera.transform;
			}
			if (!LeftController) {
				LeftController = FindNodeTransform ("Left Controller", "LeftHand", "Left Hand");
			}
			if (!RightController) {
				RightController = FindNodeTransform ("Right Controller", "RightHand", "Right Hand");
			}
			if (PreferRayInteractorOrigin) {
				if (!LeftControllerAim) {
					LeftControllerAim = FindRayOriginTransform (LeftController, XRNode.LeftHand);
				}
				if (!RightControllerAim) {
					RightControllerAim = FindRayOriginTransform (RightController, XRNode.RightHand);
				}
			}
			if (!LeftControllerAim) {
				LeftControllerAim = LeftController;
			}
			if (!RightControllerAim) {
				RightControllerAim = RightController;
			}
		}

		private Transform FindNodeTransform (params string[] containsNames)
		{
			Transform[] sceneTransforms = GameObject.FindObjectsOfType<Transform> (true);
			for (int i = 0; i < sceneTransforms.Length; i++) {
				for (int n = 0; n < containsNames.Length; n++) {
					if (sceneTransforms [i].name.IndexOf (containsNames [n], System.StringComparison.OrdinalIgnoreCase) >= 0) {
						return sceneTransforms [i];
					}
				}
			}
			return null;
		}

		private Transform FindRayOriginTransform (Transform controller, XRNode node)
		{
			if (controller != null) {
				XRRayInteractor rayInteractor = controller.GetComponentInChildren<XRRayInteractor> (true);
				if (rayInteractor != null && rayInteractor.rayOriginTransform != null) {
					return rayInteractor.rayOriginTransform;
				}

				Transform namedRay = FindChildTransform (controller, "Ray Interactor");
				if (namedRay != null) {
					return namedRay;
				}
			}

			XRRayInteractor[] rayInteractors = GameObject.FindObjectsOfType<XRRayInteractor> (true);
			for (int i = 0; i < rayInteractors.Length; i++) {
				Transform t = rayInteractors [i].transform;
				string hierarchyName = t.name;
				if (t.parent != null) {
					hierarchyName = t.parent.name + "/" + hierarchyName;
				}
				if (node == XRNode.LeftHand && hierarchyName.IndexOf ("Left", System.StringComparison.OrdinalIgnoreCase) >= 0) {
					return rayInteractors [i].rayOriginTransform != null ? rayInteractors [i].rayOriginTransform : t;
				}
				if (node == XRNode.RightHand && hierarchyName.IndexOf ("Right", System.StringComparison.OrdinalIgnoreCase) >= 0) {
					return rayInteractors [i].rayOriginTransform != null ? rayInteractors [i].rayOriginTransform : t;
				}
			}

			return controller;
		}

		private Transform FindChildTransform (Transform root, string containsName)
		{
			Transform[] children = root.GetComponentsInChildren<Transform> (true);
			for (int i = 0; i < children.Length; i++) {
				if (children [i].name.IndexOf (containsName, System.StringComparison.OrdinalIgnoreCase) >= 0) {
					return children [i];
				}
			}
			return null;
		}
	}
}
