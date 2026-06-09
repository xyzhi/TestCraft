using UnityEngine;
using System.Collections;

namespace HWRWeaponSystem
{
	public class WeaponController : MonoBehaviour
	{
		// 当前武器可锁定/攻击的目标标签列表。
		public string[] TargetTag = new string[1]{ "Enemy" };
		// 该载具下挂载的所有武器发射器。
		public WeaponLauncher[] WeaponLists;
		// 当前激活武器在 WeaponLists 中的索引。
		public int CurrentWeapon = 0;
		public GameObject LockedTarget;
		// 是否让武器显示准星。
		public bool ShowCrosshair;
		// 切换武器时是否隐藏未使用武器模型。
		public bool HideNoUse = false;

		// 启动时自动收集子层级里的所有 WeaponLauncher。
		void Awake ()
		{
			// find all attached weapons.
			if (this.transform.GetComponentsInChildren (typeof(WeaponLauncher)).Length > 0) {
				var weas = this.transform.GetComponentsInChildren (typeof(WeaponLauncher));
				WeaponLists = new WeaponLauncher[weas.Length];
				for (int i = 0; i < weas.Length; i++) {
					WeaponLists [i] = weas [i].GetComponent<WeaponLauncher> ();
					WeaponLists [i].TargetTag = TargetTag;
				}
			}
		}

		// 获取当前选中的武器发射器。
		public WeaponLauncher GetCurrentWeapon ()
		{
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [CurrentWeapon] != null) {
				return WeaponLists [CurrentWeapon];
			}
			return null;
		}

		public void SetLockedTarget (GameObject target)
		{
			LockedTarget = target;
		}

		public void ClearLockedTarget (GameObject target = null)
		{
			if (target == null || LockedTarget == target) {
				LockedTarget = null;
			}
		}

		// 初始化所有武器的目标标签和准星显示状态。
		private void Start ()
		{
			for (int i = 0; i < WeaponLists.Length; i++) {
				if (WeaponLists [i] != null) {
					WeaponLists [i].TargetTag = TargetTag;
					WeaponLists [i].ShowCrosshair = ShowCrosshair;
				}
			}
		}

		// 每帧同步武器激活状态，保证只有当前武器处于激活。
		private void Update ()
		{
		
			for (int i = 0; i < WeaponLists.Length; i++) {
				if (WeaponLists [i] != null) {
					WeaponLists [i].OnActive = false;
				}
			}
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [CurrentWeapon] != null) {
				WeaponLists [CurrentWeapon].OnActive = true;
			}
	
		}

		// 指定索引并立即发射对应武器。
		public void LaunchWeapon (int index)
		{
			CurrentWeapon = index;
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [index] != null) {
				WeaponLists [index].Shoot ();
			}
		}

		// 循环切换到下一把武器，并更新显示/激活状态。
		public void SwitchWeapon ()
		{
			CurrentWeapon += 1;
			if (CurrentWeapon >= WeaponLists.Length) {
				CurrentWeapon = 0;	
			}
		
			for (int i = 0; i < WeaponLists.Length; i++) {
				if (CurrentWeapon == i) {
					WeaponLists [i].OnActive = true;
					if (HideNoUse)
						HideWeapon (WeaponLists [i].gameObject, true);
				} else {
					if (HideNoUse)
						HideWeapon (WeaponLists [i].gameObject, false);
					WeaponLists [i].OnActive = false;
				}
			}
		}

		// 统一控制武器模型显示隐藏。
		public void HideWeapon (GameObject weapon, bool show)
		{
			foreach (Renderer render in weapon.GetComponentsInChildren<Renderer>()) {
				render.enabled = show;
			}
		}

		// 发射当前选中的武器。
		public void LaunchWeapon ()
		{
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [CurrentWeapon] != null) {
				WeaponLists [CurrentWeapon].Shoot ();
			}
		}
	
	}
}
