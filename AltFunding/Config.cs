using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AltFunding
{
    public class FundingConfig
    {
        public string mode;
        public BasicFunding basicFunding = new BasicFunding();

        public void LoadConfig(ConfigNode node)
        {
            ConfigUtilities.TryParseConfig(this, node);

            if(node.HasNode("BasicFunding"))
            {
                ConfigNode bf = node.GetNode("BasicFunding");

                basicFunding.LoadConfig(bf);
            }
        }

        internal void Log()
        {
            Debug.Log(string.Format("[AltFunding] mode {0}", mode));
            basicFunding.Log();
        }
    }

    public class BasicFunding
    {
        // n-th payment
        // a + b*n + c*n^.5 + d*ln(n)
        public double payPeriod;
        public double paymentNumberMultiplier;
        public double paymentNumberOffset;
        public double basePay;
        public double linearPay;
        public double sqrtPay;
        public double logarithmicPay;

        private const double THRESHOLD = 1e-9;

        public int GetPayment(int paymentNumber)
        {
            double n = paymentNumber * paymentNumberMultiplier + paymentNumberOffset;
            double amount = basePay;
            if(Math.Abs(linearPay) > THRESHOLD)
                amount += linearPay * n;
            if(Math.Abs(sqrtPay) > THRESHOLD)
                amount += sqrtPay * Math.Sqrt(n);
            if(Math.Abs(logarithmicPay) > THRESHOLD)
                amount += logarithmicPay * Math.Log(n);
            return (int) amount;
        }

        public void LoadConfig(ConfigNode node)
        {
            ConfigUtilities.TryParseConfig(this, node);
        }

        internal void Log()
        {
            Debug.Log(string.Format("[AltFunding] {0} {1} {2} {3} {4}", payPeriod, basePay, linearPay, sqrtPay, logarithmicPay));
        }
    }

    internal class ConfigUtilities
    {
        public static void TryParseConfig(System.Object obj, ConfigNode node)
        {
            double v;
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
            }
        }
    }
}
