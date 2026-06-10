using UnityEngine;
using System.Collections;

namespace HWRWeaponSystem
{
	public class WeaponController : MonoBehaviour
	{
		// 当前武器允许搜索和锁定的目标标签列表。
		public string[] TargetTag = new string[1]{ "Enemy" };
		// 当前载具或角色身上挂载的全部武器发射器。
		public WeaponLauncher[] WeaponLists;
		// 当前激活武器在 WeaponLists 中的索引。
		public int CurrentWeapon = 0;
		// 已经完成锁定的目标。
		public GameObject LockedTarget;
		// 正在累计锁定进度的候选目标。
		public GameObject LockCandidate;
		// 当前候选目标的锁定进度，范围为 0 到 1。
		public float LockProgress;
		private float lockCandidateStartTime;
		// 是否让当前武器显示准星。
		public bool ShowCrosshair;
		// 切换武器时是否隐藏未使用的武器模型。
		public bool HideNoUse = false;

		// 启动时自动收集当前对象子层级下的全部 WeaponLauncher。
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

		// 直接设置当前已锁定目标，并把锁定进度置为完成。
		public void SetLockedTarget (GameObject target)
		{
			LockedTarget = target;
			LockCandidate = target;
			LockProgress = target != null ? 1 : 0;
			lockCandidateStartTime = Time.time;
		}

		// 清理已锁定目标；如果传入目标，则只在目标匹配时清理。
		public void ClearLockedTarget (GameObject target = null)
		{
			if (target == null || LockedTarget == target) {
				LockedTarget = null;
			}
			if (target == null || LockCandidate == target) {
				ClearLockCandidate ();
			}
		}

		// 刷新候选锁定目标，并按给定锁定时长累计进度。
		public void UpdateLockCandidate (GameObject candidate, float timeToLock)
		{
			if (candidate == null) {
				ClearLockCandidate ();
				return;
			}

			if (LockedTarget == candidate) {
				LockCandidate = candidate;
				LockProgress = 1;
				lockCandidateStartTime = Time.time;
				return;
			}

			if (LockCandidate != candidate) {
				LockCandidate = candidate;
				LockProgress = 0;
				lockCandidateStartTime = Time.time;
				return;
			}

			if (timeToLock <= 0) {
				LockProgress = 1;
				SetLockedTarget (candidate);
				return;
			}

			LockProgress = Mathf.Clamp01 ((Time.time - lockCandidateStartTime) / timeToLock);
			if (LockProgress >= 1) {
				SetLockedTarget (candidate);
			}
		}

		// 清空候选锁定目标和当前锁定进度。
		public void ClearLockCandidate ()
		{
			LockCandidate = null;
			LockProgress = 0;
			lockCandidateStartTime = 0;
		}

		// 初始化全部武器的目标标签和准星显示状态。
		private void Start ()
		{
			for (int i = 0; i < WeaponLists.Length; i++) {
				if (WeaponLists [i] != null) {
					WeaponLists [i].TargetTag = TargetTag;
					WeaponLists [i].ShowCrosshair = ShowCrosshair;
				}
			}
		}

		// 每帧同步武器激活状态，保证只有当前武器处于激活状态。
		private void Update ()
		{
			for (int i = 0; i < WeaponLists.Length; i++) {
				if (WeaponLists [i] != null) {
					WeaponLists [i].OnActive = false;
				}
			}
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [CurrentWeapon] != null) {
				WeaponLists [CurrentWeapon].OnActive = true;
				if (!WeaponLists [CurrentWeapon].Seeker) {
					ClearLockCandidate ();
				}
			} else {
				ClearLockCandidate ();
			}
		}

		// 切换到指定索引的武器，并立即发射一次。
		public void LaunchWeapon (int index)
		{
			CurrentWeapon = index;
			if (CurrentWeapon < WeaponLists.Length && WeaponLists [index] != null) {
				WeaponLists [index].Shoot ();
			}
		}

		// 循环切换到下一把武器，并同步激活显示状态。
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

		// 统一控制整把武器模型的显示和隐藏。
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
