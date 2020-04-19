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

        public BerzerkerHealConfig(float _healMult, int _EnableForAllUnits, bool _EnableForPlayer, bool _VerboseLogging)
        {
            healMult = _healMult;
            EnableForAllUnits = _EnableForAllUnits;
            EnableForPlayer = _EnableForPlayer;
            VerboseLogging = _VerboseLogging;
        }
    }

    public class BerzerkerHealSubModule : MBSubModuleBase
    {
        //XML settings
        BerzerkerHealConfig config;

        //Mission check settings
        Mission currentMission;
        bool firstCheck = false;

        protected override void OnSubModuleLoad()
        {
            //set default config params
            config = new BerzerkerHealConfig(0.25f, 0, false, false);

            //read xml and get heal mult.
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            try { 
                
                String CurrentFilePath = Directory.GetCurrentDirectory();
                Console.WriteLine("The current directory is {0}", CurrentFilePath);
                doc.Load("../../modules/BerzerkerHeal/config.xml");
                //scan through xml to find heal rate.
                foreach (XmlNode n in doc.ChildNodes)
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
                        }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                
            }
            
            
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            if (mission.IsFieldBattle)
            {
                currentMission = mission;
                firstCheck = true;
            }

        }

        protected override void OnApplicationTick(float dt)
        {
            if (currentMission != null && firstCheck)
            {
                if (currentMission.IsLoadingFinished)
                {

                    int agentNum = 0;
                    int humanNum = 0;
                    foreach (Agent a in currentMission.AllAgents)
                    {
                        agentNum++;
                        if (a.IsHuman)
                        {

                            humanNum++;
                            if (a.IsPlayerControlled)
                            {
                                //player is usually the last troop to spawn in.
                                firstCheck = false;
                            }
                            AC_HealOnHit HealOnHit = new AC_HealOnHit(a, config);
                        }
                    }
                    if(config.VerboseLogging)
                        InformationManager.DisplayMessage(new InformationMessage(String.Format("Agents Found: {0} | Human: {1}",agentNum, humanNum)));
                }
            }
        }
    }

    public class AC_HealOnHit : AgentComponent
    {
        BerzerkerHealConfig config;
        public AC_HealOnHit(Agent agent, BerzerkerHealConfig _config) : base(agent)
        {
            agent.AddComponent(this);
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
                                if (config.VerboseLogging)
                                    InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                            }
                            break;

                        case 2: //Enable for all
                            {
                                float healAmount = damage * config.healMult;
                                affectorAgent.Health += healAmount;
                                if (config.VerboseLogging)
                                    InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                            }
                            break;

                        default:
                            //defaults to only berzerker type units.
                            if (affectorAgent.Character.Culture.GetCultureCode() == CultureCode.Sturgia && (affectorAgent.Character.Level == 21 || affectorAgent.Character.Level == 26))
                            {
                                if (affectorAgent.Name.Equals("Sturgian Ulfhednar") || affectorAgent.Name.Equals("Sturgian Berserker"))
                                {
                                    float healAmount = damage * config.healMult;
                                    affectorAgent.Health += healAmount;
                                    if (config.VerboseLogging)
                                        InformationManager.DisplayMessage(new InformationMessage(String.Format("{0} hit for {1} recovered {2}", affectorAgent.Name, damage, healAmount)));
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}