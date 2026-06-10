using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HWRWeaponSystem;
using UnityEngine.SceneManagement;

namespace AirStrikeKit
{
    public class GameUI : MonoBehaviour
    {
        public GUISkin skin;
        public Texture2D Logo;
        public GameObject TargetIndicatorPrefab;
        public int Mode;

        private WeaponController weapon;
        private TargetIndicatorWorldUI lockedTargetIndicator;
        private TargetIndicatorWorldUI lockingTargetIndicator;
        private readonly List<TargetIndicatorWorldUI> targetIndicators = new List<TargetIndicatorWorldUI>();

        void Awake()
        {
            AirStrikeGame.gameUI = this;
        }

        void Start()
        {
            if (AirStrikeGame.playerController)
            {
                weapon = AirStrikeGame.playerController.GetComponent<WeaponController>();
            }
        }

        void Update()
        {
            if (AirStrikeGame.playerController && weapon == null)
            {
                weapon = AirStrikeGame.playerController.GetComponent<WeaponController>();
            }

            EnsureTargetIndicators();
            UpdateTargetIndicators();
        }

        public void TogglePause()
        {
            if (Mode == 1)
                return;

            if (Mode == 2)
            {
                Mode = 0;
                Time.timeScale = 1;
            }
            else
            {
                Mode = 2;
                Time.timeScale = 0;
            }
        }

        public void OnGUI()
        {
            if (skin)
                GUI.skin = skin;

            switch (Mode)
            {
                case 0:
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        Mode = 2;
                    }

                    if (AirStrikeGame.playerController)
                    {
                        AirStrikeGame.playerController.Active = true;

                        if (AirStrikeGame.playerController.IsVRActive)
                        {
                            break;
                        }

                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        GUI.skin.label.fontSize = 30;
                        GUI.Label(new Rect(20, 20, 200, 50), "Kills " + AirStrikeGame.gameManager.Killed.ToString());
                        GUI.Label(new Rect(20, 60, 200, 50), "Score " + AirStrikeGame.gameManager.Score.ToString());

                        GUI.skin.label.alignment = TextAnchor.UpperRight;
                        GUI.Label(new Rect(Screen.width - 220, 20, 200, 50), "ARMOR " + AirStrikeGame.playerController.GetComponent<DamageManager>().HP);
                        GUI.skin.label.fontSize = 16;

                        if (weapon.WeaponLists[weapon.CurrentWeapon].Icon)
                            GUI.DrawTexture(new Rect(Screen.width - 100, Screen.height - 100, 80, 80), weapon.WeaponLists[weapon.CurrentWeapon].Icon);

                        GUI.skin.label.alignment = TextAnchor.UpperRight;
                        if (weapon.WeaponLists[weapon.CurrentWeapon].Ammo <= 0 && weapon.WeaponLists[weapon.CurrentWeapon].ReloadingProcess > 0)
                        {
                            if (!weapon.WeaponLists[weapon.CurrentWeapon].InfinityAmmo)
                                GUI.Label(new Rect(Screen.width - 230, Screen.height - 120, 200, 30), "Reloading " + Mathf.Floor((1 - weapon.WeaponLists[weapon.CurrentWeapon].ReloadingProcess) * 100) + "%");
                        }
                        else
                        {
                            if (!weapon.WeaponLists[weapon.CurrentWeapon].InfinityAmmo)
                                GUI.Label(new Rect(Screen.width - 230, Screen.height - 120, 200, 30), weapon.WeaponLists[weapon.CurrentWeapon].Ammo.ToString());
                        }

                        GUI.skin.label.alignment = TextAnchor.UpperLeft;
                        GUI.Label(new Rect(20, Screen.height - 50, 250, 30), "R Mouse : Switch Guns C : Change Camera");
                    }
                    else
                    {
                        AirStrikeGame.playerController = (PlayerController)GameObject.FindObjectOfType(typeof(PlayerController));
                        if (AirStrikeGame.playerController)
                            weapon = AirStrikeGame.playerController.GetComponent<WeaponController>();
                    }
                    break;
                case 1:
                    if (AirStrikeGame.playerController)
                        AirStrikeGame.playerController.Active = false;

                    MouseLock.MouseLocked = false;

                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 30), "Game Over");

                    GUI.DrawTexture(new Rect(Screen.width / 2 - Logo.width / 2, Screen.height / 2 - 150, Logo.width, Logo.height), Logo);

                    if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 40), "Restart"))
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 100, 300, 40), "Back to StarFighter"))
                    {
                        SceneManager.LoadScene("StarFighter");
                    }
                    break;

                case 2:
                    if (AirStrikeGame.playerController)
                        AirStrikeGame.playerController.Active = false;

                    MouseLock.MouseLocked = false;
                    Time.timeScale = 0;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.Label(new Rect(0, Screen.height / 2 + 10, Screen.width, 30), "Pause");

                    GUI.DrawTexture(new Rect(Screen.width / 2 - Logo.width / 2, Screen.height / 2 - 150, Logo.width, Logo.height), Logo);

                    if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 40), "Resume"))
                    {
                        Mode = 0;
                        Time.timeScale = 1;
                    }
                    if (GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 100, 300, 40), "Back to StarFighter"))
                    {
                        Time.timeScale = 1;
                        Mode = 0;
                        SceneManager.LoadScene("StarFighter");
                    }
                    break;
            }
        }

        private void EnsureTargetIndicators()
        {
            if (TargetIndicatorPrefab == null || weapon == null)
            {
                return;
            }

            if (lockedTargetIndicator == null)
            {
                lockedTargetIndicator = CreateTargetIndicatorInstance("Locked");
            }

            if (lockingTargetIndicator == null)
            {
                lockingTargetIndicator = CreateTargetIndicatorInstance("Locking");
            }
        }

        private TargetIndicatorWorldUI CreateTargetIndicatorInstance(string suffix)
        {
            GameObject indicatorObject = (GameObject)Instantiate(TargetIndicatorPrefab);
            indicatorObject.name = TargetIndicatorPrefab.name + "_" + suffix;

            TargetIndicatorWorldUI indicator = indicatorObject.GetComponent<TargetIndicatorWorldUI>();
            if (indicator == null)
            {
                indicator = indicatorObject.AddComponent<TargetIndicatorWorldUI>();
            }
            return indicator;
        }

        private void UpdateTargetIndicators()
        {
            if (weapon == null)
            {
                SetTargetIndicatorVisible(lockedTargetIndicator, false);
                SetTargetIndicatorVisible(lockingTargetIndicator, false);
                HideUnusedTargetIndicators(0);
                return;
            }

            Camera indicatorCamera = GetIndicatorCamera();
            GameObject lockedTarget = weapon.LockedTarget;
            GameObject lockCandidate = weapon.LockCandidate;
            bool showLockingTarget = lockCandidate != null && lockCandidate != lockedTarget;
            List<GameObject> displayTargets = CollectDisplayTargets(lockedTarget, lockCandidate);

            if (lockedTargetIndicator != null)
            {
                lockedTargetIndicator.UpdateIndicatorTarget(lockedTarget, indicatorCamera, 1f, true, lockedTarget != null, true);
            }

            if (lockingTargetIndicator != null)
            {
                lockingTargetIndicator.UpdateIndicatorTarget(lockCandidate, indicatorCamera, weapon.LockProgress, false, showLockingTarget, true);
            }

            EnsureTargetIndicatorPool(displayTargets.Count);
            for (int i = 0; i < displayTargets.Count; i++)
            {
                targetIndicators[i].UpdateIndicatorTarget(displayTargets[i], indicatorCamera, 1f, false, true, false);
            }
            HideUnusedTargetIndicators(displayTargets.Count);
        }

        private void SetTargetIndicatorVisible(TargetIndicatorWorldUI indicator, bool visible)
        {
            if (indicator != null)
            {
                indicator.UpdateIndicatorTarget(null, null, 0f, false, visible);
            }
        }

        private List<GameObject> CollectDisplayTargets(GameObject lockedTarget, GameObject lockCandidate)
        {
            List<GameObject> result = new List<GameObject>();
            if (weapon == null || weapon.TargetTag == null || WeaponSystem.Finder == null)
            {
                return result;
            }

            for (int t = 0; t < weapon.TargetTag.Length; t++)
            {
                HWRWeaponSystem.TargetCollector collector = WeaponSystem.Finder.FindTargetTag(weapon.TargetTag[t]);
                if (collector == null || collector.Targets == null)
                {
                    continue;
                }

                for (int i = 0; i < collector.Targets.Length; i++)
                {
                    GameObject target = collector.Targets[i];
                    if (target == null || target == lockedTarget || target == lockCandidate)
                    {
                        continue;
                    }

                    if (!result.Contains(target))
                    {
                        result.Add(target);
                    }
                }
            }

            return result;
        }

        private void EnsureTargetIndicatorPool(int count)
        {
            while (targetIndicators.Count < count)
            {
                targetIndicators.Add(CreateTargetIndicatorInstance("Target_" + targetIndicators.Count));
            }
        }

        private void HideUnusedTargetIndicators(int usedCount)
        {
            for (int i = usedCount; i < targetIndicators.Count; i++)
            {
                SetTargetIndicatorVisible(targetIndicators[i], false);
            }
        }

        private Camera GetIndicatorCamera()
        {
            if (weapon != null)
            {
                WeaponLauncher currentWeapon = weapon.GetCurrentWeapon();
                if (currentWeapon != null && currentWeapon.CurrentCamera != null)
                {
                    return currentWeapon.CurrentCamera;
                }
            }

            return Camera.main;
        }

        private void OnDestroy()
        {
            if (lockedTargetIndicator != null)
            {
                Destroy(lockedTargetIndicator.gameObject);
            }

            if (lockingTargetIndicator != null)
            {
                Destroy(lockingTargetIndicator.gameObject);
            }

            for (int i = 0; i < targetIndicators.Count; i++)
            {
                if (targetIndicators[i] != null)
                {
                    Destroy(targetIndicators[i].gameObject);
                }
            }
            targetIndicators.Clear();
        }
    }
}
