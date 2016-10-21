using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI;
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

        private BundledAssets assets;
        private BudgetWindow budgetWindow;

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
            
            assets = FindObjectOfType<BundledAssets>();
            if(assets == null)
            {
                GameObject go = new GameObject("AltFundingBundledAssets");
                assets = go.AddComponent<BundledAssets>();
            }
        }

        private void ToggleVisibility()
        {
            if(!assets.Loaded)
            {
                Debug.Log("[AltFunding] Cannot display UI because AssetBundle has not finished loading!");
                return;
            }
            if(budgetWindow == null)
            {
                GameObject go = new GameObject("AltFundingBudgetWindow");
                budgetWindow = go.AddComponent<BudgetWindow>();
                budgetWindow.assets = assets;
            }
            else
            {
                Destroy(budgetWindow.gameObject);
                budgetWindow = null;
            }
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
                        s.AddPaymentHistory(payoutTime, payout, Funding.Instance.Funds);
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
            if(budgetWindow != null)
            {
                Destroy(budgetWindow.gameObject);
                budgetWindow = null;
            }
            if(assets != null)
            {
                Destroy(assets.gameObject);
                assets = null;
            }
        }
    }
}
