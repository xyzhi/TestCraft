using HWRWeaponSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AirStrikeKit
{
    public class TargetIndicatorWorldUI : MonoBehaviour
    {
        public float DistanceScale = 0.001f;
        public float MinScale = 0.7f;
        public float MaxScale = 1.4f;
        float PullTowardCameraScale = 1f;

        private Canvas rootCanvas;
        private Image lockProgress;
        private Image healthBarFill;
        private Text targetName;
        private Text distanceText;
        private GameObject currentTarget;
        private DamageManager currentDamage;

        private void Awake()
        {
            rootCanvas = GetComponent<Canvas>();
            lockProgress = FindImage("LockProgress");
            healthBarFill = FindImage("HealthBarFill");
            targetName = FindText("TargetName");
            distanceText = FindText("DistanceText");
        }

        public void UpdateIndicatorTarget(GameObject target, Camera worldCamera, float progress, bool locked)
        {
            UpdateIndicatorTarget(target, worldCamera, progress, locked, target != null);
        }

        public void UpdateIndicatorTarget(GameObject target, Camera worldCamera, float progress, bool locked, bool visible)
        {
            if (!visible || target == null || worldCamera == null)
            {
                currentTarget = null;
                currentDamage = null;
                SetVisible(false);
                return;
            }

            if (target != currentTarget)
            {
                currentTarget = target;
                currentDamage = currentTarget.GetComponent<DamageManager>();
            }

            SetVisible(true);
            if (rootCanvas != null)
            {
                rootCanvas.worldCamera = worldCamera;
            }

            if (lockProgress != null)
            {
                lockProgress.fillAmount = Mathf.Clamp01(progress);
            }

            if (targetName != null)
            {
                targetName.text = locked ? currentTarget.name + " LOCK" : currentTarget.name;
            }

            float distance = Vector3.Distance(worldCamera.transform.position, currentTarget.transform.position);
            if (distanceText != null)
            {
                distanceText.text = Mathf.FloorToInt(distance) + "m";
            }

            if (healthBarFill != null)
            {
                if (currentDamage != null && currentDamage.MaxHP > 0)
                {
                    healthBarFill.fillAmount = Mathf.Clamp01((float)currentDamage.HP / currentDamage.MaxHP);
                }
                else
                {
                    healthBarFill.fillAmount = 1f;
                }
            }

            PositionIndicator(worldCamera, distance);
        }

        private void PositionIndicator(Camera worldCamera, float distance)
        {
            Bounds targetBounds;
            Vector3 worldPosition = currentTarget.transform.position;
            float pullTowardCamera = 0f;
            if (TryGetTargetBounds(currentTarget, out targetBounds))
            {
                worldPosition = targetBounds.center;
                pullTowardCamera = targetBounds.extents.y * PullTowardCameraScale;
            }

            worldPosition += (worldCamera.transform.position - worldPosition).normalized * pullTowardCamera;

            transform.position = worldPosition;
            transform.rotation = Quaternion.LookRotation(transform.position - worldCamera.transform.position, worldCamera.transform.up);

            float scale = Mathf.Clamp(distance * DistanceScale, MinScale, MaxScale);
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private bool TryGetTargetBounds(GameObject target, out Bounds bounds)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default(Bounds);
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (rootCanvas != null)
            {
                rootCanvas.enabled = visible;
            }
        }

        private Image FindImage(string childName)
        {
            Transform child = FindDeepChild(transform, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Text FindText(string childName)
        {
            Transform child = FindDeepChild(transform, childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private Transform FindDeepChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nestedChild = FindDeepChild(child, childName);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            return null;
        }
    }
}
