using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AltFunding
{
    public abstract class FundingCalculator
    {
        public double payPeriod;

        protected FundingCalculator()
        {
            payPeriod = 648000;
        }

        public abstract int GetPayment(int paymentNumber);
        public virtual void ApplyPaymentSideEffects() { }

        public virtual void LoadConfig(ConfigNode node)
        {
            ConfigUtilities.TryParseConfig(this, node);

            if(payPeriod < 60.0)
            {
                payPeriod = 648000;
            }
        }

        internal virtual void Log() { }
    }

    public class FundingConfig
    {
        public static readonly string MODE_BASIC_FUNDING = "BasicFunding";
        public static readonly string MODE_REP_FUNDING = "RepFunding";

        public static readonly string[] modes = { MODE_BASIC_FUNDING, MODE_REP_FUNDING };

        public string mode;
        public bool locked;
        public BasicFunding basicFunding = new BasicFunding();
        public RepFunding repFunding = new RepFunding();

        public FundingCalculator GetCalculator()
        {
            if(mode == MODE_BASIC_FUNDING)
                return basicFunding;
            else if(mode == MODE_REP_FUNDING)
                return repFunding;

            return null;
        }

        public void LoadConfig(ConfigNode node)
        {
            ConfigUtilities.TryParseConfig(this, node);

            if(node.HasNode(MODE_BASIC_FUNDING))
            {
                ConfigNode bf = node.GetNode(MODE_BASIC_FUNDING);

                basicFunding.LoadConfig(bf);
            }
            if(node.HasNode(MODE_REP_FUNDING))
            {
                ConfigNode rf = node.GetNode(MODE_REP_FUNDING);

                repFunding.LoadConfig(rf);
            }
        }

        internal void Log()
        {
            Debug.Log(string.Format("[AltFunding] mode {0}", mode));
            basicFunding.Log();
            repFunding.Log();
        }
    }

    public class BasicFunding : FundingCalculator
    {
        // n-th payment
        // a + b*n + c*n^.5 + d*ln(n)
        public double paymentNumberMultiplier;
        public double paymentNumberOffset;
        public double basePay;
        public double linearPay;
        public double sqrtPay;
        public double logarithmicPay;

        protected const double THRESHOLD = 1e-9;

        public override int GetPayment(int paymentNumber)
        {
            double amount = 0;
            ApplyBasicMultipliers(ref amount, paymentNumber);
            return (int) amount;
        }
        
        protected void ApplyBasicMultipliers(ref double amount, int paymentNumber)
        {
            double n = paymentNumber * paymentNumberMultiplier + paymentNumberOffset;
            if(Math.Abs(basePay) > THRESHOLD)
                amount += basePay;
            if(Math.Abs(linearPay) > THRESHOLD)
                amount += linearPay * n;
            if(Math.Abs(sqrtPay) > THRESHOLD)
                amount += sqrtPay * Math.Sqrt(n);
            if(Math.Abs(logarithmicPay) > THRESHOLD)
                amount += logarithmicPay * Math.Log(n);
        }

        internal override void Log()
        {
            Debug.Log(string.Format("[AltFunding] BasicFunding: {0} {1} {2} {3} {4}", payPeriod, basePay, linearPay, sqrtPay, logarithmicPay));
        }
    }

    public class RepFunding : BasicFunding
    {
        public double repBonusPaymentRate;
        public double repBonusPaymentThreshold;
        public double repCostRate;

        public override int GetPayment(int paymentNumber)
        {
            double amount = 0;
            ApplyBasicMultipliers(ref amount, paymentNumber);
            ApplyRepMultipliers(ref amount);
            return (int) amount;
        }

        protected void ApplyRepMultipliers(ref double amount)
        {
            double rep = reputation;
            if(rep > THRESHOLD && repBonusPaymentRate > THRESHOLD)
                amount += rep * repBonusPaymentRate;
        }

        public override void ApplyPaymentSideEffects()
        {
            double rep = reputation;
            if(rep > THRESHOLD && repCostRate > THRESHOLD)
            {
                float cost = (float) (-1.0 * rep * repCostRate);
                float before = Reputation.Instance.reputation;
                Reputation.Instance.AddReputation(cost, TransactionReasons.None);
                float after = Reputation.Instance.reputation;
                Debug.Log(string.Format("[AltFunding] Reputation went from {0:F3} to {1:F3} (cost {2:F4})", before, after, cost));
            }
        }

        internal override void Log()
        {
            base.Log();
            Debug.Log(string.Format("[AltFunding] RepFunding: {0} {1} {2}", repBonusPaymentRate, repBonusPaymentThreshold, repCostRate));
        }

        protected double reputation
        {
            get
            {
                double rep = Reputation.Instance.reputation;
                if(repBonusPaymentThreshold > THRESHOLD)
                {
                    rep -= repBonusPaymentThreshold;
                }
                return rep;
            }
        }
    }

    internal class ConfigUtilities
    {
        public static void TryParseConfig(System.Object obj, ConfigNode node)
        {
            double v;
            bool b;
            foreach(FieldInfo field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if(field.FieldType == typeof(double))
                {
                    if(node.HasValue(field.Name) && double.TryParse(node.GetValue(field.Name), out v))
                    {
                        field.SetValue(obj, v);
                    }
                }
                else if(field.FieldType == typeof(string))
                {
                    if(node.HasValue(field.Name))
                    {
                        field.SetValue(obj, node.GetValue(field.Name));
                    }
                }
                else if(field.FieldType == typeof(bool))
                {
                    if(node.HasValue(field.Name) && bool.TryParse(node.GetValue(field.Name), out b))
                    {
                        field.SetValue(obj, b);
                    }
                }
            }
        }
    }
}
