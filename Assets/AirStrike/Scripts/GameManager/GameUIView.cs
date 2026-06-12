using UnityEngine;
using UnityEngine.UI;

namespace AirStrikeKit
{
    public class GameUIView : MonoBehaviour
    {
        public GameObject HudRoot;
        public GameObject OverlayRoot;
        public GameObject OverlayButtonGroup;
        public Image LogoImage;
        public Image WeaponIconImage;
        public Text KillsText;
        public Text ScoreText;
        public Text ArmorText;
        public Text AmmoText;
        public Text HintText;
        public Text OverlayTitleText;
        public Button PrimaryButton;
        public Button SecondaryButton;
        public Text PrimaryButtonText;
        public Text SecondaryButtonText;

        public void SetRootVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetHudVisible(bool visible)
        {
            if (HudRoot != null)
            {
                HudRoot.SetActive(visible);
            }
        }

        public void SetOverlayVisible(bool visible)
        {
            if (OverlayRoot != null)
            {
                OverlayRoot.SetActive(visible);
            }
        }

        public void SetOverlayButtonsVisible(bool visible)
        {
            if (OverlayButtonGroup != null)
            {
                OverlayButtonGroup.SetActive(visible);
            }
        }

        public void SetLogo(Sprite sprite)
        {
            if (LogoImage == null)
            {
                return;
            }

            LogoImage.sprite = sprite;
            LogoImage.enabled = sprite != null;
            LogoImage.preserveAspect = true;
        }

        public void SetWeaponIcon(Sprite sprite, bool visible)
        {
            if (WeaponIconImage == null)
            {
                return;
            }

            WeaponIconImage.sprite = sprite;
            WeaponIconImage.enabled = visible && sprite != null;
            WeaponIconImage.preserveAspect = true;
        }

        public void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
