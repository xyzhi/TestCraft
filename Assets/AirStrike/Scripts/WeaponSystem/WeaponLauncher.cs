using System;
using UnityEngine;
using Random = UnityEngine.Random;
using AirStrikeKit;

namespace HWRWeaponSystem
{
	[RequireComponent (typeof(AudioSource))]
	public class WeaponLauncher : WeaponBase
	{
		// 当前发射器是否处于激活状态，只有激活武器才会执行瞄准、HUD 和开火逻辑。
		// 当前发射器是否处于激活状态，只有激活武器才会执行瞄准、HUD 和开火逻辑。
		public bool OnActive;
		[Header ("Aiming")]
		// 是否启用锁敌导引逻辑，通常用于导弹类武器。
		// 是否启用锁敌逻辑，通常用于导弹或跟踪类武器。
		public bool Seeker;
		// 是否按照屏幕中心/鼠标位置做屏幕射线瞄准。
		// 是否按屏幕中心或鼠标位置做屏幕射线瞄准。
		public bool OnScreenAiming;
		// 可锁定目标与武器前向的最小夹角点积，越大越严格。
		// 可锁定目标与武器前向的最小点积阈值，越大越接近正前方。
		public float AimDirection = 0.8f;
		// 最大瞄准射线距离。
		// 射线瞄准的最大距离。
		public int MaxAimRange = 10000;
		// 是否让准星吸附到射线命中的真实物体表面。
		// 是否让准星吸附到射线命中的真实表面。
		public bool SnapCrosshair = true;

		[Header ("Projectile")]
		// 导弹/子弹发射口列表，可用于左右挂点轮流出弹。
		// 导弹或子弹的发射口列表，可用于多挂点轮流出弹。
		public Transform[] MissileOuter;
		// 实际生成的弹体预制体。
		// 实际生成的弹体预制体。
		public GameObject Missile;
		// 射速间隔。
		// 两次开火之间的最小间隔。
		public float FireRate = 0.1f;
		// 散布范围，用于模拟机炮散射。
		// 散布范围，用于模拟机炮散射。
		public float Spread = 1;
		// 刚体弹体发射力度。
		// 刚体弹体发射时施加的力度。
		public float ForceShoot = 8000;
		// 一次开火生成的弹丸数量。
		// 单次开火生成的弹丸数量。
		public int NumBullet = 1;
		// 当前弹药数。
		// 当前弹药数量。
		public int Ammo = 10;
		// 单次装填后的最大弹药数。
		// 单次装填完成后的最大弹药数量。
		public int AmmoMax = 10;
		// 是否无限弹药。
		// 是否拥有无限弹药。
		public bool InfinityAmmo = false;
		// 换弹耗时。
		// 换弹耗时。
		public float ReloadTime = 1;

		// 最大锁定距离。
		// 可锁定目标的最大距离。
		public float DistanceLock = 200;
		// 达成锁定所需时间。
		// 从开始跟踪到完成锁定所需的时间。
		public float TimeToLock = 2;

		[Header ("Crosshair")]
		// 是否显示当前武器的准星。
		public bool ShowCrosshair = true;
		// 普通屏幕准星和 VR 世界准星共用的贴图。
		public Texture2D CrosshairTexture;
		// 可选的准星对象预制体。
		public GameObject CrosshairObject;
		// VR 模式下是否显示世界空间准星。
		bool ShowVRWorldCrosshair = true;
		// 当没有真实命中点时，VR 准星默认放在相机前方的距离。
		float VRCrosshairFallbackDistance = 2f;
		// VR 准星贴到命中面时向相机方向前推，避免和表面闪烁。
		float VRCrosshairSurfaceOffset = 0.05f;
		// VR 准星随距离缩放的系数。
		float VRCrosshairScalePerMeter = 0.07f;
		// VR 准星最小缩放。
		float VRMinCrosshairScale = 0.05f;
		// VR 准星最大缩放。
		float VRMaxCrosshairScale = 0.5f;

		[Header ("Other FX")]
		// 弹壳预制体。
		// 抛壳特效预制体。
		public GameObject Shell;
		// 弹壳存在时间。
		// 抛壳存在时间。
		public float ShellLifeTime = 4;
		// 弹壳抛出位置列表。
		// 抛壳弹出的挂点列表。
		public Transform[] ShellOuter;
		// 弹壳推出力度。
		// 抛壳推出力度。
		public int ShellOutForce = 300;
		// 枪口火焰预制体。
		// 枪口火焰预制体。
		public GameObject Muzzle;
		// 枪口火焰存在时间。
		// 枪口火焰存在时间。
		public float MuzzleLifeTime = 2;
		// 开火时镜头震动强度。
		// 开火时对镜头或武器施加的震动强度。
		public Vector3 ShakeForce = Vector3.up;

		[Header ("Sound FX")]
		// 开火音效列表，支持随机播放。
		// 开火音效列表，支持随机播放。
		public AudioClip[] SoundGun;
		// 开始装填时音效。
		// 开始换弹时播放的音效。
		public AudioClip SoundReloading;
		// 装填完成时音效。
		// 换弹完成时播放的音效。
		public AudioClip SoundReloaded;

		// 锁定计时起点，用于计算持续锁定时间。
		private float timetolockcount = 0;
		// 下一次允许开火的时间点。
		private float nextFireTime = 0;
		// 当前已经锁定的目标。
		private GameObject target;
		private WeaponController weaponController;
		// 开火后作用在武器模型上的后坐旋转缓存。
		private Vector3 torqueTemp;
		// 本轮装填开始的时间。
		private float reloadTimeTemp;
		// 发射器使用的音频组件。
		private AudioSource audioSource;
		private GameObject crosshair;
		private GameObject vrCrosshair;
		private Material vrCrosshairMaterial;
		[HideInInspector]
		// 当前是否正在装填。
		// 当前是否正处于换弹中。
		public bool Reloading;
		[HideInInspector]
		// 当前装填进度，供外部 UI 读取。
		// 当前换弹进度，供外部 UI 读取。
		public float ReloadingProcess;
		// 武器在 UI 中显示的图标。
		// 武器在 UI 中显示的图标。
		public Texture2D Icon;

		// 初始化武器归属、音频组件和准星对象。
		private void Start ()
		{
			if (!Owner)
				Owner = this.transform.root.gameObject;

			weaponController = GetComponentInParent<WeaponController> ();

			if (!audioSource) {
				audioSource = this.GetComponent<AudioSource> ();
				if (!audioSource) {
					this.gameObject.AddComponent<AudioSource> ();	
				}
			}
			if (CrosshairObject) {
				crosshair = (GameObject)GameObject.Instantiate (CrosshairObject.gameObject, this.transform.position, CrosshairObject.transform.rotation);
			}
			if (WeaponSystem.Finder == null)
				Debug.LogWarning ("Need Weapon System object in the scene, you have to place it from WeaponSystem/WeaponSystem.prefab");
		}

		[HideInInspector]
		// 当前瞄准命中的世界坐标。
		// 当前瞄准命中的世界坐标。
		public Vector3 AimPoint;
		//[HideInInspector]
		// 当前瞄准命中的物体。
		public GameObject AimObject;
		[HideInInspector]
		// 当前瞄准点是否来自真实命中，用于 VR 准星决定贴表面还是贴相机前方。
		public bool HasValidAimPoint;
		// 可选的外部瞄准源，VR 模式下一般由头显或手柄提供。
		public Transform AimOverride;
		// 可选的外部瞄准相机，用于替代主相机进行屏幕射线。
		// 外部瞄准相机，用于替代主相机执行屏幕射线。
		public Camera AimCameraOverride;

		// 根据当前瞄准模式计算命中点和命中物体。
		private void rayAiming ()
		{
			RaycastHit hit;
			if (AimOverride != null) {
				if (Physics.Raycast (AimOverride.position, AimOverride.forward, out hit, MaxAimRange) && SnapCrosshair) {
					if (Missile != null && hit.collider.tag != Missile.tag) {
						AimPoint = hit.point;
						AimObject = hit.collider.gameObject;
						HasValidAimPoint = true;
					}
				} else {
					AimPoint = AimOverride.position + (AimOverride.forward * MaxAimRange);
					AimObject = null;
					HasValidAimPoint = false;
				}
				return;
			}else if (OnScreenAiming) {
				if (CurrentCamera) {
					var ray = CurrentCamera.ScreenPointToRay (Input.mousePosition);
					if (Physics.Raycast (ray, out hit, MaxAimRange) && SnapCrosshair) {
						if (Missile != null && hit.collider.tag != Missile.tag) {
							AimPoint = hit.point;
							AimObject = hit.collider.gameObject;
							HasValidAimPoint = true;
						}
					} else {
						AimPoint = ray.origin + (ray.direction * MaxAimRange);
						AimObject = null;
						HasValidAimPoint = false;
					}
				}
			} else {

				if (Physics.Raycast (transform.position, this.transform.forward, out hit, MaxAimRange) && SnapCrosshair) {
					if (Missile != null && hit.collider.tag != Missile.tag) {
						AimPoint = hit.point;
						AimObject = hit.collider.gameObject;
						HasValidAimPoint = true;
					}
				} else {
					AimPoint = this.transform.position + (this.transform.forward * MaxAimRange);
					AimObject = null;
					HasValidAimPoint = false;
				}
			}

		}

		private GameObject FindBestLockCandidate ()
		{
			GameObject bestCandidate = null;
			float bestDistance = float.MaxValue;

			for (int t = 0; t < TargetTag.Length; t++) {
				if (AimObject != null && AimObject.tag == TargetTag [t]) {
					float aimDistance = Vector3.Distance (AimObject.transform.position, transform.position);
					if (aimDistance < DistanceLock && aimDistance < bestDistance) {
						bestDistance = aimDistance;
						bestCandidate = AimObject;
					}
				}

				TargetCollector collector = WeaponSystem.Finder.FindTargetTag (TargetTag [t]);
				if (collector == null) {
					continue;
				}

				GameObject[] objs = collector.Targets;
				for (int i = 0; i < objs.Length; i++) {
					if (objs [i] == null || objs [i] == AimObject) {
						continue;
					}

					Vector3 dir = (objs [i].transform.position - transform.position).normalized;
					float direction = Vector3.Dot (dir, transform.forward);
					if (direction < AimDirection) {
						continue;
					}

					float distance = Vector3.Distance (objs [i].transform.position, transform.position);
					if (distance < DistanceLock && distance < bestDistance) {
						bestDistance = distance;
						bestCandidate = objs [i];
					}
				}
			}

			return bestCandidate;
		}

		// 激活状态下持续刷新瞄准射线，保证物理命中点稳定。
		void FixedUpdate ()
		{
			if (OnActive) {
				rayAiming ();
			}
		}

		// 处理锁敌、装填、相机引用和 VR 准星刷新。
		private void Update()
		{
			SyncLockedTargetReference ();
			CurrentCamera = AimCameraOverride != null ? AimCameraOverride : Camera.main;
			if (CurrentCamera == null)
			{
				CurrentCamera = Camera.current;
			}
			if (OnActive)
			{


				if (TorqueObject)
				{
					TorqueObject.transform.Rotate(torqueTemp * Time.deltaTime);
					torqueTemp = Vector3.Lerp(torqueTemp, Vector3.zero, Time.deltaTime);
				}
				if (Seeker)
				{
					GameObject candidate = FindBestLockCandidate ();
					if (weaponController != null) {
						weaponController.UpdateLockCandidate (candidate, TimeToLock);
					}
				}
				if (Reloading)
				{
					ReloadingProcess = ((1 / ReloadTime) * (reloadTimeTemp + ReloadTime - Time.time));
					if (Time.time >= reloadTimeTemp + ReloadTime)
					{
						Reloading = false;
						if (SoundReloaded)
						{
							if (audioSource)
							{
								audioSource.PlayOneShot(SoundReloaded);
							}
						}
						Ammo = AmmoMax;
					}
				}
				else
				{
					if (Ammo <= 0)
					{
						Reloading = true;
						reloadTimeTemp = Time.time;

						if (SoundReloading)
						{
							if (audioSource)
							{
								audioSource.PlayOneShot(SoundReloading);
							}
						}
					}
				}
			}

			PlayerController playerController = AirStrikeGame.playerController;
			if (playerController != null && playerController.IsVRActive && ShowCrosshair) {
				UpdateVRWorldCrosshair ();
			}
		}

		// 当前用于 HUD 与屏幕瞄准的相机引用。
		// 当前武器使用的瞄准相机。
		public Camera CurrentCamera;

		private Vector3 crosshairPos;

		private void DrawCrosshair ()
		{
			if (!ShowCrosshair)
				return;

			if (CurrentCamera) {
				Vector3 screenPosAim = CurrentCamera.WorldToScreenPoint (AimPoint);
				crosshairPos += ((screenPosAim - crosshairPos) / 5);
				if (CrosshairTexture) {
					GUI.DrawTexture (new Rect (crosshairPos.x - CrosshairTexture.width / 2, Screen.height - crosshairPos.y - CrosshairTexture.height / 2, CrosshairTexture.width, CrosshairTexture.height), CrosshairTexture);
				}
			}
		}

		private void UpdateVRWorldCrosshair ()
		{
			bool shouldShow = ShouldShowVRWorldCrosshair ();
			if (!shouldShow) {
				SetVRWorldCrosshairActive (false);
				return;
			}

			EnsureVRWorldCrosshair ();
			if (vrCrosshair == null || CurrentCamera == null) {
				return;
			}

			Vector3 cameraPosition = CurrentCamera.transform.position;
			Vector3 targetPosition = AimPoint;
			Vector3 direction = targetPosition - cameraPosition;
			float distance = direction.magnitude;
			if (!HasValidAimPoint || distance <= 0.01f) {
				direction = CurrentCamera.transform.forward;
				distance = VRCrosshairFallbackDistance;
				targetPosition = cameraPosition + (direction * distance);
			} else {
				direction /= distance;
				targetPosition += direction * VRCrosshairSurfaceOffset;
			}

			vrCrosshair.transform.position = targetPosition;
			vrCrosshair.transform.forward = (targetPosition - cameraPosition).normalized;

			float scale = Mathf.Clamp (distance * VRCrosshairScalePerMeter, VRMinCrosshairScale, VRMaxCrosshairScale);
			vrCrosshair.transform.localScale = new Vector3 (scale, scale, scale);

			if (crosshair != null) {
				crosshair.transform.position = targetPosition;
				crosshair.transform.forward = (targetPosition - cameraPosition).normalized;
			}

			SetVRWorldCrosshairActive (true);
		}

		private bool ShouldShowVRWorldCrosshair ()
		{
			if (!ShowVRWorldCrosshair || !OnActive || CurrentCamera == null) {
				return false;
			}

			if (CrosshairTexture == null && crosshair == null && CrosshairObject == null) {
				return false;
			}

			return true;
		}

		private void EnsureVRWorldCrosshair ()
		{
			if (vrCrosshair != null || CrosshairTexture == null) {
				return;
			}

			Shader shader = Shader.Find ("Unlit/Transparent");
			if (shader == null) {
				shader = Shader.Find ("Sprites/Default");
			}
			if (shader == null) {
				return;
			}

			vrCrosshair = GameObject.CreatePrimitive (PrimitiveType.Quad);
			vrCrosshair.name = this.name + "_VRWorldCrosshair";
			vrCrosshair.transform.SetParent (null, false);
			vrCrosshair.layer = LayerMask.NameToLayer ("Ignore Raycast");

			Collider quadCollider = vrCrosshair.GetComponent<Collider> ();
			if (quadCollider != null) {
				Destroy (quadCollider);
			}

			vrCrosshairMaterial = new Material (shader);
			vrCrosshairMaterial.mainTexture = CrosshairTexture;
			vrCrosshairMaterial.color = Color.white;

			Renderer renderer = vrCrosshair.GetComponent<Renderer> ();
			if (renderer != null) {
				renderer.sharedMaterial = vrCrosshairMaterial;
				renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				renderer.receiveShadows = false;
			}

			vrCrosshair.SetActive (false);
		}

		private void SetVRWorldCrosshairActive (bool active)
		{
			if (vrCrosshair != null && vrCrosshair.activeSelf != active) {
				vrCrosshair.SetActive (active);
			}
			if (crosshair != null && crosshair.activeSelf != active) {
				crosshair.SetActive (active);
			}
		}

		private void OnGUI ()
		{
			PlayerController playerController = AirStrikeGame.playerController;
			if (playerController != null && playerController.IsVRActive) {
				return;
			}

			if (OnActive) {
				DrawCrosshair ();
			}
		}

		// 清空当前锁定目标，并重置锁定计时。
		private void Unlock ()
		{
			timetolockcount = Time.time;
			SetLockedTarget (null);
		}

		private void SetLockedTarget (GameObject lockedTarget)
		{
			target = lockedTarget;
			if (weaponController != null) {
				if (lockedTarget == null) {
					weaponController.ClearLockedTarget ();
				} else {
					weaponController.SetLockedTarget (lockedTarget);
				}
			}
		}

		private void SyncLockedTargetReference ()
		{
			if (weaponController == null) {
				weaponController = GetComponentInParent<WeaponController> ();
			}

			if (weaponController != null) {
				target = weaponController.LockedTarget;
			}
		}

		// 当前轮换使用的发射口索引。
		private int currentOuter = 0;

		// 执行一次实际开火，包含出弹、后坐、枪口火焰、弹壳和音效。
		// 执行一次开火流程，并生成弹体、特效与音效。
		public void Shoot ()
		{
			if (InfinityAmmo) {
				Ammo = 1;	
			}
			if (Ammo > 0) {
				if (Time.time > nextFireTime + FireRate) {
					CameraEffects.Shake (ShakeForce, this.transform.position);
					nextFireTime = Time.time;
					torqueTemp = TorqueSpeedAxis;
					Ammo -= 1;
					Vector3 missileposition = this.transform.position;
					Quaternion missilerotate = this.transform.rotation;
					if (MissileOuter.Length > 0) {
						missilerotate = MissileOuter [currentOuter].rotation;	
						missileposition = MissileOuter [currentOuter].position;	
					}

					if (MissileOuter.Length > 0) {
						currentOuter += 1;
						if (currentOuter >= MissileOuter.Length)
							currentOuter = 0;
					}

					if (Muzzle) {
						GameObject muzzle;
						if (WeaponSystem.Pool != null) {	
							muzzle = WeaponSystem.Pool.Instantiate (Muzzle, missileposition, missilerotate, MuzzleLifeTime);
						} else {
							muzzle = (GameObject)GameObject.Instantiate (Muzzle, missileposition, missilerotate);
							GameObject.Destroy (muzzle, MuzzleLifeTime);
						}

						muzzle.transform.parent = this.transform;
						if (MissileOuter.Length > 0) {
							muzzle.transform.parent = MissileOuter [currentOuter].transform;
						}
					}

					for (int i = 0; i < NumBullet; i++) {
						if (Missile) {
							Vector3 spread = new Vector3 (Random.Range (-Spread, Spread), Random.Range (-Spread, Spread), Random.Range (-Spread, Spread)) / 100;
							Vector3 direction = this.transform.forward + spread;
							missilerotate = Quaternion.LookRotation (direction);

							GameObject bullet;
							if (WeaponSystem.Pool != null) {
								bullet = WeaponSystem.Pool.Instantiate (Missile, missileposition, missilerotate);
							} else {
								bullet = (GameObject)GameObject.Instantiate (Missile, missileposition, missilerotate);
							}
							if (bullet) {
								DamageBase damangeBase = bullet.GetComponent<DamageBase> ();
								if (damangeBase) {
									damangeBase.Owner = Owner;
									damangeBase.TargetTag = TargetTag;
									damangeBase.IgnoreTag = IgnoreTag;
									damangeBase.IgnoreSelf (this.gameObject);
								}
								WeaponBase weaponBase = bullet.GetComponent<WeaponBase> ();
								if (weaponBase) {
									weaponBase.Owner = Owner;
									weaponBase.Target = target;
									weaponBase.TargetTag = TargetTag;
									weaponBase.IgnoreTag = IgnoreTag;
								}
								if (RigidbodyProjectile) {
									if (bullet.GetComponent<Rigidbody> ()) {
										if (Owner != null && Owner.GetComponent<Rigidbody> ()) {
											bullet.GetComponent<Rigidbody> ().velocity = Owner.GetComponent<Rigidbody> ().velocity;
										}
										bullet.GetComponent<Rigidbody> ().AddForce (direction * ForceShoot);	
									}
								}
							}
                            bullet.transform.forward = direction;
                        }
					}
					if (Shell) {
						Transform shelloutpos = this.transform;
						if (ShellOuter.Length > 0) {
							shelloutpos = ShellOuter [currentOuter];
						}
						GameObject shell;
						if (WeaponSystem.Pool != null) {
							shell = WeaponSystem.Pool.Instantiate (Shell, shelloutpos.position, Random.rotation, ShellLifeTime);
						} else {
							shell = (GameObject)Instantiate (Shell, shelloutpos.position, Random.rotation);
							GameObject.Destroy (shell.gameObject, ShellLifeTime);
						}

						if (shell.GetComponent<Rigidbody> ()) {
							shell.GetComponent<Rigidbody> ().AddForce (shelloutpos.forward * ShellOutForce);
						}
					}

					if (SoundGun.Length > 0) {
						if (audioSource) {
							audioSource.PlayOneShot (SoundGun [Random.Range (0, SoundGun.Length)]);

						}
					}

					nextFireTime += FireRate;
				}
			} 
		}

		// 在 Scene 视图中绘制武器朝向辅助线。
		private void OnDestroy ()
		{
			if (vrCrosshairMaterial != null) {
				Destroy (vrCrosshairMaterial);
			}
			if (vrCrosshair != null) {
				Destroy (vrCrosshair);
			}
		}

		void OnDrawGizmos ()
		{

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine (this.transform.position + (this.transform.right * 0.1f), this.transform.position + this.transform.forward * 2);
			Gizmos.DrawLine (this.transform.position + (-this.transform.right * 0.1f), this.transform.position + this.transform.forward * 2);
			Gizmos.DrawLine (this.transform.position, this.transform.position + this.transform.forward * 2);
		}
	}
}
