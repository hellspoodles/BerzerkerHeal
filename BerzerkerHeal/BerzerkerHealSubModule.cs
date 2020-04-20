using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;


namespace BerzerkerHeal
{
    public struct BerzerkerHealConfig
    {
        public float healMult;
        public int EnableForAllUnits;
        public bool EnableForPlayer;
        public bool VerboseLogging;
        public List<int> EnabledUnits;

        public BerzerkerHealConfig(float _healMult, int _EnableForAllUnits, bool _EnableForPlayer, bool _VerboseLogging, List<int> _EnabledUnits)
        {
            healMult = _healMult;
            EnableForAllUnits = _EnableForAllUnits;
            EnableForPlayer = _EnableForPlayer;
            VerboseLogging = _VerboseLogging;
            EnabledUnits = _EnabledUnits;
        }
    }

    public class BerzerkerHealSubModule : MBSubModuleBase
    {
        //XML settings
        BerzerkerHealConfig config;

        //Mission check settings
        Mission currentMission;
        int currentMissionNumAgents = 0;

        protected override void OnSubModuleLoad()
        {
            //set default config params
            List<int> defaultEnabledTroops = new List<int>();
            
            config = new BerzerkerHealConfig(0.25f, 0, false, false, defaultEnabledTroops);

            //read xml and get heal mult.
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            try { 
                
                String CurrentFilePath = Directory.GetCurrentDirectory();
                Console.WriteLine("The current directory is {0}", CurrentFilePath);
                doc.Load("../../modules/BerzerkerHeal/config.xml");
                //scan through xml to find heal rate.
                foreach (XmlNode n in doc.DocumentElement.ChildNodes)
                {
                    if(n.Attributes != null)
                        foreach(XmlAttribute a in n.Attributes)
                        {
                            if(a.Name.Equals("HealRate"))
                            {
                                config.healMult = float.Parse(a.Value);
                            }
                            if (a.Name.Equals("EnableForAllUnits"))
                            {
                                config.EnableForAllUnits = int.Parse(a.Value);
                            }
                            if (a.Name.Equals("EnableForPlayer"))
                            {
                                config.EnableForPlayer = bool.Parse(a.Value);
                            }
                            if (a.Name.Equals("VerboseLogging"))
                            {
                                config.VerboseLogging = bool.Parse(a.Value);
                            }
                            if (a.Name.Equals("EnabledUnit"))
                            {
                                //uses a hash code list so that there are less string compares and it only compares ints (32 bit vs 128+ bit)
                                config.EnabledUnits.Add(a.Value.GetHashCode()); 
                            }
                        }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                defaultEnabledTroops.Add("Sturgian Berserker".GetHashCode());
                defaultEnabledTroops.Add("Sturgian Ulfhednar".GetHashCode());
            }
            int x = 3;
            
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {

            {
                currentMission = mission;
                if (config.VerboseLogging)
                    InformationManager.DisplayMessage(new InformationMessage(String.Format("Mission Initalized, Type: {0}", mission.Mode)));
            }

        }

        protected override void OnApplicationTick(float dt)
        {
            try
            {
                if (currentMission != null)
                {
                    if (currentMission.Mode == MissionMode.Battle ||
                        currentMission.Mode == MissionMode.Duel ||
                        currentMission.Mode == MissionMode.Tournament ||
                        currentMission.Mode == MissionMode.Deployment ||
                        currentMission.Mode == MissionMode.Stealth)
                    {
                        //check if number of agents has changed, if so, update things.
                        if (currentMission.Agents.Count != currentMissionNumAgents)
                        {
                            if (config.VerboseLogging)
                                InformationManager.DisplayMessage(new InformationMessage(String.Format("Mission Re-Check, Type: {0}", currentMission.Mode)));
                            int humanNum = 0;
                            foreach (Agent a in currentMission.Agents)
                            {
                                if (a.IsHuman)
                                {
                                    humanNum++;
                                    //check if agent has the callback component
                                    AC_HealOnHit HealOnHit = new AC_HealOnHit(a, config);
                                    if (a.GetComponent<AC_HealOnHit>() == null)
                                    {
                                        a.AddComponent(HealOnHit);
                                    }

                                }
                            }
                            currentMissionNumAgents = currentMission.Agents.Count;
                            if (config.VerboseLogging)
                                InformationManager.DisplayMessage(new InformationMessage(String.Format("Agents Found: {0} | Human: {1}", currentMissionNumAgents, humanNum)));
                        }
                    }
                }
            }
            catch (System.NullReferenceException)
            {
                currentMission = null;
            }
        }
    }

    public class AC_HealOnHit : AgentComponent
    {
        BerzerkerHealConfig config;
        public AC_HealOnHit(Agent agent, BerzerkerHealConfig _config) : base(agent)
        {
            config = _config;
        }

        protected override void OnHit(Agent affectorAgent, int damage, int weaponKind, float perkEffectOnMorale)
        {
            //quick checks to avoid string comparisons since those are expensive.
            //is a human sturgian.
            //berzerker is level 21,
            //ulfhednar is level 26
            if (affectorAgent.IsHuman)
            {
                //check if Player and Player Healing is enabled:
                if(affectorAgent.IsPlayerControlled && config.EnableForPlayer)
                {
                    float healAmount = damage * config.healMult;
                    affectorAgent.Health += healAmount;
                    if (config.VerboseLogging)
                        InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                } else
                {
                    //check if we are looking at culture.
                    switch (config.EnableForAllUnits)
                    {
                        case 1: //Enable for all sturgians
                            if (affectorAgent.Character.Culture.GetCultureCode() == CultureCode.Sturgia)
                            {
                                float healAmount = damage * config.healMult;
                                affectorAgent.Health += healAmount;
                                if (affectorAgent.HealthLimit < affectorAgent.Health) affectorAgent.Health = affectorAgent.HealthLimit;
                                if (config.VerboseLogging)
                                    InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                            }
                            break;

                        case 2: //Enable for all
                            {
                                float healAmount = damage * config.healMult;
                                affectorAgent.Health += healAmount;
                                if (affectorAgent.HealthLimit < affectorAgent.Health) affectorAgent.Health = affectorAgent.HealthLimit;
                                if (config.VerboseLogging)
                                    InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                            }
                            break;

                        default:
                            //defaults to only named type units
                            int nameHash = affectorAgent.Name.GetHashCode();
                            foreach(int a in config.EnabledUnits)
                            {
                                if(a == nameHash)
                                {
                                    float healAmount = damage * config.healMult;
                                    affectorAgent.Health += healAmount;
                                    if (affectorAgent.HealthLimit < affectorAgent.Health) affectorAgent.Health = affectorAgent.HealthLimit;
                                    if (config.VerboseLogging)
                                        InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                                    break;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}