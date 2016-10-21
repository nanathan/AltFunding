using KSP.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AltFunding
{
    class SettingsWindow : MonoBehaviour
    {
        #region static
        internal static SettingsWindow instance;

        public static void ToggleWindow()
        {
            if(IsOpen())
            {
                CloseWindow();
            }
            else
            {
                OpenWindow();
            }
        }

        public static bool IsOpen()
        {
            return instance != null;
        }

        public static void OpenWindow()
        {
            if(!IsOpen())
            {
                GameObject go = new GameObject("AltFundingSettingsWindow");
                instance = go.AddComponent<SettingsWindow>();
            }
        }

        public static void CloseWindow()
        {
            if(IsOpen())
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }
        #endregion

        public BundledAssets assets;

        private GameObject window;
        private GameObject content;

        private GameObject modeLeftButton;
        private GameObject modeRightButton;
        private Text modeText;
        private GameObject lockSettingsButton;
        private Text lockSettingsButtonText;

        private List<SettingsDisplayRow> rows = new List<SettingsDisplayRow>();

        private bool displayBaseConfig;

        private UnityEngine.EventSystems.EventSystem eventSystem;
        private bool keylock;
        
        internal void Start()
        {
            assets = FindObjectOfType<BundledAssets>();

            eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if(eventSystem == null)
            {
                Debug.Log("[AltFunding] SettingsWindow: EventSystem is null");
            }

            // Create the window
            window = Instantiate(assets.settingsWindowPrefab);
            window.AddComponent<Draggable>();
            window.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

            // Fetch important sub-components of the window
            GameObject scrollview = window.GetChild("ScrollView");
            GameObject viewport = scrollview.GetChild("Viewport");
            content = viewport.GetChild("Content");

            modeLeftButton = window.GetChild("ModeLeftButton");
            modeRightButton = window.GetChild("ModeRightButton");
            modeText = window.GetChild("ModeText").GetComponent<Text>();
            lockSettingsButton = window.GetChild("LockSettingsButton");
            lockSettingsButtonText = lockSettingsButton.GetChild("LockSettingsButtonText").GetComponent<Text>();

            // Attach functionality to the buttons
            modeLeftButton.GetComponent<Button>().onClick.AddListener(() => { UpdateConfigModeIndex(-1); BudgetWindow.UpdateBudgetList(); });
            modeRightButton.GetComponent<Button>().onClick.AddListener(() => { UpdateConfigModeIndex(1); BudgetWindow.UpdateBudgetList(); });

            window.GetChild("BaseSettingsButton").GetComponent<Button>().onClick.AddListener(() => { displayBaseConfig = true; UpdateDisplay(); });
            window.GetChild("SaveSpecificSettingsButton").GetComponent<Button>().onClick.AddListener(() => { displayBaseConfig = false; UpdateDisplay(); });

            lockSettingsButton.GetComponent<Button>().onClick.AddListener(LockSettings);
            
            // Initialize the list of settings
            ClearList();
            PopulateList();
            UpdateDisplay();
        }

        void Update()
        {
            if(eventSystem != null)
            {
                GameObject obj = eventSystem.currentSelectedGameObject;
                string name = obj == null ? "<<null>>" : obj.name;

                if(keylock && name != "SettingInputField")
                {
                    Debug.Log("[AltFunding] Unlocking keyboard input");
                    keylock = false;
                    InputLockManager.SetControlLock(ControlTypes.None, "AltFundingSettingsWindow");
                }
                else if(!keylock && name == "SettingInputField")
                {
                    Debug.Log("[AltFunding] Locking keyboard input");
                    keylock = true;
                    InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "AltFundingSettingsWindow");
                }
            }
        }

        void OnDestroy()
        {
            if(window != null)
            {
                Destroy(window);
                window = null;
            }
            instance = null;
        }
        
        private void ClearList()
        {
            List<GameObject> children = new List<GameObject>();

            Transform transform = content.transform;
            for(int index = 0; index < transform.childCount; index += 1)
            {
                children.Add(transform.GetChild(index).gameObject);
            }

            for(int index = 0; index < children.Count; index += 1)
            {
                Debug.Log(string.Format("[AltFunding] Destroying settings row {0}", index));
                Destroy(children[index]);
            }

            rows.Clear();
        }

        private void PopulateList()
        {
            AltFundingScenario scenario = AltFundingScenario.Instance;
            ConfigNode node = scenario.cn;
            FundingConfig config = scenario.config;
            FundingConfig baseConfig = scenario.baseConfig;

            rows.Add(new SettingsDoubleDisplayRow("Pay Period:", "F0", 21600, "payPeriod", typeof(FundingCalculator)));

            // These settings are used for both modes
            //if(config.mode == FundingConfig.MODE_BASIC_FUNDING || config.mode == FundingConfig.MODE_REP_FUNDING)
            {
                rows.Add(new SettingsDoubleDisplayRow("Payment # Multiplier:", "F4", "paymentNumberMultiplier", typeof(BasicFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Payment # Offset:", "F4", "paymentNumberOffset", typeof(BasicFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Base Pay:", "F2", "basePay", typeof(BasicFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Linear Pay:", "F2", "linearPay", typeof(BasicFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Square Root Pay:", "F2", "sqrtPay", typeof(BasicFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Logarithmic Pay:", "F2", "logarithmicPay", typeof(BasicFunding)));
            }

            if(config.mode == FundingConfig.MODE_REP_FUNDING)
            {
                rows.Add(new SettingsDoubleDisplayRow("Base Pay:", "F2", "basePay", typeof(RepFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Rep Bonus Payment Rate:", "F2", "repBonusPaymentRate", typeof(RepFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Rep Bonus Payment Threshold:", "F2", "repBonusPaymentThreshold", typeof(RepFunding)));
                rows.Add(new SettingsDoubleDisplayRow("Rep Cost Rate:", "F4", "repCostRate", typeof(RepFunding)));
            }

            for(int index = 0; index < rows.Count; index += 1)
            {
                rows[index].Create(assets, content);
                rows[index].SetConfig(node, baseConfig, config);
            }
        }

        void UpdateDisplay()
        {
            AltFundingScenario scenario = AltFundingScenario.Instance;
            ConfigNode node = scenario.cn;
            FundingConfig config = scenario.config;
            FundingConfig baseConfig = scenario.baseConfig;

            bool locked = config.locked;

            SettingsDisplayMode mode =
                displayBaseConfig ?
                SettingsDisplayMode.Base :
                (locked ? SettingsDisplayMode.Locked : SettingsDisplayMode.Unlocked);

            modeLeftButton.SetActive(!locked);
            modeRightButton.SetActive(!locked);

            modeText.text = config.mode;

            for(int index = 0; index < rows.Count; index += 1)
            {
                rows[index].Update(mode);
            }

            lockSettingsButton.SetActive(!locked);
        }

        void LockSettings()
        {
            AltFundingScenario scenario = AltFundingScenario.Instance;
            ConfigNode node = scenario.cn;
            FundingConfig config = scenario.config;

            config.locked = true;
            node.SetValue("locked", "true", true);

            UpdateDisplay();
        }
        
        void UpdateConfigModeIndex(int offset)
        {
            AltFundingScenario scenario = AltFundingScenario.Instance;
            FundingConfig config = scenario.config;

            SetConfigModeIndex(config, GetConfigModeIndex(config) + offset);

            ClearList();
            PopulateList();
            UpdateDisplay();
        }
        
        int GetConfigModeIndex(FundingConfig config)
        {
            for(int index = 0; index < FundingConfig.modes.Length; index += 1)
            {
                if(config.mode == FundingConfig.modes[index])
                {
                    return index;
                }
            }
            return -1;
        }

        void SetConfigModeIndex(FundingConfig config, int index)
        {
            if(index < 0)
                index = FundingConfig.modes.Length - 1;
            if(index >= FundingConfig.modes.Length)
                index = 0;

            config.mode = FundingConfig.modes[index];
        }
    }

    enum SettingsDisplayMode { Base, Unlocked, Locked }

    class SettingsDisplayRow
    {
        public delegate string Validate(string value);

        protected string label;
        protected string fieldName;
        protected FieldInfo field;

        protected Text displayText;
        protected InputField inputField;
        protected Button addButton;
        protected Button removeButton;

        protected ConfigNode node;
        protected FundingConfig baseConf;
        protected FundingConfig conf;
        
        internal SettingsDisplayRow(string label, string fieldName, Type type)
        {
            this.label = label;
            this.fieldName = fieldName;
            this.field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if(field == null)
            {
                Debug.Log(string.Format("[AltFunding] Field '{0}' does not exist for type '{1}'", fieldName, type.Name));
            }
        }
        
        internal void Create(BundledAssets assets, GameObject container)
        {
            GameObject row = UnityEngine.Object.Instantiate(assets.settingsRowPrefab);
            row.transform.SetParent(container.transform);
            row.transform.localPosition = new Vector3(0f, 0f, 0f);
            row.GetChild("SettingName").GetComponent<Text>().text = label;

            displayText = row.GetChild("SettingValueText").GetComponent<Text>();
            inputField = row.GetChild("SettingInputField").GetComponent<InputField>();
            addButton = row.GetChild("SettingCustomAdd").GetComponent<Button>();
            removeButton = row.GetChild("SettingCustomRemove").GetComponent<Button>();

            inputField.onValueChange.AddListener((s) => { UpdateValue(s); BudgetWindow.UpdateBudgetList(); });
            addButton.onClick.AddListener(() => { AddValue(); Update(SettingsDisplayMode.Unlocked); });
            removeButton.onClick.AddListener(() => { RemoveValue(); Update(SettingsDisplayMode.Unlocked); BudgetWindow.UpdateBudgetList(); });
        }

        internal void SetConfig(ConfigNode node, FundingConfig baseConf, FundingConfig conf)
        {
            this.node = node;
            this.baseConf = baseConf;
            this.conf = conf;
        }

        internal void Update(SettingsDisplayMode mode)
        {
            inputField.gameObject.SetActive(mode == SettingsDisplayMode.Unlocked && HasValue());
            addButton.gameObject.SetActive(mode == SettingsDisplayMode.Unlocked && !HasValue());
            removeButton.gameObject.SetActive(mode == SettingsDisplayMode.Unlocked && HasValue());

            switch(mode)
            {
                case SettingsDisplayMode.Base:
                    displayText.text = GetValue(GetCalculator(baseConf, conf.mode));
                    break;

                case SettingsDisplayMode.Locked:
                    displayText.text = GetValue(GetCalculator(conf, conf.mode));
                    break;

                case SettingsDisplayMode.Unlocked:
                    if(HasValue())
                    {
                        inputField.text = GetValue(GetCalculator(conf, conf.mode));
                    }
                    else
                    {
                        displayText.text = GetValue(GetCalculator(conf, conf.mode));
                    }
                    break;
            }
        }

        protected FundingCalculator GetCalculator(FundingConfig config, string mode)
        {
            if(mode == FundingConfig.MODE_BASIC_FUNDING)
                return config.basicFunding;
            else if(mode == FundingConfig.MODE_REP_FUNDING)
                return config.repFunding;
            else
                return null;
        }

        protected ConfigNode GetConfigNode()
        {
            return node.GetNode(conf.mode);
        }

        protected virtual bool HasValue()
        {
            return GetConfigNode().HasValue(fieldName);
        }

        protected virtual string GetValue(object conf)
        {
            return (string) field.GetValue(conf);
        }
        
        protected virtual void UpdateValue(string text)
        {
            GetConfigNode().SetValue(fieldName, text, true);
            field.SetValue(GetCalculator(conf, conf.mode), text);
        }

        protected virtual void RemoveValue()
        {
            GetConfigNode().RemoveValue(fieldName);
            field.SetValue(GetCalculator(conf, conf.mode), field.GetValue(GetCalculator(baseConf, conf.mode)));
        }

        protected virtual void AddValue()
        {
            GetConfigNode().SetValue(fieldName, field.GetValue(GetCalculator(baseConf, conf.mode)).ToString(), true);
        }
    }

    class SettingsDoubleDisplayRow : SettingsDisplayRow
    {
        protected string format;
        protected double minimumValue;

        internal SettingsDoubleDisplayRow(string label, string format, string fieldName, Type type)
            : this(label, format, -1, fieldName, type)
        {
        }

        internal SettingsDoubleDisplayRow(string label, string format, double minimumValue, string fieldName, Type type)
            : base(label, fieldName, type)
        {
            this.format = format;
            this.minimumValue = minimumValue;
        }

        protected override string GetValue(object conf)
        {
            double value = (double) field.GetValue(conf);
            if(format == null)
                return value.ToString();
            else
                return value.ToString(format);
        }

        protected override void UpdateValue(string text)
        {
            double v;
            if(double.TryParse(text, out v))
            {
                if(minimumValue <= 0 || v >= minimumValue)
                {
                    GetConfigNode().SetValue(fieldName, text, true);
                    field.SetValue(GetCalculator(conf, conf.mode), v);
                }
            }
        }
    }
}
