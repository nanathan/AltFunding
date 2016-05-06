using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace AltFunding
{
    using Toolbar;

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AltFundingAddon : MonoBehaviour
    {
        private IButton button;
        private ApplicationLauncherButton stockButton;
        private bool visible;
        private Rect position = new Rect(400, 200, 250, 100);
        private int windowId = 9653;

        private GUIStyle alignLeft;
        private GUIStyle alignRight;

        public void Start()
        {
            Debug.Log("[AltFunding] AltFundingAddon.Start()");
            if(HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                Debug.Log("[AltFunding] Not running in game mode " + HighLogic.CurrentGame.Mode);
                Destroy(this);
                return;
            }

            AltFundingScenario.Initialize();

            if(ToolbarManager.ToolbarAvailable)
            {
                button = ToolbarManager.Instance.add("AltFunding", "GUI");
                button.TexturePath = "AltFunding/icon24";
                button.ToolTip = "AltFunding";
                button.Enabled = true;
                button.OnClick += (e) => { ToggleVisibility(); };
            }
            else
            {
                Texture2D texture = GameDatabase.Instance.GetTexture("AltFunding/icon38", false);
                if(texture != null)
                {
                    stockButton = ApplicationLauncher.Instance.AddModApplication(
                        ToggleVisibility, ToggleVisibility, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER, texture);
                }
            }
        }

        private void ToggleVisibility()
        {
            visible = !visible;
            position.height = 100;
        }

        internal void OnGUI()
        {
            if(visible)
            {
                GUI.skin = HighLogic.Skin;

                position = GUILayout.Window(windowId, position, DrawWindow, "AltFunding", GUILayout.ExpandHeight(true));
            }
        }

        internal void DrawWindow(int wid)
        {
            if(Time.timeSinceLevelLoad < 1)
            {
                return;
            }

            if(alignRight == null)
            {
                alignLeft = GUI.skin.label;
                alignLeft.alignment = TextAnchor.MiddleLeft;
                alignRight = new GUIStyle(alignLeft);
                alignRight.alignment = TextAnchor.MiddleRight;
            }

            AltFundingScenario s = AltFundingScenario.Instance;
            FundingCalculator calculator = s.config.GetCalculator();
            if(calculator == null)
            {
                visible = false;
                return;
            }

            GUILayout.BeginVertical();

            double payPeriod = calculator.payPeriod;
            Date date = Calendar.Now;

            double payout;

            if(s.lastPayoutAmount > 0 && s.lastPayoutTime > 0)
            {
                Date lastPayout = AltFundingScenario.LastPayoutDate;
                payout = AltFundingScenario.Instance.lastPayoutAmount;

                GUILayout.Label("Last Payout:", GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Year {0} Day {1}", lastPayout.Year, lastPayout.DayOfYear),
                    alignLeft, GUILayout.Width(125));
                GUILayout.Label(string.Format("${0:F0}", payout), alignRight, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUILayout.Label(" ");
            }

            Date nextPayout = AltFundingScenario.NextPayoutDate;
            payout = calculator.GetPayment((int) Math.Round(nextPayout.UT / payPeriod));

            GUILayout.Label("Next Payout:", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Year {0} Day {1}", nextPayout.Year, nextPayout.DayOfYear),
                alignLeft, GUILayout.Width(125));
            GUILayout.Label(string.Format("${0:F0}", payout), alignRight, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.Label(" ");

            double timeToPayout = nextPayout.UT - Planetarium.GetUniversalTime();
            Date toNextPayout = Calendar.FromUT(timeToPayout);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Next Payout:", alignLeft, GUILayout.Width(125));
            GUILayout.Label(string.Format("{0}d {1:00}:{2:00}:{3:00}", toNextPayout.DayOfYear - 1, toNextPayout.Hour, toNextPayout.Minute, toNextPayout.Second),
                alignRight, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public void Update()
        {
            if(Time.timeSinceLevelLoad < 1)
            {
                return;
            }

            double now = Planetarium.GetUniversalTime();

            if(AltFundingScenario.Instance != null)
            {
                AltFundingScenario s = AltFundingScenario.Instance;
                FundingCalculator calculator = s.config.GetCalculator();

                if(calculator != null)
                {
                    double payPeriod = calculator.payPeriod;

                    if(s.lastPayoutTime < 0)
                    {
                        Debug.Log("[AltFunding] Last payout < 0, catching up");
                        s.lastPayoutTime = 0;
                        while(s.lastPayoutTime + payPeriod < now)
                        {
                            s.lastPayoutTime += payPeriod;
                        }
                        Debug.Log("[AltFunding] Last payout time = " + s.lastPayoutTime);
                    }

                    double payoutTime = s.lastPayoutTime + payPeriod;
                    if(payoutTime < now)
                    {
                        Debug.Log("[AltFunding] Prior funds " + Funding.Instance.Funds);
                        double payout = calculator.GetPayment((int)Math.Round(payoutTime / payPeriod));
                        Debug.Log("[AltFunding] Adding funds " + payout);
                        Funding.Instance.AddFunds(payout, TransactionReasons.None);
                        s.lastPayoutTime = payoutTime;
                        s.lastPayoutAmount = payout;
                        Debug.Log("[AltFunding] Post funds " + Funding.Instance.Funds);

                        calculator.ApplyPaymentSideEffects();
                    }
                }
                else
                {
                    Debug.Log("[AltFunding] mode is " + s.config.mode);
                    Destroy(this);
                }
            }
        }

        public void OnDestroy()
        {
            Debug.Log("[AltFunding] AltFundingAddon.OnDestroy()");
            if(button != null)
            {
                button.Destroy();
                button = null;
            }
            if(stockButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stockButton);
                stockButton = null;
            }
        }
    }
}
