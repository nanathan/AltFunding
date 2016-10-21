using KSP.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AltFunding
{
    class BudgetWindow : MonoBehaviour
    {
        public BundledAssets assets;

        private GameObject window;
        private GameObject content;
        private Button settingsButton;
        private Text nextPayoutText;

        private List<BudgetRow> rows = new List<BudgetRow>();

        private double payoutUT = -1;

        void Start()
        {
            window = Instantiate(assets.mainWindowPrefab);
            window.AddComponent<Draggable>();
            window.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

            GameObject scrollview = window.GetChild("ScrollView");
            GameObject viewport = scrollview.GetChild("Viewport");
            content = viewport.GetChild("Content");
            settingsButton = window.GetChild("SettingsButton").GetComponent<Button>();
            nextPayoutText = window.GetChild("NextPayoutText").GetComponent<Text>();

            settingsButton.onClick.AddListener(ToggleSettings);

            nextPayoutText.text = "";

            // Remove the default listings that come with the prefab
            List<GameObject> children = new List<GameObject>();

            Transform transform = content.transform;
            for(int index = 0; index < transform.childCount; index += 1)
            {
                children.Add(transform.GetChild(index).gameObject);
            }

            for(int index = 0; index < children.Count; index += 1)
            {
                Destroy(children[index]);
            }
        }

        void OnDestroy()
        {
            if(window != null)
            {
                Destroy(window);
                window = null;
            }
            SettingsWindow.CloseWindow();
        }

        void ToggleSettings()
        {
            SettingsWindow.ToggleWindow();
        }

        void Update()
        {
            if(Time.timeSinceLevelLoad < 1)
            {
                return;
            }

            double now = Planetarium.GetUniversalTime();

            if(AltFundingScenario.Instance != null)
            {
                AltFundingScenario s = AltFundingScenario.Instance;

                if(payoutUT < 0 || s.lastPayoutTime > payoutUT)
                {
                    ClearList();
                }

                if(s.lastPayoutTime > payoutUT && s.lastPayoutTime >= 0)
                {
                    PopulateList();
                }

                UpdateNextPayoutText();
            }
        }

        private void ClearList()
        {
            for(int index = 0; index < rows.Count; index += 1)
            {
                Destroy(rows[index].gameObject);
            }
            rows.Clear();
        }

        private void PopulateList()
        {
            AltFundingScenario s = AltFundingScenario.Instance;
            FundingCalculator calculator = s.config.GetCalculator();
            double payPeriod = calculator.payPeriod;

            payoutUT = s.lastPayoutTime;
            
            rows.Add(new BudgetRow(assets, content));

            double cumulative = 0;

            for(int index = 0; index < s.history.Count; index += 1)
            {
                AltFundingLineItem item = s.history[index];
                cumulative += item.amount;

                rows.Add(new BudgetRow(assets, content, item.date, item.amount, cumulative, item.balance));
            }

            Date previousPayout = Calendar.FromUT(s.lastPayoutTime > 0 ? s.lastPayoutTime : 0);
            double payout = 0;
            cumulative = 0;
            double total = Funding.Instance.Funds;

            for(int nbr = 0; nbr < 10; nbr += 1)
            {
                Date nextPayout = AltFundingScenario.GetPayoutDateAfter(previousPayout);
                payout = calculator.GetPayment((int) Math.Round(nextPayout.UT / payPeriod));
                cumulative += payout;
                total += payout;

                rows.Add(new BudgetRow(assets, content, nextPayout, payout, cumulative, total));

                previousPayout = nextPayout;
            }
        }

        public static void UpdateBudgetList()
        {
            BudgetWindow instance = FindObjectOfType<BudgetWindow>();
            if(instance != null)
            {
                instance.UpdateList();
            }
        }

        internal void UpdateList()
        {
            AltFundingScenario s = AltFundingScenario.Instance;
            FundingCalculator calculator = s.config.GetCalculator();
            double payPeriod = calculator.payPeriod;

            Date previousPayout = Calendar.FromUT(s.lastPayoutTime > 0 ? s.lastPayoutTime : 0);
            double payout = 0;
            double cumulative = 0;
            double total = Funding.Instance.Funds;

            for(int index = Math.Max(0, rows.Count - 10); index < rows.Count; index += 1)
            {
                Date nextPayout = AltFundingScenario.GetPayoutDateAfter(previousPayout);
                payout = calculator.GetPayment((int) Math.Round(nextPayout.UT / payPeriod));
                cumulative += payout;
                total += payout;

                rows[index].date = nextPayout;
                rows[index].payout = payout;
                rows[index].cumulative = cumulative;
                rows[index].balance = total;
                rows[index].Update();

                previousPayout = nextPayout;
            }
        }

        private void UpdateNextPayoutText()
        {
            double timeToPayout = AltFundingScenario.Instance.GetNextPaymentDate().UT - Planetarium.GetUniversalTime();
            Date toNextPayout = Calendar.FromUT(timeToPayout);

            nextPayoutText.text =
                string.Format("Next Payout: {0}d {1:00}:{2:00}:{3:00}", toNextPayout.DayOfYear - 1, toNextPayout.Hour, toNextPayout.Minute, toNextPayout.Second);
        }
    }

    class BudgetRow
    {
        internal Date date;
        internal double payout;
        internal double cumulative;
        internal double balance;

        internal GameObject gameObject;

        internal BudgetRow(BundledAssets assets, GameObject container)
        {
            Instantiate(assets, container);
            Update();
        }

        internal BudgetRow(BundledAssets assets, GameObject container, Date date, double payout, double cumulative, double balance)
        {
            Instantiate(assets, container);

            this.date = date;
            this.payout = payout;
            this.cumulative = cumulative;
            this.balance = balance;

            Update();
        }

        private void Instantiate(BundledAssets assets, GameObject container)
        {
            gameObject = UnityEngine.Object.Instantiate(assets.budgetRowPrefab);
            gameObject.transform.SetParent(container.transform);
            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        }

        internal void Update()
        {
            if(date == null)
            {
                gameObject.GetChild("RowDate").GetComponent<Text>().text = "Date:";
                gameObject.GetChild("RowPayout").GetComponent<Text>().text = "Payout:";
                gameObject.GetChild("RowCumulative").GetComponent<Text>().text = "Cumulative:";
                gameObject.GetChild("RowBalance").GetComponent<Text>().text = "Balance:";
            }
            else
            {
                gameObject.GetChild("RowDate").GetComponent<Text>().text = string.Format("Year {0} Day {1}", date.Year, date.DayOfYear);
                gameObject.GetChild("RowPayout").GetComponent<Text>().text = string.Format("${0:N0}", payout);
                gameObject.GetChild("RowCumulative").GetComponent<Text>().text = cumulative <= 0.1 ? "" : string.Format("${0:N0}", cumulative);
                gameObject.GetChild("RowBalance").GetComponent<Text>().text = string.Format("${0:N0}", balance);
            }
        }
    }
}
