using System.Collections.Generic;
using HWRWeaponSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AirStrikeKit
{
    public class GameUI : MonoBehaviour
    {
        public GUISkin skin;
        public Texture2D Logo;
        public GameObject TargetIndicatorPrefab;
        public GameObject UIPrefab;
        public Texture2D DefaultGunIcon;
        public Texture2D DefaultMissileIcon;
        public Texture2D DefaultHeavyMissileIcon;
        public Texture2D DefaultBombIcon;
        public string PCCanvasName = "PCCanvas";
        public string VRCanvasName = "MainCanvas";
        public int Mode;

        private WeaponController weapon;
        private GameUIView pcView;
        private GameUIView vrView;
        private TargetIndicatorWorldUI lockedTargetIndicator;
        private TargetIndicatorWorldUI lockingTargetIndicator;
        private readonly List<TargetIndicatorWorldUI> targetIndicators = new List<TargetIndicatorWorldUI>();
        private readonly Dictionary<Texture2D, Sprite> spriteCache = new Dictionary<Texture2D, Sprite>();

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

            EnsureCanvasViews();
            BindViewButtons(pcView);
            BindViewButtons(vrView);
        }

        void Update()
        {
            if (AirStrikeGame.playerController && weapon == null)
            {
                weapon = AirStrikeGame.playerController.GetComponent<WeaponController>();
            }

            EnsureCanvasViews();
            HandlePauseInput();
            UpdateGameplayState();
            UpdateCanvasViews();
            EnsureTargetIndicators();
            UpdateTargetIndicators();
        }

        public void TogglePause()
        {
            if (Mode == 1)
            {
                return;
            }

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

        private void HandlePauseInput()
        {
            if (Mode == 0 && Input.GetKeyDown(KeyCode.Escape))
            {
                Mode = 2;
            }
        }

        private void UpdateGameplayState()
        {
            if (AirStrikeGame.playerController == null)
            {
                AirStrikeGame.playerController = (PlayerController)GameObject.FindObjectOfType(typeof(PlayerController));
                if (AirStrikeGame.playerController != null)
                {
                    weapon = AirStrikeGame.playerController.GetComponent<WeaponController>();
                }
            }

            switch (Mode)
            {
                case 0:
                    if (AirStrikeGame.playerController != null)
                    {
                        AirStrikeGame.playerController.Active = true;
                    }
                    if (Time.timeScale == 0)
                    {
                        Time.timeScale = 1;
                    }
                    break;
                case 1:
                    if (AirStrikeGame.playerController != null)
                    {
                        AirStrikeGame.playerController.Active = false;
                    }
                    MouseLock.MouseLocked = false;
                    break;
                case 2:
                    if (AirStrikeGame.playerController != null)
                    {
                        AirStrikeGame.playerController.Active = false;
                    }
                    MouseLock.MouseLocked = false;
                    if (Time.timeScale != 0)
                    {
                        Time.timeScale = 0;
                    }
                    break;
            }
        }

        private void UpdateCanvasViews()
        {
            bool isVrActive = AirStrikeGame.playerController != null && AirStrikeGame.playerController.IsVRActive;

            UpdateView(pcView, false, !isVrActive);
            UpdateView(vrView, true, isVrActive);
        }

        private void UpdateView(GameUIView view, bool isVrLayout, bool visible)
        {
            if (view == null)
            {
                return;
            }

            view.SetRootVisible(visible);
            if (!visible)
            {
                return;
            }

            bool showHud = Mode == 0;
            view.SetHudVisible(showHud);

            if (showHud)
            {
                UpdateHud(view);
            }

            switch (Mode)
            {
                case 0:
                    view.SetOverlayVisible(false);
                    break;
                case 1:
                    UpdateOverlay(view, "Game Over", "Restart", true);
                    break;
                case 2:
                    UpdateOverlay(view, "Pause", "Resume", true);
                    break;
            }
        }

        private void UpdateHud(GameUIView view)
        {
            if (AirStrikeGame.gameManager != null)
            {
                view.SetText(view.KillsText, "Kills " + AirStrikeGame.gameManager.Killed);
                view.SetText(view.ScoreText, "Score " + AirStrikeGame.gameManager.Score);
            }
            else
            {
                view.SetText(view.KillsText, "Kills 0");
                view.SetText(view.ScoreText, "Score 0");
            }

            DamageManager playerDamage = AirStrikeGame.playerController != null
                ? AirStrikeGame.playerController.GetComponent<DamageManager>()
                : null;
            view.SetText(view.ArmorText, "ARMOR " + (playerDamage != null ? playerDamage.HP.ToString() : "0"));
            view.SetText(view.HintText, "R Mouse : Switch Guns C : Change Camera");

            if (weapon == null || weapon.WeaponLists == null || weapon.WeaponLists.Length == 0)
            {
                view.SetWeaponIcon(null, false);
                view.SetText(view.AmmoText, string.Empty);
                return;
            }

            WeaponLauncher currentWeapon = weapon.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                view.SetWeaponIcon(null, false);
                view.SetText(view.AmmoText, string.Empty);
                return;
            }

            Texture2D weaponIconTexture = GetWeaponIconTexture(currentWeapon);
            Sprite iconSprite = CreateSpriteFromTexture(weaponIconTexture);
            view.SetWeaponIcon(iconSprite, weaponIconTexture != null);

            if (currentWeapon.InfinityAmmo)
            {
                view.SetText(view.AmmoText, string.Empty);
                return;
            }

            if (currentWeapon.Ammo <= 0 && currentWeapon.ReloadingProcess > 0)
            {
                view.SetText(view.AmmoText, "Reloading " + Mathf.Floor((1 - currentWeapon.ReloadingProcess) * 100) + "%");
            }
            else
            {
                view.SetText(view.AmmoText, currentWeapon.Ammo.ToString());
            }
        }

        private void UpdateOverlay(GameUIView view, string title, string primaryButtonText, bool showButtons)
        {
            view.SetOverlayVisible(true);
            view.SetOverlayButtonsVisible(showButtons);
            view.SetText(view.OverlayTitleText, title);
            view.SetText(view.PrimaryButtonText, primaryButtonText);
            view.SetText(view.SecondaryButtonText, "Back to StarFighter");
        }

        private void EnsureCanvasViews()
        {
            if (UIPrefab == null)
            {
                return;
            }

            if (pcView == null)
            {
                pcView = CreateViewUnderCanvas(PCCanvasName, "PC");
                BindViewButtons(pcView);
            }

            if (vrView == null)
            {
                vrView = CreateViewUnderCanvas(VRCanvasName, "VR");
                BindViewButtons(vrView);
            }
        }

        private GameUIView CreateViewUnderCanvas(string canvasName, string suffix)
        {
            GameObject canvasObject = GameObject.Find(canvasName);
            if (canvasObject == null)
            {
                return null;
            }

            GameUIView existingView = canvasObject.GetComponentInChildren<GameUIView>(true);
            if (existingView != null)
            {
                existingView.name = "GameUI_" + suffix;
                return existingView;
            }

            GameObject viewObject = (GameObject)Instantiate(UIPrefab);
            viewObject.name = "GameUI_" + suffix;

            RectTransform viewTransform = viewObject.GetComponent<RectTransform>();
            RectTransform canvasTransform = canvasObject.GetComponent<RectTransform>();
            if (viewTransform != null && canvasTransform != null)
            {
                viewTransform.SetParent(canvasTransform, false);
                viewTransform.anchorMin = Vector2.zero;
                viewTransform.anchorMax = Vector2.one;
                viewTransform.offsetMin = Vector2.zero;
                viewTransform.offsetMax = Vector2.zero;
                viewTransform.localScale = Vector3.one;
            }
            else
            {
                viewObject.transform.SetParent(canvasObject.transform, false);
            }

            return viewObject.GetComponent<GameUIView>();
        }

        private void BindViewButtons(GameUIView view)
        {
            if (view == null)
            {
                return;
            }

            if (view.PrimaryButton != null)
            {
                view.PrimaryButton.onClick.RemoveAllListeners();
                view.PrimaryButton.onClick.AddListener(HandlePrimaryButtonClicked);
            }

            if (view.SecondaryButton != null)
            {
                view.SecondaryButton.onClick.RemoveAllListeners();
                view.SecondaryButton.onClick.AddListener(HandleBackToStarFighterClicked);
            }
        }

        private void HandlePrimaryButtonClicked()
        {
            if (Mode == 1)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

            if (Mode == 2)
            {
                Mode = 0;
                Time.timeScale = 1;
            }
        }

        private void HandleBackToStarFighterClicked()
        {
            Time.timeScale = 1;
            Mode = 0;
            SceneManager.LoadScene("StarFighter");
        }

        private Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            Sprite cachedSprite;
            if (spriteCache.TryGetValue(texture, out cachedSprite))
            {
                return cachedSprite;
            }

            cachedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            spriteCache[texture] = cachedSprite;
            return cachedSprite;
        }

        private Texture2D GetWeaponIconTexture(WeaponLauncher currentWeapon)
        {
            if (currentWeapon == null)
            {
                return null;
            }

            if (currentWeapon.Icon != null)
            {
                return currentWeapon.Icon;
            }

            if (currentWeapon.Seeker)
            {
                if (currentWeapon.DistanceLock <= 20f)
                {
                    return DefaultBombIcon != null ? DefaultBombIcon : DefaultMissileIcon;
                }

                if (currentWeapon.AmmoMax <= 2 || currentWeapon.ReloadTime >= 3f)
                {
                    return DefaultMissileIcon != null ? DefaultMissileIcon : DefaultHeavyMissileIcon;
                }

                return DefaultHeavyMissileIcon != null ? DefaultHeavyMissileIcon : DefaultMissileIcon;
            }

            return DefaultGunIcon;
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

            foreach (KeyValuePair<Texture2D, Sprite> pair in spriteCache)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }
            spriteCache.Clear();
        }
    }
}
