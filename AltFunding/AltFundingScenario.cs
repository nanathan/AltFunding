using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AltFunding
{
    class AltFundingScenario : ScenarioModule
    {
        public static AltFundingScenario Instance { get; private set; }

        private ConfigNode cn;
        public FundingConfig config = new FundingConfig();

        public double lastPayoutTime;
        public double lastPayoutAmount;

        public static Date LastPayoutDate { get { return Calendar.FromUT(Instance.lastPayoutTime); } }
        public static Date NextPayoutDate { get { return Calendar.FromUT(Instance.lastPayoutTime + Instance.payoutPeriod); } }

        private double payoutPeriod { get { return Instance.config.basicFunding.payPeriod; } }

        public static void Initialize()
        {
            Debug.Log("[AltFunding] AltFundingScenario.Initialize()");
            ProtoScenarioModule scenario = HighLogic.CurrentGame.scenarios.Find(m => m.moduleName == typeof(AltFundingScenario).Name);
            if(scenario == null)
            {
                HighLogic.CurrentGame.AddProtoScenarioModule(typeof(AltFundingScenario), GameScenes.SPACECENTER);
            }
            else
            {
                if(!scenario.targetScenes.Contains(GameScenes.SPACECENTER))
                {
                    scenario.targetScenes.Add(GameScenes.SPACECENTER);
                }
            }
        }

        public override void OnLoad(ConfigNode gameNode)
        {
            Debug.Log("[AltFunding] AltFundingScenario.OnLoad()");
            Instance = this;

            cn = null;
            config = new FundingConfig();

            ConfigNode[] cf = GameDatabase.Instance.GetConfigNodes("ALT_FUNDING");

            if(cf != null)
            {
                Debug.Log("[AltFunding] config nodes: " + cf.Length);
                foreach(ConfigNode node in cf)
                {
                    ConfigNode fundingConfig = node.GetNode("FundingConfig");
                    config.LoadConfig(fundingConfig);
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
        }

        public override void OnSave(ConfigNode gameNode)
        {
            Debug.Log("[AltFunding] AltFundingScenario.OnSave()");
            if(cn != null)
            {
                gameNode.AddNode(cn.CreateCopy());
            }

            gameNode.SetValue("lastPayoutTime", lastPayoutTime.ToString(), true);
        }
    }
}
