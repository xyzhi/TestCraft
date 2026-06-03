using UnityEngine;

namespace AirStrikeKit
{
	public class VRHeadPoseConstraint : MonoBehaviour
	{
		public bool LockPosition = true;
		public bool LockX = true;
		public bool LockY = true;
		public bool LockZ = true;
		public Vector3 LockedLocalPosition = Vector3.zero;

		private void LateUpdate ()
		{
			if (!LockPosition)
				return;

			Vector3 localPosition = transform.localPosition;
			if (LockX) {
				localPosition.x = LockedLocalPosition.x;
			}
			if (LockY) {
				localPosition.y = LockedLocalPosition.y;
			}
			if (LockZ) {
				localPosition.z = LockedLocalPosition.z;
			}
			transform.localPosition = localPosition;
		}
	}
}
