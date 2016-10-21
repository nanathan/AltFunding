using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AltFunding
{
    [KSPScenario(ScenarioCreationOptions.AddToNewCareerGames | ScenarioCreationOptions.AddToExistingCareerGames, GameScenes.SPACECENTER)]
    class AltFundingScenario : ScenarioModule
    {
        public static AltFundingScenario Instance { get; private set; }

        public ConfigNode cn;
        public FundingConfig config = new FundingConfig();
        public FundingConfig baseConfig = new FundingConfig();

        // Used to prevent giving the player a bunch of payments if they install the mod in an existing save
        public double lastPayoutTime;

        public List<AltFundingLineItem> history = new List<AltFundingLineItem>();

        private double payoutPeriod { get { return config.GetCalculator().payPeriod; } }

        public static Date GetPayoutDateAfter(Date payout)
        {
            return Calendar.FromUT(payout.UT + Instance.payoutPeriod);
        }

        public Date GetNextPaymentDate()
        {
            if(history.Count == 0)
            {
                return Calendar.FromUT(payoutPeriod);
            }
            else
            {
                AltFundingLineItem item = history[history.Count - 1];
                return GetPayoutDateAfter(item.date);
            }
        }

        public void AddPaymentHistory(double ut, double amount, double balance)
        {
            history.Add(new AltFundingLineItem(Calendar.FromUT(ut), amount, balance));

            lastPayoutTime = ut;
        }

        public override void OnAwake()
        {
            base.OnAwake();
            Debug.Log("[AltFunding] AltFundingScenario.OnAwake()");
            Instance = this;
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            Debug.Log("[AltFunding] AltFundingScenario.OnLoad()");
            Instance = this;

            cn = null;
            config = new FundingConfig();
            baseConfig = new FundingConfig();

            ConfigNode[] cf = GameDatabase.Instance.GetConfigNodes("ALT_FUNDING");

            if(cf != null)
            {
                Debug.Log("[AltFunding] config nodes: " + cf.Length);
                foreach(ConfigNode node in cf)
                {
                    ConfigNode fundingConfig = node.GetNode("FundingConfig");
                    config.LoadConfig(fundingConfig);
                    baseConfig.LoadConfig(fundingConfig);
                }
            }

            if(gameNode.HasNode("FundingConfig"))
            {
                Debug.Log("[AltFunding] persistent config found");
                ConfigNode fundingConfig = gameNode.GetNode("FundingConfig");
                config.LoadConfig(fundingConfig);

                cn = fundingConfig.CreateCopy();
            }
            else
            {
                cn = new ConfigNode("FundingConfig");
            }
            if(!cn.HasValue("mode"))
            {
                cn.SetValue("mode", config.mode, true);
            }
            if(!cn.HasNode("BasicFunding"))
            {
                cn.AddNode("BasicFunding");
            }
            if(!cn.HasNode("RepFunding"))
            {
                cn.AddNode("RepFunding");
            }

            config.Log();

            double v;
            if(gameNode.HasValue("lastPayoutTime") && double.TryParse(gameNode.GetValue("lastPayoutTime"), out v))
            {
                lastPayoutTime = v;
            }
            else
            {
                lastPayoutTime = -1;
            }

            List<AltFundingLineItem> items = new List<AltFundingLineItem>();
            if(gameNode.HasNode("History"))
            {
                ConfigNode historyNode = gameNode.GetNode("History");
                ConfigNode[] nodes = historyNode.GetNodes("LineItem");

                if(nodes != null)
                {
                    for(int index = 0; index < nodes.Length; index += 1)
                    {
                        AltFundingLineItem item = AltFundingLineItem.Parse(nodes[index]);
                        if(item != null)
                        {
                            items.Add(item);
                        }
                    }
                }
            }
            else
            {
                // Load limited history from the old save format
                double lastPayoutAmount = 0;
                if(gameNode.HasValue("lastPayoutAmount") && double.TryParse(gameNode.GetValue("lastPayoutAmount"), out v))
                {
                    lastPayoutAmount = v;
                }
                else
                {
                    lastPayoutAmount = 0;
                }

                if(lastPayoutTime > 0 && lastPayoutAmount > 0)
                {
                    items.Add(new AltFundingLineItem(Calendar.FromUT(lastPayoutTime), lastPayoutAmount, Funding.Instance.Funds));
                }
            }
            history = items;
        }

        public override void OnSave(ConfigNode gameNode)
        {
            Debug.Log("[AltFunding] AltFundingScenario.OnSave()");
            if(cn != null)
            {
                gameNode.AddNode(cn.CreateCopy());
            }

            ConfigNode historyNode = gameNode.AddNode("History");
            for(int index = 0; index < history.Count; index += 1)
            {
                ConfigNode lineItem = historyNode.AddNode("LineItem");
                AltFundingLineItem item = history[index];

                lineItem.SetValue("ut", item.date.UT.ToString("F0"), true);
                lineItem.SetValue("amount", item.amount.ToString("F0"), true);
                lineItem.SetValue("balance", item.balance.ToString("F0"), true);
            }

            gameNode.SetValue("lastPayoutTime", lastPayoutTime.ToString("F0"), true);
        }
    }

    class AltFundingLineItem
    {
        public Date date;
        public double amount;
        public double balance;

        public AltFundingLineItem(Date date, double amount, double balance)
        {
            this.date = date;
            this.amount = amount;
            this.balance = balance;
        }

        public static AltFundingLineItem Parse(ConfigNode node)
        {
            Date date = null;
            double amount = 0;
            double balance = 0;

            double v;
            if(node.HasValue("ut") && double.TryParse(node.GetValue("ut"), out v))
            {
                date = Calendar.FromUT(v);
            }

            if(node.HasValue("amount") && double.TryParse(node.GetValue("amount"), out v))
            {
                amount = v;
            }

            if(node.HasValue("balance") && double.TryParse(node.GetValue("balance"), out v))
            {
                balance = v;
            }

            if(date != null && amount > 0)
            {
                return new AltFundingLineItem(date, amount, balance);
            }
            return null;
        }
    }
}
