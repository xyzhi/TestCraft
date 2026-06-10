using UnityEngine;
using System.Collections;
using AirStrikeKit;

namespace HWRWeaponSystem
{
	public class DamageManager : MonoBehaviour
	{
		// 死亡时播放或生成的特效预制体。
		public GameObject Effect;
		// 最大生命值，未手动指定时会在启动时取 HP 初始值。
		public int MaxHP = -1;
		// 当前生命值。
		public int HP = 100;
		private ObjectPool objPool;
		[HideInInspector]
		// 最近一次命中当前对象的来源对象。
		public GameObject LatestHit;

		private void Awake ()
		{
			objPool = this.GetComponent<ObjectPool> ();
			if (MaxHP <= 0) {
				MaxHP = HP;
			}
		}

		// 按伤害包扣血，并记录伤害来源。
		public virtual void ApplyDamage (DamagePack damage)
		{
			if (IsPlayerInvincibleForTesting ())
				return;

			if (HP < 0)
				return;

			LatestHit = damage.Owner;
			HP -= damage.Damage;
			if (HP <= 0) {
				Dead ();
			}
		}

		// 直接按数值扣血。
		public virtual void ApplyDamage (int damage)
		{
			if (IsPlayerInvincibleForTesting ())
				return;

			if (HP < 0)
				return;

			HP -= damage;
			if (HP <= 0) {
				Dead ();
			}
		}

		// 处理死亡特效、对象回收和死亡消息广播。
		public void Dead ()
		{
			if (Effect) {
				if (WeaponSystem.Pool != null && Effect.GetComponent<ObjectPool>()) {
					WeaponSystem.Pool.Instantiate (Effect, transform.position, transform.rotation);
				} else {
					GameObject.Instantiate (Effect, transform.position, transform.rotation);
				}
			}
			if (objPool != null) {
				objPool.Destroying ();
			} else {
				Destroy (this.gameObject);
			}
			this.gameObject.SendMessage ("OnDead", SendMessageOptions.DontRequireReceiver);
		}

		private bool IsPlayerInvincibleForTesting ()
		{
			return AirStrikeGame.playerController != null && AirStrikeGame.playerController.gameObject == this.gameObject;
		}
	}
}
